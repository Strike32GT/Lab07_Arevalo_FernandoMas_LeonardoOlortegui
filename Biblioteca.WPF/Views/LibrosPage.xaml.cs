using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF.Views;

// Capa de Presentacion de libros: solo recoge datos, llama a Negocio y
// muestra resultados. No hay SQL ni reglas aqui, y no se captura SqlException.
public partial class LibrosPage : Page
{
    private readonly LibroNegocio _librosNegocio = new LibroNegocio();

    // Espera un momento despues de cada tecla antes de buscar (evita una consulta por letra).
    private readonly DispatcherTimer _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };

    // Libro en edicion. null = se esta registrando uno nuevo.
    private Libro _editando;

    // Opciones del combo de autores, cargadas desde la capa de Negocio.
    private List<Autor> _autores = new List<Autor>();

    public LibrosPage()
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
            // Las consultas corren fuera del hilo de la ventana, asi no se congela.
            var lista = await Task.Run(() => _librosNegocio.Listar(texto, incluirInactivos: true));

            dgLibros.ItemsSource = lista;
            txtEmpty.Text = string.IsNullOrWhiteSpace(texto)
                ? "Todavia no hay libros. Registre el primero con el formulario."
                : $"Ningun libro coincide con \"{texto}\".";
            emptyState.Visibility = lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Si se estaba editando, se vuelve a marcar la fila en la lista recargada.
            if (_editando != null)
                dgLibros.SelectedItem = lista.FirstOrDefault(l => l.LibroId == _editando.LibroId);
        }, SetBusy);
    }

    // Los autores son la lista maestra del formulario: solo los activos.
    private async Task CargarAutoresAsync()
    {
        await UiServices.RunAsync(async () =>
        {
            _autores = await Task.Run(() => _librosNegocio.ListarAutores());
            cboAutores.ItemsSource = _autores;
            cboAutores.DisplayMemberPath = "Nombre";
        }, SetBusy);
    }

    private async Task GuardarAsync()
    {
        var autor = cboAutores.SelectedItem as Autor;
        if (autor == null && int.TryParse(cboAutores.Text?.Trim(), out int autorId))
            autor = _autores.FirstOrDefault(a => a.AutorId == autorId);

        var libro = new Libro
        {
            LibroId = _editando?.LibroId ?? 0,
            Titulo = txtTitulo.Text,
            ISBN = txtIsbn.Text,
            AutorId = autor?.AutorId ?? 0,
            Ejemplares = (int)(nbEjemplares.Value > 0 ? nbEjemplares.Value : 1),
            Activo = _editando?.Activo ?? true
        };
        bool esNuevo = libro.LibroId == 0;

        bool ok = await UiServices.RunAsync(() => Task.Run(() =>
        {
            if (esNuevo)
                _librosNegocio.Insertar(libro);
            else
                _librosNegocio.Actualizar(libro);
        }), SetBusy);

        if (!ok) return;

        UiServices.ShowSuccess(esNuevo
            ? $"\"{libro.Titulo}\" se registro correctamente."
            : $"\"{libro.Titulo}\" se actualizo correctamente.");

        // Tras registrar se limpia el formulario para seguir ingresando; tras editar se mantiene.
        if (esNuevo) LimpiarFormulario();
        await CargarAsync();
    }

    private async Task DarDeBajaAsync()
    {
        if (_editando == null) return;

        var libro = _editando;
        bool confirmado = await UiServices.ConfirmAsync(
            "Dar de baja",
            $"Desea dar de baja \"{libro.Titulo}\"?\nEl registro se conserva, pero ya no se puede prestar.",
            "Dar de baja");
        if (!confirmado) return;

        bool ok = await UiServices.RunAsync(
            () => Task.Run(() => _librosNegocio.DarDeBaja(libro.LibroId)), SetBusy);
        if (!ok) return;

        UiServices.ShowSuccess($"\"{libro.Titulo}\" quedo dado de baja.");
        LimpiarFormulario();
        await CargarAsync();
    }

    // Carga la fila seleccionada en el formulario (modo edicion).
    private void EditarLibro(Libro libro)
    {
        _editando = libro;
        txtTitulo.Text = libro.Titulo;
        txtIsbn.Text = libro.ISBN;
        nbEjemplares.Value = libro.Ejemplares;
        cboAutores.SelectedItem = _autores.FirstOrDefault(a => a.AutorId == libro.AutorId);
        cboAutores.Text = libro.NombreAutor;

        txtFormTitle.Text = "Editar libro";
        txtFormHint.Text = $"Editando \"{libro.Titulo}\". Pulse Esc para cancelar.";
        iconForm.Symbol = SymbolRegular.BookOpen24;
        ActualizarBotones();
    }

    private void LimpiarFormulario()
    {
        _editando = null;
        dgLibros.SelectedItem = null;
        txtTitulo.Clear();
        txtIsbn.Clear();
        nbEjemplares.Value = 1;
        cboAutores.Text = string.Empty;

        txtFormTitle.Text = "Nuevo libro";
        txtFormHint.Text = "Complete los datos y pulse Guardar.";
        iconForm.Symbol = SymbolRegular.BookAdd24;
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

    private async void btnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await CargarAutoresAsync();
        await CargarAsync();
    }

    private async void dgLibros_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgLibros.SelectedItem is Libro libro)
            EditarLibro(libro);

        await CargarAutoresAsync();
    }

    private async void btnNuevo_Click(object sender, RoutedEventArgs e)
    {
        LimpiarFormulario();
        await CargarAutoresAsync();
        txtTitulo.Focus();
    }

    private async void btnGuardar_Click(object sender, RoutedEventArgs e)
    {
        await CargarAutoresAsync();
        await GuardarAsync();
    }

    private async void btnDarDeBaja_Click(object sender, RoutedEventArgs e) => await DarDeBajaAsync();

    // Atajos de teclado: Ctrl+S guardar, Ctrl+N nuevo, Esc cancelar edicion.
    private async void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S && btnGuardar.IsEnabled)
        {
            e.Handled = true;
            await CargarAutoresAsync();
            await GuardarAsync();
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            e.Handled = true;
            LimpiarFormulario();
            await CargarAutoresAsync();
        }
        else if (e.Key == Key.Escape && _editando != null)
        {
            e.Handled = true;
            LimpiarFormulario();
        }
    }
}
