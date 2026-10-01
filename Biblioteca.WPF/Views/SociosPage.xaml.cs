using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF.Views;

// Capa de Presentacion de socios: solo recoge datos, llama a Negocio y
// muestra resultados. No hay SQL ni reglas aqui, y no se captura SqlException.
public partial class SociosPage : Page
{
    private readonly SocioNegocio _sociosNegocio = new SocioNegocio();

    private readonly DispatcherTimer _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };

    // Socio en edicion. null = se esta registrando uno nuevo.
    private Socio _editando;

    public SociosPage()
    {
        InitializeComponent();

        _searchTimer.Tick += async (s, e) =>
        {
            _searchTimer.Stop();
            await CargarAsync();
        };

        PreviewKeyDown += Page_PreviewKeyDown;
        Loaded += async (s, e) => await CargarAsync();
        LimpiarFormulario();
    }

    private async Task CargarAsync()
    {
        string texto = txtSearch.Text;

        await UiServices.RunAsync(async () =>
        {
            var lista = await Task.Run(() => _sociosNegocio.Listar(texto, incluirInactivos: true));

            dgSocios.ItemsSource = lista;
            txtEmpty.Text = string.IsNullOrWhiteSpace(texto)
                ? "Todavia no hay socios. Registre el primero con el formulario."
                : $"Ningun socio coincide con \"{texto}\".";
            emptyState.Visibility = lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (_editando != null)
                dgSocios.SelectedItem = lista.FirstOrDefault(s => s.SocioId == _editando.SocioId);
        }, SetBusy);
    }

    private async Task GuardarAsync()
    {
        var socio = new Socio
        {
            SocioId = _editando?.SocioId ?? 0,
            DNI = txtDni.Text,
            Nombre = txtNombre.Text,
            Email = txtEmail.Text,
            Activo = _editando?.Activo ?? true
        };
        bool esNuevo = socio.SocioId == 0;

        bool ok = await UiServices.RunAsync(() => Task.Run(() =>
        {
            if (esNuevo)
                _sociosNegocio.Insertar(socio);
            else
                _sociosNegocio.Actualizar(socio);
        }), SetBusy);

        if (!ok) return;

        UiServices.ShowSuccess(esNuevo
            ? $"{socio.Nombre} se registro correctamente."
            : $"{socio.Nombre} se actualizo correctamente.");

        if (esNuevo) LimpiarFormulario();
        await CargarAsync();
    }

    private async Task DarDeBajaAsync()
    {
        if (_editando == null) return;

        var socio = _editando;
        bool confirmado = await UiServices.ConfirmAsync(
            "Dar de baja",
            $"Desea dar de baja a {socio.Nombre} ({socio.DNI})?\nEl registro se conserva, pero no podra volver a prestar.",
            "Dar de baja");
        if (!confirmado) return;

        bool ok = await UiServices.RunAsync(
            () => Task.Run(() => _sociosNegocio.DarDeBaja(socio.SocioId)), SetBusy);
        if (!ok) return;

        UiServices.ShowSuccess($"{socio.Nombre} quedo dado de baja.");
        LimpiarFormulario();
        await CargarAsync();
    }

    // Carga la fila seleccionada en el formulario (modo edicion).
    private void EditarSocio(Socio socio)
    {
        _editando = socio;
        txtDni.Text = socio.DNI;
        txtNombre.Text = socio.Nombre;
        txtEmail.Text = socio.Email;

        txtFormTitle.Text = "Editar socio";
        txtFormHint.Text = $"Editando a {socio.Nombre}. Pulse Esc para cancelar.";
        iconForm.Symbol = SymbolRegular.PersonEdit24;
        ActualizarBotones();
    }

    private void LimpiarFormulario()
    {
        _editando = null;
        dgSocios.SelectedItem = null;
        txtDni.Clear();
        txtNombre.Clear();
        txtEmail.Clear();

        txtFormTitle.Text = "Nuevo socio";
        txtFormHint.Text = "Complete los datos y pulse Guardar.";
        iconForm.Symbol = SymbolRegular.PersonAdd24;
        ActualizarBotones();
    }

    private void SetBusy(bool busy)
    {
        busyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        btnGuardar.IsEnabled = !busy;
        btnNuevo.IsEnabled = !busy;
        btnRefresh.IsEnabled = !busy;
        ActualizarBotones(busy);
    }

    private void ActualizarBotones(bool busy = false)
    {
        btnDarDeBaja.IsEnabled = !busy && _editando != null;
    }

    // ----- Eventos de la pantalla -----

    private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private async void btnRefresh_Click(object sender, RoutedEventArgs e) => await CargarAsync();

    private void dgSocios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgSocios.SelectedItem is Socio socio)
            EditarSocio(socio);
    }

    private void btnNuevo_Click(object sender, RoutedEventArgs e)
    {
        LimpiarFormulario();
        txtDni.Focus();
    }

    private async void btnGuardar_Click(object sender, RoutedEventArgs e) => await GuardarAsync();

    private async void btnDarDeBaja_Click(object sender, RoutedEventArgs e) => await DarDeBajaAsync();

    // Atajos de teclado: Ctrl+S guardar, Ctrl+N nuevo, Esc cancelar edicion.
    private async void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S && btnGuardar.IsEnabled)
        {
            e.Handled = true;
            await GuardarAsync();
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            e.Handled = true;
            LimpiarFormulario();
        }
        else if (e.Key == Key.Escape && _editando != null)
        {
            e.Handled = true;
            LimpiarFormulario();
        }
    }
}
