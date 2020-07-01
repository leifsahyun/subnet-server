using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using static SubnetServer.ToolListFactory;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SubnetServer
{
    class Program
    {
        static void Main(string[] args)
        {
			var server = new SocketServer();
            var console = new BaseServerConsole();
			console.Tools = ServerToolList(console);
			var source = new CancellationTokenSource();
			var tok = source.Token;
			var t1 = CreateHostBuilder(args).Build().RunAsync(tok);
			var t2 = console.Run(tok);
			var t3 = server.Start(tok);
			Task[] tasks = {t1, t2, t3};
			Task.WaitAny(tasks);
			source.Cancel();
			Task.WaitAll(tasks);
        }
		
		public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseUrls("http://+:5656/");
					webBuilder.UseStartup<Startup>();
				});
    }
}
