namespace TelegramBotEngine.Models
{
    public class ToxicUser
    {
        public Guid Id { get; set; } = Guid.Empty;
        public Guid FromUserId { get; set; }
        public FromUser? FromUser { get; set; }
        public Guid ChatId { get; set; }
        public Chat? Chat { get; set; }
        public float Score { get; set; } = 0; 
    }
}
