using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ConsoleApplication1
{
    class HTTPServer
    {
        //Создаем слушателя
        TcpListener _tcpListener;

        public HTTPServer(int port)
        {
            //Создаем подключение
            _tcpListener = new TcpListener(port);
            _tcpListener.Start();

            //Начинаем принимать клиентов
            while(true)
            {
                Client _cl = new Client(_tcpListener.AcceptTcpClient());
            }
        }
    }
}
