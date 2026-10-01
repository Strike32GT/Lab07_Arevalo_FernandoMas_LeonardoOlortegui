using Grades.Data.Interfaces;
using Grades.Entities;
using Microsoft.Data.SqlClient;

namespace Grades.Data;

// Capa de Datos para las notas. Mismo estilo que StudentRepository.
public class GradeRepository : IGradeRepository
{
    private const string SelectColumns =
        "SELECT GradeId, StudentId, Course, Score1, Score2, Score3 FROM Grades";

    public List<Grade> ListByStudent(int studentId)
    {
        const string sql = SelectColumns + " WHERE StudentId = @StudentId AND IsActive = 1 ORDER BY Course";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@StudentId", studentId);
        cn.Open();
        return ReadAll(cmd);
    }

    // Todas las notas activas: Negocio las usa para calcular los promedios generales.
    public List<Grade> ListAll()
    {
        const string sql = SelectColumns + " WHERE IsActive = 1";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cn.Open();
        return ReadAll(cmd);
    }

    public bool ExistsCourse(int studentId, string course, int excludeGradeId)
    {
        const string sql = @"SELECT COUNT(1) FROM Grades
                             WHERE StudentId = @StudentId AND Course = @Course
                               AND IsActive = 1 AND GradeId <> @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@StudentId", studentId);
        cmd.Parameters.AddWithValue("@Course", course);
        cmd.Parameters.AddWithValue("@Id", excludeGradeId);
        cn.Open();
        return (int)cmd.ExecuteScalar() > 0;
    }

    public void Insert(Grade grade)
    {
        const string sql = @"INSERT INTO Grades (StudentId, Course, Score1, Score2, Score3)
                             VALUES (@StudentId, @Course, @Score1, @Score2, @Score3)";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AddParameters(cmd, grade);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    public void Update(Grade grade)
    {
        const string sql = @"UPDATE Grades
                             SET Course = @Course, Score1 = @Score1, Score2 = @Score2, Score3 = @Score3
                             WHERE GradeId = @Id AND StudentId = @StudentId";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AddParameters(cmd, grade);
        cmd.Parameters.AddWithValue("@Id", grade.GradeId);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    // Eliminación lógica.
    public void SoftDelete(int gradeId)
    {
        const string sql = "UPDATE Grades SET IsActive = 0 WHERE GradeId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", gradeId);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    private static void AddParameters(SqlCommand cmd, Grade grade)
    {
        cmd.Parameters.AddWithValue("@StudentId", grade.StudentId);
        cmd.Parameters.AddWithValue("@Course", grade.Course);
        cmd.Parameters.AddWithValue("@Score1", grade.Score1);
        cmd.Parameters.AddWithValue("@Score2", grade.Score2);
        cmd.Parameters.AddWithValue("@Score3", grade.Score3);
    }

    private static List<Grade> ReadAll(SqlCommand cmd)
    {
        var list = new List<Grade>();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            list.Add(new Grade
            {
                GradeId = dr.GetInt32(0),
                StudentId = dr.GetInt32(1),
                Course = dr.GetString(2),
                Score1 = dr.GetDecimal(3),
                Score2 = dr.GetDecimal(4),
                Score3 = dr.GetDecimal(5)
            });
        }
        return list;
    }
}
