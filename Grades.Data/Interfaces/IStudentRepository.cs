using Grades.Entities;

namespace Grades.Data.Interfaces;

// Contrato de acceso a datos de estudiantes.
// Negocio depende de esta interfaz, no de la clase concreta (se podría cambiar SQL Server por otro origen).
public interface IStudentRepository
{
    List<Student> List();
    List<Student> Search(string text);
    Student GetById(int studentId);
    bool ExistsCode(string code, int excludeStudentId);
    void Insert(Student student);
    void Update(Student student);
    void SoftDelete(int studentId);
}
