using System.Windows;
using Biblioteca.WPF.Services;
using Biblioteca.WPF.Views;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF;

// Ventana principal: solo arma el menu, las notificaciones y el tema.
// El trabajo lo hacen las paginas.
public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();

        UiServices.Initialize(SnackbarPresenter, RootDialogHost, RootNavigation);

        Loaded += (s, e) =>
        {
            RootNavigation.Navigate(typeof(LibrosPage));
            UpdateThemeIcon();
        };
    }

    private void btnTheme_Click(object sender, RoutedEventArgs e)
    {
        var next = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;

        App.SetTheme(next);
        UpdateThemeIcon();
    }

    // El icono muestra el tema al que se cambiara: luna en claro, sol en oscuro.
    private void UpdateThemeIcon()
    {
        bool isDark = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;
        btnTheme.Icon = new SymbolIcon(isDark ? SymbolRegular.WeatherSunny24 : SymbolRegular.WeatherMoon24);
    }
}
