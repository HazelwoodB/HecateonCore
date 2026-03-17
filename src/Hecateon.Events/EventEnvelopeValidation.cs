namespace Hecateon.Events;

public sealed record EventEnvelopeValidationError(string Code, string Message);

public sealed record EventEnvelopeValidationResult
{
    public required IReadOnlyList<EventEnvelopeValidationError> Errors { get; init; }
    public bool IsValid => Errors.Count == 0;
}
