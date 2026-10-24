using ChateauDuPet.App.Core.Servicos;

namespace ChateauDuPet.App.Servicos;

public class ArmazenamentoSeguroMaui : IArmazenamentoSeguro
{
    public Task<string?> LerAsync(string chave) => SecureStorage.Default.GetAsync(chave);

    public Task GravarAsync(string chave, string valor) => SecureStorage.Default.SetAsync(chave, valor);

    public void Remover(string chave) => SecureStorage.Default.Remove(chave);
}
