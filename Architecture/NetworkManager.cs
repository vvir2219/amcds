using System;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Protocol;

namespace Project
{
    class NetworkManager
    {
        public SystemInfo SystemInfo { get; set; }
        public EventLoop EventLoop { get; set; }

        private bool running = true;
        public bool Running { set { running = value; }}

        public void StartListener()
        {
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, SystemInfo.SELF_PORT);

            Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(endPoint);
            socket.Listen(0);

            while (running) {
                Socket handler = socket.Accept();
                handler.Receive()
            }
        }

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

            SendMessage(outerMessage, host, port);
        }

        public void SendMessage(Message message, string host, int port)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAddress = IPAddress.Parse(host);
            IPEndPoint endPoint = new IPEndPoint(ipAddress, port);

            byte[] bytes = message.ToByteArray();

            socket.Connect(endPoint);
            socket.Send(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length)));
            socket.Send(bytes);
            socket.Close();
        }
    }
}