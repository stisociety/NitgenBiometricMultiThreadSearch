using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using STI.Biometria.Dominio.Infra.Integracoes.Nitgen;
using STI.Infra.Crosscutting.Configuration;

namespace STI.Biometria.Dominio.Testes
{
    [TestClass]
    public class ServicoIdentificacaoDigitalComNitegenTestes
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

        [TestMethod]
        public async Task Dada_Digital_Nao_Cadastrada_Nao_Posso_Localizar_Socio_Com_Apenas_Uma_Instancia()
        {
            var digitais = _digitaisDataAccessTestes.CarregarDigitaisParaIdentificacao();
            var servico = new ServicoIdentificacaoDigital(_sdk, digitais);
            var digital = _digitaisDataAccessTestes.CarregarDigitalNaoEncontrada();

            var resultado = await servico.IdentificarAsync(digital.TemplateIso);

            resultado.EhFalha.ShouldBeTrue();
            resultado.Falha.Codigo.ShouldBe(404);
        }

        [TestMethod]
        public async Task Dada_Digital_Nao_Cadastrada_Nao_Posso_Localizar_Socio_Com_Mais_Instancias()
        {
            var digitais = _digitaisDataAccessTestes.CarregarDigitaisParaIdentificacao();
            var servico1 = new ServicoIdentificacaoDigital(_sdk, digitais.Page(1, 2000).ToList());
            var servico2 = new ServicoIdentificacaoDigital(_sdk, digitais.Page(2, 2000).ToList());
            var servico3 = new ServicoIdentificacaoDigital(_sdk, digitais.Page(3, 2000).ToList());
            var digital = _digitaisDataAccessTestes.CarregarDigitalNaoEncontrada();

            var resultado1 = servico1.IdentificarAsync(digital.TemplateIso);
            var resultado2 = servico2.IdentificarAsync(digital.TemplateIso);
            var resultado3 = servico3.IdentificarAsync(digital.TemplateIso);
            Task.WaitAll(resultado1, resultado2, resultado3);

            resultado1.Result.EhFalha.ShouldBeTrue();
            resultado2.Result.EhFalha.ShouldBeTrue();
            resultado3.Result.EhFalha.ShouldBeTrue();
            resultado1.Result.Falha.Codigo.ShouldBe(404);
            resultado2.Result.Falha.Codigo.ShouldBe(404);
            resultado3.Result.Falha.Codigo.ShouldBe(404);
        }

        [TestMethod]
        public async Task Dada_Digital_Cadastrada_Devo_Localizar_Socio_Com_Apenas_Uma_Instancia()
        {
            var digitais = _digitaisDataAccessTestes.CarregarDigitaisParaIdentificacao();
            var servico = new ServicoIdentificacaoDigital(_sdk, digitais);
            var digital = _digitaisDataAccessTestes.CarregarDigitalEncontrada();

            var resultado = await servico.IdentificarAsync(digital.TemplateIso);

            resultado.EhSucesso.ShouldBeTrue();
            resultado.Sucesso.Id.ShouldBe(digital.Id);
        }

        [TestMethod]
        public async Task Dada_Digital_Cadastrada_Devo_Localizar_Socio_Com_Mais_Instancias()
        {
            var digitais = _digitaisDataAccessTestes.CarregarDigitaisParaIdentificacao();
            var servico1 = new ServicoIdentificacaoDigital(_sdk, digitais.Page(1, 2000).ToList());
            var servico2 = new ServicoIdentificacaoDigital(_sdk, digitais.Page(2, 2000).ToList());
            var servico3 = new ServicoIdentificacaoDigital(_sdk, digitais.Page(3, 2000).ToList());
            var digital = _digitaisDataAccessTestes.CarregarDigitalEncontrada();

            var resultado1 = servico1.IdentificarAsync(digital.TemplateIso);
            var resultado2 = servico2.IdentificarAsync(digital.TemplateIso);
            var resultado3 = servico3.IdentificarAsync(digital.TemplateIso);
            Task.WaitAll(resultado1, resultado2, resultado3);

            resultado1.Result.EhFalha.ShouldBeTrue();
            resultado2.Result.EhFalha.ShouldBeTrue();
            resultado3.Result.EhSucesso.ShouldBeTrue();
            resultado1.Result.Falha.Codigo.ShouldBe(404);
            resultado2.Result.Falha.Codigo.ShouldBe(404);
            resultado3.Result.Sucesso.Id.ShouldBe(digital.Id);
        }
    }

    public static class EnumerableExtensions
    {
        public static IEnumerable<TSource> Page<TSource>(this IEnumerable<TSource> source, int page, int pageSize)
        {
            return source.Skip((page - 1) * pageSize).Take(pageSize);
        }

    }
}
