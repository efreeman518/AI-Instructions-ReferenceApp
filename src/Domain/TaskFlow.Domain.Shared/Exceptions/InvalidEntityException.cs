namespace TaskFlow.Domain.Shared.Exceptions;

/// <summary>Models invalid entity exception domain behavior and invariants.</summary>
public class InvalidEntityException : Exception
{
    /// <summary>Initializes invalid entity exception with required dependencies and default state.</summary>
    public InvalidEntityException(string message) : base(message)
    {
    }

    /// <summary>Initializes invalid entity exception with required dependencies and default state.</summary>
    public InvalidEntityException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
