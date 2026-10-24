using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChateauDuPet.Contracts;

/// <summary>
/// Serialização gerada em tempo de compilação: sem reflexão, o que deixa o app
/// compatível com trimming/AOT e acelera a primeira requisição no celular.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(UsuarioInfo))]
[JsonSerializable(typeof(List<VagaResumo>))]
[JsonSerializable(typeof(VagaDetalhe))]
[JsonSerializable(typeof(List<CandidaturaResumo>))]
[JsonSerializable(typeof(List<CandidatoResumo>))]
[JsonSerializable(typeof(PerfilProfissional))]
[JsonSerializable(typeof(ProblemaApi))]
public partial class ChateauJsonContext : JsonSerializerContext;

/// <summary>Os campos do ProblemDetails (RFC 9457) que o app usa para mostrar o erro.</summary>
public record ProblemaApi(string? Title, string? Detail, int? Status);
