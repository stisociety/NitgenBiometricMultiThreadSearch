using NITGEN.SDK.NBioBSP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TasksDemo.Sample1
{
    public class DigitalTask
    {
        public DigitalTask(Guid id, Task<int> task, CancellationTokenSource cancellationSource)
        {
            Id = id;
            Task = task;
            CancellationSource = cancellationSource;
        }

        public Guid Id { get; }
        public Task<int> Task { get; }
        public CancellationTokenSource CancellationSource { get; }
    }

    public class BiometriaIndentificacaoContexto
    {
        public BiometriaIndentificacaoContexto(Guid id, NBioAPI.IndexSearch mecanismoBusca, NBioAPI.Export conversor)
        {
            Id = id;
            MecanismoBusca = mecanismoBusca;
            Conversor = conversor;
        }

        public Guid Id { get; }
        public NBioAPI.IndexSearch MecanismoBusca { get; }
        public NBioAPI.Export Conversor { get; }
        public NBioAPI.Type.HFIR TemplateLido { get; set; }

        public static BiometriaIndentificacaoContexto Novo(NBioAPI.IndexSearch mecanismoBusca, NBioAPI.Export conversor)
            => new BiometriaIndentificacaoContexto(Guid.NewGuid(), mecanismoBusca, conversor);
    }

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private static ConcurrentBag<DigitalTask> _tasks;

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var contextosIdentificacao = new List<BiometriaIndentificacaoContexto>();
                _tasks = new ConcurrentBag<DigitalTask>();

                // Captura da digital (trocar para o que vem da catraca posteriormente)
                var nitgenMainApi = new NBioAPI();
                NBioAPI.Type.HFIR template;
                var window = new NBioAPI.Type.WINDOW_OPTION();
                nitgenMainApi.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
                nitgenMainApi.Capture(out template, 0, window);
                nitgenMainApi.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);

                var repositorio = new DigitalRepositorio();
                var digitaisIniciais = repositorio.RecuperarPrimeirosRegistros();
                var digitaisFinais = repositorio.RecuperarUltimosRegistros();

                var task1 = Create(template, digitaisIniciais);
                var task2 = Create(template, digitaisFinais);
                
                _tasks.Add(task1);
                _tasks.Add(task2);

                task1.Task.Start();
                Thread.Sleep(500);
                task2.Task.Start();

                var relogio = new Stopwatch();
                relogio.Start();

                var possoSair = false;
                DigitalTask resultado;
                while (!possoSair )
                {
                    if (_tasks.Any(t=> t.Task.IsCompleted))
                    {
                        var completadas = _tasks.Where(t => t.Task.IsCompleted);
                        resultado = completadas.FirstOrDefault(c => c.Task.Result > 0);

                        if (resultado != null)
                        {
                            foreach (var task in _tasks.Where(t => t.Id != resultado.Id))
                                task.CancellationSource.Cancel();
                            possoSair = true;
                        }
                    }

                    if (_tasks.All(t => t.Task.IsCompleted))
                        possoSair = true;

                    Thread.Sleep(10);
                }

                relogio.Stop();
                MessageBox.Show($"Terminou em {relogio.Elapsed.TotalSeconds}");
            }
            catch (OperationCanceledException ex)
            {
                foreach (var task in _tasks)
                {
                    task.CancellationSource.Dispose();
                }
            }
        }

        private DigitalTask Create(NBioAPI.Type.HFIR template, IEnumerable<Biometria> biometrias)
        {
            
            var nitgenMainApi = new NBioAPI();
            var nitgenSearchApi = new NBioAPI.IndexSearch(nitgenMainApi);
            nitgenSearchApi.InitEngine();
            var nitgenConvertApi = new NBioAPI.Export(nitgenMainApi);
            var contextoIdentificacao = BiometriaIndentificacaoContexto.Novo(nitgenSearchApi, nitgenConvertApi);
            contextoIdentificacao.TemplateLido = template;
            var cancellationToken = new CancellationTokenSource();
            var token = cancellationToken.Token;

            foreach (var biometria in biometrias)
            {
                NBioAPI.Type.HFIR handle;
                NBioAPI.IndexSearch.FP_INFO[] nitgenBiometria;
                nitgenConvertApi.FDxToNBioBSPEx(biometria.TemplateISO, (uint)biometria.TemplateISO.Length,
                    NBioAPI.Type.MINCONV_DATA_TYPE.MINCONV_TYPE_ISO, NBioAPI.Type.FIR_PURPOSE.ENROLL_FOR_IDENTIFICATION_ONLY,
                    out handle);

                nitgenSearchApi.AddFIR(handle, (uint)biometria.Id, out nitgenBiometria);
            }

            var task = new Task<int>((parametroState) =>
            {
                var contexto = parametroState as BiometriaIndentificacaoContexto;
                Console.WriteLine($"{contexto.Id} - Iniciado ");

                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                // Faz o Index Search
                var cbInfo = new NBioAPI.IndexSearch.CALLBACK_INFO_0();
                NBioAPI.IndexSearch.FP_INFO nitgenBiometria;
                var relogio = new Stopwatch();
                Console.WriteLine($"{contexto.Id} - Localizando biometria...");
                relogio.Start();
                var retorno = nitgenSearchApi.IdentifyData(contexto.TemplateLido, NBioAPI.Type.FIR_SECURITY_LEVEL.HIGH,
                    out nitgenBiometria, cbInfo);
                relogio.Stop();
                Console.WriteLine($"{contexto.Id} - Localizado {nitgenBiometria.ID} em {relogio.Elapsed.TotalSeconds}");

                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();

                Console.WriteLine($"{contexto.Id} - Finalizado ");
                return (int)nitgenBiometria.ID;
            }, contextoIdentificacao, token);

            return new DigitalTask(contextoIdentificacao.Id, task, cancellationToken);
        }
    }
}