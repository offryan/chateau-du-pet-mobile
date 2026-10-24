using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ChateauDuPet.Contracts;

namespace ChateauDuPet.App.Core.Servicos;

public interface IChateauApi
{
    Task<LoginResponse> EntrarAsync(string email, string senha, CancellationToken ct = default);
    Task<IReadOnlyList<VagaResumo>> ListarVagasAsync(string? busca, CancellationToken ct = default);
    Task<VagaDetalhe> ObterVagaAsync(int id, CancellationToken ct = default);
    Task CandidatarAsync(int idVaga, CancellationToken ct = default);
    Task CancelarCandidaturaAsync(int idVaga, CancellationToken ct = default);
    Task<IReadOnlyList<CandidaturaResumo>> MinhasCandidaturasAsync(CancellationToken ct = default);
    Task<PerfilProfissional> MeuPerfilAsync(CancellationToken ct = default);
    Task<IReadOnlyList<VagaResumo>> MinhasVagasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CandidatoResumo>> CandidatosDaVagaAsync(int idVaga, CancellationToken ct = default);

    // Gestão (empresa)
    Task<VagaDetalhe> PublicarVagaAsync(VagaFormulario vaga, CancellationToken ct = default);
    Task<VagaDetalhe> EditarVagaAsync(int idVaga, VagaFormulario vaga, CancellationToken ct = default);
    Task ExcluirMinhaVagaAsync(int idVaga, CancellationToken ct = default);
    Task<PerfilProfissional> CurriculoAsync(int idProfissional, CancellationToken ct = default);

    // Gestão (administrador)
    Task<IReadOnlyList<ProfissionalAdmin>> ListarProfissionaisAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EmpresaAdmin>> ListarEmpresasAsync(CancellationToken ct = default);
    Task ExcluirProfissionalAsync(int id, CancellationToken ct = default);
    Task ExcluirEmpresaAsync(int id, CancellationToken ct = default);
    Task ExcluirVagaComoAdminAsync(int idVaga, CancellationToken ct = default);
}

/// <summary>
/// Cliente HTTP da API. Converte qualquer falha em <see cref="ApiException"/> com
/// mensagem em português, para as telas só precisarem tratar um tipo de erro.
/// </summary>
public class ChateauApi(HttpClient http, Sessao sessao) : IChateauApi
{
    private static readonly ChateauJsonContext Json = ChateauJsonContext.Default;

    public async Task<LoginResponse> EntrarAsync(string email, string senha, CancellationToken ct = default)
    {
        using var resposta = await EnviarAsync(HttpMethod.Post, "api/auth/login", ct,
            JsonContent.Create(new LoginRequest(email, senha), Json.LoginRequest), autenticar: false);
        return await LerAsync(resposta, Json.LoginResponse, ct);
    }

    public async Task<IReadOnlyList<VagaResumo>> ListarVagasAsync(string? busca, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(busca) ? "api/vagas" : $"api/vagas?busca={Uri.EscapeDataString(busca.Trim())}";
        return await ObterAsync(url, Json.ListVagaResumo, ct);
    }

    public Task<VagaDetalhe> ObterVagaAsync(int id, CancellationToken ct = default) =>
        ObterAsync($"api/vagas/{id}", Json.VagaDetalhe, ct);

    public async Task CandidatarAsync(int idVaga, CancellationToken ct = default)
    {
        using var _ = await EnviarAsync(HttpMethod.Post, $"api/vagas/{idVaga}/candidatura", ct);
    }

    public async Task CancelarCandidaturaAsync(int idVaga, CancellationToken ct = default)
    {
        using var _ = await EnviarAsync(HttpMethod.Delete, $"api/vagas/{idVaga}/candidatura", ct);
    }

    public async Task<IReadOnlyList<CandidaturaResumo>> MinhasCandidaturasAsync(CancellationToken ct = default) =>
        await ObterAsync("api/me/candidaturas", Json.ListCandidaturaResumo, ct);

    public Task<PerfilProfissional> MeuPerfilAsync(CancellationToken ct = default) =>
        ObterAsync("api/me/perfil", Json.PerfilProfissional, ct);

    public async Task<IReadOnlyList<VagaResumo>> MinhasVagasAsync(CancellationToken ct = default) =>
        await ObterAsync("api/me/vagas", Json.ListVagaResumo, ct);

    public async Task<IReadOnlyList<CandidatoResumo>> CandidatosDaVagaAsync(int idVaga, CancellationToken ct = default) =>
        await ObterAsync($"api/me/vagas/{idVaga}/candidatos", Json.ListCandidatoResumo, ct);

    public async Task<VagaDetalhe> PublicarVagaAsync(VagaFormulario vaga, CancellationToken ct = default)
    {
        using var resposta = await EnviarAsync(HttpMethod.Post, "api/me/vagas", ct, JsonContent.Create(vaga, Json.VagaFormulario));
        return await LerAsync(resposta, Json.VagaDetalhe, ct);
    }

    public async Task<VagaDetalhe> EditarVagaAsync(int idVaga, VagaFormulario vaga, CancellationToken ct = default)
    {
        using var resposta = await EnviarAsync(HttpMethod.Put, $"api/me/vagas/{idVaga}", ct, JsonContent.Create(vaga, Json.VagaFormulario));
        return await LerAsync(resposta, Json.VagaDetalhe, ct);
    }

    public async Task ExcluirMinhaVagaAsync(int idVaga, CancellationToken ct = default)
    {
        using var _ = await EnviarAsync(HttpMethod.Delete, $"api/me/vagas/{idVaga}", ct);
    }

    public Task<PerfilProfissional> CurriculoAsync(int idProfissional, CancellationToken ct = default) =>
        ObterAsync($"api/profissionais/{idProfissional}/curriculo", Json.PerfilProfissional, ct);

    public async Task<IReadOnlyList<ProfissionalAdmin>> ListarProfissionaisAsync(CancellationToken ct = default) =>
        await ObterAsync("api/admin/profissionais", Json.ListProfissionalAdmin, ct);

    public async Task<IReadOnlyList<EmpresaAdmin>> ListarEmpresasAsync(CancellationToken ct = default) =>
        await ObterAsync("api/admin/empresas", Json.ListEmpresaAdmin, ct);

    public async Task ExcluirProfissionalAsync(int id, CancellationToken ct = default)
    {
        using var _ = await EnviarAsync(HttpMethod.Delete, $"api/admin/profissionais/{id}", ct);
    }

    public async Task ExcluirEmpresaAsync(int id, CancellationToken ct = default)
    {
        using var _ = await EnviarAsync(HttpMethod.Delete, $"api/admin/empresas/{id}", ct);
    }

    public async Task ExcluirVagaComoAdminAsync(int idVaga, CancellationToken ct = default)
    {
        using var _ = await EnviarAsync(HttpMethod.Delete, $"api/admin/vagas/{idVaga}", ct);
    }

    private async Task<T> ObterAsync<T>(string url, JsonTypeInfo<T> tipo, CancellationToken ct)
    {
        using var resposta = await EnviarAsync(HttpMethod.Get, url, ct);
        return await LerAsync(resposta, tipo, ct);
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string url, CancellationToken ct,
        HttpContent? corpo = null, bool autenticar = true)
    {
        using var requisicao = new HttpRequestMessage(metodo, url) { Content = corpo };
        var token = autenticar ? sessao.Token : null;
        if (token is not null)
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage resposta;
        try
        {
            resposta = await http.SendAsync(requisicao, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Sem rede, DNS, timeout ou circuito aberto pela política de resiliência
            // (as exceções do Polly não herdam de HttpRequestException)
            throw new ApiException("Não foi possível conectar ao servidor. Confira sua internet e tente de novo.", causa: ex);
        }

        if (resposta.IsSuccessStatusCode) return resposta;

        using (resposta)
        {
            if (resposta.StatusCode == HttpStatusCode.Unauthorized && token is not null)
            {
                await sessao.EncerrarAsync();
                throw new SessaoExpiradaException();
            }

            throw new ApiException(await MensagemDeErroAsync(resposta, ct), (int)resposta.StatusCode);
        }
    }

    private static async Task<T> LerAsync<T>(HttpResponseMessage resposta, JsonTypeInfo<T> tipo, CancellationToken ct)
    {
        try
        {
            return await resposta.Content.ReadFromJsonAsync(tipo, ct)
                ?? throw new ApiException("O servidor devolveu uma resposta vazia.", (int)resposta.StatusCode);
        }
        catch (JsonException ex)
        {
            throw new ApiException("O servidor devolveu uma resposta inesperada.", (int)resposta.StatusCode, ex);
        }
    }

    private static async Task<string> MensagemDeErroAsync(HttpResponseMessage resposta, CancellationToken ct)
    {
        try
        {
            var problema = await resposta.Content.ReadFromJsonAsync(Json.ProblemaApi, ct);
            if (!string.IsNullOrWhiteSpace(problema?.Detail)) return problema.Detail;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Corpo vazio ou que não é ProblemDetails: usa a mensagem padrão do status
        }

        return resposta.StatusCode switch
        {
            HttpStatusCode.NotFound => "Não encontramos o que você procurou. Talvez tenha sido removido.",
            HttpStatusCode.Forbidden => "Sua conta não tem acesso a esta área.",
            HttpStatusCode.TooManyRequests => "Muitas tentativas seguidas. Aguarde um minuto e tente de novo.",
            >= HttpStatusCode.InternalServerError => "O servidor teve um problema. Tente de novo em instantes.",
            _ => "Não foi possível concluir a operação."
        };
    }
}
