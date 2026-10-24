using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ChateauDuPet.Api.Autenticacao;

/// <summary>Descreve o login por token no documento OpenAPI, para o Scalar mostrar o campo de autenticação.</summary>
internal sealed class OpenApiBearer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token devolvido por POST /api/auth/login"
        };
        document.Security =
        [
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }
        ];
        return Task.CompletedTask;
    }
}
