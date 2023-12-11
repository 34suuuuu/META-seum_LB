using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using StarterAssets.Packet;
using System.Diagnostics;
using System.Threading;

namespace Server
{
    public class UDPSyncServer
    {
        protected int port;
        protected Socket udp;
        protected IPAddress ip;
        protected int idAssignIndex = 0;

        private Dictionary<EndPoint, Client> clients;

        protected Stopwatch totalTimeWatch;
        protected int packets;

        private bool isFirstPacket = true; // Add a variable to track if the first packet has been received

        private DateTime startTime;

        public UDPSyncServer(int serverPort)
        {
            port = serverPort;
            udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            clients = new Dictionary<EndPoint, Client>();
            ip = IPAddress.Parse("127.0.0.1");

            totalTimeWatch = new Stopwatch();
            packets = 0;

            BeginReceive();
            StartPacketCounting();
        }

        public void Start()
        {
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, port);
            udp.Bind(localEP);
            Console.WriteLine($"Sync Server ({port}) Start!");
        }

        private void StartPacketCounting()
        {
            startTime = DateTime.Now;
            totalTimeWatch.Start();
            Timer timer = new Timer(StopPacketCounting, null, 10000, Timeout.Infinite);
            Timer displayTimer = new Timer(DisplayElapsedTime, null, 0, 100);
        }

        private void DisplayElapsedTime(object state)
        {
            if (isFirstPacket)
            {
                Console.WriteLine("Waiting for the first packet...");
                return;
            }

            TimeSpan elapsed = DateTime.Now - startTime;

            Console.WriteLine($"Sync Server ({port}) Elapsed Time: {elapsed.TotalSeconds} seconds");

        }


        private void StopPacketCounting(object state)
        {
            Console.WriteLine($"Sync Server ({port}): {packets} packets");
            Console.WriteLine($"Sync Server ({port}) will now exit.");
            Environment.Exit(0);
        }

        private void BeginReceive()
        {
            byte[] buffer = new byte[1024];
            EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
            udp.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref clientEP, new AsyncCallback(OnReceive), buffer);
        }

        private void OnReceive(IAsyncResult ar)
        {
            byte[] buffer = (byte[])ar.AsyncState;
            EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
            int bytesRead = udp.EndReceiveFrom(ar, ref clientEP);


            if (isFirstPacket)
            {
                isFirstPacket = false;
                startTime = DateTime.Now;
                Console.WriteLine($"Sync Server ({port}) : Received the first packet. Starting the timer.");
            }

            PacketDatagram packet = PacketSerializer.Deserializer(buffer) as PacketDatagram;

            if (packet != null)
            {
                if (packet.status == "request")
                {
                    if (packet.source == "client" && packet.dest == "server")
                    {
                        HandleNewClient(ref packet, (IPEndPoint)clientEP);
                    }
                }
                else if (packet.status == "quit")
                {
                    DisconnectClient(ref packet);
                }
                else if (packet.status == "connected")
                {
                    packets++;
                    HandleConnectedClient(ref packet);
                }
            }
            BeginReceive();
        }

        protected virtual void HandleNewClient(ref PacketDatagram packet, IPEndPoint remoteEP)
        {
            packet.packetNum = 0;
            packet.status = "request";
            packet.portNum = remoteEP.Port;
            packet.playerInfoPacket.id = idAssignIndex++;
            packet.source = remoteEP.Address.ToString();

            SendPacket(ref packet, remoteEP);

            packet.status = "connected";
            if (remoteEP.Port != 1234)
            {
                AddClient(ref packet);
                BroadcastToNewClient(ref packet);
                SendPositionToAllClients(ref packet);
            }

        }

        protected virtual void AddClient(ref PacketDatagram pd)
        {
            IPEndPoint clientEP = new IPEndPoint(IPAddress.Parse(pd.source), pd.portNum);

            if (!clients.ContainsKey(clientEP))
            {
                clients.Add(clientEP, new Client(pd.playerInfoPacket.id, pd));
            }
        }

        protected virtual void BroadcastToNewClient(ref PacketDatagram pd)
        {
            IPEndPoint clientEP = new IPEndPoint(IPAddress.Parse(pd.source), pd.portNum);
            foreach (KeyValuePair<EndPoint, Client> client in clients)
            {
                SendPacket(ref client.Value.pd, clientEP); // 기존 패킷 to New Client
            }
        }

        protected virtual void SendPositionToAllClients(ref PacketDatagram pd)
        {
            foreach (KeyValuePair<EndPoint, Client> p in clients)
            {
                SendPacket(ref pd, p.Key);
            }
        }

        protected virtual void HandleConnectedClient(ref PacketDatagram pd)
        {
            IPEndPoint clientEP = new IPEndPoint(IPAddress.Parse(pd.source), pd.portNum);
            int packetId = pd.playerInfoPacket.id;
            int seqNum = pd.packetNum;
            if (packetId == -1 || seqNum == -1)
                return;
            HandleUserMoveInput(ref pd, clientEP, seqNum);
        }

        protected virtual void HandleUserMoveInput(ref PacketDatagram pd, EndPoint clientEP, int seqNumber)
        {
            UpdatePosition(clientEP, ref pd);
            SendPositionToAllClients(ref pd);
        }

        protected virtual void UpdatePosition(EndPoint addr, ref PacketDatagram pd)
        {
            if (clients.ContainsKey(addr))
            {
                clients[addr].pd = pd;
                clients[addr].pos.x = pd.playerPosPacket.x;
                clients[addr].pos.y = pd.playerPosPacket.y;
                clients[addr].pos.z = pd.playerPosPacket.z;
                clients[addr].cam.x = pd.playerCamPacket.x;
                clients[addr].cam.y = pd.playerCamPacket.y;
                clients[addr].cam.z = pd.playerCamPacket.z;
                clients[addr].cam.w = pd.playerCamPacket.w;
            }
        }

        protected virtual void DisconnectClient(ref PacketDatagram pd)
        {
            IPEndPoint clientEP = new IPEndPoint(IPAddress.Parse(pd.source), pd.portNum);
            Console.WriteLine($"id:{pd.playerInfoPacket.id}, disconnect client");
            if (clients.ContainsKey(clientEP))
                clients.Remove(clientEP);
            Broadcast(ref pd);
        }

        protected virtual void Broadcast(ref PacketDatagram pd)
        {
            foreach (KeyValuePair<EndPoint, Client> p in clients)
                SendPacket(ref pd, p.Key);
        }

        protected virtual void SendPacket(ref PacketDatagram pd, EndPoint addr)
        {
            byte[] packet = PacketSerializer.Serializer(pd);
            udp.SendTo(packet, addr);
        }
    }
}
