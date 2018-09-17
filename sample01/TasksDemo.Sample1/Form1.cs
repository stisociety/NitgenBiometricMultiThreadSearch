using NITGEN.SDK.NBioBSP;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TasksDemo.Sample1
{
    public class DigitalTask
    {
        public DigitalTask(Guid id, CancellationTokenSource cancellationSource)
        {
            Id = id;
            CancellationSource = cancellationSource;
        }

        public Guid Id { get; }
        public CancellationTokenSource CancellationSource { get; }
    }

    public class BiometriaIndentificacaoContexto
    {
        public BiometriaIndentificacaoContexto(Guid id, ConcurrentBag<DigitalTask> tasks, NBioAPI.IndexSearch mecanismoBusca, NBioAPI.Export conversor)
        {
            Id = id;
            Tasks = tasks;
            MecanismoBusca = mecanismoBusca;
            Conversor = conversor;
        }

        public Guid Id { get; }
        public ConcurrentBag<DigitalTask> Tasks { get; }
        public NBioAPI.IndexSearch MecanismoBusca { get; }
        public NBioAPI.Export Conversor { get; }

        public static BiometriaIndentificacaoContexto Novo(NBioAPI.IndexSearch mecanismoBusca, NBioAPI.Export conversor)
            => new BiometriaIndentificacaoContexto(Guid.NewGuid(), new ConcurrentBag<DigitalTask>(), mecanismoBusca, conversor);
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
            _taks = new ConcurrentBag<DigitalTask>();
            try
            {

                var nitgenMainApi = new NBioAPI();

                var nitgenSearchApi = new NBioAPI.IndexSearch(nitgenMainApi);
                var nitgenConvertApi = new NBioAPI.Export(nitgenMainApi);

                var contextoTask1 = BiometriaIndentificacaoContexto.Novo(nitgenSearchApi, nitgenConvertApi);


                _taks.Add(Create(contextoTask1));
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
            var task = new TaskFactory().StartNew<int>((parametroState) =>
            {
                var contexto = parametroState as BiometriaIndentificacaoContexto;
                
                Thread.Sleep(1000);

                Console.WriteLine("Cancelando");

                foreach (var taskParaCancelar in contexto.Tasks)
                {
                    taskParaCancelar.CancellationSource.Cancel();
                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();
                }

                Console.WriteLine("Cancelado");

                return 0;
            }, contextoIndentificacao, token);
            return new DigitalTask(contextoIndentificacao.Id, cancellationToken);
        }
    }
}
