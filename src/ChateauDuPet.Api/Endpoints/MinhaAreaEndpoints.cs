using System.Security.Claims;
using ChateauDuPet.Api.Autenticacao;
using ChateauDuPet.Api.Dados;
using ChateauDuPet.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ChateauDuPet.Api.Endpoints;

/// <summary>Rotas que sempre agem sobre os dados do próprio usuário logado (o id vem do token, nunca da URL).</summary>
public static class MinhaAreaEndpoints
{
    public static void MapMinhaArea(this IEndpointRouteBuilder app)
    {
        var profissional = app.MapGroup("/api/me").WithTags("Profissional").RequireAuthorization(Papeis.Profissional);

        profissional.MapGet("/candidaturas", async (ClaimsPrincipal usuario, VagasRepositorio vagas, CancellationToken ct) =>
                TypedResults.Ok(await vagas.CandidaturasDoProfissionalAsync(usuario.Id(), ct)))
            .WithSummary("Vagas em que o profissional se candidatou");

        profissional.MapGet("/perfil", async Task<Results<Ok<PerfilProfissional>, NotFound>> (
                ClaimsPrincipal usuario, PerfilRepositorio perfis, CancellationToken ct) =>
                await perfis.ObterProfissionalAsync(usuario.Id(), ct) is { } perfil
                    ? TypedResults.Ok(perfil)
                    : TypedResults.NotFound())
            .WithSummary("Currículo do profissional: dados, experiências e certificados");

        var empresa = app.MapGroup("/api/me/vagas").WithTags("Empresa").RequireAuthorization(Papeis.Empresa);

        empresa.MapGet("/", async (ClaimsPrincipal usuario, VagasRepositorio vagas, CancellationToken ct) =>
                TypedResults.Ok(await vagas.ListarDaEmpresaAsync(usuario.Id(), ct)))
            .WithSummary("Vagas publicadas pela empresa");

        empresa.MapGet("/{id:int}/candidatos", async Task<Results<Ok<List<CandidatoResumo>>, NotFound>> (
                int id, ClaimsPrincipal usuario, VagasRepositorio vagas, CancellationToken ct) =>
                await vagas.CandidatosDaVagaAsync(id, usuario.Id(), ct) is { } candidatos
                    ? TypedResults.Ok(candidatos)
                    : TypedResults.NotFound())
            .WithSummary("Candidatos de uma vaga da empresa");
    }
}
