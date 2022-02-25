using System;

namespace NewTCPServer
{
    class NewServerWorker
    {
        public void Start()
        {
            String path = Environment.GetEnvironmentVariable("AbstractServerConf");
            if (path == null)
            {
                path = @"C:\logfiler\";
            }

            MyNewServer server = new MyNewServer(path);
            server.Start();
        }
    }
}