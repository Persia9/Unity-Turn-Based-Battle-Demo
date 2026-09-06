namespace Application.Messages
{
    public readonly struct PlaySeRequestedMessage
    {
        public string EventId { get; }

        public PlaySeRequestedMessage(string eventId)
        {
            EventId = eventId;
        }
    }
}