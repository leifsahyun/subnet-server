using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SubnetServer
{
	class SocketDriver : ICommDriver
	{
		// Client  socket.
		private Socket workSocket = null;  
		// Size of receive buffer.  
		private const int BufferSize = 1024;  
		// Receive buffer.  
		private byte[] buffer = new byte[BufferSize];  
		// Received data string.  
		private StringBuilder sb = new StringBuilder();
		private bool newlyCreated = true;
		
		public SocketDriver(Socket sock)
		{
			workSocket = sock;
		}
		
		public bool IsOpen
		{
			get{return workSocket.Connected || newlyCreated;}
		}
		
		public async Task<string> ReceiveAsync()
		{
			string message = await Task.Run(()=>
			{
				int bytesRead = workSocket.Receive(buffer, 0, SocketDriver.BufferSize, 0);
				newlyCreated = false;
				if(IsOpen)
					return Encoding.ASCII.GetString(buffer, 0, bytesRead);
				else
					return null;
			});
			return message;
		}
		
		public async Task SendSafe(string message)
		{
			await Task.Run(()=>
			{
				lock(workSocket)
				{
					workSocket.Send(Encoding.ASCII.GetBytes(message));
				}
			});
		}
	}
}