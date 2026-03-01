using TelegramBotEngine.Models;

namespace TelegramBotEngine.Pages
{
    public class Lyrics
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public Guid PerformerId {  get; set; }
        public SongPerformer? Performer { get; set; }
    }
}
