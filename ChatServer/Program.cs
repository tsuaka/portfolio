﻿using System;

namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverOption = ParseCommandLine(args);
            if( serverOption == null )
            {
                return;
            }

            var serverApp = new MainServer();
            serverApp.InitConfig(serverOption);

            serverApp.CreateStartServer();

            MainServer.MainLogger.Info("Press q to shut down the server");

            while( true )
            {
                System.Threading.Thread.Sleep(50);

                if( Console.KeyAvailable )
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if( key.KeyChar == 'q' )
                    {
                        Console.WriteLine("Server Terminate ~~~");
                        serverApp.StopServer();
                        break;
                    }
                }

            }
        }

        static ChatServerOption? ParseCommandLine(string[] args)
        {
            var result = CommandLine.Parser.Default.ParseArguments<ChatServerOption>(args) as CommandLine.Parsed<ChatServerOption>;

            if( result == null )
            {
                System.Console.WriteLine("Failed Command Line");
                return null;
            }

            return result.Value;
        }

    }
}
