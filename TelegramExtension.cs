using System.Text.Json;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using TelegramBotEngine.Models;

namespace TelegramBotEngine
{
    public static class TelegramExtension
    {

        public static async Task SendMenu(long chatId, string code, string text, TelegramBotClient client)
        {
            if (string.IsNullOrEmpty(text))
            {
                text = "Menu";
            }

            var keyboard = JsonSerializer.Deserialize<InlineKeyboardMarkup>(code);

            await client.SendMessageAsync(
                chatId: chatId,
                text: text,
                replyMarkup: keyboard,
                parseMode: "HTML");

        }

        public static async Task SendMessage(long chatId, string text, TelegramBotClient client, int replyMessageId = 0)
        {
            await client.SendMessageAsync(
                chatId: chatId,
                text: text,
                replyParameters: replyMessageId != 0 ? new ReplyParameters { MessageId = replyMessageId} : null,
                parseMode: "HTML");
        }

        public static async Task SendQuizDeepSeek(long chatId, TelegramBotClient client, string quizTextJson)
        {
            var question = JsonSerializer.Deserialize<MusicQuizQuestion>(quizTextJson);
                
            var poll = new SendPollArgs(
                chatId,
                question.Question,
                [
                    new InputPollOption(question.Options[0]),
                    new InputPollOption(question.Options[1]),
                    new InputPollOption(question.Options[2]),
                    new InputPollOption(question.Options[3]),
                    new InputPollOption(question.Options[4])
                ]
            )
            {
                Type = "quiz",
                CorrectOptionId = question.AnswerIndex,
                IsAnonymous = false
            };

            await client.SendPollAsync( poll );
        }

        public static async Task SendQuiz(long chatId, TelegramBotClient client, MusicQuizQuestion musicQuiz)
        {
            var poll = new SendPollArgs(
                chatId,
                musicQuiz.Question,
                [
                    new InputPollOption(musicQuiz.Options[0]),
                    new InputPollOption(musicQuiz.Options[1]),
                    new InputPollOption(musicQuiz.Options[2]),
                    new InputPollOption(musicQuiz.Options[3]),
                    new InputPollOption(musicQuiz.Options[4])
                ]
            )
            {
                Type = "quiz",
                CorrectOptionId = musicQuiz.AnswerIndex,
                IsAnonymous = false
            };

            await client.SendPollAsync(poll);
        }

        public static async Task SendImage(long chatId, TelegramBotClient client, string url)
        {
            await client.SendPhotoAsync(
                chatId: chatId,
                photo: url,
                parseMode: "HTML");
        }
    }
}
