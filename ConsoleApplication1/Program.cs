using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            // Создадим новый сервер на порту 8280
            new HTTPSServer("SslServer.cer");
            //new HTTPServer(8280);
        }
    }
}
