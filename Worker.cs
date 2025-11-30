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

                    var tasks = bots.Select(bot => ProcessBotAsync(bot, stoppingToken)).ToArray();

                    await Task.WhenAll(tasks);

                    tasks = bots.Select(bot => ProcessMessagesAsync(bot, stoppingToken)).ToArray();

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Worker main loop");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ProcessBotAsync(Bot bot, CancellationToken ct)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TelegramBotEngineDbContext>();

            try
            {
                var lastUpdate = await db.UpdateIds
                    .FirstOrDefaultAsync(u => u.BotId == bot.Id, ct);

                int offset = lastUpdate?.LastUpdateId + 1 ?? 0;

                var client = new TelegramBotClient(bot.Token);
                var updates = await client.GetUpdatesAsync(offset: offset == 0 ? null : offset, timeout: 1, cancellationToken: ct);

                if (updates.Count() == 0)
                {
                    return;
                }

                _logger.LogInformation("Bot {BotId}: {BotName}. Received {Count} update(s)", bot.Id, bot.Name, updates.Count());

                int newLastUpdateId = await Handlers.UpdateHandler(updates, bot, db, _logger);

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
                _logger.LogError(ex, "Error processing bot {BotId}: {BotName}", bot.Id, bot.Name);
            }
        }

        private async Task ProcessMessagesAsync(Bot bot, CancellationToken ct)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TelegramBotEngineDbContext>();

            var chats = await db.Chats
                .Where(c => c.BotId == bot.Id)
                .ToListAsync(ct);
            
            var handlers = await db.Handlers
                .Where(h => h.BotId == bot.Id && h.IsActive)
                .ToListAsync(ct);

            var client = new TelegramBotClient(bot.Token);

            foreach (var chat in chats)
            {
                var messages = await db.Messages
                    .Where(m => m.ChatId == chat.Id && !m.Processed)
                    .ToListAsync(ct);

                foreach (var message in messages)
                {
                    try
                    {
                        if (handlers.Count() != 0)
                        {
                            await Handlers.MessageHandler(bot, chat, handlers, message, client, _logger);
                        }
                        message.Processed = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message {MessageId} in chat {ChatId}", message.Id, chat.Id);
                    }
                }
                
                await db.SaveChangesAsync(ct);
            }

        }
    }
}