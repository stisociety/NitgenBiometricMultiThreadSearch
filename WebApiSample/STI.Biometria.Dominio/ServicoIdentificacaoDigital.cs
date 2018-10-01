using STI.Compartilhado.Core;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace STI.Biometria.Dominio
{
    public sealed class ServicoIdentificacaoDigital
    {
        private ConcurrentBag<Digital> _bancoDigitais;
        private CancellationTokenSource _cancelamentoTask;
        private IMotorBuscaBiometrica _motorBusca;

        public ServicoIdentificacaoDigital(IMotorBuscaBiometrica motorBusca, IEnumerable<Digital> bancoDigitais)
        {
            _motorBusca = motorBusca;
            _bancoDigitais = new ConcurrentBag<Digital>(bancoDigitais);
        }

        public async Task<Resultado<Digital,Falha>> IdentificarAsync(byte[] digitalNaoIdentificacada)
        {
            var contextoParaTask = new ContextoTask(_bancoDigitais.ToList(), digitalNaoIdentificacada, _motorBusca);
            _cancelamentoTask = new CancellationTokenSource();
            var token = _cancelamentoTask.Token;
            var task = await new TaskFactory<Resultado<Digital, Falha>>().StartNew((contextoRecebido) => {
                var tempoLocalizacao = new Stopwatch();
                tempoLocalizacao.Start();
                var contexto = contextoRecebido as ContextoTask;
                var resultado = Resultado<Digital, Falha>.NovaFalha(Falha.Nova(404, "Digital não encontrada"));
                foreach (var digital in contexto.BancoDigitais)
                {
                    if (token.IsCancellationRequested)
                        break;
                    if(contexto.MotorBusca.IdentificarDigital(contexto.DigitalNaoIdentificacada, digital) is var resultadoIdentificacao && resultadoIdentificacao.EhSucesso)
                    {
                        resultado = resultadoIdentificacao.Sucesso;
                        break;
                    }
                }
                tempoLocalizacao.Stop();
                return resultado;
            }, contextoParaTask, token);
            return task;
        }
        
        private sealed class ContextoTask
        {
            public ContextoTask(List<Digital> bancoDigitais, byte[] digitalNaoIdentificacada, IMotorBuscaBiometrica motorBusca)
            {
                BancoDigitais = bancoDigitais;
                DigitalNaoIdentificacada = digitalNaoIdentificacada;
                MotorBusca = motorBusca;
            }

            public List<Digital> BancoDigitais { get; }
            public byte[] DigitalNaoIdentificacada { get;  }
            public IMotorBuscaBiometrica MotorBusca { get; }
        }

    }
}
