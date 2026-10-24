using ChateauDuPet.App.Core.Servicos;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace ChateauDuPet.App.Servicos;

public class PlataformaMaui : IPlataforma
{
    public Task MostrarAvisoAsync(string mensagem) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        // No Windows o Toast vira notificação do sistema, que exige o app instalado como pacote;
        // no app avulso (zip) o aviso aparece como um diálogo simples
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
            await Shell.Current.DisplayAlertAsync("Chateau Du Pet", mensagem, "OK");
        else
            await Toast.Make(mensagem, ToastDuration.Long).Show();
    });

    public Task<bool> ConfirmarAsync(string titulo, string mensagem, string aceitar, string cancelar) =>
        Shell.Current.DisplayAlertAsync(titulo, mensagem, aceitar, cancelar);

    public Task CompartilharAsync(string titulo, string texto) =>
        Share.Default.RequestAsync(new ShareTextRequest { Title = titulo, Text = texto });

    public async Task AbrirLinkAsync(string url)
    {
        try
        {
            var uri = new Uri(url);
            // Páginas abrem no navegador; e-mail (mailto:) e telefone (tel:) no app padrão do aparelho
            if (uri.Scheme is "http" or "https")
                await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            else
                await Launcher.Default.OpenAsync(uri);
        }
        catch (Exception)
        {
            await MostrarAvisoAsync("Nenhum aplicativo do aparelho consegue abrir este link.");
        }
    }
}
