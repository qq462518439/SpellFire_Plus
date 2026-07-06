using System;
using SpellFire.RuntimeHost.Services;

namespace SpellFire.RuntimeHost.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return RuntimeHostCommandLineRunner.Run(args ?? Array.Empty<string>(), Console.WriteLine);
        }
    }
}
