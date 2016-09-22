using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EndClient
{
	class Program
	{
		static void Main(string[] arg)
		{
			string input = "";
			IPAddress ip = IPAddress.Any;
			int port = -1;

			while (!IPAddress.TryParse(input, out ip))
			{
				Console.Write("IP: ");
				input = Console.ReadLine();
			}
			while (!int.TryParse(input, out port))
			{
				Console.Write("Port: ");
				input = Console.ReadLine();
				Console.Clear();
			}


			IPEndPoint hostEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
			Client client = new Client(hostEndPoint);
			client.Connect();

			while (true)
			{
				input = string.Empty;
				input = Console.ReadLine();
				if (input != string.Empty)
				{
					client.SendData(input);
				}
			}
		}
	}
}
