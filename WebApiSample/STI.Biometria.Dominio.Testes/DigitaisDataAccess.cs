using Dapper;
using STI.Infra.Crosscutting.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace STI.Biometria.Dominio.Testes
{
    public sealed class DigitaisDataAccess
    {
        private readonly string _stringConexao;

        public DigitaisDataAccess(IConfiguration configuracao)
        {
            _stringConexao = configuracao.GetConnectionString();
        }

        public IEnumerable<Digital> CarregarDigitaisParaIdentificacao()
        {
            using (var conexao = new SqlConnection(_stringConexao))
            {
                var sql = @"SELECT id, matricula, templateISO FROM Digital_Escopo_Testes (NOLOCK) WHERE ModoTeste = 'IDENTIFY'";
                return conexao.Query<Digital>(sql, new { });
            }
        }

        public Digital CarregarDigitalEncontrada()
        {
            using (var conexao = new SqlConnection(_stringConexao))
            {
                var sql = @"SELECT id, matricula, templateISO FROM Digital_Escopo_Testes (NOLOCK) WHERE Id = 5836";
                return conexao.Query<Digital>(sql, new { }).FirstOrDefault();
            }
        }

        public Digital CarregarDigitalNaoEncontrada()
        {
            using (var conexao = new SqlConnection(_stringConexao))
            {
                var sql = @"SELECT id, matricula, templateISO FROM Digital_Escopo_Testes (NOLOCK) WHERE Id = 5835";
                return conexao.Query<Digital>(sql, new { }).FirstOrDefault();
            }
        }        
    }
}
