using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace SubnetServer
{
	class TopologyBuilderFactory
	{
		public static Dictionary<string, Type> references = new Dictionary<string, Type>
		{
			{"FullyConnected", typeof(FullyConnectedTopologyBuilder)},
			{"Ring", typeof(RingTopologyBuilder)}
		};
		
		public static ITopologyBuilder GetTopologyBuilder(string type)
		{
			return Activator.CreateInstance(references[type]) as ITopologyBuilder;
		}
	}
	
	interface ITopologyBuilder
	{
		List<Connection> ConstructTopology(List<ObjectId> sessionIdList);
	}
}