using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

// Capa de Presentacion del reporte: elige el intervalo de fechas y muestra
// lo que devuelve Negocio. El INNER JOIN de las cuatro tablas vive en Datos.
public partial class ReportePage : Page
{
    private readonly PrestamoNegocio _prestamosNegocio = new PrestamoNegocio();

    public ReportePage()
    {
        InitializeComponent();

        dpDesde.SelectedDate = DateTime.Today.AddMonths(-1);
        dpHasta.SelectedDate = DateTime.Today.AddMonths(1);

        PreviewKeyDown += Page_PreviewKeyDown;
        Loaded += async (s, e) => await ConsultarAsync();
    }

    private async Task ConsultarAsync()
    {
        var desde = dpDesde.SelectedDate ?? DateTime.Today.AddMonths(-1);
        var hasta = dpHasta.SelectedDate ?? DateTime.Today.AddMonths(1);

        await UiServices.RunAsync(async () =>
        {
            var lista = await Task.Run(() => _prestamosNegocio.ReportePorIntervalo(desde, hasta));

            dgReporte.ItemsSource = lista;
            txtTotal.Text = lista.Count == 1 ? "1 fila" : $"{lista.Count} filas";
            emptyState.Visibility = lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }, SetBusy);
    }

    private void SetBusy(bool busy)
    {
        busyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        btnConsultar.IsEnabled = !busy;
        btnLimpiar.IsEnabled = !busy;
    }

    // ----- Eventos de la pantalla -----

    private async void btnConsultar_Click(object sender, RoutedEventArgs e) => await ConsultarAsync();

    private async void btnLimpiar_Click(object sender, RoutedEventArgs e)
    {
        dpDesde.SelectedDate = DateTime.Today.AddMonths(-1);
        dpHasta.SelectedDate = DateTime.Today.AddMonths(1);
        await ConsultarAsync();
    }

    // Atajo de teclado: Enter consulta el reporte.
    private async void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && btnConsultar.IsEnabled)
        {
            e.Handled = true;
            await ConsultarAsync();
        }
    }
}
