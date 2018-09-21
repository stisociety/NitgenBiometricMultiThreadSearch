using NITGEN.SDK.NBioBSP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nitgen.Identificacao.Multithread.Demo
{
    public sealed class IdentificarBiometriaHandler
    {
        private static IList<NitgenBiometriaTask> _mecanismosBusca;

        public IdentificarBiometriaHandler()
        {
            _mecanismosBusca = new List<NitgenBiometriaTask>();
        }

        public void AdicionarMecanismoBuscaPara(IEnumerable<Biometria> biometrias)
        {
            _mecanismosBusca.Add(NitgenBiometriaTask.Novo(biometrias));
        }

        public int IdentificarBiometria(NBioAPI.Type.HFIR template)
        {
            var tasks = new Dictionary<Guid, Task<int>>();
            foreach (var buscaNitgen in _mecanismosBusca)
            {
                var task = buscaNitgen.CriarTaskParaIdentificacaoBiometrica(template);
                tasks.Add(buscaNitgen.Id, task);
                if (!task.IsCanceled)
                    task.Start();
            }

            Console.WriteLine($"Localizado digital ...");
            var relogio = new Stopwatch();
            relogio.Start();
            var possoSair = false;
            //KeyValuePair<Guid, Task<int>> resultado = new KeyValuePair<Guid, Task<int>>(Guid.Empty, null);
            var cancelation = new CancellationTokenSource();
            //Task.WaitAll(tasks.Select(t => t.Value).ToArray(), cancelation.Token);
            //var resultado = tasks.FirstOrDefault(x => x.Value.Status == TaskStatus.RanToCompletion && x.Value.Result > 0);

            while (tasks.Count() > 0)
            {
                var indice = Task.WaitAny(tasks.Select(t => t.Value).ToArray());
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
            Console.WriteLine($"Localizado digital em > {relogio.Elapsed.TotalSeconds} segundos");
            return 0;

            //while (!possoSair)
            //{
            //    if (tasks.Any(t => t.Value.IsCompleted))
            //    {
            //        var completadas = tasks.Where(t => t.Value.IsCompleted && !t.Value.IsCanceled);
            //        resultado = completadas.FirstOrDefault(c => c.Value.Result > 0);

            //        if (resultado.Key != Guid.Empty)
            //        {
            //            foreach (var task in tasks.Where(t => t.Key != resultado.Key))
            //                _mecanismosBusca.FirstOrDefault(m => m.Id.Equals(task.Key)).CancellationSource.Cancel();
            //            possoSair = true;
            //        }
            //    }

            //    if (tasks.All(t => t.Value.IsCompleted))
            //        possoSair = true;

            //    Thread.Sleep(10);
            //}
            //relogio.Stop();
            //Console.WriteLine($"Localizado digital em > {relogio.Elapsed.TotalSeconds} segundos");

            //return resultado.Key == Guid.Empty
            //    ? 0
            //    : resultado.Value.Result;
        }

        public int IdentificarBiometriaV2(NBioAPI.Type.HFIR template)
        {
            var relogio = new Stopwatch();
            relogio.Start();
            Console.WriteLine($"Localizado digital ...");

            var repositorio = new DigitaisRepositorio();
            var tasks = new ConcurrentDictionary<Guid, Task<int>>();
            var numeroTotalBiometrias = repositorio.RecuperarNumeroTotalBiometrias();
            var biometriasPorPagina = (numeroTotalBiometrias / Environment.ProcessorCount) + 10;
            for (int pagina = 1; pagina <= Environment.ProcessorCount; pagina++)
            {
                var biometriasRecuperadas = repositorio.RecuperarPagina(pagina, biometriasPorPagina);
                var buscaNitgen = NitgenBiometriaTaskV2.Novo(biometriasRecuperadas);
                var task = buscaNitgen.CriarTaskParaIdentificacaoBiometrica(template, biometriasRecuperadas);
                tasks.TryAdd(buscaNitgen.Id, task);
                task.Start();
            }

            var cancelation = new CancellationTokenSource();
            Task.WaitAll(tasks.Select(t => t.Value).ToArray(), cancelation.Token);
            var resultado = tasks.FirstOrDefault(x => x.Value.Status == TaskStatus.RanToCompletion && x.Value.Result > 0);

            var biometria = resultado.Key == Guid.Empty
                ? 0
                : resultado.Value.Result;

            foreach (var item in tasks)
            {
                item.Value.Dispose();
            }

            relogio.Stop();
            Console.WriteLine($"Localizado digital em > {relogio.Elapsed.TotalSeconds} segundos");

            return biometria;
        }
    }
}