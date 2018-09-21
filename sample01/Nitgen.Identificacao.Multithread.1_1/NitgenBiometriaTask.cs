using NITGEN.SDK.NBioBSP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nitgen.Identificacao.Multithread._1_1
{
    public sealed class NitgenBiometriaTask
    {
        internal NitgenBiometriaTask(Guid id, NBioAPI mecanismoBusca, IEnumerable<Biometria> biometrias)
        {
            Id = id;
            MecanismoBusca = mecanismoBusca;
            Biometrias = biometrias;
        }

        public Guid Id { get; }
        public NBioAPI MecanismoBusca { get; }
        public IEnumerable<Biometria> Biometrias { get; private set; }
        public CancellationTokenSource CancellationSource { get; private set; }

        public static NitgenBiometriaTask Novo(IEnumerable<Biometria> biometrias)
        {
            var nitgenMainApi = new NBioAPI();
            return new NitgenBiometriaTask(Guid.NewGuid(), nitgenMainApi, biometrias);
        }

        public Task<int> CriarTaskParaIdentificacaoBiometrica(NBioAPI.Type.HFIR template, NBioAPI.IndexSearch mecanismoConfirmacao)
        {
            var contextoIdentificacao = new ContextoParaIndentificacaoBiometrica(Id, MecanismoBusca, mecanismoConfirmacao, template, Biometrias);
            CancellationSource = new CancellationTokenSource();
            var token = CancellationSource.Token;
            return new Task<int>((parametroState) =>
            {
                var contexto = parametroState as ContextoParaIndentificacaoBiometrica;

                if (token.IsCancellationRequested)
                    return 0;

                var encontrou = false;
                var relogio = new Stopwatch();
                relogio.Start();
                foreach (var biometria in contexto.Biometrias)
                {
                    if (token.IsCancellationRequested)
                        return 0;

                    var payload = new NBioAPI.Type.FIR_PAYLOAD();
                    var match = new NBioAPI.Type.MATCH_OPTION_0();
                    var templateTexto = new NBioAPI.Type.FIR_TEXTENCODE { TextFIR = biometria.TemplateISOText };
                    var retorno = contexto.MecanismoBusca.VerifyMatch(contexto.TemplateLido, templateTexto, out encontrou, payload);
                    if (encontrou)
                    {
                        contexto.MecanismoConfirmacao.InitEngine();
                        NBioAPI.IndexSearch.FP_INFO[] informacaoBiometria;
                        var ret = contexto.MecanismoConfirmacao.AddFIR(templateTexto, (uint)biometria.Id, out informacaoBiometria);
                        
                        var cbInfo = new NBioAPI.IndexSearch.CALLBACK_INFO_0();
                        NBioAPI.IndexSearch.FP_INFO nitgenBiometria;
                        retorno = contexto.MecanismoConfirmacao.IdentifyData(contexto.TemplateLido, NBioAPI.Type.FIR_SECURITY_LEVEL.HIGH,
                            out nitgenBiometria, cbInfo);

                        var idEncontrado = nitgenBiometria.ID;
                        contexto.MecanismoConfirmacao.RemoveUser((uint)biometria.Id);
                        if (idEncontrado > 0)
                        {
                            relogio.Stop();
                            Console.WriteLine($"{contexto.Id} - Localizado {biometria.Id} em {relogio.Elapsed.TotalSeconds}");                            
                            return (int)idEncontrado;
                        }                        
                    }
                }

                relogio.Stop();
                Console.WriteLine($"{contexto.Id} - Nenhuma biometria encontrada em {relogio.Elapsed.TotalSeconds}");

                if (token.IsCancellationRequested)
                    return 0;

                return 0;
            }, contextoIdentificacao, token);
        }

        private sealed class ContextoParaIndentificacaoBiometrica
        {
            public ContextoParaIndentificacaoBiometrica(Guid id, NBioAPI mecanismoBusca, NBioAPI.IndexSearch mecanismoConfirmacao, NBioAPI.Type.HFIR templateLido, IEnumerable<Biometria> biometrias)
            {
                Id = id;
                MecanismoBusca = mecanismoBusca;
                TemplateLido = templateLido;
                Biometrias = biometrias;
                MecanismoConfirmacao = mecanismoConfirmacao;
            }

            public Guid Id { get; }
            public NBioAPI MecanismoBusca { get; }
            public NBioAPI.Type.HFIR TemplateLido { get; }
            public IEnumerable<Biometria> Biometrias { get; }
            public NBioAPI.IndexSearch MecanismoConfirmacao { get; set; }
        }
    }
}