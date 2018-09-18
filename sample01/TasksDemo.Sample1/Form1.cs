using NITGEN.SDK.NBioBSP;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TasksDemo.Sample1
{
    public class DigitalTask
    {
        public DigitalTask(Task task, CancellationTokenSource cancellationSource)
        {
            Task = task;
            CancellationSource = cancellationSource;
        }

        public Task Task { get; }
        public CancellationTokenSource CancellationSource { get; }
    }

    public class BiometriaIndentificacaoContexto
    {
        public BiometriaIndentificacaoContexto(Guid id, ConcurrentBag<DigitalTask> tasks, NBioAPI.IndexSearch mecanismoBusca, NBioAPI.Export conversor)
        {
            Id = id;
            DemaisTasks = tasks;
            MecanismoBusca = mecanismoBusca;
            Conversor = conversor;
        }

        public Guid Id { get; }
        public ConcurrentBag<DigitalTask> DemaisTasks { get; }
        public DigitalTask TaskPropria { get; set; }
        public NBioAPI.IndexSearch MecanismoBusca { get; }
        public NBioAPI.Export Conversor { get; }

        public static BiometriaIndentificacaoContexto Novo(NBioAPI.IndexSearch mecanismoBusca, NBioAPI.Export conversor)
            => new BiometriaIndentificacaoContexto(Guid.NewGuid(), new ConcurrentBag<DigitalTask>(), mecanismoBusca, conversor);

        public void AdicionarTask(DigitalTask task)
            => DemaisTasks.Add(task);

        public void AdicionarTaskPropria(DigitalTask digitalTask)
            => TaskPropria = digitalTask;
    }

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private ConcurrentBag<DigitalTask> _taks;

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var contextosIdentificacao = new List<BiometriaIndentificacaoContexto>();
                var tasks = new ConcurrentBag<DigitalTask>();
                for (int i = 0; i < 3; i++)
                {
                    var nitgenMainApi = new NBioAPI();
                    var nitgenSearchApi = new NBioAPI.IndexSearch(nitgenMainApi);
                    var nitgenConvertApi = new NBioAPI.Export(nitgenMainApi);
                    var contextoIdentificacao = BiometriaIndentificacaoContexto.Novo(nitgenSearchApi, nitgenConvertApi);
                    var digitalTask = Create(contextoIdentificacao);
                    contextoIdentificacao.AdicionarTaskPropria(digitalTask);
                    contextosIdentificacao.Add(contextoIdentificacao);
                    tasks.Add(digitalTask);
                }

                foreach (var contexto in contextosIdentificacao)
                    foreach (var digitalTask in tasks)
                        if (contexto.TaskPropria.Task.Id != digitalTask.Task.Id)
                            contexto.AdicionarTask(digitalTask);

                foreach (var digitalTask in tasks)
                    digitalTask.Task.Start();
            }
            catch (OperationCanceledException ex)
            {
                foreach (var task in _taks)
                {
                    task.CancellationSource.Dispose();
                }
            }
        }

        private DigitalTask Create(BiometriaIndentificacaoContexto contextoIndentificacao)
        {
            var cancellationToken = new CancellationTokenSource();
            var token = cancellationToken.Token;
            
            var task = new Task(() =>
            {
                Console.WriteLine("Cancelando");

                foreach (var taskParaCancelar in contextoIndentificacao.DemaisTasks)
                {
                    taskParaCancelar.CancellationSource.Cancel();
                    if (taskParaCancelar.CancellationSource.IsCancellationRequested)
                        taskParaCancelar.CancellationSource.Token.ThrowIfCancellationRequested();
                }

                Console.WriteLine("Cancelado");
            }, cancellationToken.Token);

            return new DigitalTask(task, cancellationToken);

            //var task = new TaskFactory().StartNew<int>((parametroState) =>
            //{
            //    var contexto = parametroState as BiometriaIndentificacaoContexto;

            //    Thread.Sleep(1000);

            //    Console.WriteLine("Cancelando");

            //    foreach (var taskParaCancelar in contexto.Tasks)
            //    {
            //        taskParaCancelar.CancellationSource.Cancel();
            //        if (token.IsCancellationRequested)
            //            token.ThrowIfCancellationRequested();
            //    }

            //    Console.WriteLine("Cancelado");

            //    return 0;
            //}, contextoIndentificacao, token);
        }
    }
}