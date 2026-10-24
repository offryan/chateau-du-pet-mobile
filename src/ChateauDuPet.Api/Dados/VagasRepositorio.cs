using ChateauDuPet.Contracts;
using Dapper;
using Npgsql;

namespace ChateauDuPet.Api.Dados;

public enum ResultadoCandidatura { Ok, VagaNaoEncontrada, JaCandidatado }

public class VagasRepositorio(NpgsqlDataSource banco)
{
    // A ordem das colunas segue o construtor do record VagaResumo, que o Dapper usa para mapear
    private const string SelectResumo = """
        SELECT v.IdVaga AS Id,
               v.NmVaga AS Titulo,
               COALESCE(NULLIF(e.DsNomeFantasia, ''), e.NmRecrutador) AS Empresa,
               e.DsCidade AS Cidade,
               v.DsTipoRegistro AS TipoRegistro,
               v.DataPublicacao AS PublicadaEm,
               (SELECT count(*) FROM Candidatos c WHERE c.FKVaga = v.IdVaga)::int AS TotalCandidatos
        FROM Vaga v
        JOIN Empresa e ON e.IdEmpresa = v.FKEmpresa
        """;

    public async Task<List<VagaResumo>> ListarAsync(string? busca, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return (await conn.QueryAsync<VagaResumo>(new CommandDefinition($"""
            {SelectResumo}
            WHERE @busca::text IS NULL
               OR v.NmVaga ILIKE '%' || @busca || '%'
               OR v.DsDescricao ILIKE '%' || @busca || '%'
               OR e.DsNomeFantasia ILIKE '%' || @busca || '%'
               OR e.DsCidade ILIKE '%' || @busca || '%'
            ORDER BY v.DataPublicacao DESC, v.IdVaga DESC
            LIMIT 200
            """, new { busca = TermoDeBusca(busca) }, cancellationToken: ct))).AsList();
    }

    public async Task<List<VagaResumo>> ListarDaEmpresaAsync(int idEmpresa, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return (await conn.QueryAsync<VagaResumo>(new CommandDefinition($"""
            {SelectResumo}
            WHERE v.FKEmpresa = @idEmpresa
            ORDER BY v.DataPublicacao DESC, v.IdVaga DESC
            """, new { idEmpresa }, cancellationToken: ct))).AsList();
    }

    /// <summary>Com idProfissional informado, preenche se ele já se candidatou.</summary>
    public async Task<VagaDetalhe?> ObterAsync(int id, int? idProfissional, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<VagaDetalhe>(new CommandDefinition("""
            SELECT v.IdVaga AS Id,
                   v.NmVaga AS Titulo,
                   COALESCE(NULLIF(e.DsNomeFantasia, ''), e.NmRecrutador) AS Empresa,
                   e.DsCidade AS Cidade,
                   v.DsTipoRegistro AS TipoRegistro,
                   v.DsExperiencia AS Experiencia,
                   v.DsSexo AS Sexo,
                   v.Validade,
                   v.DsDescricao AS Descricao,
                   v.DataPublicacao AS PublicadaEm,
                   (SELECT count(*) FROM Candidatos c WHERE c.FKVaga = v.IdVaga)::int AS TotalCandidatos,
                   CASE WHEN @idProfissional::int IS NULL THEN NULL
                        ELSE EXISTS (SELECT 1 FROM Candidatos c WHERE c.FKVaga = v.IdVaga AND c.FKProfissional = @idProfissional)
                   END AS JaCandidatado
            FROM Vaga v
            JOIN Empresa e ON e.IdEmpresa = v.FKEmpresa
            WHERE v.IdVaga = @id
            """, new { id, idProfissional }, cancellationToken: ct));
    }

    public async Task<ResultadoCandidatura> CandidatarAsync(int idProfissional, int idVaga, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        var inseridas = await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO Candidatos (FKProfissional, FKVaga)
            SELECT @idProfissional, @idVaga
            WHERE EXISTS (SELECT 1 FROM Vaga WHERE IdVaga = @idVaga)
            ON CONFLICT (FKProfissional, FKVaga) DO NOTHING
            """, new { idProfissional, idVaga }, cancellationToken: ct));
        if (inseridas > 0) return ResultadoCandidatura.Ok;

        var vagaExiste = await conn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM Vaga WHERE IdVaga = @idVaga)", new { idVaga }, cancellationToken: ct));
        return vagaExiste ? ResultadoCandidatura.JaCandidatado : ResultadoCandidatura.VagaNaoEncontrada;
    }

    public async Task<bool> CancelarCandidaturaAsync(int idProfissional, int idVaga, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Candidatos WHERE FKProfissional = @idProfissional AND FKVaga = @idVaga",
            new { idProfissional, idVaga }, cancellationToken: ct)) > 0;
    }

    public async Task<List<CandidaturaResumo>> CandidaturasDoProfissionalAsync(int idProfissional, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return (await conn.QueryAsync<CandidaturaResumo>(new CommandDefinition("""
            SELECT c.FKVaga AS IdVaga,
                   v.NmVaga AS Vaga,
                   COALESCE(NULLIF(e.DsNomeFantasia, ''), e.NmRecrutador) AS Empresa,
                   c.DsData AS Data
            FROM Candidatos c
            JOIN Vaga v    ON v.IdVaga = c.FKVaga
            JOIN Empresa e ON e.IdEmpresa = v.FKEmpresa
            WHERE c.FKProfissional = @idProfissional
            ORDER BY c.DsData DESC
            """, new { idProfissional }, cancellationToken: ct))).AsList();
    }

    /// <summary>Candidatos de uma vaga da empresa. Retorna null se a vaga não for dela.</summary>
    public async Task<List<CandidatoResumo>?> CandidatosDaVagaAsync(int idVaga, int idEmpresa, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        var ehDaEmpresa = await conn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM Vaga WHERE IdVaga = @idVaga AND FKEmpresa = @idEmpresa)",
            new { idVaga, idEmpresa }, cancellationToken: ct));
        if (!ehDaEmpresa) return null;

        return (await conn.QueryAsync<CandidatoResumo>(new CommandDefinition("""
            SELECT p.IdProfissional,
                   p.NmProfissional AS Nome,
                   p.DsEmail AS Email,
                   p.FormacaoEscolar AS Formacao,
                   c.DsData AS Data
            FROM Candidatos c
            JOIN Profissional p ON p.IdProfissional = c.FKProfissional
            WHERE c.FKVaga = @idVaga
            ORDER BY c.DsData
            """, new { idVaga }, cancellationToken: ct))).AsList();
    }

    /// <summary>Publica uma vaga da empresa e devolve o id criado. O formulário já deve estar validado.</summary>
    public async Task<int> PublicarAsync(int idEmpresa, VagaFormulario vaga, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition("""
            INSERT INTO Vaga (NmVaga, DsExperiencia, DsTipoRegistro, DsDescricao, DsSexo, Validade, FKEmpresa)
            VALUES (@Titulo, @Experiencia, @TipoRegistro, @Descricao, @Sexo, @Validade, @idEmpresa)
            RETURNING IdVaga
            """, ParametrosDaVaga(vaga, new { idEmpresa }), cancellationToken: ct));
    }

    /// <summary>Altera a vaga só se ela pertencer à empresa; retorna false se não for dela ou não existir.</summary>
    public async Task<bool> EditarAsync(int idVaga, int idEmpresa, VagaFormulario vaga, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE Vaga SET NmVaga = @Titulo, DsExperiencia = @Experiencia, DsTipoRegistro = @TipoRegistro,
                   DsDescricao = @Descricao, DsSexo = @Sexo, Validade = @Validade
            WHERE IdVaga = @idVaga AND FKEmpresa = @idEmpresa
            """, ParametrosDaVaga(vaga, new { idVaga, idEmpresa }), cancellationToken: ct)) > 0;
    }

    /// <summary>Exclui a vaga (e as candidaturas, em cascata). Com idEmpresa = null, é o administrador excluindo qualquer vaga.</summary>
    public async Task<bool> ExcluirAsync(int idVaga, int? idEmpresa, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Vaga WHERE IdVaga = @idVaga AND (@idEmpresa::int IS NULL OR FKEmpresa = @idEmpresa)",
            new { idVaga, idEmpresa }, cancellationToken: ct)) > 0;
    }

    /// <summary>A empresa só vê o currículo de quem se candidatou a alguma vaga dela.</summary>
    public async Task<bool> EhCandidatoDaEmpresaAsync(int idProfissional, int idEmpresa, CancellationToken ct)
    {
        await using var conn = banco.CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(new CommandDefinition("""
            SELECT EXISTS (
                SELECT 1 FROM Candidatos c JOIN Vaga v ON v.IdVaga = c.FKVaga
                WHERE c.FKProfissional = @idProfissional AND v.FKEmpresa = @idEmpresa)
            """, new { idProfissional, idEmpresa }, cancellationToken: ct));
    }

    private static DynamicParameters ParametrosDaVaga(VagaFormulario vaga, object chaves)
    {
        var parametros = new DynamicParameters(chaves);
        parametros.AddDynamicParams(new { vaga.Titulo, vaga.Experiencia, vaga.TipoRegistro, vaga.Descricao, vaga.Sexo, vaga.Validade });
        return parametros;
    }

    /// <summary>Remove espaços e escapa os curingas do ILIKE, para "%" ou "_" serem buscados literalmente.</summary>
    internal static string? TermoDeBusca(string? busca) =>
        string.IsNullOrWhiteSpace(busca)
            ? null
            : busca.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
