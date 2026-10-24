using System.Security.Claims;
using ChateauDuPet.Api.Autenticacao;
using ChateauDuPet.Api.Dados;
using ChateauDuPet.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ChateauDuPet.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/auth").WithTags("Autenticação");

        grupo.MapPost("/login", async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> (
                LoginRequest req, UsuariosRepositorio usuarios, TokenService tokens, CancellationToken ct) =>
            {
                var usuario = await usuarios.AutenticarAsync(req.Email ?? "", req.Senha ?? "", ct);
                return usuario is null
                    ? TypedResults.Problem("E-mail ou senha inválidos.", statusCode: StatusCodes.Status401Unauthorized)
                    : TypedResults.Ok(tokens.Emitir(usuario));
            })
            .RequireRateLimiting(LimitesDeAcesso.Login)
            .WithSummary("Entra com e-mail e senha e recebe um token JWT");

        grupo.MapGet("/eu", (ClaimsPrincipal usuario) => TypedResults.Ok(usuario.Info()))
            .RequireAuthorization()
            .WithSummary("Dados do usuário dono do token");
    }
}
