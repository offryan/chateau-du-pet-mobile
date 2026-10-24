using ChateauDuPet.Api.Autenticacao;
using ChateauDuPet.Api.Dados;
using ChateauDuPet.Api.Endpoints;
using ChateauDuPet.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Render injeta a porta na variável PORT
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } porta)
    builder.WebHost.UseSetting("http_ports", porta);

builder.Services.AddBancoChateau(builder.Configuration);

builder.Services.AddOptions<JwtOpcoes>()
    .Bind(builder.Configuration.GetSection(JwtOpcoes.Secao))
    .Validate(o => o.Chave.Length >= 32, "Configure Jwt__Chave com pelo menos 32 caracteres.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<Microsoft.Extensions.Options.IOptions<JwtOpcoes>>((opcoes, jwt) =>
    {
        // Mantém os nomes curtos das claims (sub, name, role) em vez dos URIs do .NET Framework
        opcoes.MapInboundClaims = false;
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Value.Emissor,
            ValidAudience = jwt.Value.Publico,
            IssuerSigningKey = jwt.Value.ChaveDeAssinatura(),
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = ClaimsDoUsuario.Papel,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Papeis.Empresa, p => p.RequireRole(Papeis.Empresa))
    .AddPolicy(Papeis.Profissional, p => p.RequireRole(Papeis.Profissional));

builder.Services.AddLimitesDeAcesso(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, ChateauJsonContext.Default));
builder.Services.AddOpenApi(o => o.AddDocumentTransformer<OpenApiBearer>());

builder.Services.Configure<ForwardedHeadersOptions>(opcoes =>
{
    // O Render termina o HTTPS no proxy dele e repassa a requisição em HTTP
    opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opcoes.KnownIPNetworks.Clear();
    opcoes.KnownProxies.Clear();
});

var app = builder.Build();

// Confere a conexão com o banco já na subida, para o problema aparecer no log do deploy
try
{
    await using var conn = await app.Services.GetRequiredService<NpgsqlDataSource>().OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand("SELECT count(*) FROM Vaga", conn);
    app.Logger.LogInformation("Banco conectado: {Vagas} vagas cadastradas", await cmd.ExecuteScalarAsync());
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Não foi possível conectar ao banco. Confira a variável DATABASE_URL.");
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Documentação interativa em /scalar (a API é pública e só expõe dados de demonstração)
app.MapOpenApi();
app.MapScalarApiReference(o => o.WithTitle("Chateau Du Pet API"));
app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
app.MapGet("/health", () => TypedResults.Ok("ok")).ExcludeFromDescription();

app.MapAuth();
app.MapVagas();
app.MapMinhaArea();

app.Run();

// Exposto para os testes de integração (WebApplicationFactory<Program>)
public partial class Program;
