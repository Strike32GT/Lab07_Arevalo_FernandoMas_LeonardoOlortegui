using Grades.Entities;

namespace Grades.Business.Interfaces;

// Contrato que usa la capa de Presentación. La pantalla no conoce la clase concreta.
public interface IStudentService
{
    List<Student> List();
    List<Student> Search(string text);
    StudentSummary GetSummary();
    void Register(Student student);
    void Update(Student student);
    void Delete(int studentId);
}
