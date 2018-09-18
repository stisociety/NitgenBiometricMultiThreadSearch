using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TasksDemo.Sample1
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

        public IEnumerable<Biometria> RecuperarPrimeirosRegistros()
        {
            using (var conexao = new SqlConnection(_stringConexao))
            {
                var sql = @"SELECT TOP 1900 id, CAST(templateISO AS IMAGE) AS templateISO
                            FROM Digital (NOLOCK)
                            ORDER BY acessos";
                return conexao.Query<Biometria>(sql);
            }
        }

        public IEnumerable<Biometria> RecuperarUltimosRegistros()
        {
            using (var conexao = new SqlConnection(_stringConexao))
            {
                var sql = @"SELECT TOP 1900 id, CAST(templateISO AS IMAGE) AS templateISO
                            FROM Digital (NOLOCK)
                            ORDER BY acessos DESC";
                return conexao.Query<Biometria>(sql);
            }
        }
    }

    public class Biometria
    {
        public int Id { get; set; }
        public byte[] TemplateISO { get; set; }
    }
}