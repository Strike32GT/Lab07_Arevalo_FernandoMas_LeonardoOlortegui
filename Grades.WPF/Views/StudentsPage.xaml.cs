using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Grades.Business;
using Grades.Business.Interfaces;
using Grades.Entities;
using Grades.WPF.Services;
using Wpf.Ui.Controls;

namespace Grades.WPF.Views;

// Capa de Presentación: solo recoge datos, llama a Negocio y muestra resultados.
// No hay SQL ni reglas aquí, y no se captura SqlException.
public partial class StudentsPage : Page
{
    // La pantalla conoce la interfaz; la clase concreta solo aparece al crearla.
    private readonly IStudentService _studentService = new StudentService();

    // Espera un momento después de cada tecla antes de buscar (evita una consulta por letra).
    private readonly DispatcherTimer _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };

    // Estudiante en edición. null = se está registrando uno nuevo.
    private Student _editing;

    public StudentsPage()
    {
        InitializeComponent();

        _searchTimer.Tick += async (s, e) =>
        {
            _searchTimer.Stop();
            await LoadAsync();
        };

        PreviewKeyDown += Page_PreviewKeyDown;
        Loaded += async (s, e) => await LoadAsync();
        ClearForm();
    }

    private async Task LoadAsync()
    {
        string text = txtSearch.Text;

        await UiServices.RunAsync(async () =>
        {
            // Las consultas corren fuera del hilo de la ventana, así no se congela.
            var (students, summary) = await Task.Run(() =>
                (_studentService.Search(text), _studentService.GetSummary()));

            dgStudents.ItemsSource = students;
            txtEmpty.Text = string.IsNullOrWhiteSpace(text)
                ? "No students yet. Register the first one using the form."
                : $"No students match \"{text}\".";
            emptyState.Visibility = students.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            txtTotal.Text = summary.TotalStudents.ToString();
            txtApproved.Text = summary.Approved.ToString();
            txtFailed.Text = summary.Failed.ToString();
            txtAverage.Text = summary.OverallAverage.ToString("0.00");

            // Si se estaba editando, se vuelve a marcar la fila en la lista recargada.
            if (_editing != null)
                dgStudents.SelectedItem = students.FirstOrDefault(s => s.StudentId == _editing.StudentId);
        }, SetBusy);
    }

    private async Task SaveAsync()
    {
        var student = new Student
        {
            StudentId = _editing?.StudentId ?? 0,
            Code = txtCode.Text,
            FirstName = txtFirstName.Text,
            LastName = txtLastName.Text,
            Email = txtEmail.Text
        };
        bool isNew = student.StudentId == 0;

        bool ok = await UiServices.RunAsync(() => Task.Run(() =>
        {
            if (isNew)
                _studentService.Register(student);
            else
                _studentService.Update(student);
        }), SetBusy);

        if (!ok) return;

        UiServices.ShowSuccess(isNew
            ? $"{student.FullName} was registered."
            : $"{student.FullName} was updated.");

        // Tras registrar se limpia el formulario para seguir ingresando; tras editar se mantiene.
        if (isNew) ClearForm();
        await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (_editing == null) return;

        var student = _editing;
        bool confirmed = await UiServices.ConfirmAsync(
            "Delete student",
            $"Do you want to delete {student.FullName} ({student.Code})?\nTheir grades will also be removed.",
            "Delete");
        if (!confirmed) return;

        bool ok = await UiServices.RunAsync(
            () => Task.Run(() => _studentService.Delete(student.StudentId)), SetBusy);
        if (!ok) return;

        UiServices.ShowSuccess($"{student.FullName} was deleted.");
        ClearForm();
        await LoadAsync();
    }

    // Carga la fila seleccionada en el formulario (modo edición).
    private void EditStudent(Student student)
    {
        _editing = student;
        txtCode.Text = student.Code;
        txtFirstName.Text = student.FirstName;
        txtLastName.Text = student.LastName;
        txtEmail.Text = student.Email;

        txtFormTitle.Text = "Edit student";
        txtFormHint.Text = $"Editing {student.Code}. Press Esc to cancel.";
        iconForm.Symbol = SymbolRegular.PersonEdit24;
        UpdateButtons();
    }

    private void ClearForm()
    {
        _editing = null;
        dgStudents.SelectedItem = null;
        txtCode.Clear();
        txtFirstName.Clear();
        txtLastName.Clear();
        txtEmail.Clear();

        txtFormTitle.Text = "New student";
        txtFormHint.Text = "Fill in the data and press Save.";
        iconForm.Symbol = SymbolRegular.PersonAdd24;
        UpdateButtons();
        txtCode.Focus();
    }

    private void OpenGrades()
    {
        if (_editing == null) return;

        GradesPage.RequestedStudentId = _editing.StudentId;
        UiServices.Navigation?.Navigate(typeof(GradesPage));
    }

    private void SetBusy(bool busy)
    {
        busyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        btnSave.IsEnabled = !busy;
        btnNew.IsEnabled = !busy;
        btnRefresh.IsEnabled = !busy;
        UpdateButtons(busy);
    }

    private void UpdateButtons(bool busy = false)
    {
        btnDelete.IsEnabled = !busy && _editing != null;
        btnViewGrades.IsEnabled = !busy && _editing != null;
    }

    // ----- Eventos de la pantalla -----

    private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private async void btnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private void dgStudents_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgStudents.SelectedItem is Student student)
            EditStudent(student);
    }

    private void dgStudents_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (dgStudents.SelectedItem is Student)
            OpenGrades();
    }

    private void btnNew_Click(object sender, RoutedEventArgs e) => ClearForm();

    private async void btnSave_Click(object sender, RoutedEventArgs e) => await SaveAsync();

    private async void btnDelete_Click(object sender, RoutedEventArgs e) => await DeleteAsync();

    private void btnViewGrades_Click(object sender, RoutedEventArgs e) => OpenGrades();

    // Atajos de teclado: Ctrl+S guardar, Ctrl+N nuevo, Esc cancelar edición.
    private async void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S && btnSave.IsEnabled)
        {
            e.Handled = true;
            await SaveAsync();
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            e.Handled = true;
            ClearForm();
        }
        else if (e.Key == Key.Escape && _editing != null)
        {
            e.Handled = true;
            ClearForm();
        }
    }
}
