using ChateauDuPet.App.Core.Servicos;

namespace ChateauDuPet.App;

public partial class App : Application
{
    private readonly AppShell _shell;

    public App(AppShell shell, Sessao sessao)
    {
        InitializeComponent();
        _shell = shell;

        // Recupera o login salvo no aparelho; as telas se atualizam pelo evento Sessao.Alterada
        _ = sessao.RestaurarAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var janela = new Window(_shell) { Title = "Chateau Du Pet" };
        if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
        {
            // No PC, abre numa janela confortável para a gestão em vez do tamanho padrão do sistema
            janela.Width = 1100;
            janela.Height = 800;
            janela.MinimumWidth = 420;
            janela.MinimumHeight = 600;
        }
        return janela;
    }
}
