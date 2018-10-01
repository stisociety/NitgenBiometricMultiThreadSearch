using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using STI.Biometria.Dominio.Comandos;
using STI.Biometria.Dominio.Infra.Integracoes;
using STI.Biometria.Dominio.Infra.SqlServer;
using STI.Infra.Crosscutting.Configuration;
using STI.Infra.Crosscutting.Tenants;
using STI.Society.Tenancy.Dominio.Tenants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STI.Biometria.Dominio.Testes
{
    [TestClass]
    public sealed class IdentificarDigitalComandoTestes
    {
        private Microsoft.Extensions.Configuration.IConfiguration _configSettings;
        private DigitaisDataAccess _digitaisDataAccessTestes;
        private IFabricaMotorBuscaPorParceiro _fabricaMotorBusca;
        private IDigitaisRepositorio _digitaisRepositorio;
        private IConfiguracoesBiometriaRepositorio _configuracoesRepositorio;
        private IIdentificarSocioPorDigitalComandoHandler _handler;

        [TestInitialize]
        public void Setup()
        {
            _configSettings = new ConfigurationBuilder()
                                   .AddJsonFile("appsettings.json")
                                   .Build();
            var configuracao = new JsonConfiguration(_configSettings);
            _digitaisDataAccessTestes = new DigitaisDataAccess(configuracao);
            _fabricaMotorBusca = new FabricaMotorBuscaBiometrica();
            var tenant = new Tenant(Guid.NewGuid(), "society", "Society", new List<Contexto> {
                Contexto.Novo("associados", configuracao.GetConnectionString("associados"), configuracao.GetConnectionString())
            },
            null);
            _digitaisRepositorio = new DigitaisRepositorio(tenant);
            _configuracoesRepositorio = new ConfiguracoesBiometriaRepositorio(tenant);
            _handler = new DigitaisComandosHandler(_digitaisRepositorio, _configuracoesRepositorio, _fabricaMotorBusca);
        }

        [TestMethod]
        public void Dada_Digital_Nao_Cadastrada_Devo_Receber_Falha_Ao_Identificar_Socio_Pela_Digital_Com_3_Threads()
        {
            var digital = _digitaisDataAccessTestes.CarregarDigitalNaoEncontrada();
            var comando = new IdentificarSocioPorDigitalComando(digital.TemplateIso, 1);

            var resultado = _handler.Executar(comando);

            resultado.EhFalha.ShouldBeTrue();
            resultado.Falha.Codigo.ShouldBe(404);
        }

        [TestMethod]
        public void Dada_Digital_Cadastrada_Devo_Identificar_Socio_Pela_Digital_Com_3_Threads()
        {
            var digital = _digitaisDataAccessTestes.CarregarDigitalEncontrada();
            var comando = new IdentificarSocioPorDigitalComando(digital.TemplateIso, 1);

            var resultado = _handler.Executar(comando);

            resultado.EhSucesso.ShouldBeTrue();
            resultado.Sucesso.Id.ShouldBe(digital.Id);
        }
    }
}
