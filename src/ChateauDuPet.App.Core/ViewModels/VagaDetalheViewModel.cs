using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChateauDuPet.App.Core.ViewModels;

public partial class VagaDetalheViewModel(IChateauApi api, Sessao sessao, INavegacao navegacao, IPlataforma plataforma)
    : ViewModelBase, IAoAparecer
{
    private int _idVaga;
    // Usuário com que a vaga foi carregada: se mudar (login/logout), recarrega
    private (bool Carregada, int? IdUsuario) _carregadaPara;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PodeCandidatar), nameof(JaCandidatado), nameof(ConvidarParaEntrar),
        nameof(SomenteProfissionais), nameof(Candidatos))]
    public partial VagaDetalhe? Vaga { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CandidatarCommand), nameof(CancelarCandidaturaCommand))]
    public partial bool Enviando { get; set; }

    public bool PodeCandidatar => Vaga?.JaCandidatado == false;
    public bool JaCandidatado => Vaga?.JaCandidatado == true;
    public bool ConvidarParaEntrar => Vaga is not null && !sessao.Logado;
    public bool SomenteProfissionais => Vaga is not null && sessao.Logado && Vaga.JaCandidatado is null;

    public string Candidatos => Vaga is null ? "" : Formatos.Quantidade(Vaga.TotalCandidatos, "candidato", "candidatos");

    public void Abrir(int idVaga)
    {
        if (_idVaga == idVaga) return;
        _idVaga = idVaga;
        _carregadaPara = default;
    }

    public async Task AoAparecerAsync()
    {
        if (_carregadaPara != (true, sessao.Usuario?.Id)) await CarregarAsync();
    }

    public override async Task CarregarAsync()
    {
        var usuario = sessao.Usuario?.Id;
        if (await CarregarComIndicadorAsync(async () => Vaga = await api.ObterVagaAsync(_idVaga)))
            _carregadaPara = (true, usuario);
    }

    [RelayCommand(CanExecute = nameof(PodeEnviar))]
    private Task CandidatarAsync() => EnviarAsync(async vaga =>
    {
        await api.CandidatarAsync(vaga.Id);
        Vaga = vaga with { JaCandidatado = true, TotalCandidatos = vaga.TotalCandidatos + 1 };
        await plataforma.MostrarAvisoAsync("Candidatura enviada! Boa sorte 🐾");
    });

    [RelayCommand(CanExecute = nameof(PodeEnviar))]
    private Task CancelarCandidaturaAsync() => EnviarAsync(async vaga =>
    {
        if (!await plataforma.ConfirmarAsync("Cancelar candidatura",
                $"Você deixará de concorrer à vaga de {vaga.Titulo}.", "Cancelar candidatura", "Voltar"))
            return;

        await api.CancelarCandidaturaAsync(vaga.Id);
        Vaga = vaga with { JaCandidatado = false, TotalCandidatos = Math.Max(0, vaga.TotalCandidatos - 1) };
        await plataforma.MostrarAvisoAsync("Candidatura cancelada.");
    });

    [RelayCommand]
    private Task EntrarAsync() => navegacao.AbrirLoginAsync();

    [RelayCommand]
    private Task CompartilharAsync() => Vaga is null
        ? Task.CompletedTask
        : plataforma.CompartilharAsync(Vaga.Titulo,
            $"Vaga de {Vaga.Titulo} na {Vaga.Empresa}{(Vaga.Cidade is null ? "" : $" ({Vaga.Cidade})")}. " +
            "Veja no app Chateau Du Pet ou em https://chateau-du-pet-demo.onrender.com/Vagas");

    private bool PodeEnviar() => !Enviando;

    private async Task EnviarAsync(Func<VagaDetalhe, Task> acao)
    {
        if (Vaga is not { } vaga) return;
        Enviando = true;
        try
        {
            await acao(vaga);
        }
        catch (SessaoExpiradaException ex)
        {
            await plataforma.MostrarAvisoAsync(ex.Message);
            OnPropertyChanged(string.Empty);
            await navegacao.AbrirLoginAsync();
        }
        catch (ApiException ex)
        {
            await plataforma.MostrarAvisoAsync(ex.Message);
            // A situação mudou no servidor (ex.: já candidatado em outro aparelho): sincroniza
            if (ex.Status is 404 or 409) await CarregarAsync();
        }
        finally
        {
            Enviando = false;
        }
    }
}
