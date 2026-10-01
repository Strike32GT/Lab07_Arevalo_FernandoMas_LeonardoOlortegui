namespace Grades.Entities;

// Resumen para las tarjetas del panel principal. Lo arma la capa de Negocio.
public class StudentSummary
{
    public int TotalStudents { get; set; }
    public int Approved { get; set; }
    public int Failed { get; set; }
    public decimal OverallAverage { get; set; }
}
