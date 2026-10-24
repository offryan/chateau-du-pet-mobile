using Npgsql;

namespace ChateauDuPet.Api.Dados;

public static class BancoDeDados
{
    /// <summary>
    /// Registra o pool de conexões e os repositórios. A string de conexão vem da
    /// variável DATABASE_URL (formato URI do Supabase) ou de ConnectionStrings:Chateau.
    /// </summary>
    public static IServiceCollection AddBancoChateau(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config["DATABASE_URL"]
            ?? config.GetConnectionString("Chateau")
            ?? throw new InvalidOperationException("Configure a variável de ambiente DATABASE_URL.");

        services.AddSingleton(_ => NpgsqlDataSource.Create(NormalizarConnectionString(connectionString)));
        services.AddSingleton<UsuariosRepositorio>();
        services.AddSingleton<VagasRepositorio>();
        services.AddSingleton<PerfilRepositorio>();
        return services;
    }

    /// <summary>
    /// Aceita tanto o formato chave=valor do Npgsql quanto a URI
    /// postgresql://usuario:senha@host:porta/banco que o Supabase mostra.
    /// </summary>
    public static string NormalizarConnectionString(string valor)
    {
        if (!valor.StartsWith("postgres://") && !valor.StartsWith("postgresql://"))
            return valor;

        var uri = new Uri(valor);
        var credenciais = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.Trim('/') is { Length: > 0 } banco ? banco : "postgres",
            Username = Uri.UnescapeDataString(credenciais[0]),
            Password = credenciais.Length > 1 ? Uri.UnescapeDataString(credenciais[1]) : null,
            SslMode = SslMode.Require
        };
        return builder.ConnectionString;
    }
}
