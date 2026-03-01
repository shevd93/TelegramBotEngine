namespace TelegramBotEngine.Models
{
    public class SongPerformer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;    
    }
}
