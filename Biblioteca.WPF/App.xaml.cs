using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Arranca con el mismo tema (claro/oscuro) que tiene Windows, pero con nuestro acento.
        ApplicationThemeManager.ApplySystemTheme(false);
        SetTheme(ApplicationThemeManager.GetAppTheme());
    }

    // Cambia el tema aplicando nuestro color de acento.
    public static void SetTheme(ApplicationTheme theme)
    {
        ApplyAccent(theme);
        ApplicationThemeManager.Apply(theme, WindowBackdropType.Mica, false);
        ApplyAccent(theme);
    }

    // Color de acento propio (azul), igual en cualquier PC.
    private static void ApplyAccent(ApplicationTheme theme)
    {
        if (theme == ApplicationTheme.Dark)
            ApplicationAccentColorManager.Apply(Rgb(0x25, 0x63, 0xEB),
                Rgb(0x60, 0xA5, 0xFA), Rgb(0x93, 0xC5, 0xFD), Rgb(0xBF, 0xDB, 0xFE));
        else
            ApplicationAccentColorManager.Apply(Rgb(0x25, 0x63, 0xEB),
                Rgb(0x1D, 0x4E, 0xD8), Rgb(0x1E, 0x40, 0xAF), Rgb(0x1E, 0x3A, 0x8A));
    }

    private static Color Rgb(byte r, byte g, byte b) => Color.FromRgb(r, g, b);
}
