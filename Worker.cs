using Telegram.BotAPI;
using Telegram.BotAPI.GettingUpdates;

namespace TelegramBotEngine
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TelegramBotEngineDbContext _db;

        public Worker(ILogger<Worker> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                var bots = _db.Bots.Where(b => b.IsActive && b.UsePulling).ToList();

                foreach (var bot in bots)
                {

                    var update = new List<Update>();

                    IEnumerable<Update> updates = update;

                    try
                    {
                        var telegramBotClient = new TelegramBotClient(bot.Token);

                        updates = await telegramBotClient.GetUpdatesAsync();

                        _logger.LogInformation("Bot {botId}: {bot.Name}. Updates received.", bot.Id, bot.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Bot {botId}: {bot.Name}. Error receiving updates.", bot.Id, bot.Name);
                    }

                    if (updates.Count() > 0)
                    {
                        Handlers.UpdateHandler(updates, bot, _db, _logger);
                    }
                }
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
