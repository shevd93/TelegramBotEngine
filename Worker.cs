using TelegramBotEngine.Models;
using Telegram.BotAPI;
using Telegram.BotAPI.GettingUpdates;
using Microsoft.EntityFrameworkCore;

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

                var bots = await _db.Bots.Where(b => b.IsActive && b.UsePulling).ToListAsync();

                Task[] botTasks = new Task[bots.Count];

                var index = 0;

                foreach (var bot in bots)
                {

                    botTasks[index] = new Task(() => ProcessBot(bot, _logger));

                    index++;
                }

                foreach (var task in botTasks)
                {
                    task.Start();
                }

                await Task.WhenAll(botTasks);

                await Task.Delay(10000, stoppingToken);
            }
        }

        async private static void ProcessBot(Bot bot, ILogger _logger)
        {
            using var _db = new TelegramBotEngineDbContext();

            var updateId = 0;

            var lastUpdate = await _db.UpdateIds.FirstOrDefaultAsync(u => u.BotId == bot.Id);

            if (lastUpdate != null)
            {
                updateId = lastUpdate.LastUpdateId + 1;
            }

            var update = new List<Update>();

            IEnumerable<Update> updates = update;

            var telegramBotClient = new TelegramBotClient(bot.Token);

            try
            {
                if (updateId == 0)
                {
                    updates = telegramBotClient.GetUpdates();
                }
                else
                {
                    updates = telegramBotClient.GetUpdates(offset: updateId);
                }

                _logger.LogInformation("Bot {botId}: {bot.Name}. Updates received.", bot.Id, bot.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bot {botId}: {bot.Name}. Error receiving updates.", bot.Id, bot.Name);
            }

            if (updates.Count() > 0)
            {
                updateId = await Handlers.UpdateHandler(updates, bot, _db, _logger);

                if (lastUpdate != null)
                {
                    lastUpdate.LastUpdateId = updateId;
                    
                    _db.UpdateIds.Update(lastUpdate);
                }
                else
                {
                    var newUpdateId = new UpdateId()
                    {
                        BotId = bot.Id,
                        LastUpdateId = updateId
                    };

                    await _db.UpdateIds.AddAsync(newUpdateId);
                }

                await _db.SaveChangesAsync();
            }
        }
    }
}
