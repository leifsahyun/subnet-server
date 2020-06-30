using System.Collections.Generic;
using System;
using MongoDB.Bson;
#pragma warning disable 0649

namespace SubnetServer
{
	class User
	{
		public ObjectId _id;
		public string Username;
		public string Password;
	}
	
	class Session
	{
		public ObjectId _id;
		public string Username;
		public string SessionKey;
		public string IpAddress;
		public DateTime CreatedAt;
	}
	
	class Subnet
	{
		public ObjectId _id;
		public bool Closed; //whether Requirements prevents additional nodes from being added to this network
		public List<BsonDocument> Requirements = new List<BsonDocument>(); //requirements imposed on the network by requests
		public List<Connection> Topology = new List<Connection>();
	}
	
	class Connection
	{
		public ObjectId Origin; //session id
		public List<ObjectId> Neighbors = new List<ObjectId>(); //session ids
		public static implicit operator BsonValue(Connection c)
		{
			return new BsonDocument{{"Origin",c.Origin},{"Neighbors",new BsonArray(c.Neighbors)}};
		}
	}
	
	class Request
	{
		public ObjectId Origin;
		public List<BsonDocument> Requirements = new List<BsonDocument>();
		/* These are string-int or string-string key-value pairs specifying various network requirements, examples below:
		public int TotalUsers; //desired total number of users in the network, must be the same or null for all members
		public string SharedKey; //a key shared among a group of users that want to create a private subnet, must be the same for all members
		public string TopologyType; //ideally make this an enum since there will be a limited number of supported types, must be the same or null for all members
		//current thoughts on topology types: hierarchical (or tree), fully connected, ring
		public string Role; //ideally make this an enum too, associated with previous since roles are dependent on topology type
		//role is currently only significant for the hierarchical form
		*/
	}
}