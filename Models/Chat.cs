namespace TelegramBotEngine.Models
{
    public class Chat
    {
        public Guid Id { get; set; } = Guid.Empty;
        public long ExternalId { get; set; } = 0;
        public Guid BotId { get; set; }
        public Bot? Bot { get; set; } 
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
