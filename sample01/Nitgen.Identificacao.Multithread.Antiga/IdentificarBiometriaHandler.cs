using NBioBSPCOMLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nitgen.Identificacao.Multithread.Antiga
{
    public class IdentificarBiometriaHandler
    {
        private NBioBSP _nitgen;
        private IFPData _data;
        private IIndexSearch _indexSearch;
        private static IList<NitgenBiometriaTaskSemCargaIndividual> _mecanismosBusca;

        public IdentificarBiometriaHandler()
        {
            _nitgen = new NBioBSP();
            _data = _nitgen.FPData;
            _indexSearch = _nitgen.IndexSearch;
            _mecanismosBusca = new List<NitgenBiometriaTaskSemCargaIndividual>();
        }

        public void AdicionarMecanismoBuscaPara(IEnumerable<Biometria> biometrias)
        {
            _mecanismosBusca.Add(NitgenBiometriaTaskSemCargaIndividual.Novo(biometrias));
        }

        public int IdentificarBiometria(byte[] template)
        {
            //_data.Import(1, 0, 1, 7, 404, template);
            _indexSearch.IdentifyUser(template, 7);
            return _indexSearch.UserID;
        }

        public int IdentificarBiometriaSemCarga(byte[] template)
        {
            var relogio = new Stopwatch();
            relogio.Start();
            Console.WriteLine($"Localizando digital ...");
            var repositorio = new DigitaisRepositorio();

            var tasks = new Dictionary<Guid, Task<int>>();
            var numeroTotalBiometrias = repositorio.RecuperarNumeroTotalBiometrias();
            Console.WriteLine($"Abrindo {Environment.ProcessorCount} threads...");
            var biometriasPorPagina = (numeroTotalBiometrias / Environment.ProcessorCount) + 10;
            for (int pagina = 1; pagina <= Environment.ProcessorCount; pagina++)
            {
                var biometriasRecuperadas = repositorio.RecuperarPagina(pagina, biometriasPorPagina);
                var buscaNitgen = NitgenBiometriaTask.Novo(biometriasRecuperadas);
                var task = buscaNitgen.CriarTaskParaIdentificacaoBiometrica(template, biometriasRecuperadas);
                //tasks.TryAdd(buscaNitgen.Id, task);
                tasks.Add(buscaNitgen.Id, task);
                task.Start();
            }

            // var cancelation = new CancellationTokenSource();
            //Task.WaitAll(tasks.Select(t => t.Value).ToArray(), cancelation.Token);
            
            while (tasks.Count() > 0)
            {
                //var indice = Task.WaitAny(tasks.Select(t => t.Value).ToArray());
                var indice = Task.WaitAny(tasks.Select(t => t.Value).ToArray());
                //resultado = tasks.Select(t => t.Value).ElementAt(indice).Result;
                var resultado = tasks.ElementAt(indice);

                Console.WriteLine($"{resultado.Key} - Terminou com {resultado.Value.Result}");

                if (resultado.Value.Result > 0)
                {
                    relogio.Stop();
                    Console.WriteLine($"Localizada digital em > {relogio.Elapsed.TotalSeconds} segundos");
                    return resultado.Value.Result;
                }

                tasks.Remove(resultado.Key);
            }

            relogio.Stop();
            Console.WriteLine($"Nenhuma digital localizada em > {relogio.Elapsed.TotalSeconds} segundos");
            return 0;

            //var resultado = tasks.FirstOrDefault(x => x.Value.Status == TaskStatus.RanToCompletion && x.Value.Result > 0);

            //var biometria = resultado.Key == Guid.Empty
            //    ? 0
            //    : resultado.Value.Result;

            //foreach (var item in tasks)
            //{
            //    item.Value.Dispose();
            //}
            //return biometria;
        }

        public int IdentificarBiometriaComCarga(byte[] template)
        {
            var relogio = new Stopwatch();
            relogio.Start();
            Console.WriteLine($"Localizando digital ...");

            var tasks = new Dictionary<Guid, Task<int>>();
            foreach (var buscaNitgen in _mecanismosBusca)
            {
                var task = buscaNitgen.CriarTaskParaIdentificacaoBiometrica(template);
                tasks.Add(buscaNitgen.Id, task);
                if (!task.IsCanceled)
                    task.Start();
            }

            //while (tasks.Count() > 0)
            //{
            //    var indice = Task.WaitAny(tasks.Select(t => t.Value).ToArray());
            //    var resultado = tasks.ElementAt(indice);

            //    Console.WriteLine($"{resultado.Key} - Terminou com {resultado.Value.Result}");

            //    if (resultado.Value.Result > 0)
            //    {
            //        relogio.Stop();
            //        Console.WriteLine($"Localizada digital em > {relogio.Elapsed.TotalSeconds} segundos");
            //        return resultado.Value.Result;
            //    }

            //    tasks.Remove(resultado.Key);
            //}

            var possoSair = false;
            while (!possoSair)
            {
                if (tasks.Any(t => t.Value.IsCompleted))
                {
                    var completadas = tasks.Where(t => t.Value.IsCompleted && !t.Value.IsCanceled);
                    var resultado = completadas.FirstOrDefault(c => c.Value.Result > 0);

                    if (resultado.Key != Guid.Empty)
                    {
                        foreach (var task in tasks.Where(t => t.Key != resultado.Key))
                            _mecanismosBusca.FirstOrDefault(m => m.Id.Equals(task.Key)).CancellationSource.Cancel();

                        relogio.Stop();
                        Console.WriteLine($"Digital localizada em {relogio.Elapsed.TotalSeconds} segundos");
                        return resultado.Value.Result;
                    }
                }

                if (tasks.All(t => t.Value.IsCompleted))
                    possoSair = true;

                Thread.Sleep(10);
            }

            relogio.Stop();
            Console.WriteLine($"Nenhuma digital localizada em {relogio.Elapsed.TotalSeconds} segundos");
            return 0;
        }        
    }
}