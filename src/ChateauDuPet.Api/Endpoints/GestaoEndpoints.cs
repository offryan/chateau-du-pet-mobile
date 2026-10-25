using System.Security.Claims;
using ChateauDuPet.Api.Autenticacao;
using ChateauDuPet.Api.Dados;
using ChateauDuPet.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ChateauDuPet.Api.Endpoints;

/// <summary>
/// Gestão do portal: a empresa publica e mantém as próprias vagas e vê o currículo dos candidatos;
/// o administrador gerencia profissionais, empresas e vagas. São as funções do antigo app desktop.
/// </summary>
public static class GestaoEndpoints
{
    public static void MapGestao(this IEndpointRouteBuilder app)
    {
        var empresa = app.MapGroup("/api/me/vagas").WithTags("Empresa").RequireAuthorization(Papeis.Empresa);

        empresa.MapPost("/", async Task<Results<Created<VagaDetalhe>, ValidationProblem>> (
                VagaFormulario formulario, ClaimsPrincipal usuario, VagasRepositorio vagas, CancellationToken ct) =>
            {
                if (Invalido(formulario) is { } problema) return problema;
                var id = await vagas.PublicarAsync(usuario.Id(), formulario.Normalizado(), ct);
                var vaga = await vagas.ObterAsync(id, null, ct);
                return TypedResults.Created($"/api/vagas/{id}", vaga);
            })
            .WithSummary("Publica uma vaga da empresa");

        empresa.MapPut("/{id:int}", async Task<Results<Ok<VagaDetalhe>, NotFound, ValidationProblem>> (
                int id, VagaFormulario formulario, ClaimsPrincipal usuario, VagasRepositorio vagas, CancellationToken ct) =>
            {
                if (Invalido(formulario) is { } problema) return problema;
                if (!await vagas.EditarAsync(id, usuario.Id(), formulario.Normalizado(), ct)) return TypedResults.NotFound();
                return TypedResults.Ok((await vagas.ObterAsync(id, null, ct))!);
            })
            .WithSummary("Edita uma vaga da empresa");

        empresa.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>> (
                int id, ClaimsPrincipal usuario, VagasRepositorio vagas, CancellationToken ct) =>
                await vagas.ExcluirAsync(id, usuario.Id(), ct) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithSummary("Exclui uma vaga da empresa e as candidaturas dela");

        app.MapGet("/api/profissionais/{id:int}/curriculo", async Task<Results<Ok<PerfilProfissional>, NotFound>> (
                int id, ClaimsPrincipal usuario, VagasRepositorio vagas, PerfilRepositorio perfis, CancellationToken ct) =>
            {
                // Para a empresa, quem não é candidato dela simplesmente "não existe"
                if (usuario.Tipo() == TipoUsuario.Empresa && !await vagas.EhCandidatoDaEmpresaAsync(id, usuario.Id(), ct))
                    return TypedResults.NotFound();
                return await perfis.ObterProfissionalAsync(id, ct) is { } perfil ? TypedResults.Ok(perfil) : TypedResults.NotFound();
            })
            .RequireAuthorization(Papeis.Recrutador)
            .WithTags("Empresa")
            .WithSummary("Currículo de um candidato: empresas veem só os próprios candidatos; administradores, todos");

        var admin = app.MapGroup("/api/admin").WithTags("Administração").RequireAuthorization(Papeis.Administrativo);

        admin.MapGet("/profissionais", async (AdminRepositorio cadastro, CancellationToken ct) =>
                TypedResults.Ok(await cadastro.ListarProfissionaisAsync(ct)))
            .WithSummary("Lista os candidatos cadastrados");

        admin.MapDelete("/profissionais/{id:int}", async Task<Results<NoContent, NotFound>> (
                int id, AdminRepositorio cadastro, CancellationToken ct) =>
                await cadastro.ExcluirProfissionalAsync(id, ct) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithSummary("Exclui um candidato com currículo e candidaturas");

        admin.MapGet("/empresas", async (AdminRepositorio cadastro, CancellationToken ct) =>
                TypedResults.Ok(await cadastro.ListarEmpresasAsync(ct)))
            .WithSummary("Lista as empresas cadastradas");

        admin.MapDelete("/empresas/{id:int}", async Task<Results<NoContent, NotFound>> (
                int id, AdminRepositorio cadastro, CancellationToken ct) =>
                await cadastro.ExcluirEmpresaAsync(id, ct) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithSummary("Exclui uma empresa com as vagas dela");

        admin.MapDelete("/vagas/{id:int}", async Task<Results<NoContent, NotFound>> (
                int id, VagasRepositorio vagas, CancellationToken ct) =>
                await vagas.ExcluirAsync(id, null, ct) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithSummary("Exclui qualquer vaga");
    }

    private static ValidationProblem? Invalido(VagaFormulario formulario) =>
        formulario.Validar() is { } erro
            ? TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["vaga"] = [erro] }, detail: erro)
            : null;
}
