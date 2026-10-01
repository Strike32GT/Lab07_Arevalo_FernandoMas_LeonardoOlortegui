using Grades.Data.Interfaces;
using Grades.Entities;
using Microsoft.Data.SqlClient;

namespace Grades.Data;

// Capa de Datos: solo habla con la base de datos. No valida reglas ni muestra mensajes.
// Consultas siempre parametrizadas (nunca concatenadas) para evitar inyección SQL.
public class StudentRepository : IStudentRepository
{
    private const string SelectColumns =
        "SELECT StudentId, Code, FirstName, LastName, Email FROM Students";

    public List<Student> List()
    {
        const string sql = SelectColumns + " WHERE IsActive = 1 ORDER BY LastName, FirstName";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cn.Open();
        return ReadAll(cmd);
    }

    public List<Student> Search(string text)
    {
        // El comodín % se agrega al valor del parámetro, no al texto del SQL.
        const string sql = SelectColumns + @"
                            WHERE IsActive = 1
                              AND (Code LIKE @Text OR FirstName LIKE @Text OR LastName LIKE @Text)
                            ORDER BY LastName, FirstName";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Text", $"%{text}%");
        cn.Open();
        return ReadAll(cmd);
    }

    public Student GetById(int studentId)
    {
        const string sql = SelectColumns + " WHERE StudentId = @Id AND IsActive = 1";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", studentId);
        cn.Open();
        return ReadAll(cmd).FirstOrDefault();
    }

    // Se excluye el propio registro para que, al editar, no choque consigo mismo.
    public bool ExistsCode(string code, int excludeStudentId)
    {
        const string sql = @"SELECT COUNT(1) FROM Students
                             WHERE Code = @Code AND StudentId <> @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Code", code);
        cmd.Parameters.AddWithValue("@Id", excludeStudentId);
        cn.Open();
        return (int)cmd.ExecuteScalar() > 0;
    }

    public void Insert(Student student)
    {
        const string sql = @"INSERT INTO Students (Code, FirstName, LastName, Email)
                             VALUES (@Code, @FirstName, @LastName, @Email)";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AddParameters(cmd, student);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    public void Update(Student student)
    {
        const string sql = @"UPDATE Students
                             SET Code = @Code, FirstName = @FirstName, LastName = @LastName, Email = @Email
                             WHERE StudentId = @Id";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        AddParameters(cmd, student);
        cmd.Parameters.AddWithValue("@Id", student.StudentId);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    // Eliminación lógica: nunca un DELETE físico. También se desactivan sus notas.
    public void SoftDelete(int studentId)
    {
        const string sql = @"UPDATE Grades   SET IsActive = 0 WHERE StudentId = @Id;
                             UPDATE Students SET IsActive = 0 WHERE StudentId = @Id;";

        using var cn = DbConnectionFactory.Create();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Id", studentId);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    private static void AddParameters(SqlCommand cmd, Student student)
    {
        cmd.Parameters.AddWithValue("@Code", student.Code);
        cmd.Parameters.AddWithValue("@FirstName", student.FirstName);
        cmd.Parameters.AddWithValue("@LastName", student.LastName);
        // Un null de C# no es un NULL de SQL: hay que enviar DBNull.Value.
        cmd.Parameters.AddWithValue("@Email", (object)student.Email ?? DBNull.Value);
    }

    private static List<Student> ReadAll(SqlCommand cmd)
    {
        var list = new List<Student>();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            list.Add(new Student
            {
                StudentId = dr.GetInt32(0),
                Code = dr.GetString(1),
                FirstName = dr.GetString(2),
                LastName = dr.GetString(3),
                Email = dr.IsDBNull(4) ? null : dr.GetString(4)
            });
        }
        return list;
    }
}
