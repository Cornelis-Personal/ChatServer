using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Viewer
{
    internal class TcpChatViewer
    {
        public readonly string ServerAddress;
        public readonly int Port;
        private TcpClient _client;
        public bool Running { get; private set; }
        private bool _disconnectedRequested = false;

        // Buffer & messaging
        public readonly int BufferSize = 2 * 1024; // 2KB

        private NetworkStream _msgStream = null;

        public TcpChatViewer(string serverAddress, int port)
        {
            // Create a connection
            _client = new TcpClient();
            _client.SendBufferSize = BufferSize;
            _client.ReceiveBufferSize = BufferSize;
            Running = false;

            // Set the outher things
            ServerAddress = serverAddress;
            Port = port;
        }

        // Connect
        public void Connect()
        {
            // Now try to connect
            _client.Connect(ServerAddress, Port); // Will resolve DNS for us; blocks
            EndPoint endPoint = _client.Client.RemoteEndPoint;

            // Check that we are connected
            if (_client.Connected)
            {
                // got in!
                Console.WriteLine("Connected to the server at {0}.", endPoint);

                // === AUTHENTICATION ===
                // Send them the message that we're a viewer
                _msgStream = _client.GetStream();

                // === ENCRUPTION/ENCODING ===
                byte[] msgBuffer = Encoding.UTF8.GetBytes("viewer");

                _msgStream.Write(msgBuffer, 0, msgBuffer.Length);

                // Check that we're still connected, if the server has not kicked us we're in
                if (!_isDisconnected(_client))
                {
                    Running = true;
                    Console.WriteLine("Press Ctrl-C to exit the Viewer at any time.");
                }
                else
                {
                    // Server doens't see us as a viewer, cleanup
                    _cleanupNetworkResources();
                    Console.WriteLine("The server didn't recognise us as a Viewer.\n:[");
                }
            }
            else
            {
                _cleanupNetworkResources();
                Console.WriteLine("Wasn't able to connect to the server at {0}.", endPoint);
            }
        }

        // Requests a disconnect
        public void Disconnect()
        {
            Running = false;
            _disconnectedRequested = true;
            Console.WriteLine("Disconnecting from the chat...");
        }

        public void ListenForMessages()
        {
            bool wasRunning = Running;

            // Listen for messages
            while (true)
            {
                // Do we have a new message
                int messageLength = _client.Available;

                if (messageLength > 0)
                {
                    // Read the whole message
                    byte[] msgBuffer = new byte[messageLength];
                    _msgStream = _client.GetStream();
                    _msgStream.Read(msgBuffer, 0, messageLength);

                    // Decode it and print it
                    string msg = Encoding.UTF8.GetString(msgBuffer);
                    Console.WriteLine(msg);
                }

                // === OPTIMIZATION ===
                // Use less cpu
                Thread.Sleep(10);

                // Check that the server didn't disconnect us
                if (_isDisconnected(_client))
                {
                    Running = false;
                    Console.WriteLine("Server has disconnected from us. \n:[");
                }

                // Check that a cancel has been requested by the user
                Running &= !_disconnectedRequested;
            }

            // Cleanup
            _cleanupNetworkResources();
            if (wasRunning)
                Console.WriteLine("Disconnected.");
        }

        // Cleans any leftover network resources
        private void _cleanupNetworkResources()
        {
            _msgStream?.Close();
            _msgStream = null;
            _client.Close();
        }

        // Checks if a socket has disconnected
        // Adapted from -- http://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket
        private static bool _isDisconnected(TcpClient client)
        {
            try
            {
                Socket s = client.Client;
                return s.Poll(10 * 1000, SelectMode.SelectRead) && (s.Available == 0);
            }
            catch (SocketException se)
            {
                // We got a socket error, assume it's disconnected
                return true;
            }
        }

        public static TcpChatViewer viewer;

        protected static void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            viewer.Disconnect();
            args.Cancel = true;
        }

        public static void Main(string[] args)
        {
            //Setup the viewer
            string host = "localhost";
            int port = 6000;
            viewer = new TcpChatViewer(host, port);

            // Add a hangler for a Ctrl-C press
            Console.CancelKeyPress += InterruptHandler;

            // Try to connect & view messages
            viewer.Connect();
            viewer.ListenForMessages();
        }
    }
}