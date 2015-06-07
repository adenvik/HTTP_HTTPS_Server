using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ConsoleApplication1
{
    class HTTPSServer
    {
        TcpListener _tcpListener;
        public HTTPSServer(string _pathToSertificate)
        {
            _tcpListener = new TcpListener(443);
            _tcpListener.Start();

            while (true)
            {
                new HTTPSClient(_tcpListener.AcceptTcpClient(), _pathToSertificate);
            }
        }
    }
}
