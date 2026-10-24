using ChateauDuPet.Contracts;
using Dapper;
using Npgsql;

namespace ChateauDuPet.Api.Dados;

public class PerfilRepositorio(NpgsqlDataSource banco)
{
    private sealed record ProfissionalLinha(int Id, string Nome, string Email, string Telefone,
        string? Biografia, string? Formacao, string? Cidade, string? UF);

    public async Task<PerfilProfissional?> ObterProfissionalAsync(int id, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        await using var resultado = await conn.QueryMultipleAsync(new CommandDefinition("""
            SELECT IdProfissional AS Id, NmProfissional AS Nome, DsEmail AS Email, NroTelefone AS Telefone,
                   DsBiografia AS Biografia, FormacaoEscolar AS Formacao, DsCidade AS Cidade, DsUF AS UF
            FROM Profissional WHERE IdProfissional = @id;

            SELECT DsCargo AS Cargo, DsEmpresa AS Empresa, DsTipoContrato AS TipoContrato, DsLocalidade AS Localidade,
                   DataInicio AS Inicio, DataFinal AS Fim
            FROM Experiencia WHERE FKProfissional = @id ORDER BY DataInicio DESC;

            SELECT DsCurso AS Curso, DsInstituicao AS Instituicao, DataInicio AS Inicio, DataConclusao AS Conclusao,
                   DsDescricao AS Descricao
            FROM Certificado WHERE FKProfissional = @id ORDER BY DataInicio DESC;
            """, new { id }, cancellationToken: ct));

        var p = await resultado.ReadSingleOrDefaultAsync<ProfissionalLinha>();
        if (p is null) return null;

        // O Npgsql 10 lê colunas DATE como DateOnly, o mesmo tipo dos contratos
        var experiencias = (await resultado.ReadAsync<ExperienciaInfo>()).AsList();
        var certificados = (await resultado.ReadAsync<CertificadoInfo>()).AsList();

        return new PerfilProfissional(p.Id, p.Nome, p.Email, p.Telefone, p.Biografia, p.Formacao, p.Cidade, p.UF,
            experiencias, certificados);
    }
}
