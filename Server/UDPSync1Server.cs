using System;
using System.Net;
using System.Net.Sockets;
using StarterAssets.Packet;
using System.Collections.Generic;


namespace Server
{
    public class UDPSync1Server
    {
        private int port = 6061;
        //private int room1Port = 8080;
        private Socket udp;
        private IPAddress ip;
        private int idAssignIndex = 0;

        private Dictionary<EndPoint, Client> clients;

        public UDPSync1Server()
        {
            udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            clients = new Dictionary<EndPoint, Client>();
            ip = IPAddress.Parse("127.0.0.1");

            BeginReceive();
        }

        public void Start()
        {
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, port);
            udp.Bind(localEP);
            Console.WriteLine("Sync1 Server Start!");

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
                    HandleConnectedClient(ref packet);
                }
            }
            BeginReceive();
        }

        private void HandleNewClient(ref PacketDatagram packet, IPEndPoint remoteEP)
        {
            //IPEndPoint clientEP = new IPEndPoint(ip, packet.portNum);

            packet.packetNum = 0;
            packet.status = "request";
            packet.portNum = remoteEP.Port;
            packet.playerInfoPacket.id = idAssignIndex++;
            packet.source = remoteEP.Address.ToString();

            //Console.WriteLine("Sync1-pd.source: {0}", packet.source);

            SendPacket(ref packet, remoteEP);

            packet.status = "connected";
            if (remoteEP.Port != 1234)
            {
                AddClient(ref packet);
                BroadcastToNewClient(ref packet);
                SendPositionToAllClients(ref packet);
            }

            //Console.WriteLine("Sync1 ------ Send Packet to Client!\n");
        }

        private void AddClient(ref PacketDatagram pd)
        {

            IPEndPoint clientEP = new IPEndPoint(IPAddress.Parse(pd.source), pd.portNum);

            if (!clients.ContainsKey(clientEP))
            {
                clients.Add(clientEP, new Client(pd.playerInfoPacket.id, pd));
            }
        }

        void BroadcastToNewClient(ref PacketDatagram pd)
        {
            IPEndPoint clientEP = new IPEndPoint(IPAddress.Parse(pd.source), pd.portNum);
            foreach (KeyValuePair<EndPoint, Client> client in clients)
            {
                SendPacket(ref client.Value.pd, clientEP); // 기존 패킷 to New Client
            }
        }

        void SendPositionToAllClients(ref PacketDatagram pd)
        {
            foreach (KeyValuePair<EndPoint, Client> p in clients)
            {
                SendPacket(ref pd, p.Key);
            }
        }

        private void HandleConnectedClient(ref PacketDatagram pd)
        {
            IPEndPoint clientEP = new IPEndPoint(IPAddress.Parse(pd.source), pd.portNum);
            int packetId = pd.playerInfoPacket.id;
            int seqNum = pd.packetNum;
            if (packetId == -1 || seqNum == -1)
                return;
            HandleUserMoveInput(ref pd, clientEP, seqNum);
        }

        void HandleUserMoveInput(ref PacketDatagram pd, EndPoint clientEP, int seqNumber)
        {
            //if (!clients.ContainsKey(clientEP) || clients[clientEP].lastSeqNumber > seqNumber)
            //    return;

            //if (!clients[clientEP].history.ContainsKey(seqNumber))
            //{
            //    clients[clientEP].UpdateStateHistory(seqNumber);
            //    clients[clientEP].lastSeqNumber = seqNumber;
            //}
            UpdatePosition(clientEP, ref pd);
            /* so that clients see newly connected clients */
            SendPositionToAllClients(ref pd);
        }

        void UpdatePosition(EndPoint addr, ref PacketDatagram pd)
        {
            //Debug.Log($"packetId: {pd.playerInfoPacket.id}" +
            //          $"position: {pd.playerPosPacket.toString()}");
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

        void DisconnectClient(ref PacketDatagram pd)
        {
            IPEndPoint clientEP = new IPEndPoint(IPAddress.Parse(pd.source), pd.portNum);
            Console.WriteLine($"id:{pd.playerInfoPacket.id}, disconnect client");
            if (clients.ContainsKey(clientEP))
                clients.Remove(clientEP);
            Broadcast(ref pd);
        }
        void Broadcast(ref PacketDatagram pd)
        {
            foreach (KeyValuePair<EndPoint, Client> p in clients)
                SendPacket(ref pd, p.Key);
        }

        private void SendPacket(ref PacketDatagram pd, EndPoint addr)
        {
            byte[] packet = PacketSerializer.Serializer(pd);
            //IPEndPoint ipEndpoint = addr as IPEndPoint; // EndPoint를 IPEndPoint로 형변환
            udp.SendTo(packet, addr);
        }
    }
}