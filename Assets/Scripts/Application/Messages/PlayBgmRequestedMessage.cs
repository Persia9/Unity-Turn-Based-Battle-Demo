namespace Application.Messages
{
    public readonly struct PlayBgmRequestedMessage
    {
        public string EventId { get; }

        public PlayBgmRequestedMessage(string eventId)
        {
            EventId = eventId;
        }
    }
}