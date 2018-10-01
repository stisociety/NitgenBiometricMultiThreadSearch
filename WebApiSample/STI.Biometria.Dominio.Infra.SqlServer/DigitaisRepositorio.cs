using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using STI.Compartilhado.Core;
using STI.Infra.Crosscutting.Tenants;

namespace STI.Biometria.Dominio.Infra.SqlServer
{
    public sealed class DigitaisRepositorio : IDigitaisRepositorio
    {
        private readonly string _connectionString;

        public DigitaisRepositorio(ITenant tenant)
        {
            _connectionString = tenant.Contextos.FirstOrDefault(x => x.Nome == "associados").ConexaoBancoDados.UsoGeral;
        }

        public Resultado<int, Falha> RecuperarNumeroTotalDigitais()
        {
            using (var conexao = new SqlConnection(_connectionString))
            {
                return conexao.Query<int>("SELECT COUNT(id) FROM Digital").First();
            }
        }

        public Resultado<IEnumerable<Digital>, Falha> RecuperarPagina(int pagina, int quantidadePorPagina)
        {
            using (var conexao = new SqlConnection(_connectionString))
            {
                var inicio = quantidadePorPagina * (pagina - 1) + 1;
                var fim = quantidadePorPagina * (pagina);
                var sql = @"WITH Biometrias AS
                            (
                                SELECT id, matricula, templateISO, indice = ROW_NUMBER() OVER (ORDER BY id) FROM Digital (NOLOCK)
                            )
                            SELECT id, Matricula, TemplateISO FROM Biometrias WHERE indice BETWEEN @Inicio AND @Fim";
                return conexao.Query<Digital>(sql, new { Inicio = inicio, Fim = fim }).ToList();
            }
        }
    }
}
