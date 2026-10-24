using ChateauDuPet.App.Core.Servicos;
using Microsoft.Extensions.Logging;

namespace ChateauDuPet.App.Servicos;

/// <summary>
/// Guarda o login no armazenamento seguro do sistema. Se ele não estiver disponível (por exemplo,
/// o app do Windows rodando a partir do zip, sem instalação), o login vale só até fechar o app.
/// </summary>
public class ArmazenamentoSeguroMaui(ILogger<ArmazenamentoSeguroMaui> log) : IArmazenamentoSeguro
{
    public Task<string?> LerAsync(string chave) => SecureStorage.Default.GetAsync(chave);

    public async Task GravarAsync(string chave, string valor)
    {
        try
        {
            await SecureStorage.Default.SetAsync(chave, valor);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Armazenamento seguro indisponível; o login não ficará salvo");
        }
    }

    public void Remover(string chave)
    {
        try
        {
            SecureStorage.Default.Remove(chave);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Não foi possível apagar o login salvo");
        }
    }
}
