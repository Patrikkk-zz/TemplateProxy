using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EndClient
{
	public class Client
	{

		/// <summary>
		/// Socket connection of our client.
		/// </summary>
		public Socket ClientSocket { get; set; }

		/// <summary>
		/// Indicates if our client is actually connected.
		/// </summary>
		public bool IsConnected { get; set; }

		/// <summary>
		/// IPEndPoint of our target server.
		/// </summary>
		public IPEndPoint HostEndPoint { get; set; }

		SocketAsyncEventArgs RecSendAsyncEventArgs { get; set; }

		public int BufferSize { get; set; }

		public Client(IPEndPoint hostEndPoint)
		{
			HostEndPoint = hostEndPoint;
			ClientSocket = new Socket(HostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			BufferSize = 1024;
		}

		public void Connect()
		{
			RecSendAsyncEventArgs = new SocketAsyncEventArgs();

			RecSendAsyncEventArgs.UserToken = ClientSocket;
			RecSendAsyncEventArgs.RemoteEndPoint = HostEndPoint;
			RecSendAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);

			bool willRaiseEvent = ClientSocket.ConnectAsync(RecSendAsyncEventArgs);
			if (!willRaiseEvent)
			{
				ProcessConnect(RecSendAsyncEventArgs);
			}
		}

		private void ProcessConnect(SocketAsyncEventArgs connectArgs)
		{
			if (IsConnected)
			{
				return;
			}
			if (connectArgs.SocketError == SocketError.SocketError)
			{
				Console.WriteLine("Failed to connect to target host!");
				// Close connection and client. 
				return;
			}
			Console.WriteLine("Successfully connected to target host!");
			IsConnected = true;
			StartReceive(connectArgs);
		}
		private void StartReceive(SocketAsyncEventArgs receiveSendEventArgs)
		{
			// Reset the buffer for the receive operation.
			receiveSendEventArgs.SetBuffer(new byte[BufferSize], 0, BufferSize);

			// Post async receive operation on the socket.
			bool willRaiseEvent = receiveSendEventArgs.ConnectSocket.ReceiveAsync(receiveSendEventArgs);
			if (!willRaiseEvent)
			{
				ProcessReceive(receiveSendEventArgs);
			}
		}

		private void IO_Completed(object sender, SocketAsyncEventArgs e)
		{
			// determine which type of operation just completed and call the associated handler
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Connect:
					ProcessConnect(e);
					break;

				case SocketAsyncOperation.Receive:
					ProcessReceive(e);
					break;

				case SocketAsyncOperation.Send:
					//ProcessSend(e);
					break;

				case SocketAsyncOperation.Disconnect:
					ProcessDisconnectAndCloseSocket(e);
					break;
				default:
					break;
			}
		}

		private void ProcessReceive(SocketAsyncEventArgs e)
		{
			// Display Data
			if (e.SocketError == SocketError.Success)
			{
				Console.WriteLine("Received data!");
				Console.WriteLine($"Length: {e.BytesTransferred}");
				byte[] data = new byte[e.BytesTransferred];
				Buffer.BlockCopy(e.Buffer, 0, data, 0, data.Length);
				string text = Encoding.Default.GetString(data);
				Console.WriteLine($"Data: {text}");
			}
			StartReceive(e);
		}

		public void SendData(string text)
		{
			byte[] data = Encoding.Default.GetBytes(text);
			ClientSocket.Send(data);
		}

		private void ProcessSend(SocketAsyncEventArgs receiveSendEventArgs)
		{
			if (receiveSendEventArgs.SocketError == SocketError.Success)
			{
				//StartReceive(receiveSendEventArgs);
			}
		}

		private void ProcessDisconnectAndCloseSocket(SocketAsyncEventArgs receiveSendEventArgs)
		{
			receiveSendEventArgs.AcceptSocket.Close();
		}
	}
}
