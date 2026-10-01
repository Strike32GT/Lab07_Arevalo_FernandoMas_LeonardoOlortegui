using Grades.Entities;

namespace Grades.Data.Interfaces;

// Contrato de acceso a datos de notas.
public interface IGradeRepository
{
    List<Grade> ListByStudent(int studentId);
    List<Grade> ListAll();
    bool ExistsCourse(int studentId, string course, int excludeGradeId);
    void Insert(Grade grade);
    void Update(Grade grade);
    void SoftDelete(int gradeId);
}
