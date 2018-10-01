using STI.Compartilhado.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace STI.Biometria.Dominio
{
    public interface IDigitaisRepositorio
    {
        Resultado<int, Falha> RecuperarNumeroTotalDigitais();
        Resultado<IEnumerable<Digital>, Falha> RecuperarPagina(int pagina, int quantidadePorPagina);
    }
}
