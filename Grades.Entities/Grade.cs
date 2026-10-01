namespace Grades.Entities;

// Nota de un estudiante en un curso (tres evaluaciones).
public class Grade
{
    public int GradeId { get; set; }
    public int StudentId { get; set; }
    public string Course { get; set; }
    public decimal Score1 { get; set; }
    public decimal Score2 { get; set; }
    public decimal Score3 { get; set; }

    // Estas dos propiedades las calcula la capa de Negocio (no se guardan en la BD).
    public decimal Average { get; set; }
    public string Status { get; set; }
}
