using System.Security.Claims;
using System.Text;
using ChateauDuPet.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ChateauDuPet.Api.Autenticacao;

public class JwtOpcoes
{
    public const string Secao = "Jwt";

    /// <summary>Segredo HMAC com pelo menos 32 caracteres. Em produção vem da variável Jwt__Chave.</summary>
    public string Chave { get; set; } = "";
    public string Emissor { get; set; } = "chateau-du-pet-api";
    public string Publico { get; set; } = "chateau-du-pet-app";
    public TimeSpan Validade { get; set; } = TimeSpan.FromDays(7);

    public SymmetricSecurityKey ChaveDeAssinatura() => new(Encoding.UTF8.GetBytes(Chave));
}

public static class Papeis
{
    public const string Empresa = nameof(TipoUsuario.Empresa);
    public const string Profissional = nameof(TipoUsuario.Profissional);
}

public class TokenService(IOptions<JwtOpcoes> opcoes, TimeProvider relogio)
{
    private readonly JsonWebTokenHandler _handler = new();

    public LoginResponse Emitir(UsuarioInfo usuario)
    {
        var jwt = opcoes.Value;
        var expiraEm = relogio.GetUtcNow().Add(jwt.Validade);
        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Emissor,
            Audience = jwt.Publico,
            Expires = expiraEm.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, usuario.Nome),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(ClaimsDoUsuario.Papel, usuario.Tipo.ToString())
            ]),
            SigningCredentials = new SigningCredentials(jwt.ChaveDeAssinatura(), SecurityAlgorithms.HmacSha256)
        });
        return new LoginResponse(token, expiraEm, usuario);
    }
}

public static class ClaimsDoUsuario
{
    public const string Papel = "role";

    public static int Id(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    /// <summary>Tipo do usuário logado, ou null se for visitante.</summary>
    public static TipoUsuario? Tipo(this ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true && Enum.TryParse<TipoUsuario>(principal.FindFirstValue(Papel), out var tipo)
            ? tipo
            : null;

    public static UsuarioInfo Info(this ClaimsPrincipal principal) => new(
        principal.Id(),
        principal.FindFirstValue(JwtRegisteredClaimNames.Name)!,
        principal.FindFirstValue(JwtRegisteredClaimNames.Email)!,
        principal.Tipo()!.Value);
}
