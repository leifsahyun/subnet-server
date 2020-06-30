using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SubnetServer
{
    class BaseServerConsole : AbstractServerConsole
    {
		public List<ConsoleTool> Tools {get; set;}
		
		/*public BaseServerConsole(List<ConsoleTool> tools)
		{
			Tools = tools;
		}*/
		
		override protected void Motd()
        {
			Console.WriteLine("Login & Subnet Server Console");
			Console.WriteLine("-----------------------------");
			Console.WriteLine("commands:");
			Console.WriteLine("x - exit");
			foreach(ConsoleTool t in Tools)
			{
				Console.WriteLine(t.Info);
			}
			if(printQueue.Any())
			{
				Console.WriteLine("-----------------------------");
			}
		}
		
		override protected async Task HandleInput(string input)
		{
			await base.HandleInput(input);
			foreach(ConsoleTool t in Tools)
			{
				if(t.Command.IsMatch(input))
				{
					await t.Function();
				}
			}
		}
	}
}