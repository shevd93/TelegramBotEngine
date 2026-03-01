using System.Text.Json.Serialization;

namespace TelegramBotEngine.Models
{
    public class MusicQuizQuestion
    {
        [JsonPropertyName("QUESTION")]
        public string Question { get; set; } = string.Empty;

        [JsonPropertyName("OPTIONS")]
        public List<string> Options { get; set; }  = new List<string>();

        [JsonPropertyName("ANSWER_INDEX")]
        public int AnswerIndex { get; set; }

        [JsonPropertyName("FACT")]
        public string Fact { get; set; } = string.Empty;
    }
}