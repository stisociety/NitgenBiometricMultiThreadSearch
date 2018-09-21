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
        internal NitgenBiometriaTask(Guid id, NBioAPI mecanismoBusca, NBioAPI.Export conversor, IList<Biometria> biometrias)
        {
            Id = id;
            MecanismoBusca = mecanismoBusca;
            Conversor = conversor;
            Biometrias = biometrias;
        }

        public Guid Id { get; }
        public NBioAPI MecanismoBusca { get; }
        public NBioAPI.Export Conversor { get; }
        public IList<Biometria> Biometrias { get; }
        public CancellationTokenSource CancellationSource { get; private set; }

        public void AdicionarDiferencas(IList<Biometria> biometrias)
        {
            foreach (var biometria in biometrias)
                Biometrias.Add(biometria);
        }

        public static NitgenBiometriaTask Novo(IList<Biometria> biometrias)
        {
            var nitgenMainApi = new NBioAPI();
            var conversor = new NBioAPI.Export(nitgenMainApi);
            return new NitgenBiometriaTask(Guid.NewGuid(), nitgenMainApi, conversor, biometrias);
        }

        public Task<int> CriarTaskParaIdentificacaoBiometrica(NBioAPI.Type.HFIR template, NBioAPI.IndexSearch mecanismoConfirmacao)
        {
            var contextoIdentificacao = new ContextoParaIndentificacaoBiometrica(Id, MecanismoBusca, mecanismoConfirmacao, Conversor, template, Biometrias);
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

                    NBioAPI.Type.HFIR handler;
                    contexto.Conversor.FDxToNBioBSPEx(biometria.TemplateISO, (uint)biometria.TemplateISO.Length, NBioAPI.Type.MINCONV_DATA_TYPE.MINCONV_TYPE_ISO,
                        NBioAPI.Type.FIR_PURPOSE.ENROLL_FOR_IDENTIFICATION_ONLY, out handler);

                    var retorno = contexto.MecanismoBusca.VerifyMatch(contexto.TemplateLido, handler, out encontrou, payload);
                    if (encontrou)
                    {
                        contexto.MecanismoConfirmacao.InitEngine();
                        NBioAPI.IndexSearch.FP_INFO[] informacaoBiometria;
                        var ret = contexto.MecanismoConfirmacao.AddFIR(handler, (uint)biometria.Id, out informacaoBiometria);

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
            public ContextoParaIndentificacaoBiometrica(Guid id, NBioAPI mecanismoBusca, NBioAPI.IndexSearch mecanismoConfirmacao,
                NBioAPI.Export conversor, NBioAPI.Type.HFIR templateLido, IEnumerable<Biometria> biometrias)
            {
                Id = id;
                MecanismoBusca = mecanismoBusca;
                TemplateLido = templateLido;
                Biometrias = biometrias;
                MecanismoConfirmacao = mecanismoConfirmacao;
                Conversor = conversor;
            }

            public Guid Id { get; }
            public NBioAPI MecanismoBusca { get; }
            public NBioAPI.Type.HFIR TemplateLido { get; }
            public IEnumerable<Biometria> Biometrias { get; }
            public NBioAPI.IndexSearch MecanismoConfirmacao { get; }
            public NBioAPI.Export Conversor { get; }
        }
    }
}