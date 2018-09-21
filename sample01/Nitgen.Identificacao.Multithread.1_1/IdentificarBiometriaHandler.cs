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
        private DigitaisRepositorio _repositorio;
        private DateTime _dataUltimaBusca;

        public IdentificarBiometriaHandler()
        {
            _repositorio = new DigitaisRepositorio();
            _mecanismosBusca = new List<NitgenBiometriaTask>();
            var nitgenMainApi = new NBioAPI();
            _mecanismoConfirmacao = new NBioAPI.IndexSearch(nitgenMainApi);
            _mecanismoConfirmacao.InitEngine();
            _dataUltimaBusca = DateTime.Now;
        }

        public void AdicionarMecanismoBuscaPara(IList<Biometria> biometrias)
        {
            _mecanismosBusca.Add(NitgenBiometriaTask.Novo(biometrias));
        }

        public int IdentificarBiometria(NBioAPI.Type.HFIR template)
        {
            var primeiraTask = true;
            var tasks = new Dictionary<Guid, Task<int>>();
            foreach (var buscaNitgen in _mecanismosBusca)
            {
                // Se for a primeira task, adiciona as biometrias capturadas nos últimos momentos
                if (primeiraTask)
                {
                    Console.WriteLine($"{buscaNitgen.Id} - Adicionando diferenças na Task que já possui {buscaNitgen.Biometrias.Count()} biometrias");
                    var diferenca = _repositorio.RecuperarDiferenca(_dataUltimaBusca);
                    Console.WriteLine($"{buscaNitgen.Id} - {diferenca.Count()} diferenças encontradas...");
                    if (diferenca.Count() > 0)
                    {
                        buscaNitgen.AdicionarDiferencas(diferenca);
                        Console.WriteLine($"{buscaNitgen.Id} - Diferenças adicionadas. Task com {buscaNitgen.Biometrias.Count()} biometrias...");
                    }
                   
                    _dataUltimaBusca = DateTime.Now;
                    primeiraTask = false;
                }

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