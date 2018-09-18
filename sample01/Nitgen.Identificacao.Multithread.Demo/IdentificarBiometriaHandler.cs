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
            var tasks = new ConcurrentDictionary<Guid, Task<int>>();
            foreach (var buscaNitgen in _mecanismosBusca)
            {
                var task = buscaNitgen.CriarTaskParaIdentificacaoBiometrica(template);
                tasks.TryAdd(buscaNitgen.Id, task);
                if (!task.IsCanceled)
                    task.Start();
            }

            Console.WriteLine($"Localizado digital ...");
            var relogio = new Stopwatch();
            relogio.Start();
            var possoSair = false;
            KeyValuePair<Guid, Task<int>> resultado = new KeyValuePair<Guid, Task<int>>(Guid.Empty, null);
            while (!possoSair)
            {
                if (tasks.Any(t => t.Value.IsCompleted))
                {
                    var completadas = tasks.Where(t => t.Value.IsCompleted && !t.Value.IsCanceled);
                    resultado = completadas.FirstOrDefault(c => c.Value.Result > 0);

                    if (resultado.Key != Guid.Empty)
                    {
                        foreach (var task in tasks.Where(t => t.Key != resultado.Key))
                            _mecanismosBusca.FirstOrDefault(m => m.Id.Equals(task.Key)).CancellationSource.Cancel();
                        possoSair = true;
                    }
                }

                if (tasks.All(t => t.Value.IsCompleted))
                    possoSair = true;

                Thread.Sleep(10);
            }
            relogio.Stop();
            Console.WriteLine($"Localizado digital em > {relogio.Elapsed.TotalSeconds} segundos");

            return resultado.Key == Guid.Empty
                ? 0
                : resultado.Value.Result;
        }
    }
}