using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using StarterAssets.Packet;

namespace Server
{

    public class UDPRoomServer
    {
        private int port = 1234;
        private int sync1Port = 6061;
        private int sync2Port = 6062;
        private int sync3Port = 6063;

        private Socket udp;
        private IPAddress ip;
        private Dictionary<EndPoint, Client> clients;
        private List<IPEndPoint> servers;   // connected sync server

        private const int group1Weight = 2;
        private const int group2Weight = 3;
        private const int group3Weight = 5;

        private int[] groupWeights = new int[3] { group1Weight, group2Weight, group3Weight };
        private Dictionary<IPEndPoint, int> serverWeights;

        private List<int> originalServerWeights;  
        private int currentIndex;

        private Dictionary<IPEndPoint, int> packetsReceivedCount; 
        private Stopwatch totalTimeWatch;

        public UDPRoomServer()
        {
            udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            clients = new Dictionary<EndPoint, Client>();
            ip = IPAddress.Parse("127.0.0.1");

            servers = new List<IPEndPoint>
            {
                new IPEndPoint(ip, 6061),
                new IPEndPoint(ip, 6062),
                new IPEndPoint(ip, 6063),
            };

            serverWeights = new Dictionary<IPEndPoint, int>
            {
                [servers[0]] = 100,
                [servers[1]] = 50,
                [servers[2]] = 20,
            };

            originalServerWeights = new List<int>(serverWeights.Values);

            packetsReceivedCount = new Dictionary<IPEndPoint, int>();
            totalTimeWatch = new Stopwatch();

            BeginReceive();
            StartPacketCounting();
        }

        public void Start()
        {
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, port);
            udp.Bind(localEP);

            Console.WriteLine("Room1 Server Start!");
        }

        private void StartPacketCounting()
        {
            totalTimeWatch.Start();
            Timer timer = new Timer(StopPacketCounting, null, 20000, Timeout.Infinite);
        }

        private void StopPacketCounting(object state)
        {
            Console.WriteLine(packetsReceivedCount.Keys.Count);
            Console.WriteLine("Packet counts received during the last 10 seconds:");

            foreach (var kvp in packetsReceivedCount)
            {
                Console.WriteLine($"Server {kvp.Key}: {kvp.Value} packets");
            }
            Environment.Exit(0);
        }

        private void InitializeServerWeights()
        {
            //foreach (var server in servers)
            //{
            //    serverWeights[server] = 0;
            //}
            serverWeights = new Dictionary<IPEndPoint, int>(originalServerWeights.Count);
            int index = 0;

            // 변경된 값들을 originalServerWeights의 값들로 초기화
            foreach (var server in servers)
            {
                serverWeights[server] = originalServerWeights[index];
                index++;
            }
        }

        private void BeginReceive()
        {
            byte[] buffer = new byte[1024];
            EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                udp.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref clientEP, new AsyncCallback(OnReceive), buffer);
            }
            catch (Exception e)
            {
                Console.WriteLine("Winsock error: " + e.ToString());
            };
        }

        private void OnReceive(IAsyncResult ar)
        {
            byte[] buffer = (byte[])ar.AsyncState;
            EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
            int bytesRead = udp.EndReceiveFrom(ar, ref clientEP);

            IPEndPoint clientIPEndPoint = (IPEndPoint)clientEP;
         
            PacketDatagram packet = PacketSerializer.Deserializer(buffer) as PacketDatagram;

            if (packet != null)
            {
                HandlePacket(ref packet, (IPEndPoint)clientEP);
                IncrementPacketCount(clientIPEndPoint);
            }

            BeginReceive();
        }

        private void HandlePacket(ref PacketDatagram pd, IPEndPoint clientEP)
        {
            pd.portNum = clientEP.Port;
            pd.source = clientEP.Address.ToString();

            if (!clients.ContainsKey(clientEP))
            {
                clients.Add(clientEP, new Client(pd.playerInfoPacket.id, pd));
            }
            if (pd.status.Equals("connected"))
            {
                if (serverWeights.Count == 0)
                {
                    Console.WriteLine("No servers available");
                    return;
                }

                int maxWeight = serverWeights.Values.Max();

                while (true)
                {
                    IPEndPoint currentServer = servers[currentIndex];
                    int currentWeight = serverWeights[currentServer];

                    if (currentWeight >= maxWeight)
                    {
                        SendPacket(ref pd, currentServer);

                        if (serverWeights[currentServer] - GetGroupWeight(ref pd) >= 0)
                        {
                            serverWeights[currentServer] -= GetGroupWeight(ref pd);
                        }

                        if (ChkResetCondition())
                        {
                            ResetWeights();
                        }
                        
                        currentIndex = (currentIndex + 1) % servers.Count;
                        break;
                    }

                    currentIndex = (currentIndex + 1) % servers.Count;
                }
            }
        }

        private void IncrementPacketCount(IPEndPoint syncServer)
        {
            if (!packetsReceivedCount.ContainsKey(syncServer))
            {
                packetsReceivedCount[syncServer] = 1;
            }
            else
            {
                packetsReceivedCount[syncServer]++;
            }
        }

        private int GetGroupWeight(ref PacketDatagram pd)
        {
            if (pd.playerInfoPacket.group == 1) return 5;
            else if (pd.playerInfoPacket.group == 2) return 3;
            else return 1;
        }

        private void SendPacket(ref PacketDatagram pd, EndPoint addr)
        {
            byte[] packet = PacketSerializer.Serializer(pd);
            udp.SendTo(packet, addr);
        }

        private void ResetWeights()
        {
            // serverWeights를 originalServerWeights 값으로 복사하여 초기화
            //serverWeights = new Dictionary<IPEndPoint, int>(originalServerWeights.Count);

            int index = 0;
            foreach (var server in servers)
            {
                serverWeights[server] = originalServerWeights[index];
                index++;
            }
        }

        // true: 더 처리할 수 있는 상태 -> false: reset
        private Boolean ChkResetCondition()
        {
            foreach (var serverWeight in serverWeights)
            {
                foreach (var groupWeight in groupWeights)
                {
                    if (serverWeight.Value % groupWeight == 0) continue;
                    else return false;
                }
            }
            return true;
        }
    }

}
