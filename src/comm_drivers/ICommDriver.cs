using System.Threading.Tasks;

namespace SubnetServer
{
	interface ICommDriver
	{
		bool IsOpen {get;}
		Task<string> ReceiveAsync();
		Task SendSafe(string message);
	}
}