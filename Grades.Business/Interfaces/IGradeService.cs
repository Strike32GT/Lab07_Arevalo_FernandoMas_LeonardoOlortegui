using Grades.Entities;

namespace Grades.Business.Interfaces;

// Contrato de las operaciones con notas.
public interface IGradeService
{
    // Límites expuestos para que la pantalla configure sus controles sin conocer el App.config.
    decimal MinScore { get; }
    decimal MaxScore { get; }
    decimal PassingScore { get; }

    // Calcula promedio y condición sin guardar nada: la pantalla lo usa como vista previa.
    // Así la regla vive solo en Negocio y no se duplica en la interfaz gráfica.
    (decimal Average, string Status) Preview(decimal score1, decimal score2, decimal score3);

    List<Grade> ListByStudent(int studentId);
    void Register(Grade grade);
    void Update(Grade grade);
    void Delete(int gradeId);
}
