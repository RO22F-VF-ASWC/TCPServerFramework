using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TCPServers.server;

namespace NewTCPServer
{
    class MyNewServer : AbstractTcpServer
    {
        public MyNewServer(int port, string name) : base(port, name)
        {
        }

        public MyNewServer(string pathname) : base(pathname)
        {
        }

        protected override void TcpWorkerTemplate(StreamReader sr, StreamWriter sw)
        {
            // echo eg. read then write
            String str = sr.ReadLine();
            sw.WriteLine(str);
        }
    }
}
