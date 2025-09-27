namespace TelegramBotEngine.Models
{
    public class Video
    {
        public Guid Id { get; set; } = Guid.Empty;
        public  int Duration { get; set; } = 0;
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; } = 0;
        public string FileUniqueId { get; set; } = string.Empty;
        public int Height { get; set; } = 0;
        public int Width { get; set; } = 0;
        public string MimeType { get; set; } = string.Empty;
        public Guid MessageId { get; set; }
        public Message? Message { get; set; }
    }
}
