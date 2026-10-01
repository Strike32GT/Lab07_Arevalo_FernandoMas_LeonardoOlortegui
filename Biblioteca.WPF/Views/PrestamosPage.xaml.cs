using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF.Views;

// Capa de Presentacion del prestamo: arma la lista de libros y la envia a
// PrestamoNegocio. Todas las reglas (limite de 3, activos, ejemplares) se
// validan en Negocio; aca solo se recogen datos y se muestra el resultado.
public partial class PrestamosPage : Page
{
    private readonly PrestamoNegocio _prestamosNegocio = new PrestamoNegocio();
    private readonly LibroNegocio _librosNegocio = new LibroNegocio();
    private readonly SocioNegocio _sociosNegocio = new SocioNegocio();

    private readonly DispatcherTimer _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };

    // Socios que se pueden elegir: solo los activos (Negocio descarta el resto).
    private List<Socio> _socios = new List<Socio>();

    // Libros agregados al prestamo que se esta armando.
    private readonly List<Libro> _librosElegidos = new List<Libro>();

    public PrestamosPage()
    {
        InitializeComponent();

        _searchTimer.Tick += async (s, e) =>
        {
            _searchTimer.Stop();
            await CargarCatalogoAsync();
        };

        // nbDias se carga desde el codigo (no desde el XAML) para que su evento
        // ValueChanged no se dispare antes de que existan los demas controles.
        nbDias.Value = ParametrosNegocio.DiasPrestamo;
        dpFechaPrestamo.SelectedDate = DateTime.Today;
        txtLimite.Text = ParametrosNegocio.MaximoLibrosPendientes.ToString();
        ActualizarFechaLimite();

        PreviewKeyDown += Page_PreviewKeyDown;
        Loaded += async (s, e) =>
        {
            ActualizarFechaLimite();
            await CargarSociosAsync();
            await CargarCatalogoAsync();
        };
    }

    private async Task CargarSociosAsync()
    {
        await UiServices.RunAsync(async () =>
        {
            _socios = await Task.Run(() => _sociosNegocio.Listar(string.Empty, incluirInactivos: false));
            cboSocios.ItemsSource = _socios;
        }, SetBusy);
    }

    private async Task CargarCatalogoAsync()
    {
        string texto = txtBuscarLibro.Text;

        await UiServices.RunAsync(async () =>
        {
            var lista = await Task.Run(() => _librosNegocio.Listar(texto, incluirInactivos: false));

            // Oculta del catalogo lo que ya se agrego, para no duplicar libros en el prestamo.
            var disponibles = lista.Where(l => !_librosElegidos.Any(e => e.LibroId == l.LibroId)).ToList();
            dgCatalogo.ItemsSource = disponibles;

            txtEmptyCatalogo.Text = string.IsNullOrWhiteSpace(texto)
                ? "No hay libros activos en el catalogo."
                : $"Ningun libro coincide con \"{texto}\".";
            emptyCatalogo.Visibility = disponibles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }, SetBusyCatalogo);
    }

    private async Task AgregarLibroAsync()
    {
        if (dgCatalogo.SelectedItem is not Libro libro) return;

        if (_librosElegidos.Any(l => l.LibroId == libro.LibroId))
        {
            UiServices.ShowWarning($"\"{libro.Titulo}\" ya esta en la lista del prestamo.");
            return;
        }

        if (_librosElegidos.Count >= 3)
        {
            UiServices.ShowWarning("Un prestamo admite hasta 3 libros.");
            return;
        }

        _librosElegidos.Add(libro);
        RefrescarListaPrestamo();
        await CargarCatalogoAsync();
    }

    private async Task QuitarLibroAsync()
    {
        if (dgPrestamo.SelectedItem is not Libro libro) return;

        _librosElegidos.RemoveAll(l => l.LibroId == libro.LibroId);
        RefrescarListaPrestamo();
        await CargarCatalogoAsync();
    }

    private void RefrescarListaPrestamo()
    {
        dgPrestamo.ItemsSource = null;
        dgPrestamo.ItemsSource = _librosElegidos;
        txtHintLista.Text = _librosElegidos.Count == 1
            ? "1 libro agregado."
            : $"{_librosElegidos.Count} libros agregados.";
    }

    // Al elegir un socio se muestra cuantos libros pendientes ya tiene.
    // El dato ya viene en la entidad (lo calculo el repositorio), asi que
    // no hace falta una consulta nueva ni bloquear la ventana.
    private void cboSocios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        txtPendientes.Text = (cboSocios.SelectedItem is Socio socio)
            ? socio.LibrosPendientes.ToString()
            : "0";
    }

    private async Task RegistrarAsync()
    {
        var socio = cboSocios.SelectedItem as Socio;
        var fecha = dpFechaPrestamo.SelectedDate ?? DateTime.Today;
        int dias = (int)(nbDias.Value > 0 ? nbDias.Value : ParametrosNegocio.DiasPrestamo);
        var libros = _librosElegidos.Select(l => l.LibroId).ToList();

        int prestamoId = 0;
        bool ok = await UiServices.RunAsync(() => Task.Run(() =>
        {
            prestamoId = _prestamosNegocio.Registrar(socio?.SocioId ?? 0, libros, fecha, dias);
        }), SetBusy);

        if (!ok) return;

        UiServices.ShowSuccess($"El prestamo {prestamoId} se registro con {libros.Count} libro(s).");

        LimpiarLista();
        await CargarSociosAsync();
        await CargarCatalogoAsync();
    }

    private void LimpiarLista()
    {
        _librosElegidos.Clear();
        dgPrestamo.ItemsSource = null;
        dgPrestamo.ItemsSource = _librosElegidos;
        txtHintLista.Text = "0 libro(s) agregados.";
        cboSocios.SelectedItem = null;
        txtPendientes.Text = "0";
        ActualizarFechaLimite();
    }

    private void ActualizarFechaLimite()
    {
        // Guarda: durante InitializeComponent este evento puede dispararse antes
        // de que esten creados todos los controles.
        if (dpFechaPrestamo == null || txtFechaLimite == null || nbDias == null) return;

        var fecha = dpFechaPrestamo.SelectedDate ?? DateTime.Today;
        int dias = (int)(nbDias.Value > 0 ? nbDias.Value : ParametrosNegocio.DiasPrestamo);
        txtFechaLimite.Text = fecha.AddDays(dias).ToString("dd/MM/yyyy");
    }

    private void SetBusy(bool busy)
    {
        busyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        btnRegistrar.IsEnabled = !busy;
        btnAgregar.IsEnabled = !busy;
        btnQuitar.IsEnabled = !busy;
    }

    private void SetBusyCatalogo(bool busy) =>
        busyCatalogo.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;

    // ----- Eventos de la pantalla -----

    private void txtBuscarLibro_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private async void btnAgregar_Click(object sender, RoutedEventArgs e) => await AgregarLibroAsync();

    private async void btnQuitar_Click(object sender, RoutedEventArgs e) => await QuitarLibroAsync();

    private void nbDias_ValueChanged(object sender, NumberBoxValueChangedEventArgs e) =>
        ActualizarFechaLimite();

    private async void btnRegistrar_Click(object sender, RoutedEventArgs e) => await RegistrarAsync();

    // Atajo de teclado: Ctrl+S registra el prestamo.
    private async void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S && btnRegistrar.IsEnabled)
        {
            e.Handled = true;
            await RegistrarAsync();
        }
    }
}
