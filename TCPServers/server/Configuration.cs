using System;
using System.Diagnostics;

namespace TCPServers.server
{
    public class Configuration
    {
        public int ServerPort { get; set; }
        public int ShutdownPort { get; set; }
        public String ServerName { get; set; }
        public SourceLevels DebugLevel { get; set; }
        public String LogFilePath { get; set; }

        public Configuration() // default settings
        {
            ServerPort = 65000;
            ShutdownPort = ServerPort + 1;
            ServerName = "";
            DebugLevel = SourceLevels.Error;
            LogFilePath = ".";

        }
    }
}