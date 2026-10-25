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
        new("Profissional", "candidato@demo.com", "Ver currículo e se candidatar"),
        new("Empresa", "empresa@demo.com", "Publicar vagas e ver candidatos"),
        new("Administrador", "admin@demo.com", "Gerenciar profissionais, empresas e vagas")
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
