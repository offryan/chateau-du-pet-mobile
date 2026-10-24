namespace ChateauDuPet.Contracts;

// Formato das mensagens trocadas entre a API e o app. O mesmo projeto é
// referenciado pelos dois lados, então um campo renomeado quebra a compilação
// em vez de quebrar em produção.

public enum TipoUsuario
{
    Empresa = 1,
    Profissional = 2,
    Administrativo = 3
}

public record LoginRequest(string Email, string Senha);

public record UsuarioInfo(int Id, string Nome, string Email, TipoUsuario Tipo);

public record LoginResponse(string Token, DateTimeOffset ExpiraEm, UsuarioInfo Usuario);

public record VagaResumo(
    int Id,
    string Titulo,
    string Empresa,
    string? Cidade,
    string? TipoRegistro,
    DateTime PublicadaEm,
    int TotalCandidatos);

public record VagaDetalhe(
    int Id,
    string Titulo,
    string Empresa,
    string? Cidade,
    string? TipoRegistro,
    string? Experiencia,
    string? Sexo,
    string? Validade,
    string? Descricao,
    DateTime PublicadaEm,
    int TotalCandidatos,
    // null quando quem consulta não é profissional (visitante ou empresa)
    bool? JaCandidatado);

public record CandidaturaResumo(int IdVaga, string Vaga, string Empresa, DateTime Data);

public record CandidatoResumo(int IdProfissional, string Nome, string Email, string? Formacao, DateTime Data);

public record ExperienciaInfo(string Cargo, string Empresa, string TipoContrato, string Localidade, DateOnly Inicio, DateOnly? Fim);

public record CertificadoInfo(string Curso, string Instituicao, DateOnly Inicio, DateOnly? Conclusao, string Descricao);

public record PerfilProfissional(
    int Id,
    string Nome,
    string Email,
    string Telefone,
    string? Biografia,
    string? Formacao,
    string? Cidade,
    string? UF,
    IReadOnlyList<ExperienciaInfo> Experiencias,
    IReadOnlyList<CertificadoInfo> Certificados);
