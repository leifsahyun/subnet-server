#define DEBUG
using System;
using System.Net;
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
					webBuilder.ConfigureKestrel(serverOptions =>
					{
						serverOptions.Listen(IPAddress.Any, 5656, listenOptions=>
						{
							listenOptions.UseConnectionLogging();
						});
					});
					webBuilder.UseStartup<Startup>();
				});
    }
}
