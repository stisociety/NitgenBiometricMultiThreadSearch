using Dapper;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Nitgen.Identificacao.Multithread.Demo
{
    public sealed class DigitaisRepositorio
    {
        private string _stringConexao;

        public DigitaisRepositorio()
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
                            SELECT id, CAST(templateISO AS IMAGE) AS templateISO
                            FROM Biometrias (NOLOCK)
                            WHERE indice BETWEEN @inicio AND @fim";
                return conexao.Query<Biometria>(sql);
            }
        }
    }
}