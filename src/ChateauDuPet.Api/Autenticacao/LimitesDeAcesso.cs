using System.Threading.RateLimiting;

namespace ChateauDuPet.Api.Autenticacao;

public static class LimitesDeAcesso
{
    public const string Login = "login";

    /// <summary>Limita tentativas de login por IP, contra adivinhação de senha.</summary>
    public static IServiceCollection AddLimitesDeAcesso(this IServiceCollection services, IConfiguration config)
    {
        var porMinuto = config.GetValue("LimiteDeLoginsPorMinuto", 10);
        return services.AddRateLimiter(opcoes =>
        {
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opcoes.AddPolicy(Login, contexto => RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = porMinuto, Window = TimeSpan.FromMinutes(1) }));
        });
    }
}
