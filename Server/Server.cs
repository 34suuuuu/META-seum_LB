using System;
//LB
namespace Server
{
    class Server
    {
        public static void Main(string[] args)
        {
            UDPRoomServer roomServer = new UDPRoomServer();
            UDPSync1Server sync1Server = new UDPSync1Server();
            UDPSync2Server sync2Server = new UDPSync2Server();
            UDPSync3Server sync3Server = new UDPSync3Server();

            roomServer.Start();
            sync1Server.Start();
            sync2Server.Start();
            sync3Server.Start();

            Console.ReadLine();

        }
    }
}
