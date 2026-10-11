namespace ShelfOps.Domain.Models
{
    public class AuditLog
    {
        public Guid Id { get; private set; }
        public Guid ActorUserId { get; private set; }
        public string Action { get; private set; } = string.Empty;
        public string EntityType { get; private set; } = string.Empty;
        public string EntityId { get; private set; } = string.Empty;
        public DateTime OccurredAtUtc { get; private set; }
        public string? CorrelationId { get; private set; }
        public string? DetailsJson { get; private set; }

        private AuditLog() { }

        public AuditLog(
            Guid actorUserId,
            string action,
            string entityType,
            string entityId,
            string? correlationId = null,
            string? detailsJson = null)
        {
            Id = Guid.NewGuid();
            ActorUserId = actorUserId;
            Action = action;
            EntityType = entityType;
            EntityId = entityId;
            OccurredAtUtc = DateTime.UtcNow;
            CorrelationId = correlationId;
            DetailsJson = detailsJson;
        }
    }
}