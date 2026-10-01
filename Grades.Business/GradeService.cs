using Grades.Business.Interfaces;
using Grades.Data;
using Grades.Data.Interfaces;
using Grades.Entities;

namespace Grades.Business;

// Capa de Negocio: aquí viven las reglas. No muestra mensajes ni escribe SQL.
public class GradeService : IGradeService
{
    public const string Approved = "Approved";
    public const string Failed = "Failed";

    private readonly IGradeRepository _grades;
    private readonly IStudentRepository _students;

    // Constructor por defecto: usa las implementaciones con SQL Server.
    public GradeService() : this(new GradeRepository(), new StudentRepository()) { }

    // Inyección manual: se puede pasar cualquier implementación de las interfaces (por ejemplo, en pruebas).
    public GradeService(IGradeRepository grades, IStudentRepository students)
    {
        _grades = grades;
        _students = students;
    }

    public decimal MinScore => BusinessSettings.MinScore;
    public decimal MaxScore => BusinessSettings.MaxScore;
    public decimal PassingScore => BusinessSettings.PassingScore;

    public (decimal Average, string Status) Preview(decimal score1, decimal score2, decimal score3)
    {
        decimal average = CalculateAverage(score1, score2, score3);
        return (average, GetStatus(average));
    }

    public List<Grade> ListByStudent(int studentId)
    {
        if (studentId <= 0)
            throw new BusinessRuleException("You must select a student.");

        var list = _grades.ListByStudent(studentId);
        foreach (var g in list)
        {
            g.Average = CalculateAverage(g.Score1, g.Score2, g.Score3);
            g.Status = GetStatus(g.Average);
        }
        return list;
    }

    public void Register(Grade grade)
    {
        Validate(grade);
        _grades.Insert(grade);
    }

    public void Update(Grade grade)
    {
        if (grade.GradeId <= 0)
            throw new BusinessRuleException("You must select a grade to update.");

        Validate(grade);
        _grades.Update(grade);
    }

    public void Delete(int gradeId)
    {
        if (gradeId <= 0)
            throw new BusinessRuleException("You must select a grade.");

        _grades.SoftDelete(gradeId);
    }

    // Métodos estáticos: los reutiliza StudentService para el promedio general.
    public static decimal CalculateAverage(decimal score1, decimal score2, decimal score3) =>
        Math.Round((score1 + score2 + score3) / 3m, 2);

    public static string GetStatus(decimal average) =>
        average >= BusinessSettings.PassingScore ? Approved : Failed;

    private void Validate(Grade grade)
    {
        if (grade.StudentId <= 0 || _students.GetById(grade.StudentId) == null)
            throw new BusinessRuleException("You must select a valid student.");

        grade.Course = grade.Course?.Trim();
        if (string.IsNullOrWhiteSpace(grade.Course))
            throw new BusinessRuleException("The course is required.");

        if (grade.Course.Length > 100)
            throw new BusinessRuleException("The course cannot exceed 100 characters.");

        ValidateScore(grade.Score1, "Score 1");
        ValidateScore(grade.Score2, "Score 2");
        ValidateScore(grade.Score3, "Score 3");

        if (_grades.ExistsCourse(grade.StudentId, grade.Course, grade.GradeId))
            throw new BusinessRuleException($"The student already has a grade for '{grade.Course}'.");
    }

    private static void ValidateScore(decimal score, string field)
    {
        if (score < BusinessSettings.MinScore || score > BusinessSettings.MaxScore)
            throw new BusinessRuleException(
                $"{field} must be between {BusinessSettings.MinScore} and {BusinessSettings.MaxScore}.");
    }
}
