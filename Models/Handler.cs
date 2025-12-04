namespace TelegramBotEngine.Models
{
    public class Handler
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public Guid BotId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
        public string? Text { get; set; } = string.Empty;    

        public static string[] HandlersTypeNames()
        {
            return new string[] { "Menu", "Quiz", "RequestToAI" };
        }
    }
}
