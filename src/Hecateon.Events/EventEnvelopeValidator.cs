using System.Text;
using System.Text.Json;

namespace Hecateon.Events;

public static class EventEnvelopeValidator
{
    public static EventEnvelopeValidationResult Validate(
        EventEnvelope envelope,
        int payloadSizeLimitBytes = 262_144,
        bool requireServerAssignedFields = true)
    {
        var errors = new List<EventEnvelopeValidationError>();

        if (requireServerAssignedFields && string.IsNullOrWhiteSpace(envelope.EventId))
            errors.Add(new("event_id_required", "EventId is required."));

        if (string.IsNullOrWhiteSpace(envelope.UserId))
            errors.Add(new("user_id_required", "UserId is required."));

        if (string.IsNullOrWhiteSpace(envelope.DeviceId))
            errors.Add(new("device_id_required", "DeviceId is required."));

        if (string.IsNullOrWhiteSpace(envelope.Stream))
            errors.Add(new("stream_required", "Stream is required."));
        else if (!EventStream.All.Contains(envelope.Stream, StringComparer.OrdinalIgnoreCase))
            errors.Add(new("stream_invalid", $"Stream '{envelope.Stream}' is not supported."));

        if (string.IsNullOrWhiteSpace(envelope.Type))
            errors.Add(new("type_required", "Type is required."));

        if (envelope.SchemaVersion <= 0)
            errors.Add(new("schema_version_invalid", "SchemaVersion must be greater than zero."));

        if (string.IsNullOrWhiteSpace(envelope.ClientMsgId))
            errors.Add(new("client_msg_id_required", "ClientMsgId is required."));

        if (string.IsNullOrWhiteSpace(envelope.PayloadJson))
            errors.Add(new("payload_required", "PayloadJson is required."));
        else
        {
            var payloadBytes = Encoding.UTF8.GetByteCount(envelope.PayloadJson);
            if (payloadBytes > payloadSizeLimitBytes)
                errors.Add(new("payload_too_large", $"PayloadJson exceeds the {payloadSizeLimitBytes} byte limit."));

            try
            {
                _ = JsonDocument.Parse(envelope.PayloadJson);
            }
            catch (JsonException)
            {
                errors.Add(new("payload_invalid_json", "PayloadJson must be valid JSON."));
            }
        }

        if (!string.IsNullOrWhiteSpace(envelope.EventId) && !Guid.TryParse(envelope.EventId, out _))
            errors.Add(new("event_id_invalid", "EventId must be a valid UUID."));

        if (requireServerAssignedFields)
        {
            if (!envelope.Seq.HasValue || envelope.Seq.Value <= 0)
                errors.Add(new("seq_required", "Seq must be assigned by the server and greater than zero."));

            if (!envelope.TimestampUtc.HasValue)
                errors.Add(new("timestamp_required", "TimestampUtc must be assigned by the server."));
        }

        return new EventEnvelopeValidationResult { Errors = errors };
    }
}
