using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubnetServer
{	
	struct ConsoleTool
	{
		public delegate Task ToolFunc();
		public Regex Command {get;}
		public string Info {get;}
		public ToolFunc Function;
		
		public ConsoleTool(Regex command, ToolFunc function, string info)
		{
			Command = command;
			Function = function;
			Info = info;
		}
	}
}