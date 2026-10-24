using System.Net;
using ChateauDuPet.App.Core.Servicos;
using ChateauDuPet.Contracts;

namespace ChateauDuPet.App.Core.Tests;

public class SessaoTests
{
    private readonly ArmazenamentoEmMemoria _armazenamento = new();

    private Sessao NovaSessao() => new(_armazenamento, TimeProvider.System);

    [Fact]
    public async Task Login_fica_salvo_e_e_restaurado_na_proxima_abertura()
    {
        var alteracoes = 0;
        var sessao = NovaSessao();
        sessao.Alterada += (_, _) => alteracoes++;

        await sessao.IniciarAsync(Dados.Login(Dados.Profissional));
        var reaberta = NovaSessao();
        await reaberta.RestaurarAsync();

        Assert.Equal(1, alteracoes);
        Assert.True(reaberta.Logado);
        Assert.Equal(Dados.Profissional, reaberta.Usuario);
        Assert.Equal("token-jucelino@exemplo.com", reaberta.Token);
    }

    [Fact]
    public async Task Token_vencido_e_descartado_ao_restaurar()
    {
        await NovaSessao().IniciarAsync(Dados.Login(Dados.Profissional, DateTimeOffset.UtcNow.AddMinutes(-1)));

        var sessao = NovaSessao();
        await sessao.RestaurarAsync();

        Assert.False(sessao.Logado);
        Assert.Empty(_armazenamento.Valores);
    }

    [Fact]
    public async Task Conteudo_corrompido_e_descartado_ao_restaurar()
    {
        _armazenamento.Valores["chateau.sessao"] = "{ isto não é json";

        var sessao = NovaSessao();
        await sessao.RestaurarAsync();

        Assert.False(sessao.Logado);
        Assert.Empty(_armazenamento.Valores);
    }

    [Fact]
    public async Task Encerrar_apaga_o_login_do_aparelho()
    {
        var sessao = NovaSessao();
        await sessao.IniciarAsync(Dados.Login(Dados.Empresa));

        await sessao.EncerrarAsync();

        Assert.False(sessao.Logado);
        Assert.Null(sessao.Token);
        Assert.Empty(_armazenamento.Valores);
    }
}

public class ChateauApiTests
{
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    private readonly Sessao _sessao = new(new ArmazenamentoEmMemoria(), TimeProvider.System);

    private ChateauApi Criar(HandlerFalso handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.teste/") }, _sessao);

    [Fact]
    public async Task Lista_vagas_com_busca_codificada_na_url()
    {
        var handler = new HandlerFalso(_ => HandlerFalso.Json(HttpStatusCode.OK,
            """[{"id":1,"titulo":"Tosador","empresa":"MedPet","cidade":null,"tipoRegistro":"PJ","publicadaEm":"2026-10-20T10:00:00","totalCandidatos":3}]"""));

        var vagas = await Criar(handler).ListarVagasAsync(" banho & tosa ", CT);

        Assert.Equal("https://api.teste/api/vagas?busca=banho%20%26%20tosa", handler.Requisicoes[0].RequestUri!.AbsoluteUri);
        Assert.Equal(new VagaResumo(1, "Tosador", "MedPet", null, "PJ", new DateTime(2026, 10, 20, 10, 0, 0), 3), Assert.Single(vagas));
    }

    [Fact]
    public async Task Envia_o_token_quando_ha_sessao_mas_nao_no_login()
    {
        await _sessao.IniciarAsync(Dados.Login(Dados.Profissional));
        var handler = new HandlerFalso(r => r.RequestUri!.AbsolutePath.EndsWith("login")
            ? HandlerFalso.Json(HttpStatusCode.Unauthorized, """{"detail":"E-mail ou senha inválidos."}""")
            : new HttpResponseMessage(HttpStatusCode.NoContent));
        var api = Criar(handler);

        await api.CandidatarAsync(7, CT);
        await Assert.ThrowsAsync<ApiException>(() => api.EntrarAsync("x@x.com", "errada", CT));

        Assert.Equal("Bearer token-jucelino@exemplo.com", handler.Requisicoes[0].Headers.Authorization!.ToString());
        Assert.Null(handler.Requisicoes[1].Headers.Authorization);
        Assert.True(_sessao.Logado); // senha errada no login não derruba a sessão atual
    }

    [Fact]
    public async Task Token_recusado_encerra_a_sessao()
    {
        await _sessao.IniciarAsync(Dados.Login(Dados.Profissional));
        var api = Criar(new HandlerFalso(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        await Assert.ThrowsAsync<SessaoExpiradaException>(() => api.MinhasCandidaturasAsync(CT));
        Assert.False(_sessao.Logado);
    }

    [Fact]
    public async Task Mensagem_do_ProblemDetails_chega_ao_usuario()
    {
        var api = Criar(new HandlerFalso(_ => HandlerFalso.Json(HttpStatusCode.Conflict,
            """{"title":"Conflict","status":409,"detail":"Você já se candidatou a esta vaga."}""")));

        var erro = await Assert.ThrowsAsync<ApiException>(() => api.CandidatarAsync(1, CT));

        Assert.Equal("Você já se candidatou a esta vaga.", erro.Message);
        Assert.Equal(409, erro.Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "Não encontramos o que você procurou. Talvez tenha sido removido.")]
    [InlineData(HttpStatusCode.TooManyRequests, "Muitas tentativas seguidas. Aguarde um minuto e tente de novo.")]
    [InlineData(HttpStatusCode.BadGateway, "O servidor teve um problema. Tente de novo em instantes.")]
    public async Task Status_sem_corpo_recebe_mensagem_padrao(HttpStatusCode status, string mensagem)
    {
        var api = Criar(new HandlerFalso(_ => new HttpResponseMessage(status)));

        var erro = await Assert.ThrowsAsync<ApiException>(() => api.ObterVagaAsync(1, CT));

        Assert.Equal(mensagem, erro.Message);
    }

    [Fact]
    public async Task Falha_de_rede_vira_erro_de_conexao()
    {
        var api = Criar(new HandlerFalso(_ => throw new HttpRequestException("sem rede")));

        var erro = await Assert.ThrowsAsync<ApiException>(() => api.ListarVagasAsync(null, CT));

        Assert.True(erro.SemConexao);
        Assert.StartsWith("Não foi possível conectar", erro.Message);
    }

    [Fact]
    public async Task Cancelamento_pedido_por_quem_chamou_nao_vira_erro_de_conexao()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var api = Criar(new HandlerFalso(_ => throw new TaskCanceledException()));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => api.ListarVagasAsync(null, cts.Token));
    }

    [Fact]
    public async Task Json_invalido_vira_erro_amigavel()
    {
        var api = Criar(new HandlerFalso(_ => HandlerFalso.Json(HttpStatusCode.OK, "<html>manutenção</html>")));

        var erro = await Assert.ThrowsAsync<ApiException>(() => api.ObterVagaAsync(1, CT));

        Assert.Equal("O servidor devolveu uma resposta inesperada.", erro.Message);
    }
}
