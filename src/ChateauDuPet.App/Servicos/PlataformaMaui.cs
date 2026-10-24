using ChateauDuPet.App.Core.Servicos;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace ChateauDuPet.App.Servicos;

public class PlataformaMaui : IPlataforma
{
    public Task MostrarAvisoAsync(string mensagem) =>
        MainThread.InvokeOnMainThreadAsync(() => Toast.Make(mensagem, ToastDuration.Long).Show());

    public Task<bool> ConfirmarAsync(string titulo, string mensagem, string aceitar, string cancelar) =>
        Shell.Current.DisplayAlertAsync(titulo, mensagem, aceitar, cancelar);

    public Task CompartilharAsync(string titulo, string texto) =>
        Share.Default.RequestAsync(new ShareTextRequest { Title = titulo, Text = texto });

    public async Task AbrirLinkAsync(string url)
    {
        try
        {
            if (url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                await Launcher.Default.OpenAsync(new Uri(url));
            else
                await Browser.Default.OpenAsync(new Uri(url), BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception)
        {
            await MostrarAvisoAsync("Nenhum aplicativo do aparelho consegue abrir este link.");
        }
    }
}
