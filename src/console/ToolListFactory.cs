using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace SubnetServer
{
	static class ToolListFactory
	{	
		public static List<ConsoleTool> TestToolList(AbstractServerConsole context)
		{
			return new List<ConsoleTool>{new ConsoleTool(new Regex("^ping$"), ()=>pingFunc(context), "ping - pong")};
		}
		
		public static List<ConsoleTool> ServerToolList(AbstractServerConsole context)
		{
			return new List<ConsoleTool>{
				new ConsoleTool(new Regex("^([Uu]|users)$"), ()=>ListUsers(context), "users - list all logged in users"),
				new ConsoleTool(new Regex("^([Nn]|nets)$"), ()=>ListSubnets(context), "nets - list all active subnets"),
				new ConsoleTool(new Regex("^([Ll]|login)$"), ()=>Login(context), "login - logs in to the server from console"),
				new ConsoleTool(new Regex("^([Kk]|kick)$"), ()=>Kick(context), "kick - terminate a user session"),
				new ConsoleTool(new Regex("^([Cc]|create)$"), ()=>CreateUser(context), "create - create a new user"),
				new ConsoleTool(new Regex("^([Rr]|request)$"), ()=>RequestSubnet(context), "request - request access to a subnet with specific features")
			};
		}
		
		public static async Task ListUsers(AbstractServerConsole ctx)
		{
			var users = DBAccess.Instance.ListUsers();
			if(users.Any())
			{
				foreach (var name in users)
				{
					ctx.printQueue.Enqueue(name);
				}
			}
			else
			{
				ctx.printQueue.Enqueue("No users logged in");
			}
		}
		public static async Task ListSubnets(AbstractServerConsole ctx)
		{
			var nets = DBAccess.Instance.ListSubnets();
			if(nets.Any())
			{
				foreach (var subnet in nets)
				{
					ctx.printQueue.Enqueue(subnet._id.ToString()+" - "+subnet.Closed.ToString());
					foreach (var conn in subnet.Topology)
					{
						var line = "\t"+conn.Origin.ToString()+": ";
						foreach (var neighbor in conn.Neighbors)
						{
							line += neighbor.ToString()+" ";
						}
						ctx.printQueue.Enqueue(line);
					}
				}
			}
			else
			{
				ctx.printQueue.Enqueue("No active subnets");
			}
		}
		public static async Task CreateUser(AbstractServerConsole ctx)
		{
			Console.WriteLine("Enter username");
			var username = Console.ReadLine();
			Console.WriteLine("Enter password");
			var password = Console.ReadLine();
			if(await DBAccess.Instance.CreateUser(username, password))
			{
				ctx.printQueue.Enqueue("Created user "+username);
			}
			else
			{
				ctx.printQueue.Enqueue("User creation failed - invalid characters or duplicate username");
			}
		}
		public static async Task Login(AbstractServerConsole ctx)
		{
			try
			{
				Console.WriteLine("Enter username");
				var username = Console.ReadLine();
				Console.WriteLine("Enter password");
				var password = Console.ReadLine();
				var sessionId = await DBAccess.Instance.Login(username, password, "127.0.0.1");
				if(sessionId is null)
				{
					ctx.printQueue.Enqueue("No user with matching username and password");
				}
				else
				{
					ctx.printQueue.Enqueue("Logged in as user "+username);
					ctx.printQueue.Enqueue("Your session id is "+sessionId);
				}
			}
			catch (Exception)
			{
				ctx.printQueue.Enqueue("Error logging in, that user may already be logged in");
			}
		}
		public static async Task Kick(AbstractServerConsole ctx)
		{
			Console.WriteLine("Enter session id to terminate");
			try
			{
				var idString = Console.ReadLine();
				var sessionId = new ObjectId(idString);
				if(await DBAccess.Instance.Logout(sessionId))
				{
					ctx.printQueue.Enqueue("Successfully terminated session "+idString);
				}
				else
				{
					ctx.printQueue.Enqueue("A session with that id does not exist");
				}
			}
			catch (Exception)
			{
				ctx.printQueue.Enqueue("Error kicking user, check id string is valid");
			}
		}
		public static async Task RequestSubnet(AbstractServerConsole ctx)
		{
			var req = new Request();
			
			Console.WriteLine("Enter session id");
			try
			{
				var idString = Console.ReadLine();
				req.Origin = new ObjectId(idString);
			}
			catch (Exception)
			{
				ctx.printQueue.Enqueue("Error parsing id string");
				return;
			}
			
			Console.WriteLine("Enter network size or press enter for any size");
			int numUsers=0;
			var input = Console.ReadLine();
			if(input!="")
			{
				var parseSuccess = Int32.TryParse(input, out numUsers);
				if(parseSuccess && numUsers>0)
				{
					req.Requirements.Add(new BsonDocument{{"TotalUsers", numUsers}});
				}
				else
				{
					ctx.printQueue.Enqueue("Failed to parse input to int");
					return;
				}
				req.Requirements.Add(new BsonDocument{{"TotalUsers", numUsers}});
			}
			
			Console.WriteLine("Enter shared key (or leave blank)");
			var sharedKey = Console.ReadLine();
			if(sharedKey!="")
				req.Requirements.Add(new BsonDocument{{"SharedKey", sharedKey}});
			
			Console.WriteLine("Enter desired topology type (or leave blank)");
			var topologyType = Console.ReadLine();
			if(topologyType!="")
			{
				Console.WriteLine("Enter desired role in specified topology type (or leave blank)");
				var role = Console.ReadLine();
				if(role!="")
					req.Requirements.Add(new BsonDocument{{"TopologyType", topologyType}, {"Role", role}});
				else
					req.Requirements.Add(new BsonDocument{{"TopologyType", topologyType}});
			}
			
			var success = await DBAccess.Instance.RequestSubnet(req);
			
			if(success)
			{
				ctx.printQueue.Enqueue("A network is available to serve your request, access will be granted shortly");
			}
			else
			{
				ctx.printQueue.Enqueue("Could not immediately find a network to serve your request, your request has been saved for later");
			}
		}
		
		
		public static async Task pingFunc(AbstractServerConsole ctx)
		{
			ctx.printQueue.Enqueue("pong");
		}
	}
}