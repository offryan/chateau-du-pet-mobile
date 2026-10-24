using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ChateauDuPet.Api.Autenticacao;

/// <summary>
/// Descreve o login por token no documento OpenAPI. Só as rotas que exigem login
/// são marcadas como autenticadas; as públicas (login, lista de vagas) ficam livres no Scalar.
/// </summary>
internal sealed class OpenApiBearer : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    private const string Esquema = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Info.Title = "Chateau Du Pet API";
        document.Info.Description =
            "API do app mobile do Chateau Du Pet: vagas do mundo pet, candidaturas e currículos. " +
            "Para as rotas com cadeado, entre em POST /api/auth/login (ex.: jucelino@exemplo.com / demo1234) " +
            "e use o token devolvido.";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[Esquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token devolvido por POST /api/auth/login"
        };
        return Task.CompletedTask;
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        var metadados = context.Description.ActionDescriptor.EndpointMetadata;
        var exigeLogin = metadados.OfType<IAuthorizeData>().Any() && !metadados.OfType<IAllowAnonymous>().Any();
        if (exigeLogin)
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(Esquema, context.Document)] = [] }
            ];
        }
        return Task.CompletedTask;
    }
}
