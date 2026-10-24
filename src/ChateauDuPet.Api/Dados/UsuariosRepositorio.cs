using ChateauDuPet.Contracts;
using Dapper;
using Npgsql;

namespace ChateauDuPet.Api.Dados;

public class UsuariosRepositorio(NpgsqlDataSource banco)
{
    // Usado quando o e-mail não existe, para a resposta levar o mesmo tempo
    // de quando a senha está errada e não revelar quais e-mails têm conta.
    private static readonly Lazy<string> HashFicticio = new(() => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), 11));

    private sealed class Credencial
    {
        public int Id { get; init; }
        public string Nome { get; init; } = "";
        public string Email { get; init; } = "";
        public string Hash { get; init; } = "";
        public int Tipo { get; init; }
    }

    /// <summary>
    /// Procura o e-mail nos três tipos de usuário (na mesma prioridade do site:
    /// administrador, empresa, profissional). Retorna null se o e-mail não existir
    /// ou a senha não conferir, sem revelar qual dos dois falhou.
    /// </summary>
    public async Task<UsuarioInfo?> AutenticarAsync(string email, string senha, CancellationToken ct)
    {
        email = email.Trim();
        if (email.Length == 0 || senha.Length == 0) return null;

        await using var conn = banco.CreateConnection();
        var credencial = await conn.QueryFirstOrDefaultAsync<Credencial>(new CommandDefinition("""
            SELECT * FROM (
                SELECT IdAdministrativo AS Id, NmADM AS Nome, DsEmail AS Email, SenhaADM AS Hash, 3 AS Tipo, 1 AS Prioridade
                FROM Administrativo WHERE lower(DsEmail) = lower(@email)
                UNION ALL
                SELECT IdEmpresa, COALESCE(NULLIF(DsNomeFantasia, ''), NmRecrutador), DsEmail, SenhaEmpresa, 1, 2
                FROM Empresa WHERE lower(DsEmail) = lower(@email)
                UNION ALL
                SELECT IdProfissional, NmProfissional, DsEmail, SenhaProfissional, 2, 3
                FROM Profissional WHERE lower(DsEmail) = lower(@email)
            ) u
            ORDER BY Prioridade
            LIMIT 1
            """, new { email }, cancellationToken: ct));

        if (credencial is null)
        {
            BCrypt.Net.BCrypt.Verify(senha, HashFicticio.Value);
            return null;
        }

        return BCrypt.Net.BCrypt.Verify(senha, credencial.Hash)
            ? new UsuarioInfo(credencial.Id, credencial.Nome, credencial.Email, (TipoUsuario)credencial.Tipo)
            : null;
    }
}
