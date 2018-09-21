using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalPersona.Identificacao.Simples.Demo
{
    public class DigitalRepositorio
    {
        private string _stringConexao;

        public DigitalRepositorio()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = @"DRACO-VM\SQLEXPRESS2014_0",
                InitialCatalog = "Society_180",
                UserID = "sa",
                Password = "STI000"
            };
            _stringConexao = builder.ConnectionString;
        }

        public IEnumerable<Biometria> RecuperarPagina(int pagina, int registros)
        {
            using (var conexao = new SqlConnection(_stringConexao))
            {
                var inicio = registros * (pagina - 1) + 1;
                var fim = registros * (pagina);
                var sql = @"WITH Biometrias AS
                            (
                                SELECT id, CAST(templateISO AS IMAGE) AS templateISO, indice = ROW_NUMBER() OVER (ORDER BY id)
                                FROM Digital (NOLOCK)
                            )
                            SELECT id, templateISO
                            FROM Biometrias
                            WHERE indice BETWEEN @Inicio AND @Fim";
                return conexao.Query<Biometria>(sql, new { Inicio = inicio, Fim = fim });
            }
        }

        public int RecuperarNumeroTotalBiometrias()
        {
            using (var conexao = new SqlConnection(_stringConexao))
            {
                var sql = @"SELECT COUNT(id)
                            FROM Digital (NOLOCK)
                            WHERE ISNULL(templateISOText, '') != ''";
                return conexao.Query<int>(sql).FirstOrDefault();
            }
        }
    }
}
