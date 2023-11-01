﻿using System;
using StarterAssets.Packet;
    {
        private List<IPEndPoint> servers;
        private Dictionary<IPEndPoint, (int currentWeight, int maxWeight)> serverWeights;
        private int resetInterval = 20; // reset for 20 request

        public LoadBalancer(List<IPEndPoint> servers, Dictionary<IPEndPoint,int> weights)
        {
            this.servers = servers;
            InitalizeServerMaxWeights(weights);
        }


        private void InitalizeServerMaxWeights(Dictionary<IPEndPoint, int> weights)
        {
            serverWeights = new Dictionary<IPEndPoint, (int currentWeight, int maxWeight)>();
            foreach(var server in servers) 
            {
                if (weights.ContainsKey(server))
                {
                    serverWeights[server] = (0, weights[server]);
                }
                else
                {
                    serverWeights[server] = (0, 1);
                }
            }
        }


        public IPEndPoint SelectServer()
        {
            IPEndPoint selectedServer = null;
            int maxWeight = 0;

            foreach(var server in servers)
            {
                if (serverWeights.ContainsKey(server))
                {
                    var (currentWeight, max) = serverWeights[server];
                    if(currentWeight > maxWeight)
                    {
                        maxWeight = currentWeight;
                        selectedServer = server;
                    }

                    serverWeights[server] = (currentWeight + max, max);
                }
            }

            if(resetInterval > 0)
            {
                if(resetInterval % servers.Count == 0)
                {
                    ResetWeights();
                }
                resetInterval++;
            }
            
            return selectedServer;
        }


        private void ResetWeights()
        {
            Dictionary<IPEndPoint, (int currentWeight, int maxWeight)> newWeights = new Dictionary<IPEndPoint, (int currentWeight, int maxWeight)>();

            foreach (var server in serverWeights.Keys)
            {
                newWeights[server] = (0, serverWeights[server].maxWeight);
            }

            serverWeights = newWeights;
        }
    }
        private List<IPEndPoint> servers;   // connected sync server
        private LoadBalancer loadBalancer;

        private const int group1Weight = 1;


        private List<int> originalServerWeights;    // weight of sync server -> immutable
        private int currentIndex;


            servers = new List<IPEndPoint>
            {
                new IPEndPoint(ip, 6061),
                new IPEndPoint(ip, 6062),
                new IPEndPoint(IPAddress.Parse("112.153.144.131"), 6063),
                //new IPEndPoint(ip, 6064),
                //new IPEndPoint(ip, 6065),

            };

            serverWeights = new Dictionary<IPEndPoint, int>
            {
                [servers[0]] = 2,
                [servers[1]] = 4,
                [servers[2]] = 5,
                //[servers[3]] = 8,
                //[servers[4]] = 5,
            };
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
            {
                udp.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref clientEP, new AsyncCallback(OnReceive), buffer);
            }catch(Exception e) {
                Console.WriteLine("Winsock error: " + e.ToString());
            };
                //HandlePacket(ref packet, (IPEndPoint)clientEP); //Meta-seum
                //HandlePacket_RR(ref packet, (IPEndPoint)clientEP);  //RoundRobin
                HandlePacket_WRR(ref packet, (IPEndPoint)clientEP); //Weighted RoundRobin

                //IPEndPoint selectedServer = loadBalancer.SelectServer();
                //if (selectedServer == null)
                //{
                //    //Console.WriteLine("clientEP");
                //    HandlePacket_SRR(ref packet, (IPEndPoint)clientEP);
                //}
                //else
                //{
                //    //Console.WriteLine("selectedServer");
                //    HandlePacket_SRR(ref packet, selectedServer);
                //}

            }


        // Meta-seum
        private void HandlePacket(ref PacketDatagram pd, IPEndPoint clientEP)
        {
            pd.portNum = clientEP.Port;
            pd.source = clientEP.Address.ToString();

            Console.WriteLine("Room-pd.source: {0}", pd.source);
            if (!clients.ContainsKey(clientEP))
            {
                clients.Add(clientEP, new Client(pd.playerInfoPacket.id, pd));
            }
            if (pd.status.Equals("connected"))
            {
                IPEndPoint minEndPoint = ReturnEndPoint();
                CalcWeight(minEndPoint, ref pd);
                Console.WriteLine("SendTo {0}", minEndPoint);   
                SendPacket(ref pd, minEndPoint);
            }
        }


        // Smoothed Round-Robin
        private void HandlePacket_SRR(ref PacketDatagram pd, IPEndPoint clientEP)
                if (serverWeights.Count == 0)

                        // 처리 가능 가중치 -1
                        serverWeights[currentServer] -= 1;
                            // 처리 후 서버 가중치 초기화
                            InitializeServerWeights();
                        }

                    currentIndex = (currentIndex + 1) % servers.Count;


        // Round-Robin
        private void HandlePacket_RR(ref PacketDatagram pd, IPEndPoint clientEP)
                if (serverWeights.Count == 0)

                if (currentIndex >= servers.Count)
                {
                    currentIndex %= servers.Count;
                }
                IPEndPoint minEndPoint = servers[currentIndex];

                Console.WriteLine("SendTo {0}", minEndPoint);
                SendPacket(ref pd, minEndPoint);

                currentIndex++;
            }


        // Weighted Round-Robin
        private void HandlePacket_WRR(ref PacketDatagram pd, IPEndPoint clientEP)
        {
            pd.portNum = clientEP.Port;
            pd.source = clientEP.Address.ToString();

            Console.WriteLine("Room-pd.source: {0}", pd.source);
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
                        Console.WriteLine("SendTo {0}", currentServer);
                        SendPacket(ref pd, currentServer);

                        // 처리 가능 가중치 - group weight
                        if (serverWeights[currentServer] - GetGroupWeight(ref pd) > 0)
                        {
                            serverWeights[currentServer] -= GetGroupWeight(ref pd);
                        }


                        if (serverWeights.Values.All(w => w == 0))
                        {
                            // 처리 후 서버 가중치 originalServerWeights값들로 초기화
                            Console.WriteLine("Reset!");
                            ResetWeights();
                        }
                        currentIndex = (currentIndex + 1) % servers.Count;
                        break;
                    }

                    currentIndex = (currentIndex + 1) % servers.Count;
                }
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
                Console.WriteLine(serverWeights[server]); 
            }


        void CalcWeight(IPEndPoint SyncEp, ref PacketDatagram pd)
        {
            int groupId = pd.playerInfoPacket.group;
            int weight = groupId == 1 ? group1Weight : groupId == 2 ? group2Weight : group3Weight;

            List<IPEndPoint> keysToRemove = new List<IPEndPoint>();

            foreach (KeyValuePair<IPEndPoint, int> w in serverWeights)
            {
                if (w.Value > int.MaxValue - 10000)
                    keysToRemove.Add(w.Key);
            }
            if (keysToRemove.Count == 3)
            {
                foreach (var key in keysToRemove)
                {
                    serverWeights[key] = 0;
                }
            }
            serverWeights[SyncEp] += weight;
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
    }