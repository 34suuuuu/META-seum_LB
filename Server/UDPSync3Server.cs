using System;
using System.Net;
using System.Net.Sockets;
using StarterAssets.Packet;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Server
{

    public class UDPSync3Server : UDPSyncServer
    {
        public UDPSync3Server() : base(6063) { }
    }
}
