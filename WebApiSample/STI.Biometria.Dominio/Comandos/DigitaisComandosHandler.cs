using STI.Compartilhado.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STI.Biometria.Dominio.Comandos
{
    public interface IIdentificarSocioPorDigitalComandoHandler
    {
        Resultado<Digital, Falha> Executar(IdentificarSocioPorDigitalComando comando);
    }

    public sealed class DigitaisComandosHandler : IIdentificarSocioPorDigitalComandoHandler
    {
        private static ConcurrentBag<ServicoIdentificacaoDigital> _identificadoresDeDigital;
        private readonly IDigitaisRepositorio _digitaisRepositorio;
        private readonly IConfiguracoesBiometriaRepositorio _configuracoesBiometriaRepositorio;
        private readonly IFabricaMotorBuscaPorParceiro _fabricaMotorBuscaBiometrica;

        public DigitaisComandosHandler(
            IDigitaisRepositorio digitaisRepositorio,
            IConfiguracoesBiometriaRepositorio configuracoesBiometriaRepositorio,
            IFabricaMotorBuscaPorParceiro fabricaMotorBuscaBiometrica)
        {
            _digitaisRepositorio = digitaisRepositorio;
            _configuracoesBiometriaRepositorio = configuracoesBiometriaRepositorio;
            _fabricaMotorBuscaBiometrica = fabricaMotorBuscaBiometrica;
        }

        public Resultado<Digital, Falha> Executar(IdentificarSocioPorDigitalComando comando)
        {
            var tempoIdentificacao = new Stopwatch();
            tempoIdentificacao.Start();

            if (_identificadoresDeDigital == null)
                _identificadoresDeDigital = new ConcurrentBag<ServicoIdentificacaoDigital>();
            if (_identificadoresDeDigital.Count == 0)
            {
                if (_configuracoesBiometriaRepositorio.Recuperar(comando.Estacao) is var configuracaoBiometria && configuracaoBiometria.EhFalha)
                    return configuracaoBiometria.Falha;

                if (_fabricaMotorBuscaBiometrica.CriarMotorDeBuscaBiometrica(configuracaoBiometria.Sucesso.ParceiroSdkDigital) is var motorBusca && motorBusca.EhFalha)
                    return motorBusca.Falha;

                if (_digitaisRepositorio.RecuperarNumeroTotalDigitais() is var quantidadeDigitais && quantidadeDigitais.EhFalha)
                    return quantidadeDigitais.Falha;

                var digitaisPorPagina = (quantidadeDigitais.Sucesso / configuracaoBiometria.Sucesso.QuantidadeThreadsIdentificacaoDigital) + configuracaoBiometria.Sucesso.QuantidadeThreadsIdentificacaoDigital;
                for (int pagina = 1; pagina <= configuracaoBiometria.Sucesso.QuantidadeThreadsIdentificacaoDigital; pagina++)
                {
                    if (_digitaisRepositorio.RecuperarPagina(pagina, digitaisPorPagina) is var digitais && digitais.EhFalha)
                        return digitais.Falha;
                    if (digitais.Sucesso.Count() > 0)
                        _identificadoresDeDigital.Add(new ServicoIdentificacaoDigital(motorBusca.Sucesso, digitais.Sucesso));
                }
            }

            var digital = Digital.NovaNaoEncontrada();
            Parallel.ForEach(_identificadoresDeDigital, async (identificadorDigital, estadoLoop) =>
            {
                var resultado = await identificadorDigital.IdentificarAsync(comando.Template);
                if(resultado.EhSucesso)
                {
                    digital = resultado.Sucesso;
                    estadoLoop.Break();
                }
            });
            tempoIdentificacao.Stop();
            return digital.Id == 0 
                    ? Resultado<Digital, Falha>.NovaFalha(Falha.Nova(404, "Digitão não encontrada")) 
                    : Resultado<Digital, Falha>.NovoSucesso(digital);
        }
    }
}
