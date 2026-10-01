using Biblioteca.Negocio;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF.Services;

// Utilidades de interfaz compartidas por las paginas: notificaciones,
// confirmaciones y traduccion de excepciones a mensajes.
// Solo pertenece a la capa de Presentacion; no contiene reglas de negocio.
public static class UiServices
{
    private static readonly SnackbarService Snackbar = new SnackbarService();
    private static readonly ContentDialogService Dialogs = new ContentDialogService();

    public static NavigationView Navigation { get; private set; }

    // La ventana principal registra aqui sus contenedores al iniciar.
    public static void Initialize(SnackbarPresenter snackbarPresenter, ContentDialogHost dialogHost, NavigationView navigation)
    {
        Snackbar.SetSnackbarPresenter(snackbarPresenter);
        Dialogs.SetDialogHost(dialogHost);
        Navigation = navigation;
    }

    public static void ShowSuccess(string message) =>
        Snackbar.Show("Listo", message, ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));

    // Advertencia: se usa para las reglas de negocio (el usuario puede corregir el dato).
    public static void ShowWarning(string message) =>
        Snackbar.Show("Revise los datos", message, ControlAppearance.Caution,
            new SymbolIcon(SymbolRegular.Warning24), TimeSpan.FromSeconds(5));

    // Error inesperado (por ejemplo, no hay conexion con la base de datos).
    public static void ShowError(string message) =>
        Snackbar.Show("Ocurrio un problema", message, ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(6));

    public static async Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            PrimaryButtonAppearance = ControlAppearance.Danger,
            CloseButtonText = "Cancelar"
        };

        var result = await Dialogs.ShowAsync(dialog, CancellationToken.None);
        return result == ContentDialogResult.Primary;
    }

    // Ejecuta una operacion mostrando el indicador de carga y traduciendo las
    // excepciones a mensajes. Primero la excepcion de negocio (ReglaNegocioException),
    // luego cualquier otra. Devuelve true si la operacion termino sin errores.
    public static async Task<bool> RunAsync(Func<Task> action, Action<bool> setBusy)
    {
        try
        {
            setBusy(true);
            await action();
            return true;
        }
        catch (ReglaNegocioException ex)
        {
            ShowWarning(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            setBusy(false);
        }
        return false;
    }
}
