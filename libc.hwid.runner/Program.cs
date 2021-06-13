using System;
using System.IO;

namespace libc.hwid.runner
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var hwid = HwId.Generate();
            Console.WriteLine(hwid);
            File.WriteAllText("./key.txt", hwid);
        }
    }
}