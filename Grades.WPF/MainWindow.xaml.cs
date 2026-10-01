using System.Windows;
using Grades.WPF.Services;
using Grades.WPF.Views;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Grades.WPF;

// Ventana principal: solo arma el menú, las notificaciones y el tema. El trabajo lo hacen las páginas.
public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();

        UiServices.Initialize(SnackbarPresenter, RootDialogHost, RootNavigation);

        Loaded += (s, e) =>
        {
            RootNavigation.Navigate(typeof(StudentsPage));
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

    // El ícono muestra el tema al que se cambiará: luna en claro, sol en oscuro.
    private void UpdateThemeIcon()
    {
        bool isDark = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;
        btnTheme.Icon = new SymbolIcon(isDark ? SymbolRegular.WeatherSunny24 : SymbolRegular.WeatherMoon24);
    }
}
