using System.Globalization;

namespace ChateauDuPet.App.Core;

/// <summary>Textos exibidos nas telas, em português do Brasil.</summary>
public static class Formatos
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>"hoje", "ontem", "há 3 dias", "há 2 semanas"... ou a data, se passou de dois meses.</summary>
    public static string TempoRelativo(DateTime data, DateTime agora)
    {
        var dias = (agora.Date - data.Date).Days;
        return dias switch
        {
            <= 0 => "hoje",
            1 => "ontem",
            < 7 => $"há {dias} dias",
            < 14 => "há 1 semana",
            < 30 => $"há {dias / 7} semanas",
            < 60 => "há 1 mês",
            _ => data.ToString("dd/MM/yyyy", PtBr)
        };
    }

    /// <summary>"fev/2020 – atual" ou "jan/2010 – jan/2020".</summary>
    public static string Periodo(DateOnly inicio, DateOnly? fim, string seAberto = "atual") =>
        $"{MesAno(inicio)} – {(fim is { } f ? MesAno(f) : seAberto)}";

    private static string MesAno(DateOnly data) => data.ToString("MMM'/'yyyy", PtBr).Replace(".", "");

    /// <summary>Até duas iniciais para o avatar: "Jucelino Silva" → "JS".</summary>
    public static string Iniciais(string? nome)
    {
        var partes = (nome ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..1].ToUpperInvariant(),
            _ => $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant()
        };
    }

    /// <summary>11987654321 → (11) 98765-4321</summary>
    public static string Telefone(string? numero) => numero switch
    {
        { Length: 11 } n => $"({n[..2]}) {n[2..7]}-{n[7..]}",
        { Length: 10 } n => $"({n[..2]}) {n[2..6]}-{n[6..]}",
        _ => numero ?? ""
    };

    public static string Quantidade(int total, string singular, string plural) =>
        total == 1 ? $"1 {singular}" : $"{total} {plural}";
}
