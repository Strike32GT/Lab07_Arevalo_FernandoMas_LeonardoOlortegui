using System.Net.Mail;
using Grades.Business.Interfaces;
using Grades.Data;
using Grades.Data.Interfaces;
using Grades.Entities;

namespace Grades.Business;

// Capa de Negocio para estudiantes: valida, calcula y delega el acceso a datos en los repositorios.
public class StudentService : IStudentService
{
    public const string NoGrades = "No grades";
    private const int MaxCodeLength = 10;

    private readonly IStudentRepository _students;
    private readonly IGradeRepository _grades;

    // Constructor por defecto: usa las implementaciones con SQL Server.
    public StudentService() : this(new StudentRepository(), new GradeRepository()) { }

    // Inyección manual: Negocio depende de interfaces, no de clases concretas.
    public StudentService(IStudentRepository students, IGradeRepository grades)
    {
        _students = students;
        _grades = grades;
    }

    public List<Student> List() => WithAverages(_students.List());

    public List<Student> Search(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? List()
            : WithAverages(_students.Search(text.Trim()));

    // Resumen para el panel: solo cuentan como aprobados/desaprobados los que tienen notas.
    public StudentSummary GetSummary()
    {
        var list = List();
        var withGrades = list.Where(s => s.Status != NoGrades).ToList();

        return new StudentSummary
        {
            TotalStudents = list.Count,
            Approved = withGrades.Count(s => s.Status == GradeService.Approved),
            Failed = withGrades.Count(s => s.Status == GradeService.Failed),
            OverallAverage = withGrades.Count == 0
                ? 0
                : Math.Round(withGrades.Average(s => s.OverallAverage), 2)
        };
    }

    public void Register(Student student)
    {
        Validate(student);
        _students.Insert(student);
    }

    public void Update(Student student)
    {
        if (student.StudentId <= 0)
            throw new BusinessRuleException("You must select a student to update.");

        Validate(student);
        _students.Update(student);
    }

    public void Delete(int studentId)
    {
        if (studentId <= 0)
            throw new BusinessRuleException("You must select a student.");

        _students.SoftDelete(studentId);
    }

    // Promedio general = promedio de los promedios de cada curso.
    // Se traen todas las notas en una sola consulta (evita una consulta por estudiante).
    private List<Student> WithAverages(List<Student> students)
    {
        var gradesByStudent = _grades.ListAll().ToLookup(g => g.StudentId);

        foreach (var s in students)
        {
            var averages = gradesByStudent[s.StudentId]
                .Select(g => GradeService.CalculateAverage(g.Score1, g.Score2, g.Score3))
                .ToList();

            if (averages.Count == 0)
            {
                s.OverallAverage = 0;
                s.Status = NoGrades;
            }
            else
            {
                s.OverallAverage = Math.Round(averages.Average(), 2);
                s.Status = GradeService.GetStatus(s.OverallAverage);
            }
        }
        return students;
    }

    private void Validate(Student student)
    {
        // Normalizar antes de validar: quitar espacios y dejar el código en mayúsculas.
        student.Code = student.Code?.Trim().ToUpperInvariant();
        student.FirstName = student.FirstName?.Trim();
        student.LastName = student.LastName?.Trim();
        student.Email = string.IsNullOrWhiteSpace(student.Email) ? null : student.Email.Trim();

        if (string.IsNullOrWhiteSpace(student.Code))
            throw new BusinessRuleException("The student code is required.");

        if (student.Code.Length > MaxCodeLength)
            throw new BusinessRuleException($"The code cannot exceed {MaxCodeLength} characters.");

        if (string.IsNullOrWhiteSpace(student.FirstName))
            throw new BusinessRuleException("The first name is required.");

        if (string.IsNullOrWhiteSpace(student.LastName))
            throw new BusinessRuleException("The last name is required.");

        if (student.Email != null && !IsValidEmail(student.Email))
            throw new BusinessRuleException("The email format is not valid.");

        if (_students.ExistsCode(student.Code, student.StudentId))
            throw new BusinessRuleException($"The code '{student.Code}' is already registered.");
    }

    // MailAddress hace una validación básica del formato sin usar expresiones regulares.
    private static bool IsValidEmail(string email) =>
        MailAddress.TryCreate(email, out var address) && address.Address == email;
}
