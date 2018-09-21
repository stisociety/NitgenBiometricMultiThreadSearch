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
            var biometriaEncontrada = 0;
            while (tasks.Count() > 0)
            {
                var indice = Task.WaitAny(tasks.Select(t => t.Value).ToArray());
                var resultado = tasks.ElementAt(indice);
                Console.WriteLine($"{resultado.Key} - Terminou com {resultado.Value.Result} e foi {resultado.Value.Status}");
                if (resultado.Value.Status == TaskStatus.RanToCompletion && resultado.Value.Result > 0)
                {
                    foreach (var task in tasks.Where(t=> t.Key != resultado.Key))
                        _mecanismosBusca.FirstOrDefault(c => c.Id.Equals(task.Key)).CancellationSource.Cancel();
                    relogio.Stop();
                    Console.WriteLine($"Localizada digital em > {relogio.Elapsed.TotalSeconds} segundos");
                    biometriaEncontrada = resultado.Value.Result;
                }
                tasks.Remove(resultado.Key);
            }
            relogio.Stop();
            Console.WriteLine($"Nenhuma biometria encontrada em > {relogio.Elapsed.TotalSeconds} segundos");
            return biometriaEncontrada;
        }
    }
}