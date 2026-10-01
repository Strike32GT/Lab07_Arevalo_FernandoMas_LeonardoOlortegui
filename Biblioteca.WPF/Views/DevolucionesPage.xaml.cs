using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

// Capa de Presentacion de la devolucion: elige prestamo y libro, muestra la
// multa que devuelve Negocio y confirma. No calcula multas ni escribe SQL.
public partial class DevolucionesPage : Page
{
    private readonly PrestamoNegocio _prestamosNegocio = new PrestamoNegocio();

    // Prestamos pendientes que aun tienen algo por devolver.
    private List<Prestamo> _prestamos = new List<Prestamo>();

    public DevolucionesPage()
    {
        InitializeComponent();

        dpDevolucion.SelectedDate = DateTime.Today;
        PreviewKeyDown += Page_PreviewKeyDown;
        Loaded += async (s, e) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        await UiServices.RunAsync(async () =>
        {
            _prestamos = await Task.Run(() => _prestamosNegocio.ListarPendientes());
            cboPrestamos.ItemsSource = _prestamos;

            txtEmpty.Text = "No hay prestamos pendientes de devolucion.";
            emptyState.Visibility = _prestamos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (cboPrestamos.SelectedItem == null && _prestamos.Count > 0)
                cboPrestamos.SelectedIndex = 0;
        }, SetBusy);
    }

    // Al elegir el prestamo se cargan sus libros pendientes.
    private async void cboPrestamos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboPrestamos.SelectedItem is not Prestamo prestamo)
        {
            dgDetalles.ItemsSource = null;
            txtSocio.Text = "-";
            txtLimite.Text = "-";
            txtHint.Text = "Seleccione un prestamo.";
            btnDevolver.IsEnabled = false;
            return;
        }

        txtSocio.Text = $"{prestamo.NombreSocio} ({prestamo.DNI})";
        txtLimite.Text = prestamo.FechaLimite.ToString("dd/MM/yyyy");
        txtHint.Text = "Seleccione el libro que se devuelve.";

        await UiServices.RunAsync(async () =>
        {
            var detalles = await Task.Run(() => _prestamosNegocio.ListarDetalles(prestamo.PrestamoId));
            dgDetalles.ItemsSource = detalles;

            if (detalles.Count > 0)
                dgDetalles.SelectedIndex = 0;
        }, SetBusy);

        await ActualizarMultaAsync();
    }

    private async Task ActualizarMultaAsync()
    {
        var prestamo = cboPrestamos.SelectedItem as Prestamo;
        if (prestamo == null)
        {
            txtMulta.Text = "S/ 0.00";
            txtDias.Text = "0 dia(s) de retraso";
            return;
        }

        var fecha = dpDevolucion.SelectedDate ?? DateTime.Today;

        await UiServices.RunAsync(async () =>
        {
            // La multa la calcula PrestamoNegocio.CalcularMulta (regla de Negocio).
            var resultado = await Task.Run(() => _prestamosNegocio.CalcularMulta(prestamo.PrestamoId, fecha));

            txtMulta.Text = $"S/ {resultado.Multa:0.00}";
            txtDias.Text = resultado.DiasRetraso == 0
                ? "Devuelto dentro del plazo"
                : $"{resultado.DiasRetraso} dia(s) de retraso";
        }, SetBusy);
    }

    private async Task DevolverAsync()
    {
        if (cboPrestamos.SelectedItem is not Prestamo prestamo) return;
        if (dgDetalles.SelectedItem is not DetallePrestamo detalle) return;

        var fecha = dpDevolucion.SelectedDate ?? DateTime.Today;

        bool confirmado = await UiServices.ConfirmAsync(
            "Registrar devolucion",
            $"Devolver \"{detalle.Titulo}\" del prestamo {prestamo.PrestamoId}?\nFecha: {fecha:dd/MM/yyyy}",
            "Devolver");
        if (!confirmado) return;

        ResultadoDevolucion resultado = null;

        bool ok = await UiServices.RunAsync(() => Task.Run(() =>
        {
            resultado = _prestamosNegocio.Devolver(prestamo.PrestamoId, detalle.LibroId, fecha);
        }), SetBusy);

        if (!ok) return;

        string mensaje = resultado.DiasRetraso == 0
            ? $"\"{detalle.Titulo}\" se devolvio sin multa."
            : $"\"{detalle.Titulo}\" se devolvio con {resultado.DiasRetraso} dia(s) de retraso: multa de S/ {resultado.Multa:0.00}.";

        UiServices.ShowSuccess(mensaje);

        await CargarAsync();
    }

    private void SetBusy(bool busy)
    {
        busyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        btnDevolver.IsEnabled = !busy && dgDetalles.SelectedItem is DetallePrestamo;
        btnActualizar.IsEnabled = !busy;
    }

    // ----- Eventos de la pantalla -----

    private async void btnActualizar_Click(object sender, RoutedEventArgs e) => await CargarAsync();

    private async void dgDetalles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        btnDevolver.IsEnabled = dgDetalles.SelectedItem is DetallePrestamo;
        await ActualizarMultaAsync();
    }

    private async void dpDevolucion_SelectedDateChanged(object sender, SelectionChangedEventArgs e) =>
        await ActualizarMultaAsync();

    private async void btnDevolver_Click(object sender, RoutedEventArgs e) => await DevolverAsync();

    // Atajo de teclado: Ctrl+S confirma la devolucion.
    private async void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S && btnDevolver.IsEnabled)
        {
            e.Handled = true;
            await DevolverAsync();
        }
    }
}
