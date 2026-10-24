using System.Reflection;

namespace ChateauDuPet.App;

public static class Configuracao
{
    /// <summary>Endereço da API, definido na compilação pela propriedade ChateauApiUrl do .csproj.</summary>
    public static Uri ApiUrl { get; } = new(
        typeof(Configuracao).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .First(a => a.Key == "ChateauApiUrl").Value!.TrimEnd('/') + "/");
}
