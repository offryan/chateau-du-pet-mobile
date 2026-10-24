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

    protected override Window CreateWindow(IActivationState? activationState) => new(_shell);
}
