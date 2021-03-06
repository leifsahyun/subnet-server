using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;

namespace SubnetServer
{
	class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            /*services.AddLogging(builder =>
            {
                builder.AddConsole()
                    .AddDebug()
                    .AddFilter<ConsoleLoggerProvider>(category: null, level: LogLevel.Debug)
                    .AddFilter<DebugLoggerProvider>(category: null, level: LogLevel.Debug);
            });*/
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseDeveloperExceptionPage();

            #region UseWebSocketsOptions
            var webSocketOptions = new WebSocketOptions() 
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            };

            app.UseWebSockets(webSocketOptions);
            #endregion

            #region AcceptWebSocket
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
					Console.WriteLine("Received message on websocket path");
                    if (context.WebSockets.IsWebSocketRequest)
                    {
						Console.WriteLine("Received websocket request");
						foreach (var v in context.Request.Headers)
						{
							Console.WriteLine(v.ToString());
						}
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        ClientHandler handler = new ClientHandler(new WebsocketDriver(webSocket));
						await handler.Open();
                    }
                    else
                    {
						Console.WriteLine("Received non-websocket request");
						foreach (var v in context.Request.Headers)
						{
							Console.WriteLine(v.ToString());
						}
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });
			#endregion
        }
	}
}