using System.Data.SqlClient;
using System.Linq;
using Dapper;
using STI.Compartilhado.Core;
using STI.Infra.Crosscutting.Tenants;

namespace STI.Biometria.Dominio.Infra.SqlServer
{
    public sealed class ConfiguracoesBiometriaRepositorio : IConfiguracoesBiometriaRepositorio
    {
        private readonly string _connectionString;

        public ConfiguracoesBiometriaRepositorio(ITenant tenant)
        {
            _connectionString = tenant.Contextos.FirstOrDefault(x => x.Nome == "associados").ConexaoBancoDados.UsoGeral;
        }

        public Resultado<ConfiguracaoBiometria, Falha> Recuperar(int estacao)
        {
            using (var conexao = new SqlConnection(_connectionString))
            {
                var resultado = conexao.Query<ConfiguracaoBiometria>(@"SELECT QuantidadeThreadsIdentificacaoDigital, ParceiroSdkDigital FROM ConfiguracoesBiometria WHERE Estacao = @estacao", new { estacao }).FirstOrDefault();
                if (resultado == null)
                    return Falha.Nova(404, $"Nenhuma configuração para estação {estacao}");
                return resultado;
            }
        }
    }
}
