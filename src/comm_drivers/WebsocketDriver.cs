using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SubnetServer
{
	class WebsocketDriver : ICommDriver
	{
		private const int buffSize = 1024*4;
		private WebSocket webSocket;
		private byte[] buffer;
		private ArraySegment<byte> bufferMem;
		
		public WebsocketDriver(WebSocket socket)
		{
			webSocket = socket;
			buffer = new byte[buffSize];
			bufferMem = new ArraySegment<byte>(buffer, 0, buffSize);
		}
		
		public bool IsOpen
		{
			get{return !webSocket.CloseStatus.HasValue;}
		}
		
		public async Task<string> ReceiveAsync()
		{
			WebSocketReceiveResult result = await webSocket.ReceiveAsync(bufferMem, CancellationToken.None);
			if(IsOpen)
				return Encoding.ASCII.GetString(buffer, 0, result.Count);
			else
				return null;
		}
		
		public async Task SendSafe(string message)
		{
			await Task.Run(()=>
			{
				lock(webSocket)
				{
					var t = webSocket.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
					t.Wait();
				}
			});
		}
	}
}