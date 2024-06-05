using System;
using HeyShelli;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Shelli shell = new Shelli();

            shell.OutputDataReceived = (s) =>
            {
                if (!String.IsNullOrEmpty(s)) Console.WriteLine(s);
            };

            shell.ErrorDataReceived = (s) =>
            {
                if (!String.IsNullOrEmpty(s)) Console.WriteLine("*** " + s);
            };

            while (true)
            {
                Console.Write("Command [q to quit]: ");
                string userInput = Console.ReadLine();
                if (String.IsNullOrEmpty(userInput)) continue;
                if (userInput.Equals("q")) break;
                int returnCode = shell.Go(userInput);
                Console.WriteLine("");
                Console.WriteLine("Return code: " + returnCode);
            }
        }
    }
}
