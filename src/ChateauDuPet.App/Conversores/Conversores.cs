using System.Globalization;
using ChateauDuPet.App.Core;
using ChateauDuPet.Contracts;

namespace ChateauDuPet.App.Conversores;

// Conversores só para exibição (OneWay); a lógica de formatação fica em Core/Formatos.cs, coberta por testes.

public abstract class ConversorDeExibicao<T> : IValueConverter
{
    protected abstract string Converter(T valor);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is T valor ? Converter(valor) : "";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Data de publicação → "há 3 dias". As datas vêm do banco em UTC.</summary>
public class TempoRelativoConverter : ConversorDeExibicao<DateTime>
{
    protected override string Converter(DateTime valor) => Formatos.TempoRelativo(valor, DateTime.UtcNow);
}

public class IniciaisConverter : ConversorDeExibicao<string>
{
    protected override string Converter(string valor) => Formatos.Iniciais(valor);
}

public class TelefoneConverter : ConversorDeExibicao<string>
{
    protected override string Converter(string valor) => Formatos.Telefone(valor);
}

/// <summary>Experiência ou certificado → "fev/2020 – atual".</summary>
public class PeriodoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ExperienciaInfo e => Formatos.Periodo(e.Inicio, e.Fim),
        CertificadoInfo c => Formatos.Periodo(c.Inicio, c.Conclusao, "em andamento"),
        _ => ""
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
