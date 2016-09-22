using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EndServer
{
	public class Client
	{
		/// <summary>
		/// The eventArgs object we use between client and server.
		/// </summary>
		public SocketAsyncEventArgs ReceiveSendEventArgs { get; set; }

		/// <summary>
		/// The ID of the client.
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// Indicates wether the client slot is connected the the server.
		/// </summary>
		public bool IsConnected { get; set; }


		public Client(SocketAsyncEventArgs receiveSendEventArgs, int id, bool isConnected)
		{
			this.ReceiveSendEventArgs = receiveSendEventArgs;
			this.Id = id;
			this.IsConnected = isConnected;
		}

		public void Reset()
		{
			ReceiveSendEventArgs = null;
			Id = -1;
			this.IsConnected = false;
		}
	}
}
