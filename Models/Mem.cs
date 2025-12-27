using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TelegramBotEngine.Models
{
    public class Mem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Url { get; set; } = string.Empty;
    }
}
