using Microsoft.EntityFrameworkCore;
using Telegram.BotAPI.GettingUpdates;
using TelegramBotEngine.Models;

namespace TelegramBotEngine
{
    public static class Handlers
    {
        public static async Task<int> UpdateHandler(
            IEnumerable<Update> updates,
            Bot bot,
            TelegramBotEngineDbContext db,
            ILogger logger)
        {
            int lastUpdateId = 0;

            // Кэшируем чаты и пользователей в памяти за один проход
            var chatCache = new Dictionary<long, Chat>();
            var userCache = new Dictionary<long, FromUser>();

            foreach (var update in updates)
            {
                lastUpdateId = update.UpdateId;

                if (update.Message == null && update.EditedMessage == null)
                    continue;

                var message = update.EditedMessage ?? update.Message!;
                if (message.Chat == null || message.From == null)
                    continue;

                // ── Работа с Chat ─────────────────────────────────────
                Chat chat;
                if (!chatCache.TryGetValue(message.Chat.Id, out chat!))
                {
                    chat = await db.Chats.FirstOrDefaultAsync(c => c.ExternalId == message.Chat.Id && c.BotId == bot.Id);

                    if (chat == null)
                    {
                        chat = new Chat
                        {
                            ExternalId = message.Chat.Id,
                            BotId = bot.Id,
                            Type = message.Chat.Type.ToString(),
                            Title = message.Chat.Title ?? string.Empty,
                            Username = message.Chat.Username ?? string.Empty,
                            FirstName = message.Chat.FirstName ?? string.Empty,
                            LastName = message.Chat.LastName ?? string.Empty
                        };
                        db.Chats.Add(chat);
                    }
                    else
                    {
                        // Обновляем только если что-то реально изменилось
                        bool chatChanged = chat.Type != message.Chat.Type.ToString() ||
                                          chat.Title != (message.Chat.Title ?? string.Empty) ||
                                          chat.Username != (message.Chat.Username ?? string.Empty) ||
                                          chat.FirstName != (message.Chat.FirstName ?? string.Empty) ||
                                          chat.LastName != (message.Chat.LastName ?? string.Empty);

                        if (chatChanged)
                        {
                            chat.Type = message.Chat.Type.ToString();
                            chat.Title = message.Chat.Title ?? string.Empty;
                            chat.Username = message.Chat.Username ?? string.Empty;
                            chat.FirstName = message.Chat.FirstName ?? string.Empty;
                            chat.LastName = message.Chat.LastName ?? string.Empty;
                        }
                    }
                    chatCache[message.Chat.Id] = chat;
                }

                // ── Работа с FromUser ─────────────────────────────────
                FromUser fromUser;
                if (!userCache.TryGetValue(message.From.Id, out fromUser!))
                {
                    fromUser = await db.FromUsers.FirstOrDefaultAsync(u => u.ExternalId == message.From.Id);

                    if (fromUser == null)
                    {
                        fromUser = new FromUser
                        {
                            ExternalId = message.From.Id,
                            FirstName = message.From.FirstName ?? string.Empty,
                            LastName = message.From.LastName ?? string.Empty,
                            Username = message.From.Username ?? string.Empty,
                            IsBot = message.From.IsBot
                        };
                        db.FromUsers.Add(fromUser);
                    }
                    else
                    {
                        bool userChanged = fromUser.FirstName != (message.From.FirstName ?? string.Empty) ||
                                          fromUser.LastName != (message.From.LastName ?? string.Empty) ||
                                          fromUser.Username != (message.From.Username ?? string.Empty) ||
                                          fromUser.IsBot != message.From.IsBot;

                        if (userChanged)
                        {
                            fromUser.FirstName = message.From.FirstName ?? string.Empty;
                            fromUser.LastName = message.From.LastName ?? string.Empty;
                            fromUser.Username = message.From.Username ?? string.Empty;
                            fromUser.IsBot = message.From.IsBot;
                        }
                    }
                    userCache[message.From.Id] = fromUser;
                }

                // ── Работа с Message ──────────────────────────────────
                var dbMessage = await db.Messages
                    .FirstOrDefaultAsync(m => m.ExternalId == message.MessageId && m.ChatId == chat.Id);

                if (dbMessage == null)
                {
                    dbMessage = new Message
                    {
                        ExternalId = message.MessageId,
                        ChatId = chat.Id,
                        FromUserId = fromUser.Id,
                        Date = DateTimeOffset.FromUnixTimeSeconds(message.Date).UtcDateTime,
                        Text = message.Text ?? string.Empty,
                        Caption = message.Caption ?? string.Empty,
                        Processed = false
                    };
                    db.Messages.Add(dbMessage);
                    logger.LogInformation("Bot {BotId} — Новое сообщение {MessageId}", bot.Id, message.MessageId);
                }
                else
                {
                    // Обновление edited message
                    dbMessage.Date = DateTimeOffset.FromUnixTimeSeconds(message.Date).UtcDateTime;
                    dbMessage.Text = message.Text ?? string.Empty;
                    dbMessage.Caption = message.Caption ?? string.Empty;
                    logger.LogInformation("Bot {BotId} — Обновлено сообщение {MessageId}", bot.Id, message.MessageId);
                }

                // ── Фото ──────────────────────────────────────────────
                if (message.Photo != null)
                {
                    foreach (var photoSize in message.Photo)
                    {
                        var exists = await db.Photos.AnyAsync(p =>
                            p.FileId == photoSize.FileId &&
                            p.FileUniqueId == photoSize.FileUniqueId &&
                            p.MessageId == dbMessage.Id);

                        if (!exists)
                        {
                            db.Photos.Add(new Photo
                            {
                                FileId = photoSize.FileId,
                                FileUniqueId = photoSize.FileUniqueId,
                                Width = photoSize.Width,
                                Height = photoSize.Height,
                                FileSize = photoSize.FileSize ?? 0,
                                MessageId = dbMessage.Id
                            });
                        }
                    }
                }

                // ── Видео ─────────────────────────────────────────────
                if (message.Video != null)
                {
                    var video = message.Video;
                    var exists = await db.Video.AnyAsync(v =>
                        v.FileId == video.FileId &&
                        v.FileUniqueId == video.FileUniqueId &&
                        v.MessageId == dbMessage.Id);

                    if (!exists)
                    {
                        db.Video.Add(new Video
                        {
                            FileId = video.FileId,
                            FileUniqueId = video.FileUniqueId,
                            FileName = video.FileName ?? string.Empty,
                            Width = video.Width,
                            Height = video.Height,
                            Duration = video.Duration,
                            FileSize = video.FileSize ?? 0,
                            MimeType = video.MimeType ?? string.Empty,
                            MessageId = dbMessage.Id
                        });
                    }
                }
            }

            try
            {
                await db.SaveChangesAsync();
                logger.LogInformation("Bot {BotId}: {BotName} — все изменения ({Count} обновлений) сохранены в БД",
                    bot.Id, bot.Name, updates.Count());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bot {BotId}: ошибка при сохранении в БД", bot.Id);
            }

            return lastUpdateId;
        }
    }
}