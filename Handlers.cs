using TelegramBotEngine.Models;
using Telegram.BotAPI.GettingUpdates;

namespace TelegramBotEngine
{
    public static class Handlers
    {
        public static void UpdateHandler(IEnumerable<Update> updates, Bot bot, TelegramBotEngineDbContext db, ILogger _logger)
        {
            foreach (var update in updates)
            {
                _logger.LogInformation(update.Message.Text);    
            }
        }
    }
}


