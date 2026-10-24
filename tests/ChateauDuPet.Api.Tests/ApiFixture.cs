using System.Net.Http.Headers;
using System.Net.Http.Json;
using ChateauDuPet.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace ChateauDuPet.Api.Tests;

/// <summary>
/// Sobe a API em memória contra um banco PostgreSQL de testes (chateau_api_testes),
/// criado do zero com database/schema.sql e database/seed.sql.
/// O servidor vem da variável CHATEAU_TEST_DB (padrão: postgres local, senha postgres).
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string NomeDoBanco = "chateau_api_testes";

    private static readonly string Servidor = Environment.GetEnvironmentVariable("CHATEAU_TEST_DB")
        ?? "Host=localhost;Username=postgres;Password=postgres";

    private readonly string _connectionString =
        new NpgsqlConnectionStringBuilder(Servidor) { Database = NomeDoBanco, Pooling = false }.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await using (var admin = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(Servidor) { Database = "postgres", Pooling = false }.ConnectionString))
        {
            await admin.OpenAsync();
            await new NpgsqlCommand($"DROP DATABASE IF EXISTS {NomeDoBanco} WITH (FORCE)", admin).ExecuteNonQueryAsync();
            await new NpgsqlCommand($"CREATE DATABASE {NomeDoBanco}", admin).ExecuteNonQueryAsync();
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        foreach (var arquivo in new[] { "schema.sql", "seed.sql" })
            await new NpgsqlCommand(await File.ReadAllTextAsync(CaminhoDoScript(arquivo)), conn).ExecuteNonQueryAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Chateau", _connectionString);
        builder.UseSetting("Jwt:Chave", "chave-dos-testes-de-integracao-com-32-caracteres-ou-mais");
        builder.UseSetting("LimiteDeLoginsPorMinuto", "1000");
    }

    /// <summary>Restaura os dados de demonstração, para cada teste começar do mesmo estado.</summary>
    public async Task RestaurarDadosAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await new NpgsqlCommand("SELECT resetar_demo()", conn).ExecuteNonQueryAsync();
    }

    public async Task<HttpClient> ClienteLogadoAsync(string email)
    {
        var cliente = CreateClient();
        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "demo1234"),
            ChateauJsonContext.Default.LoginRequest);
        resposta.EnsureSuccessStatusCode();
        var login = await resposta.Content.ReadFromJsonAsync(ChateauJsonContext.Default.LoginResponse);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return cliente;
    }

    private static string CaminhoDoScript(string arquivo)
    {
        for (var pasta = new DirectoryInfo(AppContext.BaseDirectory); pasta is not null; pasta = pasta.Parent)
        {
            var caminho = Path.Combine(pasta.FullName, "database", arquivo);
            if (File.Exists(caminho)) return caminho;
        }
        throw new FileNotFoundException($"database/{arquivo} não encontrado acima de {AppContext.BaseDirectory}");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
    }
}

[CollectionDefinition(Nome)]
public class ColecaoDaApi : ICollectionFixture<ApiFixture>
{
    public const string Nome = "API";
}
