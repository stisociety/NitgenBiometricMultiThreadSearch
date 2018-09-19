using NITGEN.SDK.NBioBSP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nitgen.Identificacao.Simples.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var repositorio = new DigitaisRepositorio();
            var nitgenMainApi = new NBioAPI();
            var handler = new IdentificarBiometriaHandler();
            var corConsoleDefault = Console.ForegroundColor;

            var biometrias = repositorio.RecuperarPagina(1, 8000);
            Console.WriteLine($"{biometrias.Count()} biometrias recuperadas...");
            handler.CarregarBiometrias(biometrias);

            var possoSair = false;
            while (!possoSair)
            {
                Console.WriteLine("Informe a digital");

                NBioAPI.Type.HFIR template;
                var window = new NBioAPI.Type.WINDOW_OPTION();
                nitgenMainApi.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
                nitgenMainApi.Capture(out template, 0, window);
                nitgenMainApi.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);

                var relogio = new Stopwatch();
                relogio.Start();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Identificando.....");
                Console.ForegroundColor = corConsoleDefault;
                var resultado = handler.IdentificarBiometria(template);
                relogio.Stop();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Id digital encontrada {resultado} em {relogio.Elapsed.TotalSeconds} segundos");
                Console.ForegroundColor = corConsoleDefault;

                Console.WriteLine();

                Console.WriteLine("\nCapturar nova digital?");
                var tecla = Console.ReadKey();
                if (tecla.Key == ConsoleKey.N)
                    possoSair = true;
            }
        }
    }
}
