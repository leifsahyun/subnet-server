using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;

namespace SubnetServer
{
	class DBAccess
	{
		public static readonly DBAccess instance = new DBAccess();
		private IMongoDatabase db;
		
		private DBAccess()
		{
			var client = new MongoClient("mongodb+srv://dbAdmin:ovg55Ee9BiqkZQR3@subnetcluster-iri9x.gcp.mongodb.net/SubnetServerDB?retryWrites=true&w=majority");
			db = client.GetDatabase("SubnetServerDB");
		}
		
		public static DBAccess Instance
		{
			get
			{
				return instance;
			}
		}
		
		public List<string> ListUsers()
		{
			var collection = db.GetCollection<Session>("sessions");
			var query = from s in collection.AsQueryable()
						select s.Username;
			return query.ToList();
		}
		public List<Subnet> ListSubnets()
		{
			var collection = db.GetCollection<Subnet>("subnets");
			var query = from net in collection.AsQueryable()
						select net;
			return query.ToList();
		}
		public async Task<bool> CreateUser(string username, string password)
		{
			var usersCollection = db.GetCollection<User>("users");
			var toInsert = new User(){Username = username, Password = password};
			try
			{
				await usersCollection.InsertOneAsync(toInsert);
				return true;
			}
			catch(Exception)
			{
				return false;
			}
		}
		
		//include ip address parameter in signature
		public async Task<string> Login(string username, string password, string ip)
		{
			var usersCollection = db.GetCollection<User>("users");
			var query = from u in usersCollection.AsQueryable()
						where (u.Username == username) && (u.Password == password)
						select u;
			if(!query.Any())
			{
				return null;
			}
			var toInsert = new Session()
			{
				_id = ObjectId.GenerateNewId(),
				Username = username,
				SessionKey = "session key",
				IpAddress = ip,
				CreatedAt = DateTime.UtcNow
			};
			var sessionsCollection = db.GetCollection<Session>("sessions");
			sessionsCollection.InsertOne(toInsert);
			
			return toInsert._id.ToString();
		}
		public async Task<bool> Logout(ObjectId session)
		{
			var sessionsCollection = db.GetCollection<Session>("sessions");
			var terminated = sessionsCollection.FindOneAndDelete(sess => sess._id==session);
			if(terminated is null)
				return false;
			//fix this request to delete networks this session is a member of, it does not work
			db.GetCollection<Subnet>("subnets").DeleteMany(
			new BsonDocument{
				{"Topology", new BsonDocument{
					{"Origin", terminated._id}
				}}
			});
			db.GetCollection<Request>("requests").DeleteMany(
			new BsonDocument{
				{"Origin", terminated._id}
			});
			return true;
		}
		
		public async Task<bool> RequestSubnet(Request req)
		{
			var sessionsCollection = db.GetCollection<Session>("sessions");
			var sessions = await sessionsCollection.FindAsync(sess => sess._id==req.Origin);
			if(!sessions.Any())
			{
				return false;
			}
			
			var requestsCollection = db.GetCollection<Request>("requests");
			requestsCollection.InsertOne(req);
			
			//start transaction
			var transactionSession = await db.Client.StartSessionAsync();
			var success = await transactionSession.WithTransactionAsync((s, tok) => UpdateNets(req, s.Client.GetDatabase("SubnetServerDB")));
			//end transaction
			return success;
		}
		private async Task<bool> UpdateNets(Request req, IMongoDatabase sessionDB)
		{
			//check for requests that match non-null fields of req
			var requirements = await sessionDB.GetCollection<BsonDocument>("requests").AggregateAsync(
				PipelineDefinition<BsonDocument, BsonDocument>.Create(new[]{
					new BsonDocument{
						{"$match", new BsonDocument{
							{"Requirements", new BsonDocument{
								{"$all", new BsonArray(req.Requirements)}
							}}
						}}
					},
					new BsonDocument{
						{"$project", new BsonDocument{
							{"Requirements", new BsonDocument{
								{"$filter", new BsonDocument{
									{"input", "$Requirements"},
									{"as", "el"},
									{"cond", new BsonDocument{
										{"$not", new BsonDocument{
											{"$in", new BsonArray{"el", new BsonArray(req.Requirements)}}
										}}
									}}
								}}
							}}
						}}
					},
					//{"$unwind", "$Requirements"},
					new BsonDocument{
						{"$group", new BsonDocument{
							{"_id", "$Requirements"},
							{"count", new BsonDocument{
								{"$sum", 1}
							}},
							{"requestIds", new BsonDocument{
								{"$addToSet", "$_id"}
							}}
						}}
					},
					new BsonDocument{
						{"$sort", new BsonDocument{
							{"count", -1}
						}}
					}
				})
			);
			
			var requestIds = new List<ObjectId>();
			BsonValue totalUsersValue = null;
			var containsTotalUsers = req.Requirements.Exists(el=>el.TryGetValue("TotalUsers", out totalUsersValue));
			int totalUsers = -1;
			if(containsTotalUsers)
				totalUsers = totalUsersValue.AsInt32;
			var reqDocList = new List<BsonDocument>();
			foreach(var requirement in requirements.ToList())
			{
				var newReqs = new List<BsonDocument>();
				foreach(var individualReq in requirement["_id"].AsBsonArray)
				{
					newReqs.Add(individualReq.AsBsonDocument);
				}
				if(CompatibleReq(req.Requirements, newReqs))
				{
					req.Requirements.AddRange(newReqs);
					foreach(var newId in requirement["requestIds"].AsBsonArray)
					{
						if(!requestIds.Contains(newId.AsObjectId))
							requestIds.Add(newId.AsObjectId);
					}
				}
				if(containsTotalUsers && totalUsers<requestIds.Count)
					break;
			}
			
			/*
			1. check requestIds longer than totalUsers if applicable
				a. then trim requestIds to totalUsers
			2. if number of requestIds is more than 1, create a new network
			3. otherwise, find a network for this request
			*/
			if(containsTotalUsers)
			{
				if(requestIds.Count>=totalUsers)
				{
					requestIds.RemoveRange(totalUsers, requestIds.Count-totalUsers);
				}
				else
				{
					return false;
				}
			}
			
			if(requestIds.Count>1)
			{
				return await ConstructNetwork(req, requestIds, sessionDB);
			}
			else
			{
				return await FindNetwork(req, sessionDB);
			}
		}
		
		private bool CompatibleReq(List<BsonDocument> reqs, IEnumerable<BsonValue> newReqs)
		{
			if(newReqs is null)
				return true;
			else
			{
				foreach(var req in reqs)
				{
					foreach(var newReq in newReqs)
					{
						if(req.Contains(newReq.AsBsonDocument.GetElement(0).Name) && req[newReq.AsBsonDocument.GetElement(0).Name]!=newReq[0]) //if requirement key = new requirement key and the values are different
							return false;
					}
				}
			}
			return true;
		}
		
		private async Task<bool> ConstructNetwork(Request req, List<ObjectId> requestIds, IMongoDatabase sessionDB)
		{
			var net = new Subnet();
			net.Requirements = req.Requirements;
			if(req.Requirements.Exists(doc=>doc.Contains("TotalUsers")))
				net.Closed = true;
			else
				net.Closed = false;
			var sessions = sessionDB.GetCollection<BsonDocument>("requests").Aggregate(
				PipelineDefinition<BsonDocument, BsonDocument>.Create(new[]{
					new BsonDocument{
						{"$match", new BsonDocument{
							{"_id", new BsonDocument{
								{"$in", new BsonArray(requestIds.ToList())}
							}}
						}}
					},
					new BsonDocument{
						{"$lookup", new BsonDocument{
							{"from", "sessions"},
							{"localField", "Origin"},
							{"foreignField", "_id"},
							{"as", "session"}
						}}
					},
					new BsonDocument{
						{"$group", new BsonDocument{
							{"_id", 1},
							{"sessionIds", new BsonDocument{
								{"$addToSet", "$session._id"}
							}}
						}}
					}
				})
			);
			var sessionList = sessions.ToList();
			
			var docList = sessionList[0]["sessionIds"];
			//ctx.printQueue.Enqueue(docList.ToString());
			var sessionIdList = new List<ObjectId>();
			foreach(var doc in docList.AsBsonArray)
			{
				if(!doc.IsBsonNull)
					sessionIdList.Add(doc[0].AsObjectId);
			}
			
			BsonValue topologyTypeValue = null;
			string topologyType = "";
			if(!req.Requirements.Exists(el=>el.TryGetValue("TopologyType", out topologyTypeValue)))
				topologyType = "FullyConnected";
			else
				topologyType = topologyTypeValue.AsString;
			var topBuilder = TopologyBuilderFactory.GetTopologyBuilder(topologyType);
			net.Topology = topBuilder.ConstructTopology(sessionIdList);
			
			sessionDB.GetCollection<Subnet>("subnets").InsertOne(net);
			
			sessionDB.GetCollection<Request>("requests").DeleteMany(new BsonDocument{
				{"_id", new BsonDocument{
					{"$in", new BsonArray(requestIds.ToList())}
				}}
			});	
			return true;
		}
		
		private async Task<bool> FindNetwork(Request req, IMongoDatabase sessionDB)
		{
			var nets = sessionDB.GetCollection<Subnet>("subnets").Find(
				new BsonDocument{
					{"Closed", false},
					{"Requirements", new BsonDocument{
						{"$all", new BsonArray(req.Requirements)}
					}}
				}
			).Limit(1);
			if(!nets.Any())
			{
				return false;
			}
			var subnet = nets.ToList()[0];
			List<ObjectId> sessions = new List<ObjectId>();
			foreach(var conn in subnet.Topology)
			{
				sessions.Add(conn.Origin);
				conn.Neighbors.Add(req.Origin);
			}
			var connection = new Connection();
			connection.Origin = req.Origin;
			connection.Neighbors = sessions;
			subnet.Topology.Add(connection);
			await sessionDB.GetCollection<Subnet>("subnets").UpdateOneAsync(
				new BsonDocument{
					{"_id", subnet._id}
				},
				new BsonDocument{
					{"$set", new BsonDocument{
						{"Topology", new BsonArray(subnet.Topology.ConvertAll<BsonDocument>(c=>(BsonDocument)c))}
					}}
				}
			);
			return true;
		}
		
		public Subnet WaitForNet(Request req)
		{
			var subnetsCollection = db.GetCollection<Subnet>("subnets");
			var cursor = subnetsCollection.Watch();
			Subnet network = new Subnet();
			bool networkServed = false;
			while(!networkServed && cursor.MoveNext())
			{
				if(cursor.Current.Count()!=0)
				{
					foreach(ChangeStreamDocument<Subnet> change in cursor.Current)
					{
						if(change.OperationType == ChangeStreamOperationType.Insert || change.OperationType == ChangeStreamOperationType.Update)
						{
							network = change.FullDocument;
							foreach(var connection in network.Topology)
							{
								if(connection.Origin == req.Origin)
								{
									networkServed = true;
									break;
								}
							}
						}
						if(networkServed)
							break;
					}
				}
			}
			cursor.Dispose();
			return network;
		}
		
		public List<NeighborObj> GetNeighbors(ObjectId origin, Subnet network)
		{
			List<ObjectId> neighborIds = network.Topology.Find(c => c.Origin==origin).Neighbors;
			var sessionsCollection = db.GetCollection<Session>("sessions");
			var neighborObjs = from sess in sessionsCollection.AsQueryable()
				where neighborIds.Contains(sess._id)
				select new NeighborObj(){Username = sess.Username, IpAddress = sess.IpAddress};
			return neighborObjs.ToList();
		}
	}
}