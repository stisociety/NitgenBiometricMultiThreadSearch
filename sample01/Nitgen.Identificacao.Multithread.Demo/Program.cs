using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nitgen.Identificacao.Multithread.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var repositorio = new DigitaisRepositorio();
            var handler = new IdentificarBiometriaHandler();

            var numeroTotalBiometrias = repositorio.RecuperarNumeroTotalBiometrias();
            var biometriasPorPagina = (numeroTotalBiometrias / 3) + 10;
            for (int pagina = 1; pagina <= 3; pagina++)
            {
                var biometriasRecuperadas = repositorio.RecuperarPagina(pagina, biometriasPorPagina);
                handler.AdicionarMecanismoBuscaPara(biometriasRecuperadas);
            }
        }
    }
}