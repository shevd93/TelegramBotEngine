namespace TelegramBotEngine.Models
{
    public class Bot
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public bool UsePulling { get; set; } = true;
        public string WebhookUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
    }
}