using System;
using System.IO;
using NET5CompileHelpers;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var compiler = new Compiler();
            var runner = new Runner();

            Runner.DLLinfo dllinfo = runner.LoadDLL(compiler.CompileDLLFromFile("CodeFile1.cs"), "B", "EntryPoint");

            string result = (string)dllinfo.EntryPoint.Invoke(dllinfo.instance, null);

            Console.WriteLine(result);
        }
    }
}
