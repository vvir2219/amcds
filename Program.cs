using System;

namespace Project
{
    class Program
    {
        static void PrintUsageAndExit()
        {
            Console.WriteLine("Usage: dotnet run hub-host hub-port owner index");
            System.Environment.Exit(1);
        }
        static void Main(string[] args)
        {
            Console.WriteLine(args.Length);
        }
    }
}
