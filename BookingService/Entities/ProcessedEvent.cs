namespace BookingService.Entities
{
    public class ProcessedEvent
    {
        public Guid EventId { get; private set; }
        public DateTimeOffset ProcessedAt { get; private set; }

        public static ProcessedEvent Create(Guid eventId, DateTimeOffset processedAt)
        {
            if (eventId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(eventId));
            }

            return new ProcessedEvent
            {
                EventId = eventId,
                ProcessedAt = processedAt,
            };
        }
    }
}
