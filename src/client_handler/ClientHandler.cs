using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace SubnetServer
{
	class ClientHandler
	{
		private ICommDriver driver;
		
		public ClientHandler(ICommDriver commDriver)
		{
			driver = commDriver;
		}
		
		public async Task Open()
		{
			while (driver.IsOpen)
            {
				string request = await driver.ReceiveAsync();
				if(request is null)
					break;
				string response = await HandleClientMessage(request);
				driver.SendSafe(response);
            }
		}
		
		public async Task<string> HandleClientMessage(string clientMessage)
		{
			Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",  
						clientMessage.Length, clientMessage );
			var js = JsonDocument.Parse(clientMessage);
			JsonElement root = js.RootElement;
			JsonElement type = root.GetProperty("Type");
			Console.WriteLine(type.GetString()+" Request");
			Console.WriteLine(clientMessage);
			switch(type.GetString())
			{
				case "Login":
					return await HandleLogin(JsonSerializer.Deserialize<LoginRequest>(clientMessage));
				case "Logout":
					return await HandleLogout(JsonSerializer.Deserialize<LogoutRequest>(clientMessage));
				case "Subnet":
					return await HandleSubnet(JsonSerializer.Deserialize<SubnetRequest>(clientMessage));
				default:
					var response = new MessageResponse()
					{
						Type = "Message",
						Message = "Unrecognized type in json request"
					};
					return JsonSerializer.Serialize(response);
			}
		}
		
		public async Task<string> HandleLogin(LoginRequest req)
		{
			var sessionId = await DBAccess.Instance.Login(req.Username, req.Password, req.IpAddress);
			if(sessionId is null)
			{
				throw new Exception("Login failed");
			}
			var response = new LoginResponse()
			{
				Type = "Login",
				SessionId = sessionId
			};
			return JsonSerializer.Serialize(response);
		}
		
		public async Task<string> HandleLogout(LogoutRequest req)
		{
			var success = await DBAccess.Instance.Logout(new ObjectId(req.SessionId));
			if(!success)
			{
				throw new Exception("Logout failed");
			}
			var response = new MessageResponse()
			{
				Type = "Message",
				Message = "Logout successful"
			};
			return JsonSerializer.Serialize(response);
		}
		
		public async Task<string> HandleSubnet(SubnetRequest req)
		{
			/*
			fork new thread that does the following
				watch the nets collection
				for each change document in the watch cursor
					check if it contains "operationType": "insert"
					extract the fullDocument property
					check if the fullDocument has a topology that includes the SessionId of the request passed to this method, req.SessionId
						if all of the above are true, notify the client, then terminate thread
						(perhaps client notification can be done via a callback passed to this method)
			*/
			Task.Run(()=>WaitForNet(req));
			
			var success = await DBAccess.Instance.RequestSubnet((Request) req);
			MessageResponse response;
			if(!success)
			{
				response = new MessageResponse()
				{
					Type = "Message",
					Message = "Could not immediately find a network to serve your request, your request has been saved for later"
				};
			}
			else
			{
				response = new MessageResponse()
				{
					Type = "Message",
					Message = "A network is available to serve your request, access will be granted shortly"
				};
			}
			return JsonSerializer.Serialize(response);
		}
		
		public async Task WaitForNet(SubnetRequest req)
		{
			Subnet network = DBAccess.Instance.WaitForNet((Request) req);
			NeighborsResponse response = new NeighborsResponse();
			response.Type = "Neighbors";
			response.Neighbors = DBAccess.Instance.GetNeighbors(new ObjectId(req.SessionId), network);
			string stringResponse = JsonSerializer.Serialize(response);
			await driver.SendSafe(stringResponse);
		}
	}
}