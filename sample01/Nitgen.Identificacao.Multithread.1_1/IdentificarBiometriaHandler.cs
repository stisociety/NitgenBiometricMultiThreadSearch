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
    public sealed class IdentificarBiometriaHandler
    {
        private static IList<NitgenBiometriaTask> _mecanismosBusca;
        private static NBioAPI.IndexSearch _mecanismoConfirmacao;

        public IdentificarBiometriaHandler()
        {
            _mecanismosBusca = new List<NitgenBiometriaTask>();
            var nitgenMainApi = new NBioAPI();
            _mecanismoConfirmacao = new NBioAPI.IndexSearch(nitgenMainApi);
            _mecanismoConfirmacao.InitEngine();
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
                var task = buscaNitgen.CriarTaskParaIdentificacaoBiometrica(template, _mecanismoConfirmacao);
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
                        return resultado.Value.Result;
                    }
                }

                if (tasks.All(t => t.Value.IsCompleted))
                    possoSair = true;

                Thread.Sleep(10);
            }

            relogio.Stop();
            Console.WriteLine($"Nenhuma biometria encontrada em > {relogio.Elapsed.TotalSeconds} segundos");
            return 0;
        }
    }
}