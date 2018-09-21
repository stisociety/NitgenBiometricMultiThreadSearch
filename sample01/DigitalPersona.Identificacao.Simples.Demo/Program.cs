using DPUruNet;
using System;
using System.Linq;

namespace DigitalPersona.Identificacao.Simples.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var repositorio = new DigitalRepositorio();
            var biometrias = repositorio.RecuperarPagina(1, 8000);
            Console.WriteLine($"{biometrias.Count()} biometrias recuperadas...");

            foreach (var biometria in biometrias)
            {
                var teste = FeatureExtraction.CreateFmdFromRaw(biometria.TemplateISO, 1, 1, 1, 1, 1, Constants.Formats.Fmd.ISO);
            }
        }
    }
}