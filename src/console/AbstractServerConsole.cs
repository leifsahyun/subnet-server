using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace SubnetServer
{
    class AbstractServerConsole
    {
		private bool running = true;
		private CancellationToken cancel;
		public Queue<string> printQueue = new Queue<string>();
		
		protected virtual void Motd()
        {
            Console.WriteLine("Login & Subnet Server Console");
			Console.WriteLine("-----------------------------");
			Console.WriteLine("commands:");
			Console.WriteLine("x - exit");
        }
		
		protected virtual void Footer()
		{
			Console.WriteLine("-----------------------------");
			Console.WriteLine("Enter Command\n");
		}
		
		private void PrintAll()
		{
			Console.Clear();
			Motd();
			while (printQueue.Count > 0) {
				Console.WriteLine(printQueue.Dequeue());
			}
			Footer();
		}
		
		public async Task Run(CancellationToken tok = default(CancellationToken))
		{
			cancel = tok;
			await Task.Run( async ()=>
			{
				do {
					PrintAll();
					var input = Console.ReadLine();
					await HandleInput(input);
				} while(running && !cancel.IsCancellationRequested);
			});
		}
		
		public void Stop()
		{
			running = false;
		}
		
		protected virtual async Task HandleInput(string input)
		{
			if (input == "X" || input == "x")
			{
				running = false;
			}
		}
    }
}
