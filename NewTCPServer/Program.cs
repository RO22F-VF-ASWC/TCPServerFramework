using System;
using System.Diagnostics;
using System.IO;

namespace NewTCPServer
{
    class Program
    {
        static void Main(string[] args)
        {


            NewServerWorker worker = new NewServerWorker();
            worker.Start();


            Console.WriteLine("Push any key to stop .....");
            Console.ReadKey();
        }
    }
}
