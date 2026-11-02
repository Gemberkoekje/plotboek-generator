namespace PlotboekGenerator.infrastructure;

/// <summary>
/// Thrown by <see cref="IDocumentSession.SaveChangesAsync"/> when a document staged with
/// <see cref="IDocumentSession.Insert{T}"/> collides with one that is already stored.
///
/// Deliberately store-agnostic: the Marten adapter translates the underlying unique-key violation into
/// this type so callers never have to reference Npgsql to recognise a duplicate. Callers are expected
/// to catch it and turn it into a user-facing message.
/// </summary>
public sealed class DocumentAlreadyExistsException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="DocumentAlreadyExistsException"/> class.</summary>
    public DocumentAlreadyExistsException()
        : base("A document with the same identity already exists.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DocumentAlreadyExistsException"/> class.</summary>
    /// <param name="message">The message that describes the error.</param>
    public DocumentAlreadyExistsException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DocumentAlreadyExistsException"/> class.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The underlying store exception.</param>
    public DocumentAlreadyExistsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
