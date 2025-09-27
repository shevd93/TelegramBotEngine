namespace TelegramBotEngine.Models
{
    public class Photo
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string FileId { get; set; } = string.Empty;
        public string FileUniqueId { get; set; } = string.Empty;
        public int Width { get; set; } = 0;
        public int Height { get; set; } = 0;
        public long FileSize { get; set; } = 0;
        public Guid MessageId { get; set; }
        public Message? Message { get; set; }
    }
}
