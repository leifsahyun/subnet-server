using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SubnetServer
{	
	class SocketServer
	{
		private bool running = false;
		private CancellationToken cancel;
		public Socket listenSocket = null;
		private IPEndPoint localEndPoint = null;
		private AutoResetEvent accepted = new AutoResetEvent(false);
		
		public SocketServer()
		{
			IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
			foreach(var ip in ipHostInfo.AddressList)
			{
				Console.WriteLine(ip.ToString());
			}
			Console.ReadLine();
			IPAddress ipaddress = ipHostInfo.AddressList[0];
			localEndPoint = new IPEndPoint(ipaddress, 5657);
			listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		}
		
		public async Task Start(CancellationToken tok = default(CancellationToken))
		{
			cancel = tok;
			running = true;
			try
			{
				listenSocket.Bind(localEndPoint);
				listenSocket.Listen(100);
				
				while (running && !cancel.IsCancellationRequested)
				{
					listenSocket.BeginAccept(new AsyncCallback(AcceptCallback), listenSocket);
					await Task.Run(()=>WaitHandle.WaitAny(new WaitHandle[] {accepted, cancel.WaitHandle}));
				}
				
				listenSocket.Close();
			}
			catch (Exception e)
			{  
				Console.WriteLine(e.ToString());  
			}
		}
		public void Stop()
		{
			running = false;
			accepted.Set();
		}
		
		public void AcceptCallback(IAsyncResult ar) {
			Console.WriteLine("Received connection request");
			// Signal the main thread to continue.  
			accepted.Set();
			Console.WriteLine("signalled main thread to continue");
	  
			// Get the socket that handles the client request.
			var buff = new byte[128];
			int length = 0;
			Socket workSocket = listenSocket.EndAccept(out buff, out length, ar);
			Console.WriteLine("Got new socket for this client");
			Console.WriteLine(Encoding.UTF8.GetString(buff, 0, length));
	  
			// Create a client handler to handle messages from this client  
			ClientHandler handler = new ClientHandler(new SocketDriver(workSocket));
			Task.Run(()=>handler.Open());
			Console.WriteLine("Created new client handler object");
		}
	}
}