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
	public class SocketListener
	{
		/// <summary>
		/// Total clients connected to the server
		/// </summary>
		public Int32 Connections { get; set; }
		/// <summary>
		/// The socket used to listen for incoming connection requests
		/// </summary>
		public Socket ListenSocket { get; set; }

		/// <summary>
		/// Number of maximum allowed connections.
		/// </summary>
		public int MaxConnections { get; set; }

		/// <summary>
		/// Pool of reusable SocketAsyncEventArgs objects for accept operations
		/// </summary>
		public SocketAsyncEventArgsPool PoolOfAcceptEventArgs { get; set; }
		/// <summary>
		/// Pool of reusable SocketAsyncEventArgs objects for receive and send socket operations
		/// </summary>
		public SocketAsyncEventArgsPool PoolOfRecSendEventArgs { get; set; }

		/// <summary>
		/// The size of the buffer array to store the data. 
		/// </summary>
		public int BufferSize { get; set; }

		/// <summary>
		/// The Server endpoint. Contains IP address and Port.
		/// </summary>
		public IPEndPoint ServerEndPoint { get; set; }

		/// <summary>
		/// The number of pending incoming connections. 
		/// </summary>
		public int BackLog { get; set; }

		/// <summary>
		/// The peak amount of clients connected at the same time.
		/// </summary>
		public int ConnectionPeak { get; set; }

		public Client[] Clients { get; set; }

		public SocketListener()
		{
			this.Connections = 0;
			this.MaxConnections = 50;
			this.PoolOfRecSendEventArgs = new SocketAsyncEventArgsPool(MaxConnections + 1);
			this.PoolOfAcceptEventArgs = new SocketAsyncEventArgsPool(30);
			this.BufferSize = 1024;
			this.ServerEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3001);
			this.Clients = new Client[MaxConnections];
		}

		public void Start()
		{
			Initialize();
			StartListen();
		}
		public void Initialize()
		{
			SocketAsyncEventArgs eventArgObjectForPool;
			// Preallocate pool of SocketAsyncEventArgs objects for accept operations           
			for (Int32 i = 0; i < 30; i++)
			{

				this.PoolOfAcceptEventArgs.Push(CreateNewSaeaForAccept(PoolOfAcceptEventArgs));
			}

			for (Int32 i = 0; i < this.MaxConnections + 1; i++)
			{
				eventArgObjectForPool = new SocketAsyncEventArgs();

				eventArgObjectForPool.SetBuffer(0, BufferSize);

				eventArgObjectForPool.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);

				// add this SocketAsyncEventArg object to the pool.
				this.PoolOfRecSendEventArgs.Push(eventArgObjectForPool);
			}
		}
		public void StartListen()
		{
			ListenSocket = new Socket(ServerEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			ListenSocket.Bind(ServerEndPoint);
			ListenSocket.Listen(BackLog);

			Console.WriteLine($"Started listening on {ServerEndPoint.Port}");
			Console.WriteLine("Accepting incoming connections!");
			StartAccept();
		}

		public void StartAccept()
		{
			SocketAsyncEventArgs acceptEventArg;

			//Get a SocketAsyncEventArgs object to accept the connection.                        
			//Get it from the pool if there is more than one in the pool.
			//We could use zero as bottom, but one is a little safer.            
			if (this.PoolOfAcceptEventArgs.Count > 1)
			{
				try
				{
					acceptEventArg = this.PoolOfAcceptEventArgs.Pop();
				}
				//or make a new one.
				catch
				{
					acceptEventArg = CreateNewSaeaForAccept(PoolOfAcceptEventArgs);
				}
			}
			//or make a new one.
			else
			{
				acceptEventArg = CreateNewSaeaForAccept(PoolOfAcceptEventArgs);
			}


			bool willRaiseEvent = ListenSocket.AcceptAsync(acceptEventArg);
			if (!willRaiseEvent)
			{
				ProcessAccept(acceptEventArg);
			}
		}


		internal SocketAsyncEventArgs CreateNewSaeaForAccept(SocketAsyncEventArgsPool pool)
		{
			SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
			acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
			return acceptEventArg;
		}

		private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
		{
			ProcessAccept(e);
		}

		private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
		{
			// This is when there was an error with the accept op. That should NOT
			// be happening often. It could indicate that there is a problem with
			// that socket. If there is a problem, then we would have an infinite
			// loop here, if we tried to reuse that same socket.
			if (acceptEventArgs.SocketError != SocketError.Success)
			{
				// Loop back to post another accept op. Notice that we are NOT
				// passing the SAEA object here.
				StartAccept();
				//Let's destroy this socket, since it could be bad. 
				acceptEventArgs.AcceptSocket.Close();
				//Put the SAEA back in the pool.
				PoolOfAcceptEventArgs.Push(acceptEventArgs);

				//Jump out of the method.
				return;
			}

			if (Connections >= MaxConnections)
			{
				StartAccept();
				//Let's destroy this socket, since it could be bad. 
				acceptEventArgs.AcceptSocket.Close();
				//Put the SAEA back in the pool.
				PoolOfAcceptEventArgs.Push(acceptEventArgs);
			}

			//Now that the accept operation completed, we can start another
			//accept operation, which will do the same. Notice that we are NOT
			//passing the SAEA object here.
			StartAccept();

			// Get a SocketAsyncEventArgs object from the pool of receive/send op 
			//SocketAsyncEventArgs objects
			SocketAsyncEventArgs receiveSendEventArgs = this.PoolOfRecSendEventArgs.Pop();

			//A new socket was created by the AcceptAsync method. The 
			//SocketAsyncEventArgs object which did the accept operation has that 
			//socket info in its AcceptSocket property. Now we will give
			//a reference for that socket to the SocketAsyncEventArgs 
			//object which will do receive/send.
			receiveSendEventArgs.AcceptSocket = acceptEventArgs.AcceptSocket;


			//We have handed off the connection info from the
			//accepting socket to the receiving socket. So, now we can
			//put the SocketAsyncEventArgs object that did the accept operation 
			//back in the pool for them. But first we will clear 
			//the socket info from that object, so it will be 
			//ready for a new socket when it comes out of the pool.
			acceptEventArgs.AcceptSocket = null;
			this.PoolOfAcceptEventArgs.Push(acceptEventArgs);


			// Get empty client slot

			for (Int32 i = 0; i < this.MaxConnections; i++)
			{
				if (Clients[i] == null)
				{
					receiveSendEventArgs.UserToken = i;
					Clients[i] = new Client(receiveSendEventArgs, i, true);
					break;
				}
			}
			Console.WriteLine($"Client connected with ID {receiveSendEventArgs.UserToken.ToString()}");
			int _connections = this.Connections;
			Connections = Interlocked.Increment(ref _connections);
			if (Connections > ConnectionPeak)
			{
				int _connectionPeak = this.ConnectionPeak;
				ConnectionPeak = Interlocked.Increment(ref _connectionPeak);
			}
			Console.WriteLine($"There are {Connections} clients connected to the server!");


			StartReceive(receiveSendEventArgs);
		}

		/// <summary>
		/// Set the receive buffer and post a receive op.
		/// </summary>
		/// <param name="receiveSendEventArgs"></param>
		private void StartReceive(SocketAsyncEventArgs receiveSendEventArgs)
		{
			// Reset the buffer for the receive operation.
			receiveSendEventArgs.SetBuffer(new byte[BufferSize], 0, BufferSize);

			// Post async receive operation on the socket.
			bool willRaiseEvent = receiveSendEventArgs.AcceptSocket.ReceiveAsync(receiveSendEventArgs);
			if (!willRaiseEvent)
			{
				ProcessReceive(receiveSendEventArgs);
			}
		}

		/// <summary>
		/// This method is called whenever a receive or send operation completes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e">Represents the SocketAsyncEventArgs object associated with the completed receive or send operation.</param>
		void IO_Completed(object sender, SocketAsyncEventArgs e)
		{
			//Any code that you put in this method will NOT be called if
			//the operation completes synchronously, which will probably happen when
			//there is some kind of socket error.

			// Determine which type of operation just completed and call the associated handler
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Receive:
					ProcessReceive(e);
					break;

				case SocketAsyncOperation.Send:
					ProcessSend(e);
					break;

				default:
					//This exception will occur if you code the Completed event of some
					//operation to come to this method, by mistake.
					throw new ArgumentException("The last operation completed on the socket was not a receive or send");
			}
		}

		private void ProcessReceive(SocketAsyncEventArgs e)
		{
			// If there was a socket error, close the connection. This is NOT a normal
			// situation, if you get an error here.
			// In the Microsoft example code they had this error situation handled
			// at the end of ProcessReceive. Putting it here improves readability
			// by reducing nesting some.
			if (e.SocketError != SocketError.Success)
			{
				CloseClientSocket(e);
				//Jump out of the ProcessReceive method.
				return;
			}

			// If no data was received, close the connection. This is a NORMAL
			// situation that shows when the client has finished sending data.
			if (e.BytesTransferred == 0)
			{
				CloseClientSocket(e);
				return;
			}

			if (e.SocketError == SocketError.Success)
			{
				Console.WriteLine("Received data!");
				Console.WriteLine($"Length: {e.Buffer.Length}");
				string text = Convert.ToBase64String(e.Buffer);
				Console.WriteLine($"Data: text");
			}
			// Display recieved data. 

			StartReceive(e);
		}

		// This method is called by I/O Completed() when an asynchronous send completes.  
		// If all of the data has been sent, then this method calls StartReceive
		//to start another receive op on the socket to read any additional 
		// data sent from the client. If all of the data has NOT been sent, then it 
		//calls StartSend to send more data.        
		private void ProcessSend(SocketAsyncEventArgs receiveSendEventArgs)
		{
			if (receiveSendEventArgs.SocketError == SocketError.Success)
			{
				StartReceive(receiveSendEventArgs);
			}
			else
			{
				CloseClientSocket(receiveSendEventArgs);
			}
		}

		public void SendData(string text, Client client)
		{

			byte[] data = Convert.FromBase64String(text);
			client.ReceiveSendEventArgs.SetBuffer(data, 0, data.Length);
            bool willRaiseEvent = client.ReceiveSendEventArgs.AcceptSocket.SendAsync(client.ReceiveSendEventArgs);
			if (!willRaiseEvent)
			{
				ProcessSend(client.ReceiveSendEventArgs);
			}
		}

		public void CloseClientSocket(SocketAsyncEventArgs e)
		{
			// do a shutdown before you close the socket
			try
			{
				e.AcceptSocket.Shutdown(SocketShutdown.Both);
			}
			// throws if socket was already closed
			catch (Exception)
			{
			}

			int id = Convert.ToInt32(e.UserToken);
			Clients[id] = null;

			//This method closes the socket and releases all resources, both
			//managed and unmanaged. It internally calls Dispose.
			e.AcceptSocket.Close();

			// Put the SocketAsyncEventArg back into the pool,
			// to be used by another client. This 
			this.PoolOfRecSendEventArgs.Push(e);

			// decrement the counter keeping track of the total number of clients 
			//connected to the server, for testing
			int _numerOfAcceptedSockets = Connections;
			Interlocked.Decrement(ref _numerOfAcceptedSockets);
			Connections = _numerOfAcceptedSockets;
			Console.WriteLine($"Client {id} has disconnected!");
		}
	}
}
