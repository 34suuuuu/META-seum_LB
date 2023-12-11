using System;
using System.Net;
using System.Net.Sockets;
using StarterAssets.Packet;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Server
{

    public class UDPSync2Server : UDPSyncServer
    {
        public UDPSync2Server() : base(6062) { }
    }
}
