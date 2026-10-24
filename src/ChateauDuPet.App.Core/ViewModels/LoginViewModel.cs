using ChateauDuPet.App.Core.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChateauDuPet.App.Core.ViewModels;

public record ContaDemo(string Perfil, string Email, string Descricao);

public partial class LoginViewModel(IChateauApi api, Sessao sessao, INavegacao navegacao) : ViewModelBase
{
    public const string SenhaDemo = "demo1234";

    public IReadOnlyList<ContaDemo> ContasDemo { get; } =
    [
        new("Profissional", "jucelino@exemplo.com", "Ver currículo e se candidatar"),
        new("Empresa", "felipe@exemplo.com", "Ver vagas publicadas e candidatos")
    ];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EntrarCommand))]
    public partial string Email { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EntrarCommand))]
    public partial string Senha { get; set; } = "";

    private bool PodeEntrar() => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrEmpty(Senha) && !Carregando;

    [RelayCommand(CanExecute = nameof(PodeEntrar))]
    private async Task EntrarAsync()
    {
        var email = Email.Trim();
        var senha = Senha;
        if (await CarregarComIndicadorAsync(async () => await sessao.IniciarAsync(await api.EntrarAsync(email, senha))))
        {
            Senha = "";
            await navegacao.VoltarAsync();
        }
    }

    [RelayCommand]
    private Task UsarContaDemoAsync(ContaDemo? conta)
    {
        if (conta is null) return Task.CompletedTask;
        Email = conta.Email;
        Senha = SenhaDemo;
        return EntrarAsync();
    }

    [RelayCommand]
    private Task FecharAsync() => navegacao.VoltarAsync();

    protected override void AoMudarCarregando() => EntrarCommand.NotifyCanExecuteChanged();
}
