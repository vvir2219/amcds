using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;
using Protocol;

namespace Project
{
    class NetworkManager
    {
        public System System { get; set; }

        private bool running = true;
        public bool Running { set { running = value; }}

        public NetworkManager(System system)
        {
            System = system;
        }

        public void StartListener()
        {
            IPAddress ipAddress = IPAddress.Parse(System.SystemInfo.SELF_HOST);
            IPEndPoint endPoint = new IPEndPoint(ipAddress, System.SystemInfo.SELF_PORT);

            (new Thread(() =>
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) {
                    socket.Bind(endPoint);
                    socket.Listen(0);
                    Console.WriteLine($"Started listening on {ipAddress.ToString()}:{System.SystemInfo.SELF_PORT}.");

                    while (running) {
                        Socket handler = socket.Accept();
                        Console.WriteLine("Connection accepted");

                        // read message length
                        var buffer = new byte[4];
                        handler.Receive(buffer);

                        int size = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
                        Console.WriteLine($"Message size: {size}");
                        buffer = new byte[size];

                        // read the message
                        handler.Receive(buffer, 0, size, SocketFlags.None);

                        var message = Message.Parser.ParseFrom(buffer);
                        Console.WriteLine($"Message: {message.ToString()}");
                        System.EventQueue.RegisterMessage(UnmarshalMessage(message));
                        handler.Close();
                    }
                }
            })).Start();
        }

        public void SendNetworkMessage(Message message, string host, int port)
        {
            var networkMessage = new NetworkMessage {
                SenderHost = System.SystemInfo.SELF_HOST,
                SenderListeningPort = System.SystemInfo.SELF_PORT,
                Message = message
            };

            var outerMessage = new Message {
                Type = Message.Types.Type.NetworkMessage,
                MessageUuid = Guid.NewGuid().ToString(),
                NetworkMessage = networkMessage
            };

            SendMessage(outerMessage, host, port);
        }

        public void SendMessage(Message message, string host, int port)
        {
            IPAddress ipAddress = IPAddress.Parse(host);
            IPEndPoint endPoint = new IPEndPoint(ipAddress, port);

            byte[] bytes = message.ToByteArray();

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) {
                socket.Connect(endPoint);
                socket.Send(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length)));
                socket.Send(bytes);
            }
        }

        private Message UnmarshalMessage(Message message)
        {
            if (message.Type == Message.Types.Type.NetworkMessage) {
                var networkMessage = message.NetworkMessage;
                var innerMessage = networkMessage.Message;

                var plDeliver = new PlDeliver {
                    Sender = System.GetProcessByHostAndPort(networkMessage.SenderHost, networkMessage.SenderListeningPort),
                    Message = innerMessage
                };

                return new Message {
                    Type = Message.Types.Type.PlDeliver,
                    MessageUuid = message.MessageUuid,
                    ToAbstractionId = message.ToAbstractionId,
                    PlDeliver = plDeliver
                };
            }
            else {
                return message;
            }
        }

    }
}