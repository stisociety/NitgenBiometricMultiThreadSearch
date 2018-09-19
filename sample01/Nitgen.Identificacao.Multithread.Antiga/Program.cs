using NBioBSPCOMLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nitgen.Identificacao.Multithread.Antiga
{
    class Program
    {
        static void Main(string[] args)
        {
            var corConsoleDefault = Console.ForegroundColor;
            var repositorio = new DigitaisRepositorio();
            var handler = new IdentificarBiometriaHandler();
            
            var numeroTotalBiometrias = repositorio.RecuperarNumeroTotalBiometrias();
            Console.WriteLine($"Trabalhando com {Environment.ProcessorCount} threads...");
            var biometriasPorPagina = (numeroTotalBiometrias / Environment.ProcessorCount) + 10;
            for (int pagina = 1; pagina <= Environment.ProcessorCount; pagina++)
            {
                var biometriasRecuperadas = repositorio.RecuperarPagina(pagina, biometriasPorPagina);
                handler.AdicionarMecanismoBuscaPara(biometriasRecuperadas);
            }

            var possoSair = false;
            while (!possoSair)
            {
                Console.WriteLine("Informe a digital");

                var nitgen = new NBioBSP();
                IFPData data = nitgen.FPData;
                IDevice device = nitgen.Device;
                IExtraction extraction = nitgen.Extraction;

                device.Open(255);
                extraction.Capture();
                var template = extraction.FIR;
                device.Close(255);

                var relogio = new Stopwatch();
                relogio.Start();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Identificando.....");
                Console.ForegroundColor = corConsoleDefault;
                var resultado = handler.IdentificarBiometriaComCarga(template);
                relogio.Stop();
                Console.ForegroundColor = ConsoleColor.Red;
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