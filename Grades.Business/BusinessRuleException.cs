namespace Grades.Business;

// Excepción propia: la capa de presentación solo conoce esta, nunca SqlException.
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
