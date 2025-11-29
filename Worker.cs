using Microsoft.EntityFrameworkCore;
using Telegram.BotAPI;
using Telegram.BotAPI.GettingUpdates;
using TelegramBotEngine.Models;

namespace TelegramBotEngine
{
    public sealed class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<TelegramBotEngineDbContext>();

                    var bots = await db.Bots
                        .Where(b => b.IsActive && b.UsePulling)
                        .ToListAsync(stoppingToken);

                    // Запускаем обработку всех ботов параллельно (но безопасно)
                    var tasks = bots.Select(bot => ProcessBotAsync(bot, stoppingToken)).ToArray();

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в основном цикле Worker");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ProcessBotAsync(Bot bot, CancellationToken ct)
        {
            // Каждый бот — в своём собственном scope → свой DbContext → нет конфликтов
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TelegramBotEngineDbContext>();

            try
            {
                var lastUpdate = await db.UpdateIds
                    .FirstOrDefaultAsync(u => u.BotId == bot.Id, ct);

                int offset = lastUpdate?.LastUpdateId + 1 ?? 0;

                var client = new TelegramBotClient(bot.Token);
                var updates = await client.GetUpdatesAsync(offset: offset == 0 ? null : offset, timeout: 10, cancellationToken: ct);

                if (updates.Count() == 0)
                {
                    return;
                }

                _logger.LogInformation("Bot {BotId}: {BotName}. Получено {Count} обновлений", bot.Id, bot.Name, updates.Count());

                int newLastUpdateId = await Handlers.UpdateHandler(updates, bot, db, _logger);

                // Обновляем или создаём запись о последнем update_id
                if (lastUpdate != null)
                {
                    lastUpdate.LastUpdateId = newLastUpdateId;
                }
                else
                {
                    await db.UpdateIds.AddAsync(new UpdateId
                    {
                        BotId = bot.Id,
                        LastUpdateId = newLastUpdateId
                    }, ct);
                }

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки бота {BotId}: {BotName}", bot.Id, bot.Name);
            }
        }
    }
}