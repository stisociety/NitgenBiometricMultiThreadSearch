using NITGEN.SDK.NBioBSP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nitgen.Identificacao.Multithread.Demo
{
    public sealed class NitgenBiometriaTask
    {
        internal NitgenBiometriaTask(Guid id, NBioAPI.IndexSearch mecanismoBusca)
        {
            Id = id;
            MecanismoBusca = mecanismoBusca;
        }

        public Guid Id { get; }
        public NBioAPI.IndexSearch MecanismoBusca { get; }
        public CancellationTokenSource CancellationSource { get; private set; }

        public static NitgenBiometriaTask Novo(IEnumerable<Biometria> biometrias)
        {
            var nitgenMainApi = new NBioAPI();
            var nitgenSearchApi = new NBioAPI.IndexSearch(nitgenMainApi);
            nitgenSearchApi.InitEngine();
            CarregarBiometriasParaNitgen(nitgenMainApi, nitgenSearchApi, biometrias);
            return new NitgenBiometriaTask(Guid.NewGuid(), nitgenSearchApi);
        }
        
        public Task<int> CriarTaskParaIdentificacaoBiometrica(NBioAPI.Type.HFIR template)
        {
            var contextoIdentificacao = new ContextoParaIndentificacaoBiometrica(Id, MecanismoBusca, template);
            CancellationSource = new CancellationTokenSource();
            var token = CancellationSource.Token;
            return new Task<int>((parametroState) =>
            {
                var contexto = parametroState as ContextoParaIndentificacaoBiometrica;
                if (token.IsCancellationRequested)
                    return 0;

                var cbInfo = new NBioAPI.IndexSearch.CALLBACK_INFO_0();
                NBioAPI.IndexSearch.FP_INFO nitgenBiometria;
                var relogio = new Stopwatch();
                relogio.Start();
                var retorno = contexto.MecanismoBusca.IdentifyData(contexto.TemplateLido, NBioAPI.Type.FIR_SECURITY_LEVEL.HIGH,
                    out nitgenBiometria, cbInfo);
                relogio.Stop();
                Console.WriteLine($"{contexto.Id} - Localizado {nitgenBiometria.ID} em {relogio.Elapsed.TotalSeconds}");

                if (token.IsCancellationRequested)
                    return 0;
                
                return (int)nitgenBiometria.ID;
            }, contextoIdentificacao, token);
        }

        private static void CarregarBiometriasParaNitgen(NBioAPI nitgenMainApi, NBioAPI.IndexSearch nitgenSearchApi, IEnumerable<Biometria> biometrias)
        {
            foreach (var biometria in biometrias)
            {
                var biometriaNitgen = new NBioAPI.Type.FIR_TEXTENCODE
                {
                    TextFIR = biometria.TemplateISOText
                };

                NBioAPI.IndexSearch.FP_INFO[] informacaoBiometria;             
                nitgenSearchApi.AddFIR(biometriaNitgen, (uint)biometria.Id, out informacaoBiometria);
            }
        }

        private sealed class ContextoParaIndentificacaoBiometrica
        {
            public ContextoParaIndentificacaoBiometrica(Guid id, NBioAPI.IndexSearch mecanismoBusca, NBioAPI.Type.HFIR templateLido)
            {
                Id = id;
                MecanismoBusca = mecanismoBusca;
                TemplateLido = templateLido;
            }

            public Guid Id { get; }
            public NBioAPI.IndexSearch MecanismoBusca { get; }
            public NBioAPI.Type.HFIR TemplateLido { get; }
        }
    }
}
