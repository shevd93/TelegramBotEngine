namespace TelegramBotEngine.Models
{
    public class FromUser
    {
        public Guid Id { get; set; } = Guid.Empty;
        public long ExternalId { get; set; } = 0;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;    
        public bool IsBot { get; set; } = false;
    }
}
