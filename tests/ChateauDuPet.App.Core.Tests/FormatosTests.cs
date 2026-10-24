namespace ChateauDuPet.App.Core.Tests;

public class FormatosTests
{
    private static readonly DateTime Agora = new(2026, 10, 24, 10, 0, 0);

    [Theory]
    [InlineData(0, "hoje")]
    [InlineData(1, "ontem")]
    [InlineData(3, "há 3 dias")]
    [InlineData(8, "há 1 semana")]
    [InlineData(21, "há 3 semanas")]
    [InlineData(45, "há 1 mês")]
    [InlineData(90, "26/07/2026")]
    public void Tempo_relativo(int diasAtras, string esperado) =>
        Assert.Equal(esperado, Formatos.TempoRelativo(Agora.AddDays(-diasAtras), Agora));

    [Fact]
    public void Tempo_relativo_considera_o_dia_e_nao_as_horas() =>
        Assert.Equal("ontem", Formatos.TempoRelativo(new DateTime(2026, 10, 23, 23, 59, 0), Agora));

    [Fact]
    public void Periodo_em_aberto_e_fechado()
    {
        Assert.Equal("fev/2020 – atual", Formatos.Periodo(new DateOnly(2020, 2, 1), null));
        Assert.Equal("jan/2010 – jan/2020", Formatos.Periodo(new DateOnly(2010, 1, 15), new DateOnly(2020, 1, 15)));
        Assert.Equal("mar/2024 – em andamento", Formatos.Periodo(new DateOnly(2024, 3, 1), null, "em andamento"));
    }

    [Theory]
    [InlineData("Jucelino Silva", "JS")]
    [InlineData("caroline de souza cordeiro", "CC")]
    [InlineData("Ana", "A")]
    [InlineData("  ", "?")]
    [InlineData(null, "?")]
    public void Iniciais(string? nome, string esperado) => Assert.Equal(esperado, Formatos.Iniciais(nome));

    [Theory]
    [InlineData("11987654321", "(11) 98765-4321")]
    [InlineData("1133334444", "(11) 3333-4444")]
    [InlineData("123", "123")]
    public void Telefone(string numero, string esperado) => Assert.Equal(esperado, Formatos.Telefone(numero));

    [Fact]
    public void Quantidade_no_singular_e_plural()
    {
        Assert.Equal("1 vaga aberta", Formatos.Quantidade(1, "vaga aberta", "vagas abertas"));
        Assert.Equal("0 vagas abertas", Formatos.Quantidade(0, "vaga aberta", "vagas abertas"));
    }
}
