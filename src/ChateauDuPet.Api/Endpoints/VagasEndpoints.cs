using System.Security.Claims;
using ChateauDuPet.Api.Autenticacao;
using ChateauDuPet.Api.Dados;
using ChateauDuPet.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ChateauDuPet.Api.Endpoints;

public static class VagasEndpoints
{
    public static void MapVagas(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/vagas").WithTags("Vagas");

        grupo.MapGet("/", async (string? busca, VagasRepositorio vagas, CancellationToken ct) =>
                TypedResults.Ok(await vagas.ListarAsync(busca, ct)))
            .WithSummary("Lista as vagas abertas, das mais recentes para as mais antigas");

        grupo.MapGet("/{id:int}", async Task<Results<Ok<VagaDetalhe>, NotFound>> (
                int id, ClaimsPrincipal usuario, VagasRepositorio vagas, CancellationToken ct) =>
            {
                int? idProfissional = usuario.Tipo() == TipoUsuario.Profissional ? usuario.Id() : null;
                return await vagas.ObterAsync(id, idProfissional, ct) is { } vaga
                    ? TypedResults.Ok(vaga)
                    : TypedResults.NotFound();
            })
            .WithSummary("Detalhes de uma vaga; para profissionais, informa se já se candidatou");

        grupo.MapPost("/{id:int}/candidatura", async Task<Results<NoContent, NotFound, ProblemHttpResult>> (
                int id, ClaimsPrincipal usuario, VagasRepositorio vagas, CancellationToken ct) =>
                await vagas.CandidatarAsync(usuario.Id(), id, ct) switch
                {
                    ResultadoCandidatura.Ok => TypedResults.NoContent(),
                    ResultadoCandidatura.VagaNaoEncontrada => TypedResults.NotFound(),
                    _ => TypedResults.Problem("Você já se candidatou a esta vaga.", statusCode: StatusCodes.Status409Conflict)
                })
            .RequireAuthorization(Papeis.Profissional)
            .WithSummary("Candidata o profissional logado à vaga");

        grupo.MapDelete("/{id:int}/candidatura", async Task<Results<NoContent, NotFound>> (
                int id, ClaimsPrincipal usuario, VagasRepositorio vagas, CancellationToken ct) =>
                await vagas.CancelarCandidaturaAsync(usuario.Id(), id, ct)
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound())
            .RequireAuthorization(Papeis.Profissional)
            .WithSummary("Cancela a candidatura do profissional logado");
    }
}
