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
        internal NitgenBiometriaTask(Guid id, NBioAPI.IndexSearch mecanismoBusca, CancellationTokenSource cancellationSource)
        {
            Id = id;
            MecanismoBusca = mecanismoBusca;
            CancellationSource = cancellationSource;
        }

        public Guid Id { get; }
        public NBioAPI.IndexSearch MecanismoBusca { get; }
        public CancellationTokenSource CancellationSource { get; }

        public static NitgenBiometriaTask Novo(IEnumerable<Biometria> biometrias)
        {
            var nitgenMainApi = new NBioAPI();
            var nitgenSearchApi = new NBioAPI.IndexSearch(nitgenMainApi);
            nitgenSearchApi.InitEngine();
            CarregarBiometriasParaNitgen(nitgenMainApi, nitgenSearchApi, biometrias);
            return new NitgenBiometriaTask(Guid.NewGuid(), nitgenSearchApi, new CancellationTokenSource());
        }
        
        public Task<int> CriarTaskParaIdentificacaoBiometrica(NBioAPI.Type.HFIR template)
        {
            var contextoIdentificacao = new ContextoParaIndentificacaoBiometrica(Id, MecanismoBusca, template);
            var token = CancellationSource.Token;
            return new Task<int>((parametroState) =>
            {
                var contexto = parametroState as ContextoParaIndentificacaoBiometrica;
                Console.WriteLine($"{contexto.Id} - Iniciado ");

                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                var cbInfo = new NBioAPI.IndexSearch.CALLBACK_INFO_0();
                NBioAPI.IndexSearch.FP_INFO nitgenBiometria;
                var relogio = new Stopwatch();
                Console.WriteLine($"{contexto.Id} - Localizando biometria...");
                relogio.Start();
                var retorno = contexto.MecanismoBusca.IdentifyData(contexto.TemplateLido, NBioAPI.Type.FIR_SECURITY_LEVEL.HIGH,
                    out nitgenBiometria, cbInfo);
                relogio.Stop();
                Console.WriteLine($"{contexto.Id} - Localizado {nitgenBiometria.ID} em {relogio.Elapsed.TotalSeconds}");

                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                Console.WriteLine($"{contexto.Id} - Finalizado ");
                return (int)nitgenBiometria.ID;
            }, contextoIdentificacao, token);
        }

        private static void CarregarBiometriasParaNitgen(NBioAPI nitgenMainApi, NBioAPI.IndexSearch nitgenSearchApi, IEnumerable<Biometria> biometrias)
        {
            foreach (var biometria in biometrias)
            {
                var nitgenConvertApi = new NBioAPI.Export(nitgenMainApi);
                NBioAPI.Type.HFIR handle;
                NBioAPI.IndexSearch.FP_INFO[] nitgenBiometria;
                nitgenConvertApi.FDxToNBioBSPEx(biometria.TemplateISO, (uint)biometria.TemplateISO.Length,
                    NBioAPI.Type.MINCONV_DATA_TYPE.MINCONV_TYPE_ISO, NBioAPI.Type.FIR_PURPOSE.ENROLL_FOR_IDENTIFICATION_ONLY,
                    out handle);
                nitgenSearchApi.AddFIR(handle, (uint)biometria.Id, out nitgenBiometria);
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
