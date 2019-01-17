using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Perfection.Addons.StreamCommunication;
using System.Threading;

namespace Messenger
{
    internal class TcpChatMessenger
    {
        /* Connection objects */
        public readonly string ServerAddress;
        public readonly int Port;
        private TcpClient _client;
        public bool Running { get; private set; }

        /* Buffer and Messeging */
        public readonly int BufferSize = 2 * 1024; // 2kb
        private NetworkStream _msgStream;

        /* Personal Data */
        private string Name { get; set; }

        public TcpChatMessenger(string serverAddres, int port, string name)
        {
            // Create a non-connected tcpClient
            _client = new TcpClient(); // Other constructors will start a connection
            _client.SendBufferSize = BufferSize;
            _client.ReceiveBufferSize = BufferSize;

            ServerAddress = serverAddres;
            Port = port;
            Name = name;
        }

        public void Connect()
        {
            // try to connect
            _client.Connect(ServerAddress, Port); // Will resolve DNS for us; blocks
            EndPoint endPoint = _client.Client.RemoteEndPoint;

            // Make sure we're connected
            if (_client.Connected)
            {
                // Got it
                Console.WriteLine($"Connected to the server at {endPoint}");

                _msgStream.StreamWrite(_client, String.Format($"name: {Name}"));

                // If We're still connected after sending our name, that means the server accepted us
                if (!_isDisconnected(_client))
                {
                    Running = true;
                }
                else
                {
                    // name was probally taken
                    _cleanupNetworkResources();
                    Console.WriteLine("Connection to the server refused...");
                }
            }
            else
            {
                _cleanupNetworkResources();
                Console.WriteLine($"Wasn't able to connect to the server at {endPoint}.");
            }
        }

        public void SendMessage()
        {
            bool wasRunning = Running;

            while (Running)
            {
                // Poll for user input
                Console.Write("{0}> ", Name);
                string msg = Console.ReadLine();

                // Quit or send a message
                if (msg.ToLower() == "quit" || msg.ToLower() == "exit")
                {
                    // User wants to quit
                    Console.WriteLine("Disconnecting...");
                    Running = false;
                }
                else if (msg != string.Empty)
                {
                    // Send the message
                    byte[] msgBuffer = Encoding.UTF8.GetBytes(msg);
                    _msgStream = _client.GetStream();
                    _msgStream.Write(msgBuffer, 0, msgBuffer.Length);
                }

                // User less CPU
                Thread.Sleep(10);

                // Check the server didn't disconnect us
                if (_isDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("Server has disconnected from us.\n:[");
                }
            }

            _cleanupNetworkResources();
            if (wasRunning)
                Console.WriteLine("Disconnected");
        }

        private void _cleanupNetworkResources()
        {
            _msgStream?.Close();
            _msgStream = null;
            _client.Close();
        }

        // Check if the socket has disconnected
        private static bool _isDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }
            catch (SocketException se)
            {
                // We got a socket error, assume its disconnectioned
                return true;
            }
        }

        public static void Main(string[] args)
        {
            // Get a name
            Console.Write("Enter a name to use: ");
            string name = Console.ReadLine();

            // Setup the messenger
            string host = "58.179.89.247";
            int port = 6000;
            TcpChatMessenger messenger = new TcpChatMessenger(host, port, name);

            // Connect and send messages
            messenger.Connect();
            messenger.SendMessage();
        }
    }
}