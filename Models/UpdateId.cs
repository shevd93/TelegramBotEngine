using System.ComponentModel.DataAnnotations;

namespace TelegramBotEngine.Models
{
    public class UpdateId
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BotId { get; set; }
        public Bot? Bot { get; set; }
        public int LastUpdateId { get; set; } = 0;
    }
}
