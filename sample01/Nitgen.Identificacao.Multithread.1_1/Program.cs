using NITGEN.SDK.NBioBSP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nitgen.Identificacao.Multithread._1_1
{
    public class Program
    {
        static void Main(string[] args)
        {
            var corConsoleDefault = Console.ForegroundColor;
            var repositorio = new DigitaisRepositorio();
            var handler = new IdentificarBiometriaHandler();

            var numeroTotalBiometrias = repositorio.RecuperarNumeroTotalBiometrias();
            var biometriasPorPagina = (numeroTotalBiometrias / 10) + 10;
            for (int pagina = 1; pagina <= 10; pagina++)
            {
                var biometriasRecuperadas = repositorio.RecuperarPagina(pagina, biometriasPorPagina);
                if (biometriasRecuperadas.Count() > 0)
                {
                    Console.WriteLine($"Thread {pagina} será aberta com {biometriasRecuperadas.Count()}");
                    handler.AdicionarMecanismoBuscaPara(biometriasRecuperadas);
                }
            }

            // Captura da digital (trocar para o que vem da catraca posteriormente)
            var nitgenMainApi = new NBioAPI();

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
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Identificando.....");
                Console.ForegroundColor = corConsoleDefault;
                var resultado = handler.IdentificarBiometria(template);
                relogio.Stop();
                Console.ForegroundColor = ConsoleColor.Yellow;
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