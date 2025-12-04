using System.Text.Json;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;

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

    }
}
