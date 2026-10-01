using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Grades.WPF;

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
    // El orden importa: los pinceles del tema toman los colores de acento al cargarse,
    // por eso primero se definen los colores y luego se carga el tema.
    public static void SetTheme(ApplicationTheme theme)
    {
        ApplyAccent(theme);
        ApplicationThemeManager.Apply(theme, WindowBackdropType.Mica, false);
        ApplyAccent(theme); // el color del texto sobre el acento depende del tema ya aplicado
    }

    // Color de acento propio (azul): los botones principales se ven igual en cualquier PC,
    // sin depender del color configurado en Windows. Cada tema usa sus propias variantes
    // para mantener buen contraste (oscuro en tema claro, claro en tema oscuro).
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
