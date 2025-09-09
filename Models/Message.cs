namespace TelegramBotEngine.Models
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.Empty;
        public int ExternalId { get; set; } = 0;
        public Guid FromUserId { get; set; }
        public FromUser? FromUser { get; set; }
        public DateTime Date { get; set; } = DateTime.MinValue;
        public Guid ChatId { get; set; }
        public Chat? Chat { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public bool Processed { get; set; } = false;    
    }
}
