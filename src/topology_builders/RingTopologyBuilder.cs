using System.Collections.Generic;
using MongoDB.Bson;

namespace SubnetServer
{
	class RingTopologyBuilder : ITopologyBuilder
	{
		public List<Connection> ConstructTopology(List<ObjectId> sessionIdList)
		{
			var top = new List<Connection>();
			Connection lastConnection = null;
			foreach(var id in sessionIdList)
			{
				var conn = new Connection();
				conn.Origin = id;
				if(!(lastConnection is null))
				{
					conn.Neighbors.Add(lastConnection.Origin);
					lastConnection.Neighbors.Add(id);
				}
				lastConnection = conn;
				top.Add(conn);
			}
			top[0].Neighbors.Add(lastConnection.Origin);
			lastConnection.Neighbors.Add(top[0].Origin);
			return top;
		}
	}
}