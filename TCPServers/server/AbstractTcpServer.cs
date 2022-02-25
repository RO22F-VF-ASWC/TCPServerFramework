using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TCPServers.server
{
    public abstract class AbstractTcpServer
    {
        /*
         * Constants - here name of configfile
         */
        /// <summary>
        /// The name og the config file - 'TcpServerConfig.xml'
        /// </summary>
        public const string CONFIG_FILE = "TcpServerConfig.xml";

        /*
         * Fields to operate the server
         */
        private bool running = true;
        private readonly List<Task> startingTasks;

        protected readonly TraceSource trace;
        protected readonly Configuration conf;

        /// <summary>
        /// Initilalize the server with port numbers, name etc. 
        /// </summary>
        /// <param name="port">The server port</param>
        /// <param name="name">The name of the server</param>
        protected AbstractTcpServer(int port, string name)
        {
            conf = new Configuration();
            conf.ServerPort = port;
            conf.ShutdownPort = port + 1;
            conf.ServerName = name;
            conf.DebugLevel = SourceLevels.All;

            trace = new TraceSource(conf.ServerName);
            trace.Switch = new SourceSwitch(conf.ServerName, conf.DebugLevel.ToString());
            SetupTracing(conf);

            startingTasks = new List<Task>();
        }

        /// <summary>
        /// Initilalize the server with port numbers, name etc. 
        /// </summary>
        /// <param name="configFilePath">The path to find the configuration file</param>
        protected AbstractTcpServer(string configFilePath)
        {
            conf = ReadConfiguration(configFilePath);

            trace = new TraceSource(conf.ServerName);
            trace.Switch = new SourceSwitch(conf.ServerName, conf.DebugLevel.ToString());
            SetupTracing(conf);

            startingTasks = new List<Task>();
            trace.TraceEvent(TraceEventType.Information, 456, $"Configuration of Server fulfilled");
        }



        /// <summary>
        /// Starts the server at server port, can be stopped at stop-port
        /// </summary>
        public void Start()
        {
            // start stop server separately
            Task.Run(() => StopServer(conf.ShutdownPort) );
            trace.TraceEvent(TraceEventType.Information, conf.ServerPort, $"The stop server started at {conf.ShutdownPort}");

            // start Main server
            TcpListener server = new TcpListener(IPAddress.Any, conf.ServerPort);
            server.Start();
            trace.TraceEvent(TraceEventType.Information, conf.ServerPort, $"The Server '{conf.ServerName}' is started at {conf.ServerPort}");

            while (running)
            {
                if (server.Pending())
                {
                    // A client is connected
                    TcpClient socket = server.AcceptTcpClient();
                    trace.TraceEvent(TraceEventType.Information, conf.ServerPort, $"New client connected");

                    startingTasks.Add(Task.Run(
                        () =>
                        {
                            TcpClient tmpsocket = socket;
                            DoClient(tmpsocket);
                        }
                    ));
                }
                else
                {
                    // no client
                    Thread.Sleep(2000); // wait 2 sec
                }
            }

            trace.TraceEvent(TraceEventType.Information, conf.ServerPort, $"The server is stopping");
            // wait for all task to finished
            foreach (Task task in startingTasks)
            {
                task.Wait();
            }

            trace.Close();

        }

        /// <summary>
        /// Maintain one client i.e. do all communication with the specific client
        /// </summary>
        /// <param name="socket">The socket to the client</param>
        private void DoClient(TcpClient socket)
        {
            using (StreamReader sr = new StreamReader(socket.GetStream()))
            using (StreamWriter sw = new StreamWriter(socket.GetStream()))
            {
                sw.AutoFlush = true;

                TcpWorkerTemplate(sr, sw);
            }

            socket?.Close();
        }

        /// <summary>
        /// The template method i.e. insert code here that handle one client 
        /// </summary>
        /// <param name="sr">The network input stream to read from</param>
        /// <param name="sw">The network output stream to write to</param>
        protected abstract void TcpWorkerTemplate(StreamReader sr, StreamWriter sw);

        /*
         * For stooping the server softly
         */

        private void StopTheServer()
        {
            running = false;
        }

        private void StopServer(int stopPort)
        {
            TcpListener stopServer = new TcpListener(IPAddress.Loopback, stopPort);
            stopServer.Start();
            bool stop = false;

            while (!stop)
            {
                using (TcpClient client = stopServer.AcceptTcpClient())
                using (StreamReader sr = new StreamReader(client.GetStream()))
                {
                    String str = sr.ReadLine();
                    if (str == "KillMe")
                    {
                        StopTheServer();
                        stop = true;
                    }
                    else
                    {
                        trace.TraceEvent(TraceEventType.Warning, conf.ServerPort, $"Someone try illegal to stop the Server - use {str}");
                    }
                }
            }

            stopServer.Stop();
        }

        /*
         * Configuration
         */
        private Configuration ReadConfiguration(string configFilePath)
        {
            Configuration conf = new Configuration();

            XmlDocument configDoc = new XmlDocument(); 
            configDoc.Load(configFilePath + @"\" + CONFIG_FILE);

            /*
             * Read Serverport
             */
            XmlNode portNode = configDoc.DocumentElement.SelectSingleNode("ServerPort");
            if (portNode != null)
            {
                String str = portNode.InnerText.Trim(); 
                conf.ServerPort = Convert.ToInt32(str);
            }

            /*
             * Read Shutdown port
             */
            XmlNode sdportNode = configDoc.DocumentElement.SelectSingleNode("ShutdownPort");
            if (sdportNode != null)
            {
                String str = sdportNode.InnerText.Trim();
                conf.ShutdownPort = Convert.ToInt32(str);
            }

            /*
             * Read server name
             */
            XmlNode nameNode = configDoc.DocumentElement.SelectSingleNode("ServerName");
            if (nameNode != null)
            {
                conf.ServerName = nameNode.InnerText.Trim();
            }

            /*
             * Read Debug Level
             */
            XmlNode debugNode = configDoc.DocumentElement.SelectSingleNode("DebugLevel");
            if (debugNode != null)
            {
                string str  = debugNode.InnerText.Trim();
                SourceLevels level = SourceLevels.All;
                SourceLevels.TryParse(str, true, out level);
                conf.DebugLevel = level;
            }

            /*
             * Read Log Files location
             */
            XmlNode logFilesNode = configDoc.DocumentElement.SelectSingleNode("LogFilesPath");
            if (debugNode != null)
            {
                conf.LogFilePath = logFilesNode.InnerText.Trim();
            }
            

            return conf;
        }

        /*
         * Setup Tracing
         */
        private void SetupTracing(Configuration configuration)
        {
            // Consol
            TraceListener tl1 = new TextWriterTraceListener(Console.Out)
                {Filter = new EventTypeFilter(configuration.DebugLevel) };
            // File
            TraceListener tl2 = new TextWriterTraceListener(new StreamWriter(configuration.LogFilePath + @"\TcpServer.txt"))
                {Filter = new EventTypeFilter(configuration.DebugLevel) };
            // XML file
            TraceListener tl3 = new XmlWriterTraceListener(new StreamWriter(configuration.LogFilePath + @"\TcpServer.xml"))
                { Filter = new EventTypeFilter(SourceLevels.Warning) };

            trace.Listeners.Add(tl1);
            trace.Listeners.Add(tl2);
            trace.Listeners.Add(tl3);
            
        }

    }
}
