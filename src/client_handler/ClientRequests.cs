using System.Collections.Generic;
using System;
using MongoDB.Bson;
using System.Text.Json;
#pragma warning disable 0649

namespace SubnetServer
{
	class JsonMessage
	{
		public string Type { get; set; }
	}
	
	class LoginRequest : JsonMessage
	{
		public string Username { get; set; }
		public string Password { get; set; }
		public string IpAddress {get; set; }
	}
	
	class LoginResponse : JsonMessage
	{
		public string SessionId { get; set; }
	}
	
	class LogoutRequest : JsonMessage
	{
		public string SessionId { get; set; }
	}
	
	class SubnetRequest : JsonMessage
	{
		public string SessionId { get; set; }
		public List<JsonElement> Requirements { get; set; } = new List<JsonElement>();
		public static explicit operator Request(SubnetRequest oldReq)
		{
			var newReqs = new List<BsonDocument>();
			foreach(var el in oldReq.Requirements)
			{
				if(el.ValueKind == JsonValueKind.Object)
				{
					var indexer = el.EnumerateObject();
					indexer.MoveNext();
					var prop = indexer.Current;
					if(prop.Value.ValueKind == JsonValueKind.String)
						newReqs.Add(new BsonDocument{{prop.Name, prop.Value.GetString()}});
					else if(prop.Value.ValueKind == JsonValueKind.Number)
						newReqs.Add(new BsonDocument{{prop.Name, prop.Value.GetInt32()}});
					else
						throw new Exception("Error converting json to bson: unrecognized value type");
				}
			}
			return new Request(){Origin = new ObjectId(oldReq.SessionId), Requirements = newReqs};
		}
	}
	
	class NeighborsResponse : JsonMessage
	{
		public List<NeighborObj> Neighbors { get; set; } = new List<NeighborObj>();
	}
	
	class NeighborObj
	{
		public string Username { get; set; }
		public string IpAddress { get; set; }
	}
	
	class MessageResponse : JsonMessage
	{
		public string Message { get; set; }
	}
}