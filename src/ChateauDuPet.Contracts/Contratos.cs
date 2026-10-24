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

// ----- Gestão: empresa publica vagas e administrador cuida do cadastro -----

/// <summary>Dados que a empresa preenche ao publicar ou editar uma vaga.</summary>
public record VagaFormulario(
    string Titulo,
    string? Experiencia,
    string? TipoRegistro,
    string? Sexo,
    string? Validade,
    string? Descricao)
{
    /// <summary>
    /// Primeiro problema do formulário, ou null se estiver válido. A API e o app usam a mesma
    /// regra: o app avisa antes de enviar e a API recusa o que vier de outro cliente.
    /// </summary>
    public string? Validar()
    {
        if (string.IsNullOrWhiteSpace(Titulo)) return "Informe o título da vaga.";
        if (string.IsNullOrWhiteSpace(Descricao)) return "Descreva a vaga.";
        if (Titulo.Trim().Length > OpcoesDeVaga.TamanhoTitulo) return $"O título pode ter até {OpcoesDeVaga.TamanhoTitulo} caracteres.";
        if (Descricao.Trim().Length > OpcoesDeVaga.TamanhoDescricao) return $"A descrição pode ter até {OpcoesDeVaga.TamanhoDescricao} caracteres.";
        if (Experiencia?.Length > OpcoesDeVaga.TamanhoExperiencia) return "Nível de experiência inválido.";
        if (TipoRegistro?.Length > OpcoesDeVaga.TamanhoTipoRegistro) return "Tipo de registro inválido.";
        if (Sexo?.Length > OpcoesDeVaga.TamanhoSexo) return "Opção de gênero inválida.";
        if (Validade?.Length > OpcoesDeVaga.TamanhoValidade) return "Validade inválida.";
        return null;
    }

    /// <summary>Remove espaços das pontas e troca campos em branco por null, como serão gravados.</summary>
    public VagaFormulario Normalizado() => new(
        Titulo.Trim(), Limpo(Experiencia), Limpo(TipoRegistro), Limpo(Sexo), Limpo(Validade), Limpo(Descricao));

    private static string? Limpo(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}

/// <summary>Opções dos campos de seleção da vaga, as mesmas do portal web.</summary>
public static class OpcoesDeVaga
{
    public static readonly IReadOnlyList<string> Experiencias = ["Até 1 ano", "De 1 a 2 anos", "De 3 a 4 anos", "Mais de 5 anos"];
    public static readonly IReadOnlyList<string> TiposDeRegistro = ["CLT", "Freelancer", "Temporário", "PJ"];
    public static readonly IReadOnlyList<string> Sexos = ["Indiferente", "Masculino", "Feminino"];
    public static readonly IReadOnlyList<string> Validades = ["1 semana", "15 dias", "1 mês", "2 meses"];

    // Tamanhos das colunas no banco
    public const int TamanhoTitulo = 100;
    public const int TamanhoExperiencia = 50;
    public const int TamanhoTipoRegistro = 15;
    public const int TamanhoSexo = 20;
    public const int TamanhoValidade = 20;
    public const int TamanhoDescricao = 4000;
}

public record ProfissionalAdmin(int Id, string Nome, string Usuario, string Email, string Telefone, string? Cidade);

public record EmpresaAdmin(int Id, string Nome, string Recrutador, string Email, string? Cnpj, int TotalVagas);
