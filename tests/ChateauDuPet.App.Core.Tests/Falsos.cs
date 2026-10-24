using System.Net;
using System.Text;
using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.Contracts;

namespace ChateauDuPet.App.Core.Tests;

// Implementações em memória das dependências dos ViewModels

public static class Dados
{
    public static readonly UsuarioInfo Profissional = new(1, "Jucelino Silva", "jucelino@exemplo.com", TipoUsuario.Profissional);
    public static readonly UsuarioInfo Empresa = new(1, "Salvadores do Bem", "felipe@exemplo.com", TipoUsuario.Empresa);

    public static VagaResumo Resumo(int id, string titulo = "Médico Veterinário") =>
        new(id, titulo, "Salvadores do Bem", "São Paulo", "CLT", new DateTime(2026, 10, 20), 2);

    public static VagaDetalhe Detalhe(int id = 1, bool? jaCandidatado = null, int candidatos = 2) =>
        new(id, "Médico Veterinário", "Salvadores do Bem", "São Paulo", "CLT", "1 ano", "Indiferente", "1 mês",
            "Cuidar dos bichos", new DateTime(2026, 10, 20), candidatos, jaCandidatado);

    public static LoginResponse Login(UsuarioInfo usuario, DateTimeOffset? expiraEm = null) =>
        new("token-" + usuario.Email, expiraEm ?? DateTimeOffset.UtcNow.AddDays(7), usuario);
}

public class ApiFalsa : IChateauApi
{
    public Func<string?, Task<IReadOnlyList<VagaResumo>>> AoListarVagas { get; set; } = _ => Task.FromResult<IReadOnlyList<VagaResumo>>([]);
    public Func<int, Task<VagaDetalhe>> AoObterVaga { get; set; } = id => Task.FromResult(Dados.Detalhe(id));
    public Func<string, string, Task<LoginResponse>> AoEntrar { get; set; } = (_, _) => throw new ApiException("E-mail ou senha inválidos.", 401);
    public Func<int, Task> AoCandidatar { get; set; } = _ => Task.CompletedTask;
    public Func<int, Task> AoCancelar { get; set; } = _ => Task.CompletedTask;
    public PerfilProfissional? Perfil { get; set; }
    public IReadOnlyList<CandidaturaResumo> Candidaturas { get; set; } = [];
    public IReadOnlyList<VagaResumo> MinhasVagas { get; set; } = [];
    public IReadOnlyList<CandidatoResumo> Candidatos { get; set; } = [];

    public List<string?> Buscas { get; } = [];
    public List<int> VagasConsultadas { get; } = [];

    public Task<LoginResponse> EntrarAsync(string email, string senha, CancellationToken ct = default) => AoEntrar(email, senha);

    public Task<IReadOnlyList<VagaResumo>> ListarVagasAsync(string? busca, CancellationToken ct = default)
    {
        Buscas.Add(busca);
        return AoListarVagas(busca);
    }

    public Task<VagaDetalhe> ObterVagaAsync(int id, CancellationToken ct = default)
    {
        VagasConsultadas.Add(id);
        return AoObterVaga(id);
    }

    public Task CandidatarAsync(int idVaga, CancellationToken ct = default) => AoCandidatar(idVaga);
    public Task CancelarCandidaturaAsync(int idVaga, CancellationToken ct = default) => AoCancelar(idVaga);
    public Task<IReadOnlyList<CandidaturaResumo>> MinhasCandidaturasAsync(CancellationToken ct = default) => Task.FromResult(Candidaturas);
    public Task<PerfilProfissional> MeuPerfilAsync(CancellationToken ct = default) => Task.FromResult(Perfil!);
    public Task<IReadOnlyList<VagaResumo>> MinhasVagasAsync(CancellationToken ct = default) => Task.FromResult(MinhasVagas);
    public Task<IReadOnlyList<CandidatoResumo>> CandidatosDaVagaAsync(int idVaga, CancellationToken ct = default) => Task.FromResult(Candidatos);

    // Gestão: registra o que foi enviado; AoPublicar/AoEditar/AoExcluir permitem simular falhas
    public List<VagaFormulario> Publicadas { get; } = [];
    public List<(int Id, VagaFormulario Vaga)> Editadas { get; } = [];
    public List<string> Exclusoes { get; } = [];
    public Func<Task> AoPublicar { get; set; } = () => Task.CompletedTask;
    public Func<string, Task> AoExcluir { get; set; } = _ => Task.CompletedTask;
    public Dictionary<int, PerfilProfissional> Curriculos { get; } = [];
    public IReadOnlyList<ProfissionalAdmin> ProfissionaisAdmin { get; set; } = [];
    public IReadOnlyList<EmpresaAdmin> EmpresasAdmin { get; set; } = [];

    public async Task<VagaDetalhe> PublicarVagaAsync(VagaFormulario vaga, CancellationToken ct = default)
    {
        await AoPublicar();
        Publicadas.Add(vaga);
        return Dados.Detalhe(99);
    }

    public Task<VagaDetalhe> EditarVagaAsync(int idVaga, VagaFormulario vaga, CancellationToken ct = default)
    {
        Editadas.Add((idVaga, vaga));
        return Task.FromResult(Dados.Detalhe(idVaga));
    }

    public Task ExcluirMinhaVagaAsync(int idVaga, CancellationToken ct = default) => Excluir($"minha-vaga/{idVaga}");

    public Task<PerfilProfissional> CurriculoAsync(int idProfissional, CancellationToken ct = default) =>
        Curriculos.TryGetValue(idProfissional, out var perfil)
            ? Task.FromResult(perfil)
            : throw new ApiException("Não encontramos o que você procurou. Talvez tenha sido removido.", 404);

    public Task<IReadOnlyList<ProfissionalAdmin>> ListarProfissionaisAsync(CancellationToken ct = default) => Task.FromResult(ProfissionaisAdmin);
    public Task<IReadOnlyList<EmpresaAdmin>> ListarEmpresasAsync(CancellationToken ct = default) => Task.FromResult(EmpresasAdmin);
    public Task ExcluirProfissionalAsync(int id, CancellationToken ct = default) => Excluir($"profissional/{id}");
    public Task ExcluirEmpresaAsync(int id, CancellationToken ct = default) => Excluir($"empresa/{id}");
    public Task ExcluirVagaComoAdminAsync(int idVaga, CancellationToken ct = default) => Excluir($"vaga/{idVaga}");

    private async Task Excluir(string alvo)
    {
        await AoExcluir(alvo);
        Exclusoes.Add(alvo);
    }
}

public class NavegacaoFalsa : INavegacao
{
    public List<string> Historico { get; } = [];

    public Task AbrirVagaAsync(int idVaga) => Registrar($"vaga/{idVaga}");
    public Task AbrirCandidatosAsync(VagaResumo vaga) => Registrar($"candidatos/{vaga.Id}");
    public Task AbrirLoginAsync() => Registrar("login");
    public Task VoltarAsync() => Registrar("voltar");
    public Task AbrirFormularioDeVagaAsync(int? idVaga) => Registrar(idVaga is null ? "vaga/nova" : $"vaga/{idVaga}/editar");
    public Task AbrirCurriculoAsync(int idProfissional) => Registrar($"curriculo/{idProfissional}");
    public Task AbrirAdministracaoAsync() => Registrar("administracao");

    private Task Registrar(string rota)
    {
        Historico.Add(rota);
        return Task.CompletedTask;
    }
}

public class PlataformaFalsa : IPlataforma
{
    public List<string> Avisos { get; } = [];
    public List<string> Links { get; } = [];
    public List<string> Compartilhados { get; } = [];
    public bool RespostaDaConfirmacao { get; set; } = true;
    public int Confirmacoes { get; private set; }

    public Task MostrarAvisoAsync(string mensagem)
    {
        Avisos.Add(mensagem);
        return Task.CompletedTask;
    }

    public Task<bool> ConfirmarAsync(string titulo, string mensagem, string aceitar, string cancelar)
    {
        Confirmacoes++;
        return Task.FromResult(RespostaDaConfirmacao);
    }

    public Task CompartilharAsync(string titulo, string texto)
    {
        Compartilhados.Add(texto);
        return Task.CompletedTask;
    }

    public Task AbrirLinkAsync(string url)
    {
        Links.Add(url);
        return Task.CompletedTask;
    }
}

public class ArmazenamentoEmMemoria : IArmazenamentoSeguro
{
    public Dictionary<string, string> Valores { get; } = [];

    public Task<string?> LerAsync(string chave) => Task.FromResult(Valores.GetValueOrDefault(chave));

    public Task GravarAsync(string chave, string valor)
    {
        Valores[chave] = valor;
        return Task.CompletedTask;
    }

    public void Remover(string chave) => Valores.Remove(chave);
}

/// <summary>Responde às requisições do HttpClient sem rede, registrando o que foi enviado.</summary>
public class HandlerFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requisicoes { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requisicoes.Add(request);
        return Task.FromResult(responder(request));
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
