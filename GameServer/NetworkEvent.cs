using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace GameServer
{
    abstract class NetworkEvent
    {
        
    }

    class PlayerJoinEvent : NetworkEvent
    {
        public TcpClient tcpClient;

        public PlayerJoinEvent(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
        }
    }
}
