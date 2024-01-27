using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClientBomberman
{
    public class Client
    {
        public Guid PlayerId { get; set; }
        private Socket _socket { get; set; }
        private IPEndPoint _endPoint;
        private byte[] _buffer;
        private ArraySegment<byte> _bufferSegment { get; set; }
        public DateTime LastPingTime { get; set; }
        public IPAddress ClientIPAddress { get; private set; }
        public int Port { get; private set; }
        public string Status { get; private set; }


        public int MapWidth { get; set; } = 13;
        public int MapHeight { get; set; } = 11;

        public int[] Player1Coorditantes { get; set; }
        public int[] Player2Coorditantes { get; set; }
        public int[,] GameState { get; set; }

        public Client(IPAddress serverIPAddress, int serverPort, int clientPort)
        {
            Status = "Ongoing";
            string clientIp = "";
            _buffer = new byte[2048];
            _bufferSegment = new(_buffer);

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var addr = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (addr != null && !addr.Address.ToString().Equals("0.0.0.0"))
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                clientIp = ip.Address.ToString();
                            }
                        }
                    }
                }
            }

            if (clientIp == "")
                throw new Exception("No network adapters with an IPv4 address in the system!");

            ClientIPAddress = IPAddress.Parse(clientIp);
            Port = serverPort;
            _endPoint = new IPEndPoint(serverIPAddress, Port);

            GameState = new int[MapWidth, MapHeight];
            Player1Coorditantes = new int[2];
            Player2Coorditantes = new int[2];

            IPEndPoint clientEndPoint = new IPEndPoint(ClientIPAddress, clientPort);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(clientEndPoint);
        }

        public async Task SendCommand(Key key)
        {
            switch (key)
            {
                case Key.W:
                    {
                        await SendToAsync(Encoding.UTF8.GetBytes($"up {PlayerId}"));
                        break;
                    }

                case Key.A:
                    {
                        await SendToAsync(Encoding.UTF8.GetBytes($"left {PlayerId}"));
                        break;
                    }

                case Key.S:
                    {
                        await SendToAsync(Encoding.UTF8.GetBytes($"down {PlayerId}"));
                        break;
                    }

                case Key.D:
                    {
                        await SendToAsync(Encoding.UTF8.GetBytes($"right {PlayerId}"));
                        break;
                    }
                case Key.Space:
                    {
                        await SendToAsync(Encoding.UTF8.GetBytes($"place {PlayerId}"));
                        break;
                    }


                default:
                    break;
            }

        }

        public void StartMessageLoop()
        {

            _ = Task.Run(async () =>
            {
                SocketReceiveFromResult result;
                while (Status == "Ongoing")
                {
                    if (DateTime.Now - LastPingTime >= TimeSpan.FromSeconds(2))
                    {
                        await SendToAsync(Encoding.UTF8.GetBytes($"ping {PlayerId}"));
                        LastPingTime = DateTime.Now;
                    }

                    result = await _socket.ReceiveFromAsync(_bufferSegment, SocketFlags.None, _endPoint);
                    var message = Encoding.UTF8.GetString(_buffer, 0, result.ReceivedBytes);

                    //Console.WriteLine($"Recieved : {message} from {result.RemoteEndPoint}");
                    string[] response = message.Split(' ');


                    switch (response[0])
                    {
                        case "203":
                            {

                                PlayerId = Guid.Parse(response[1]);
                                continue;
                            }
                        case "202":
                            {
                                Player1Coorditantes[0] = int.Parse(response[response.Length - 4]);
                                Player1Coorditantes[1] = int.Parse(response[response.Length - 3]);

                                Player2Coorditantes[0] = int.Parse(response[response.Length - 2]);
                                Player2Coorditantes[1] = int.Parse(response[response.Length - 1]);

                                //Taking gamestate data from response

                                int responseSymbolNumber = 0;

                                for (int i = 0; i < MapWidth; i++)
                                {
                                    // Filling gamestate
                                    for (int j = 0; j < MapHeight; j++)
                                    {
                                        responseSymbolNumber++;
                                        GameState[i, j] = Int32.Parse(response[responseSymbolNumber]);
                                    }
                                }
                                continue;

                            }
                        case "204":
                            {
                                if (response[1] == "1")
                                {
                                    Status = "Victory";
                                }
                                else
                                {
                                    Status = "Defeat";
                                }
                                continue;
                            }
                        default:
                            continue;
                    }

                }
                _socket.Close();
                _socket.Shutdown(SocketShutdown.Both);
            });
        }

        public async Task SendToAsync(byte[] data)
        {
            var messageToSend = new ArraySegment<byte>(data);
            await _socket.SendToAsync(messageToSend, _endPoint);
        }

        public void SendTo(byte[] data)
        {
            var messageToSend = new ArraySegment<byte>(data);
            _socket.SendTo(messageToSend, _endPoint);
        }

    }
}

