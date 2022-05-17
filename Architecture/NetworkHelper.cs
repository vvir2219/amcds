using System;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Protocol;

namespace Project
{
    class NetworkHelper
    {
        public SystemInfo SystemInfo { get; set; }

        public void SendNetworkMessage(Message message, string host, int port)
        {
                var networkMessage = new NetworkMessage {
                    SenderHost = SystemInfo.SELF_HOST,
                    SenderListeningPort = SystemInfo.SELF_PORT,
                    Message = message
                };

                var outerMessage = new Message {
                    Type = Message.Types.Type.NetworkMessage,
                    MessageUuid = System.Guid.NewGuid().ToString(),
                    NetworkMessage = networkMessage
                };

                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ipAddress = IPAddress.Parse(host);
                IPEndPoint endPoint = new IPEndPoint(ipAddress, port);

                byte[] bytes = outerMessage.ToByteArray();

                socket.Connect(endPoint);
                socket.Send(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length)));
                socket.Send(bytes);
                socket.Close();
        }
    }
}