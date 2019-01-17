using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Perfection.Addons.StreamCommunication
{
    public static class StreamWriter
    {
        public static void StreamWrite(this NetworkStream _stream, TcpClient _client, string _msg)
        {
            _stream = _client.GetStream();
            byte[] msgBuffer = Encoding.UTF8.GetBytes(String.Format(_msg));
            _stream.Write(msgBuffer, 0, msgBuffer.Length);
        }
    }
}