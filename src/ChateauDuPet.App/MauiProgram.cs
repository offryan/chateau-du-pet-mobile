using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.App.Core.ViewModels;
using ChateauDuPet.App.Servicos;
using ChateauDuPet.App.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

namespace ChateauDuPet.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var services = builder.Services;

        // Serviços de plataforma e sessão
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IArmazenamentoSeguro, ArmazenamentoSeguroMaui>();
        services.AddSingleton<INavegacao, NavegacaoShell>();
        services.AddSingleton<IPlataforma, PlataformaMaui>();
        services.AddSingleton<Sessao>();
        services.AddSingleton<AlteracoesDeDados>();

        // Cliente da API com novas tentativas automáticas. Os tempos são generosos porque
        // o plano gratuito do Render desliga a API ociosa e ela leva até ~1 min para voltar.
        services.AddHttpClient<IChateauApi, ChateauApi>(http =>
            {
                http.BaseAddress = Configuracao.ApiUrl;
                http.Timeout = Timeout.InfiniteTimeSpan; // quem controla é a política abaixo
            })
            .AddStandardResilienceHandler(opcoes =>
            {
                opcoes.AttemptTimeout.Timeout = TimeSpan.FromSeconds(45);
                opcoes.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(100);
                opcoes.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(90);
                opcoes.Retry.MaxRetryAttempts = 2;
                opcoes.Retry.DisableForUnsafeHttpMethods(); // não repete POST/DELETE
            });

        // Telas: as abas vivem o app todo; as demais são criadas a cada navegação
        services.AddSingleton<AppShell>();
        services.AddSingleton<VagasPage>().AddSingleton<VagasViewModel>();
        services.AddSingleton<MinhaAreaPage>().AddSingleton<MinhaAreaViewModel>();
        services.AddSingleton<SobrePage>().AddSingleton<SobreViewModel>();
        services.AddTransientWithShellRoute<VagaDetalhePage, VagaDetalheViewModel>(Rotas.Vaga);
        services.AddTransientWithShellRoute<CandidatosPage, CandidatosViewModel>(Rotas.Candidatos);
        services.AddTransientWithShellRoute<LoginPage, LoginViewModel>(Rotas.Login);

        // Gestão (empresa e administrador), as funções do antigo app desktop
        services.AddTransientWithShellRoute<VagaFormularioPage, VagaFormularioViewModel>(Rotas.FormularioDeVaga);
        services.AddTransientWithShellRoute<CurriculoPage, CurriculoViewModel>(Rotas.Curriculo);
        services.AddTransientWithShellRoute<AdministracaoPage, AdministracaoViewModel>(Rotas.Administracao);

        return builder.Build();
    }
}
