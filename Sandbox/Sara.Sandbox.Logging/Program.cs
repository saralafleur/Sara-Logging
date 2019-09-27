using System;
using System.Reflection;
using Sara.NETStandard.Logging;

namespace Sara.Sandbox.Logging
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");

            Log.Write("Test Message");
            Log.WriteSystem("Sandbox.Logging System Test", typeof(Program).FullName, MethodBase.GetCurrentMethod().Name);
            TestLogSize();
            Log.Archive(new ArchiveArgs());
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            Console.WriteLine("Exiting...");
            Log.Exit();
            Console.WriteLine("Read the new Entries, then press any key to exit");
            Console.ReadKey();

        }

        private static void TestLogSize()
        {
            Console.WriteLine("Starting TestLogSize");

            for (int i = 0; i < 100; i++)
            {
                Log.Write($"TestLogSize {i}");
                if ((i % 1000) == 0)
                    Console.WriteLine($"TestLogSize {i}");
            }

            Console.WriteLine("Complete TestLogSize");
        }
    }
}
