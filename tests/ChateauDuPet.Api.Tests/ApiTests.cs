using System.Net;
using System.Net.Http.Json;
using ChateauDuPet.Api.Dados;
using ChateauDuPet.Contracts;

namespace ChateauDuPet.Api.Tests;

[Collection(ColecaoDaApi.Nome)]
public class ApiTests(ApiFixture api) : IAsyncLifetime
{
    private const string Profissional = "jucelino@exemplo.com"; // candidato à vaga 1 no seed
    private const string Empresa = "felipe@exemplo.com";        // dona das vagas 1 e 2

    private static readonly ChateauJsonContext Json = ChateauJsonContext.Default;
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await api.RestaurarDadosAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Health_responde_ok()
    {
        var resposta = await api.CreateClient().GetAsync("/health", CT);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Lista_as_vagas_do_seed_com_empresa_e_total_de_candidatos()
    {
        var vagas = await api.CreateClient().GetFromJsonAsync("/api/vagas", Json.ListVagaResumo, CT);

        Assert.Equal(4, vagas!.Count);
        var veterinario = Assert.Single(vagas, v => v.Titulo == "Médico Veterinário");
        Assert.Equal("Salvadores do Bem", veterinario.Empresa);
        Assert.Equal("São Paulo", veterinario.Cidade);
        Assert.Equal(2, veterinario.TotalCandidatos);
    }

    [Theory]
    [InlineData("veterin", 1)]       // parte do título, sem diferenciar maiúsculas
    [InlineData("MedPet", 1)]        // nome fantasia da empresa
    [InlineData("são paulo", 4)]     // cidade
    [InlineData("%", 0)]             // curinga do ILIKE é buscado literalmente
    [InlineData("nada-parecido", 0)]
    public async Task Busca_filtra_por_titulo_descricao_empresa_e_cidade(string busca, int esperadas)
    {
        var vagas = await api.CreateClient().GetFromJsonAsync(
            $"/api/vagas?busca={Uri.EscapeDataString(busca)}", Json.ListVagaResumo, CT);
        Assert.Equal(esperadas, vagas!.Count);
    }

    [Fact]
    public async Task Detalhe_para_visitante_nao_informa_candidatura()
    {
        var vaga = await api.CreateClient().GetFromJsonAsync("/api/vagas/1", Json.VagaDetalhe, CT);

        Assert.Equal("Médico Veterinário", vaga!.Titulo);
        Assert.NotNull(vaga.Descricao);
        Assert.Null(vaga.JaCandidatado);
    }

    [Fact]
    public async Task Detalhe_para_profissional_informa_se_ja_se_candidatou()
    {
        var cliente = await api.ClienteLogadoAsync(Profissional);

        Assert.True((await cliente.GetFromJsonAsync("/api/vagas/1", Json.VagaDetalhe, CT))!.JaCandidatado);
        Assert.False((await cliente.GetFromJsonAsync("/api/vagas/2", Json.VagaDetalhe, CT))!.JaCandidatado);
    }

    [Fact]
    public async Task Detalhe_de_vaga_inexistente_retorna_404()
    {
        var resposta = await api.CreateClient().GetAsync("/api/vagas/999", CT);
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_valido_devolve_token_e_tipo_do_usuario()
    {
        var resposta = await api.CreateClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest("  JUCELINO@exemplo.com ", "demo1234"), Json.LoginRequest, CT);
        var login = await resposta.Content.ReadFromJsonAsync(Json.LoginResponse, CT);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.False(string.IsNullOrEmpty(login!.Token));
        Assert.Equal(TipoUsuario.Profissional, login.Usuario.Tipo);
        Assert.Equal("Jucelino Silva", login.Usuario.Nome);
        Assert.True(login.ExpiraEm > DateTimeOffset.UtcNow.AddDays(1));
    }

    [Theory]
    [InlineData("jucelino@exemplo.com", "senha-errada")]
    [InlineData("ninguem@exemplo.com", "demo1234")]
    public async Task Login_invalido_retorna_401_sem_revelar_se_o_email_existe(string email, string senha)
    {
        var resposta = await api.CreateClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, senha), Json.LoginRequest, CT);
        var problema = await resposta.Content.ReadFromJsonAsync(Json.ProblemaApi, CT);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("E-mail ou senha inválidos.", problema!.Detail);
    }

    [Fact]
    public async Task Token_adulterado_e_recusado()
    {
        var cliente = await api.ClienteLogadoAsync(Profissional);
        var token = cliente.DefaultRequestHeaders.Authorization!.Parameter!;
        cliente.DefaultRequestHeaders.Authorization = new("Bearer", token[..^4] + "AAAA");

        var resposta = await cliente.GetAsync("/api/me/candidaturas", CT);
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Profissional_se_candidata_e_cancela()
    {
        var cliente = await api.ClienteLogadoAsync(Profissional);

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsync("/api/vagas/2/candidatura", null, CT)).StatusCode);
        var vaga = await cliente.GetFromJsonAsync("/api/vagas/2", Json.VagaDetalhe, CT);
        Assert.True(vaga!.JaCandidatado);
        Assert.Equal(1, vaga.TotalCandidatos); // a vaga 2 começa sem candidatos no seed

        var candidaturas = await cliente.GetFromJsonAsync("/api/me/candidaturas", Json.ListCandidaturaResumo, CT);
        Assert.Equal([2, 1], candidaturas!.Select(c => c.IdVaga));

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync("/api/vagas/2/candidatura", CT)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.DeleteAsync("/api/vagas/2/candidatura", CT)).StatusCode);
    }

    [Fact]
    public async Task Candidatura_repetida_retorna_409_com_mensagem()
    {
        var cliente = await api.ClienteLogadoAsync(Profissional);

        var resposta = await cliente.PostAsync("/api/vagas/1/candidatura", null, CT);
        var problema = await resposta.Content.ReadFromJsonAsync(Json.ProblemaApi, CT);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal("Você já se candidatou a esta vaga.", problema!.Detail);
    }

    [Fact]
    public async Task Candidatura_em_vaga_inexistente_retorna_404()
    {
        var cliente = await api.ClienteLogadoAsync(Profissional);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PostAsync("/api/vagas/999/candidatura", null, CT)).StatusCode);
    }

    [Fact]
    public async Task Candidatura_exige_login_de_profissional()
    {
        var visitante = api.CreateClient();
        var empresa = await api.ClienteLogadoAsync(Empresa);

        Assert.Equal(HttpStatusCode.Unauthorized, (await visitante.PostAsync("/api/vagas/2/candidatura", null, CT)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await empresa.PostAsync("/api/vagas/2/candidatura", null, CT)).StatusCode);
    }

    [Fact]
    public async Task Perfil_traz_dados_experiencias_e_certificados_do_proprio_profissional()
    {
        var cliente = await api.ClienteLogadoAsync(Profissional);

        var perfil = await cliente.GetFromJsonAsync("/api/me/perfil", Json.PerfilProfissional, CT);

        Assert.Equal("Jucelino Silva", perfil!.Nome);
        Assert.Equal("Superior Completo", perfil.Formacao);
        Assert.NotEmpty(perfil.Experiencias);
        Assert.NotEmpty(perfil.Certificados);
        // Ordenadas da mais recente para a mais antiga; experiência atual sem data de fim
        Assert.Null(perfil.Experiencias[0].Fim);
        Assert.True(perfil.Experiencias[0].Inicio > perfil.Experiencias[^1].Inicio);
    }

    [Fact]
    public async Task Empresa_ve_apenas_as_proprias_vagas()
    {
        var cliente = await api.ClienteLogadoAsync(Empresa);

        var vagas = await cliente.GetFromJsonAsync("/api/me/vagas", Json.ListVagaResumo, CT);

        Assert.Equal(["Zootecnista", "Médico Veterinário"], vagas!.Select(v => v.Titulo).Order().Reverse());
        Assert.All(vagas!, v => Assert.Equal("Salvadores do Bem", v.Empresa));
    }

    [Fact]
    public async Task Empresa_ve_candidatos_da_propria_vaga_mas_nao_de_outras()
    {
        var cliente = await api.ClienteLogadoAsync(Empresa);

        var candidatos = await cliente.GetFromJsonAsync("/api/me/vagas/1/candidatos", Json.ListCandidatoResumo, CT);
        Assert.Equal(["Jucelino Silva", "Armando Pereira"], candidatos!.Select(c => c.Nome));

        // Vaga 3 é de outra empresa
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/me/vagas/3/candidatos", CT)).StatusCode);
    }

    [Fact]
    public async Task Rotas_de_empresa_e_profissional_nao_se_misturam()
    {
        var profissional = await api.ClienteLogadoAsync(Profissional);
        var empresa = await api.ClienteLogadoAsync(Empresa);

        Assert.Equal(HttpStatusCode.Forbidden, (await profissional.GetAsync("/api/me/vagas", CT)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await empresa.GetAsync("/api/me/perfil", CT)).StatusCode);
    }

    [Fact]
    public async Task Eu_devolve_o_usuario_do_token()
    {
        var cliente = await api.ClienteLogadoAsync(Empresa);

        var eu = await cliente.GetFromJsonAsync("/api/auth/eu", Json.UsuarioInfo, CT);

        Assert.Equal(new UsuarioInfo(1, "Salvadores do Bem", Empresa, TipoUsuario.Empresa), eu);
    }

    [Fact]
    public async Task Documento_OpenAPI_exige_token_so_nas_rotas_protegidas()
    {
        using var documento = System.Text.Json.JsonDocument.Parse(
            await api.CreateClient().GetStringAsync("/openapi/v1.json", CT));
        var raiz = documento.RootElement;
        bool ExigeToken(string rota, string metodo) =>
            raiz.GetProperty("paths").GetProperty(rota).GetProperty(metodo).TryGetProperty("security", out _);

        Assert.Equal("Chateau Du Pet API", raiz.GetProperty("info").GetProperty("title").GetString());
        Assert.False(raiz.TryGetProperty("security", out _)); // nada é exigido no documento inteiro
        Assert.Equal("bearer", raiz.GetProperty("components").GetProperty("securitySchemes")
            .GetProperty("Bearer").GetProperty("scheme").GetString());

        Assert.False(ExigeToken("/api/auth/login", "post"));
        Assert.False(ExigeToken("/api/vagas", "get"));
        Assert.False(ExigeToken("/api/vagas/{id}", "get"));
        Assert.True(ExigeToken("/api/vagas/{id}/candidatura", "post"));
        Assert.True(ExigeToken("/api/me/candidaturas", "get"));
        Assert.True(ExigeToken("/api/me/vagas/{id}/candidatos", "get"));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData(" tosa ", "tosa")]
    [InlineData("100%_off", @"100\%\_off")]
    public void Termo_de_busca_e_normalizado_e_escapado(string? busca, string? esperado) =>
        Assert.Equal(esperado, VagasRepositorio.TermoDeBusca(busca));
}
