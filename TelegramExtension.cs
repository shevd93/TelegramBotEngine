using System.Text.Json;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;

namespace TelegramBotEngine
{
    public static class TelegramExtension
    {

        public static async Task SendMenu(long chatId, string code, TelegramBotClient client)
        {

            var keyboard = JsonSerializer.Deserialize<InlineKeyboardMarkup>(code);

            await client.SendMessageAsync(
                chatId: chatId,
                text: "Нажми на кнопку кожаный раб!",
                replyMarkup: keyboard,
                parseMode: "HTML");

        }           

    }
}
