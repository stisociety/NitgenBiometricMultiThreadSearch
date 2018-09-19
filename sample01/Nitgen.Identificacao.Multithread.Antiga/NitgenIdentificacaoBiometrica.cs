using NBioBSPCOMLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nitgen.Identificacao.Multithread.Antiga
{
    public sealed class NitgenBiometriaTask
    {
        internal NitgenBiometriaTask(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }
        public CancellationTokenSource CancellationSource { get; private set; }

        public static NitgenBiometriaTask Novo(IEnumerable<Biometria> biometrias)
        {
            return new NitgenBiometriaTask(Guid.NewGuid());
        }

        public Task<int> CriarTaskParaIdentificacaoBiometrica(byte[] template, IEnumerable<Biometria> biometrias)
        {
            var contextoIdentificacao = new ContextoParaIndentificacaoBiometrica(Id, biometrias, template);
            CancellationSource = new CancellationTokenSource();
            var token = CancellationSource.Token;
            return new Task<int>((parametroState) =>
            {
                try
                {
                    var contexto = parametroState as ContextoParaIndentificacaoBiometrica;
                    Console.WriteLine($"{DateTime.Now} - {contexto.Id} - Localizando em {contexto.Biometrias.Count()} biometrias...");

                    if (token.IsCancellationRequested)
                        return 0;

                    var nitgenMainApi = new NBioBSP();
                    IIndexSearch nitgenSearchApi = nitgenMainApi.IndexSearch;

                    foreach (var biometria in contexto.Biometrias)
                        nitgenSearchApi.AddFIR(biometria.TemplateFIR, biometria.Id);

                    Console.WriteLine($"{DateTime.Now} - {contexto.Id} - Iniciando identificação...");

                    var relogio = new Stopwatch();
                    relogio.Start();
                    nitgenSearchApi.IdentifyUser(template, 7);
                    relogio.Stop();

                    Console.WriteLine($"{DateTime.Now} - {contexto.Id} - Localizado {nitgenSearchApi.UserID} em {relogio.Elapsed.TotalSeconds}");
                    return nitgenSearchApi.UserID;

                    //if (nitgenSearchApi.UserID > 0 && nitgenSearchApi.ErrorCode == 0)
                    //{                        
                        
                    //}
                    
                    //Console.WriteLine($"ERRO NA IDENTIFICAÇÃO: {nitgenSearchApi.ErrorCode} - {nitgenSearchApi.ErrorDescription}");
                    //return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return 0;
                }
            }, contextoIdentificacao, token);
        }

        private sealed class ContextoParaIndentificacaoBiometrica
        {
            public ContextoParaIndentificacaoBiometrica(Guid id, IEnumerable<Biometria> biometrias, byte[] templateLido)
            {
                Id = id;
                Biometrias = biometrias;
                TemplateLido = templateLido;
            }

            public Guid Id { get; }
            public IEnumerable<Biometria> Biometrias { get; }
            public byte[] TemplateLido { get; }
        }
    }

    public sealed class NitgenBiometriaTaskSemCargaIndividual
    {
        internal NitgenBiometriaTaskSemCargaIndividual(Guid id, IIndexSearch mecanismoBusca)
        {
            Id = id;
            MecanismoBusca = mecanismoBusca;
        }

        public Guid Id { get; }
        public IIndexSearch MecanismoBusca { get; }
        public CancellationTokenSource CancellationSource { get; private set; }

        public static NitgenBiometriaTaskSemCargaIndividual Novo(IEnumerable<Biometria> biometrias)
        {
            var nitgenMainApi = new NBioBSP();
            IIndexSearch MecanismoBusca = nitgenMainApi.IndexSearch;

            // Carrega as biometrias
            foreach (var biometria in biometrias)
                MecanismoBusca.AddFIR(biometria.TemplateFIR, biometria.Id);

            return new NitgenBiometriaTaskSemCargaIndividual(Guid.NewGuid(), MecanismoBusca);
        }

        public Task<int> CriarTaskParaIdentificacaoBiometrica(byte[] template)
        {
            var contextoIdentificacao = new ContextoParaIndentificacaoBiometrica(Id, MecanismoBusca, template);
            CancellationSource = new CancellationTokenSource();
            var token = CancellationSource.Token;
            return new Task<int>((parametroState) =>
            {
                var contexto = parametroState as ContextoParaIndentificacaoBiometrica;

                if (token.IsCancellationRequested)
                    return 0;

                var relogio = new Stopwatch();
                relogio.Start();
                contexto.MecanismoBusca.IdentifyUser(contexto.TemplateLido, 7);
                relogio.Stop();

                Console.WriteLine($"{contexto.Id} - Localizado {contexto.MecanismoBusca.UserID} em {relogio.Elapsed.TotalSeconds}");

                if (token.IsCancellationRequested)
                    return 0;

                return contexto.MecanismoBusca.UserID;
            }, contextoIdentificacao, token);
        }
        
        private sealed class ContextoParaIndentificacaoBiometrica
        {
            public ContextoParaIndentificacaoBiometrica(Guid id, IIndexSearch mecanismoBusca, byte[] templateLido)
            {
                Id = id;
                MecanismoBusca = mecanismoBusca;
                TemplateLido = templateLido;
            }

            public Guid Id { get; }
            public IIndexSearch MecanismoBusca { get; }
            public byte[] TemplateLido { get; }
        }
    }
}
