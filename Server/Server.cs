using System;

namespace Server
{
    class Server
    {
        public static void Main(string[] args)
        {
            //UDPServer server = new UDPServer();
            UDPRoom1Server room1Server = new UDPRoom1Server();
            UDPSync1Server sync1Server = new UDPSync1Server();
            UDPSync2Server sync2Server = new UDPSync2Server();
            //UDPSync3Server sync3Server = new UDPSync3Server();

            //server.Start();
            room1Server.Start();
            sync1Server.Start();
            sync2Server.Start();
            //sync3Server.Start();


            Console.ReadLine();
        }
    }
}
