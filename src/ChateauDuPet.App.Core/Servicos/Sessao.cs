using System.Text.Json;
using ChateauDuPet.Contracts;

namespace ChateauDuPet.App.Core.Servicos;

/// <summary>
/// Usuário logado e seu token. Fica salvo no armazenamento seguro do aparelho,
/// então o login sobrevive ao app ser fechado, até o token vencer.
/// </summary>
public class Sessao(IArmazenamentoSeguro armazenamento, TimeProvider relogio)
{
    private const string Chave = "chateau.sessao";

    private LoginResponse? _atual;

    public UsuarioInfo? Usuario => _atual?.Usuario;
    public string? Token => _atual?.Token;
    public bool Logado => _atual is not null;

    public event EventHandler? Alterada;

    public async Task RestaurarAsync()
    {
        try
        {
            var json = await armazenamento.LerAsync(Chave);
            var salva = json is null ? null : JsonSerializer.Deserialize(json, ChateauJsonContext.Default.LoginResponse);
            if (salva is not null && salva.ExpiraEm > relogio.GetUtcNow())
            {
                Definir(salva);
                return;
            }
        }
        catch (Exception)
        {
            // Conteúdo corrompido, ou chave do armazenamento seguro invalidada (por exemplo,
            // backup restaurado em outro aparelho): descarta e segue como visitante
        }

        armazenamento.Remover(Chave);
    }

    public async Task IniciarAsync(LoginResponse login)
    {
        await armazenamento.GravarAsync(Chave, JsonSerializer.Serialize(login, ChateauJsonContext.Default.LoginResponse));
        Definir(login);
    }

    public Task EncerrarAsync()
    {
        armazenamento.Remover(Chave);
        Definir(null);
        return Task.CompletedTask;
    }

    private void Definir(LoginResponse? login)
    {
        _atual = login;
        Alterada?.Invoke(this, EventArgs.Empty);
    }
}
