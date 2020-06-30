using System.Collections.Generic;
using MongoDB.Bson;

namespace SubnetServer
{
	class FullyConnectedTopologyBuilder : ITopologyBuilder
	{
		public List<Connection> ConstructTopology(List<ObjectId> sessionIdList)
		{
			var top = new List<Connection>();
			foreach(var id in sessionIdList)
			{
				var conn = new Connection();
				conn.Origin = id;
				conn.Neighbors = new List<ObjectId>(sessionIdList);
				conn.Neighbors.Remove(id);
				top.Add(conn);
			}
			return top;
		}
	}
}