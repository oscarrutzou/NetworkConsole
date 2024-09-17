using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCP
{
    internal class ClientData
    {
        public Guid id;
        public string name;
        public TcpClient tcpClient;

        public ClientData(Guid id, string name, TcpClient tcpClient)
        {
            this.id = id;
            this.name = name;
            this.tcpClient = tcpClient;
        }
    }
}
