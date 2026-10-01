using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Grades.Business;
using Grades.Business.Interfaces;
using Grades.Entities;
using Grades.WPF.Services;
using Wpf.Ui.Controls;

namespace Grades.WPF.Views;

// Capa de Presentación para las notas: igual que StudentsPage, sin SQL ni reglas.
public partial class GradesPage : Page
{
    // Estudiante que se debe mostrar al llegar desde la página de estudiantes ("View grades").
    public static int RequestedStudentId { get; set; }

    private readonly IStudentService _studentService = new StudentService();
    private readonly IGradeService _gradeService = new GradeService();

    // Nota en edición. null = se está registrando una nueva.
    private Grade _editing;

    // Evita reaccionar al SelectionChanged mientras se recarga el combo por código.
    private bool _reloadingStudents;

    public GradesPage()
    {
        InitializeComponent();
        ConfigureScoreBoxes();

        PreviewKeyDown += Page_PreviewKeyDown;
        Loaded += async (s, e) =>
        {
            int requested = RequestedStudentId;
            RequestedStudentId = 0;
            await LoadStudentsAsync(requested);
        };
        ClearForm();
    }

    private Student SelectedStudent => cboStudents.SelectedItem as Student;

    // Los límites vienen de Negocio (que a su vez los lee del App.config).
    private void ConfigureScoreBoxes()
    {
        foreach (var box in new[] { nbScore1, nbScore2, nbScore3 })
        {
            box.Minimum = (double)_gradeService.MinScore;
            box.Maximum = (double)_gradeService.MaxScore;
        }

        txtRules.Text = $"Scores from {_gradeService.MinScore} to {_gradeService.MaxScore}. " +
                        $"Passing average: {_gradeService.PassingScore}.";
    }

    // Recarga el combo de estudiantes (sus promedios cambian al guardar notas) y luego sus notas.
    private async Task LoadStudentsAsync(int selectStudentId)
    {
        await UiServices.RunAsync(async () =>
        {
            var students = await Task.Run(() => _studentService.List());

            _reloadingStudents = true;
            cboStudents.ItemsSource = students;
            cboStudents.SelectedItem = students.FirstOrDefault(s => s.StudentId == selectStudentId)
                                       ?? students.FirstOrDefault();
            _reloadingStudents = false;
        }, SetBusy);

        UpdateStudentInfo();
        await LoadGradesAsync();
    }

    private async Task LoadGradesAsync()
    {
        var student = SelectedStudent;
        if (student == null)
        {
            dgGrades.ItemsSource = null;
            ShowEmpty("Select a student to see their grades.");
            return;
        }

        int studentId = student.StudentId;
        await UiServices.RunAsync(async () =>
        {
            var grades = await Task.Run(() => _gradeService.ListByStudent(studentId));

            dgGrades.ItemsSource = grades;
            txtCourseCount.Text = grades.Count.ToString();
            if (grades.Count == 0)
                ShowEmpty($"{student.FirstName} has no grades yet. Add the first one using the form.");
            else
                emptyState.Visibility = Visibility.Collapsed;

            // Si se estaba editando, se vuelve a marcar la fila en la lista recargada.
            if (_editing != null)
                dgGrades.SelectedItem = grades.FirstOrDefault(g => g.GradeId == _editing.GradeId);
        }, SetBusy);
    }

    private async Task SaveAsync()
    {
        var student = SelectedStudent;
        if (student == null)
        {
            UiServices.ShowWarning("Select a student first.");
            return;
        }

        // Convertir el texto a número es tarea de la pantalla; validar el rango es tarea de Negocio.
        if (!TryReadScore(nbScore1, out var score1) ||
            !TryReadScore(nbScore2, out var score2) ||
            !TryReadScore(nbScore3, out var score3))
        {
            UiServices.ShowWarning("Enter the three scores as numbers.");
            return;
        }

        var grade = new Grade
        {
            GradeId = _editing?.GradeId ?? 0,
            StudentId = student.StudentId,
            Course = txtCourse.Text,
            Score1 = score1,
            Score2 = score2,
            Score3 = score3
        };
        bool isNew = grade.GradeId == 0;

        bool ok = await UiServices.RunAsync(() => Task.Run(() =>
        {
            if (isNew)
                _gradeService.Register(grade);
            else
                _gradeService.Update(grade);
        }), SetBusy);

        if (!ok) return;

        UiServices.ShowSuccess(isNew
            ? $"Grade for {grade.Course} was registered."
            : $"Grade for {grade.Course} was updated.");

        if (isNew) ClearForm();
        await LoadStudentsAsync(student.StudentId);
    }

    private async Task DeleteAsync()
    {
        if (_editing == null) return;

        var grade = _editing;
        bool confirmed = await UiServices.ConfirmAsync(
            "Delete grade",
            $"Do you want to delete the grade for {grade.Course}?",
            "Delete");
        if (!confirmed) return;

        bool ok = await UiServices.RunAsync(
            () => Task.Run(() => _gradeService.Delete(grade.GradeId)), SetBusy);
        if (!ok) return;

        UiServices.ShowSuccess($"Grade for {grade.Course} was deleted.");
        ClearForm();
        await LoadStudentsAsync(grade.StudentId);
    }

    private void EditGrade(Grade grade)
    {
        _editing = grade;
        txtCourse.Text = grade.Course;
        nbScore1.Value = (double)grade.Score1;
        nbScore2.Value = (double)grade.Score2;
        nbScore3.Value = (double)grade.Score3;

        txtFormTitle.Text = "Edit grade";
        txtFormHint.Text = $"Editing {grade.Course}. Press Esc to cancel.";
        iconForm.Symbol = SymbolRegular.Edit24;
        UpdateButtons();
    }

    private void ClearForm()
    {
        _editing = null;
        dgGrades.SelectedItem = null;
        txtCourse.Clear();
        nbScore1.Value = null;
        nbScore2.Value = null;
        nbScore3.Value = null;

        txtFormTitle.Text = "New grade";
        txtFormHint.Text = "Enter the course and its three scores.";
        iconForm.Symbol = SymbolRegular.Add24;
        UpdateButtons();
        UpdatePreview();
        txtCourse.Focus();
    }

    // Muestra promedio y condición mientras se escribe, usando la regla de Negocio.
    private void UpdatePreview()
    {
        if (TryReadScore(nbScore1, out var s1) &&
            TryReadScore(nbScore2, out var s2) &&
            TryReadScore(nbScore3, out var s3))
        {
            var (average, status) = _gradeService.Preview(s1, s2, s3);
            txtPreviewAverage.Text = average.ToString("0.00");
            previewPanel.DataContext = new Grade { Average = average, Status = status };
            previewPill.Visibility = Visibility.Visible;
        }
        else
        {
            txtPreviewAverage.Text = "–";
            previewPill.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateStudentInfo()
    {
        var student = SelectedStudent;
        studentInfo.DataContext = student;
        studentInfo.Visibility = student == null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ShowEmpty(string message)
    {
        txtEmpty.Text = message;
        txtCourseCount.Text = "0";
        emptyState.Visibility = Visibility.Visible;
    }

    private void SetBusy(bool busy)
    {
        busyRing.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        btnSave.IsEnabled = !busy;
        btnNew.IsEnabled = !busy;
        cboStudents.IsEnabled = !busy;
        UpdateButtons(busy);
    }

    private void UpdateButtons(bool busy = false) =>
        btnDelete.IsEnabled = !busy && _editing != null;

    // Se lee el texto (no Value) para tomar también lo que aún se está escribiendo.
    // Se acepta coma o punto como separador decimal.
    private static bool TryReadScore(NumberBox box, out decimal score) =>
        decimal.TryParse(box.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out score) ||
        decimal.TryParse(box.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out score);

    // ----- Eventos de la pantalla -----

    private async void cboStudents_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_reloadingStudents || SelectedStudent == null) return;

        ClearForm();
        UpdateStudentInfo();
        await LoadGradesAsync();
    }

    private void dgGrades_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgGrades.SelectedItem is Grade grade)
            EditGrade(grade);
    }

    private void Score_TextChanged(object sender, TextChangedEventArgs e) => UpdatePreview();

    private void btnNew_Click(object sender, RoutedEventArgs e) => ClearForm();

    private async void btnSave_Click(object sender, RoutedEventArgs e) => await SaveAsync();

    private async void btnDelete_Click(object sender, RoutedEventArgs e) => await DeleteAsync();

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
