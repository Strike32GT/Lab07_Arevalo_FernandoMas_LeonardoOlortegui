namespace Grades.Entities;

// Capa de Entidades: solo propiedades. No sabe nada de SQL, de reglas ni de pantallas.
public class Student
{
    public int StudentId { get; set; }
    public string Code { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }

    // Propiedad calculada de solo lectura: útil para mostrar en pantalla.
    public string FullName => $"{FirstName} {LastName}";

    // Las calcula la capa de Negocio a partir de sus notas (no se guardan en la BD).
    public decimal OverallAverage { get; set; }
    public string Status { get; set; }
}
