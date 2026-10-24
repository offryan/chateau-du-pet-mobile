using ChateauDuPet.Contracts;
using Dapper;
using Npgsql;

namespace ChateauDuPet.Api.Dados;

/// <summary>Cadastro visto pelo administrador. Exclusões apagam os dados dependentes em cascata no banco.</summary>
public class AdminRepositorio(NpgsqlDataSource banco)
{
    public async Task<List<ProfissionalAdmin>> ListarProfissionaisAsync(CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return (await conn.QueryAsync<ProfissionalAdmin>(new CommandDefinition("""
            SELECT IdProfissional AS Id, NmProfissional AS Nome, DsUser AS Usuario, DsEmail AS Email,
                   NroTelefone AS Telefone, DsCidade AS Cidade
            FROM Profissional
            ORDER BY NmProfissional
            """, cancellationToken: ct))).AsList();
    }

    public async Task<List<EmpresaAdmin>> ListarEmpresasAsync(CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return (await conn.QueryAsync<EmpresaAdmin>(new CommandDefinition("""
            SELECT e.IdEmpresa AS Id,
                   COALESCE(NULLIF(e.DsNomeFantasia, ''), e.NmRecrutador) AS Nome,
                   e.NmRecrutador AS Recrutador,
                   e.DsEmail AS Email,
                   e.NroCNPJ AS Cnpj,
                   (SELECT count(*) FROM Vaga v WHERE v.FKEmpresa = e.IdEmpresa)::int AS TotalVagas
            FROM Empresa e
            ORDER BY 2
            """, cancellationToken: ct))).AsList();
    }

    public async Task<bool> ExcluirProfissionalAsync(int id, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Profissional WHERE IdProfissional = @id", new { id }, cancellationToken: ct)) > 0;
    }

    public async Task<bool> ExcluirEmpresaAsync(int id, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Empresa WHERE IdEmpresa = @id", new { id }, cancellationToken: ct)) > 0;
    }
}
