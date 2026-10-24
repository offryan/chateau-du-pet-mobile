using ChateauDuPet.App.Core.Servicos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChateauDuPet.App.Core.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    /// <summary>Tempo até avisar que o servidor gratuito pode estar "acordando".</summary>
    public static TimeSpan TempoParaAvisoDeDemora { get; set; } = TimeSpan.FromSeconds(4);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarIndicador), nameof(MostrarConteudo))]
    public partial bool Carregando { get; set; }

    /// <summary>Ligado ao "puxar para atualizar": a lista continua visível enquanto recarrega.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarIndicador), nameof(MostrarConteudo))]
    public partial bool Atualizando { get; set; }

    /// <summary>A requisição está demorando: mostra o aviso de que o servidor pode estar iniciando.</summary>
    [ObservableProperty]
    public partial bool Demorando { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemErro), nameof(MostrarConteudo))]
    public partial string? Erro { get; set; }

    public bool TemErro => Erro is not null;

    /// <summary>Indicador de carregamento em tela cheia (no "puxar para atualizar" o RefreshView já mostra o dele).</summary>
    public bool MostrarIndicador => Carregando && !Atualizando;

    public bool MostrarConteudo => !MostrarIndicador && !TemErro;

    /// <summary>Busca os dados da tela. Telas sem dados remotos não sobrescrevem.</summary>
    public virtual Task CarregarAsync() => Task.CompletedTask;

    [RelayCommand]
    private Task TentarDeNovoAsync() => CarregarAsync();

    [RelayCommand]
    private async Task AtualizarAsync()
    {
        try
        {
            await CarregarAsync();
        }
        finally
        {
            Atualizando = false;
        }
    }

    partial void OnCarregandoChanged(bool value) => AoMudarCarregando();

    protected virtual void AoMudarCarregando()
    {
    }

    /// <summary>
    /// Roda uma operação de carregamento controlando os indicadores da tela.
    /// Falhas da API viram a mensagem de <see cref="Erro"/> em vez de exceção.
    /// </summary>
    protected async Task<bool> CarregarComIndicadorAsync(Func<Task> operacao)
    {
        Erro = null;
        Carregando = true;
        using var avisoDeDemora = new CancellationTokenSource();
        _ = AvisarSeDemorarAsync(avisoDeDemora.Token);
        try
        {
            await operacao();
            return true;
        }
        catch (ApiException ex)
        {
            Erro = ex.Message;
            return false;
        }
        finally
        {
            avisoDeDemora.Cancel();
            Carregando = false;
            Demorando = false;
        }
    }

    private async Task AvisarSeDemorarAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TempoParaAvisoDeDemora, ct);
            Demorando = true;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
