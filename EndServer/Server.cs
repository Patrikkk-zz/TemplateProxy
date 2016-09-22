using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EndServer
{
	public class Server
	{
		public SocketListener SocketListener { get; set; }
		public Server()
		{

        }

		public void Start()
		{
			SocketListener = new SocketListener();
			SocketListener.Start();

			while (true)
			{
				string input = Console.ReadLine();
				if (input != null)
				{
					string[] args = input.Split(' ');
					string command = args[0];
					args = args.Skip(1).ToArray();

					switch (command)
					{
						case "exit":
							{
								Console.WriteLine("Shutting down server!");
								Thread.Sleep(5000);
								Environment.Exit(3);
							}
							break;
						case "kick":
							{
								if (args.Count() != 1)
								{
									Console.WriteLine("Syntax: kick <user ID>");
									return;
								}
								int id = -1;
								if (!int.TryParse(args[0], out id) || id < 0 || id > SocketListener.MaxConnections)
								{
									Console.WriteLine("Please enter a valid number!");
									return;
								}
								if (!SocketListener.Clients[id].IsConnected)
								{
									Console.WriteLine("No client with that ID is connected!");
									return;
								}
								SocketListener.CloseClientSocket(SocketListener.Clients[id].ReceiveSendEventArgs);
							}
							break;
						default:
							{
								foreach (Client client in SocketListener.Clients)
								{
									if (client != null && client.IsConnected)
									{
										SocketListener.SendData(input, client);
										break;
									}
								}
							}
							break;
					}
				}
			}
		}
	}
}
