using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STI.Biometria.Dominio.Infra.Integracoes.Nitgen;
using STI.Compartilhado.Core;
using STI.Infra.Crosscutting.Configuration;

namespace STI.Biometria.Dominio.Infra.Integracoes.Testes
{
    [TestClass]
    public class NitGenSDKIdentificarDigitalTestes
    {
        private Microsoft.Extensions.Configuration.IConfiguration _configSettings;
        private DigitaisDataAccess _digitaisDataAccessTestes;
        private IMotorBuscaBiometrica _sdk;

        [TestInitialize]
        public void Setup()
        {
            _configSettings = new ConfigurationBuilder()
                                   .AddJsonFile("appsettings.json")
                                   .Build();
            var configuracao = new JsonConfiguration(_configSettings);
            _digitaisDataAccessTestes = new DigitaisDataAccess(configuracao);
            _sdk = new SdkNitgen();
        }

        [TestCleanup]
        public void Finalizar()
        {
            _sdk = null;
        }

        [TestMethod]
        public void Nao_Posso_Identificar_Digital_No_Banco_Quando_Informo_Template_Inexistente()
        {
            var digitais = _digitaisDataAccessTestes.CarregarDigitaisParaIdentificacao();
            var digitalLida = _digitaisDataAccessTestes.CarregarDigitalNaoEncontrada();

            var resultado = Resultado<Digital, Falha>.NovoSucesso(digitalLida);
            var tempo = new Stopwatch();
            tempo.Start();
            foreach (var digital in digitais)
            {
                resultado = _sdk.IdentificarDigital(digitalLida.TemplateIso, digital);
                if (resultado.EhSucesso)
                    Assert.Fail($"Digital não deveria ser localizada, mas foi dado match com {resultado.Sucesso.Matricula}");
            }
            tempo.Stop();
            Assert.IsTrue(tempo.Elapsed.TotalSeconds < 5);
            Assert.IsTrue(resultado.EhFalha);
            Assert.IsTrue(resultado.Falha.Codigo == 404);
        }

        [TestMethod]
        public void Devo_Identificar_Digital_No_Banco_Quando_Informo_Template_Existente()
        {
            var digitais = _digitaisDataAccessTestes.CarregarDigitaisParaIdentificacao().ToList();
            var digitalLida = _digitaisDataAccessTestes.CarregarDigitalEncontrada();
            digitais.Add(digitalLida);

            var resultado = Resultado<Digital, Falha>.NovaFalha(Falha.Nova(404, ""));
            var tempo = new Stopwatch();
            tempo.Start();
            foreach (var digital in digitais)
            {
                resultado = _sdk.IdentificarDigital(digitalLida.TemplateIso, digital);
                if (resultado.EhSucesso)
                    break;
            }
            tempo.Stop();
            Assert.IsTrue(tempo.Elapsed.TotalSeconds < 5);
            Assert.AreEqual(resultado.Sucesso.Id, digitalLida.Id);
        }
    }
}
