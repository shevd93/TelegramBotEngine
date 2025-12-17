using Microsoft.EntityFrameworkCore;
using System;
using Telegram.BotAPI;
using Telegram.BotAPI.GettingUpdates;
using TelegramBotEngine.Models;

namespace TelegramBotEngine
{
    public static class Handlers
    {
        public static async Task<int> UpdateHandler(
            IEnumerable<Update> updates,
            Bot bot,
            TelegramBotClient client,
            TelegramBotEngineDbContext db,
            ILogger logger)
        {
            int lastUpdateId = 0;

            // We cache chats and users in memory in one pass.
            var chatCache = new Dictionary<long, Chat>();
            var userCache = new Dictionary<long, FromUser>();

            foreach (var update in updates)
            {
                lastUpdateId = update.UpdateId;

                // -- Callback --
                if (update.CallbackQuery?.Data != null && update.CallbackQuery.Message?.Chat != null)
                {
                    var data = update.CallbackQuery.Data;
                    var chatId = update.CallbackQuery.Message.Chat.Id;          

                    await CallbackQueryHandler(data, chatId, bot, client, db, logger);
                    continue;
                }

                if (update.Message == null && update.EditedMessage == null && update.CallbackQuery == null)
                    continue;

                var message = update.EditedMessage ?? update.Message!;
                if (message.Chat == null || message.From == null)
                    continue;

                // -- Working with Chat --
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
                        // We update only if something has actually changed.
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

                // -- Working with FromUser --
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

                // -- Working with Message --
                var dbMessage = await db.Messages
                    .FirstOrDefaultAsync(m => m.ExternalId == message.MessageId && m.ChatId == chat.Id);

                if (dbMessage == null)
                {
                    dbMessage = new Message
                    {
                        ExternalId = message.MessageId,
                        ReplyToMessageExternalId = message.ReplyToMessage?.MessageId ?? 0,
                        ChatId = chat.Id,
                        FromUserId = fromUser.Id,
                        Date = DateTimeOffset.FromUnixTimeSeconds(message.Date).UtcDateTime,
                        Text = message.Text ?? string.Empty,
                        Caption = message.Caption ?? string.Empty,
                        Processed = false,
                        VerifiedOnToxics = false
                    };
                    db.Messages.Add(dbMessage);
                    logger.LogInformation("Bot {BotId} — New message {MessageId}", bot.Id, message.MessageId);
                }
                else
                {
                    // Update edited messag
                    dbMessage.Date = DateTimeOffset.FromUnixTimeSeconds(message.Date).UtcDateTime;
                    dbMessage.Text = message.Text ?? string.Empty;
                    dbMessage.Caption = message.Caption ?? string.Empty;
                    logger.LogInformation("Bot {BotId} — Message {MessageId} updated (edited)", bot.Id, message.MessageId);
                }

                // -- Photo --
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

                // -- Video --
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
                logger.LogInformation("Bot {BotId}: {BotName} — All changes saved ({Count} update(s) processed)", bot.Id, bot.Name, updates.Count());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bot {BotId}: Failed to save changes to database", bot.Id);
            }

            return lastUpdateId;
        }

        public static async Task MessageHandler(
            Bot bot,
            TelegramBotEngineDbContext db,
            Chat chat,
            IReadOnlyList<Handler> handlers,
            Message message,
            TelegramBotClient client,
            ILogger logger)
        {
            foreach(var handler in handlers)
            {
                //-- Menu --

                var externalid = string.Concat("/", handler.ExternalId);

                if (handler.Type == "Menu" && message.Text.Contains(externalid))
                {
                    try
                    {
                        await TelegramExtension.SendMenu(chat.ExternalId, handler.Code, handler.Text, client);
                    }
                    catch(Exception ex)
                    {
                        logger.LogError(ex, "Error sending menu for Bot {BotId}, Chat {ChatId}, Message {MessageId}", bot.Id, chat.Id, message.Id);
                    }
                }

                //-- CheckingAMessageForToxicity --

                if (handler.Type == "CheckingAMessageForToxicity" && message.Text.Contains(externalid) && message.ReplyToMessageExternalId !=0)
                {
                    var replyMessage = await db.Messages
                        .FirstOrDefaultAsync(m => m.ExternalId == message.ReplyToMessageExternalId && m.ChatId == chat.Id);

                    if (replyMessage != null)
                    {

                        var fromUser = await db.FromUsers
                            .FirstOrDefaultAsync(fu => fu.Id == replyMessage.FromUserId);

                        if ((DateTime.UtcNow - replyMessage.Date).TotalDays > 1)
                        {
                            try
                            {
                                await TelegramExtension.SendMessage(chat.ExternalId, "Прошло больше суток. Поезд ушел)", client, message.ExternalId);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error sending message to bot indicating more than a day has passed. Bot { BotId}, Chat { ChatId}, Message { MessageId}", bot.Id, chat.Id, message.Id);
                            }
                        }
                        else if (replyMessage.VerifiedOnToxics)
                        {
                            try
                            {
                                await TelegramExtension.SendMessage(chat.ExternalId, "Сообщение уже проверено на токсичность", client, message.ExternalId);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error sending message to bot indicating that message has already been checked for toxicity. Bot { BotId}, Chat { ChatId}, Message { MessageId}", bot.Id, chat.Id, message.Id);
                            }
                        }
                        else if (fromUser != null && fromUser.IsBot)
                        {
                            try
                            {
                                await TelegramExtension.SendMessage(chat.ExternalId, "Против бота не свидетельствуют)", client, message.ExternalId);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error sending message to bot indicating author is a bot. Bot { BotId}, Chat { ChatId}, Message { MessageId}", bot.Id, chat.Id, message.Id);
                            }
                        }
                        else if (replyMessage.FromUser == message.FromUser)
                        {
                            try
                            {
                                await TelegramExtension.SendMessage(chat.ExternalId, "Против себя не свидетельствуют)", client, message.ExternalId);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error sending message for Bot { BotId}, Chat { ChatId}, Message { MessageId}", bot.Id, chat.Id, message.Id);
                            }
                        }
                        else if (!string.IsNullOrEmpty(replyMessage.Text) || !string.IsNullOrEmpty(replyMessage.Caption))
                        {
                            var isToxic = await DeepSeekExtension.IsMessageToxic(bot.DeepSeekApiKey, string.Concat(replyMessage.Text, replyMessage.Caption));

                            if (isToxic)
                            {
                                var toxic = await db.ToxicUsers
                                    .FirstOrDefaultAsync(t => t.FromUserId == replyMessage.FromUserId && t.ChatId == chat.Id);

                                if (toxic == null)
                                {
                                    await db.ToxicUsers.AddAsync(new ToxicUser
                                    {
                                        FromUserId = replyMessage.FromUserId,
                                        ChatId = chat.Id,
                                        Score = 1.0f
                                    });
                                }
                                else
                                {
                                    toxic.Score += 1.0f;
                                }

                                var kpi = await db.KPIs
                                    .FirstOrDefaultAsync(k => k.FromUserId == message.FromUserId && k.ChatId == chat.Id);

                                if (kpi == null)
                                {
                                    await db.KPIs.AddAsync(new KPI
                                    {
                                        FromUserId = message.FromUserId,
                                        ChatId = chat.Id,
                                        Score = 1.0f
                                    });
                                }
                                else
                                {
                                    kpi.Score += 1.0f;
                                }

                                try
                                {
                                    //await TelegramExtension.SendMessage(chat.ExternalId, "The message you replied to was detected as toxic.", client, message.ExternalId);
                                    await TelegramExtension.SendMessage(chat.ExternalId, "Красавчик! Сдал токсика, ему +балл за токсичность, а тебе +KPI!", client, message.ExternalId);
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Error sending toxicity detected message for Bot {BotId}, Chat {ChatId}, Message {MessageId}", bot.Id, chat.Id, message.Id);
                                }
                            }
                            else
                            {
                                var toxic = await db.ToxicUsers
                                   .FirstOrDefaultAsync(t => t.FromUserId == message.FromUserId && t.ChatId == chat.Id);

                                if (toxic == null)
                                {
                                    await db.ToxicUsers.AddAsync(new ToxicUser
                                    {
                                        FromUserId = message.FromUserId,
                                        ChatId = chat.Id,
                                        Score = 1.0f
                                    });
                                }
                                else
                                {
                                    toxic.Score += 1.0f;
                                }

                                var kpi = await db.KPIs
                                    .FirstOrDefaultAsync(k => k.FromUserId == message.FromUserId && k.ChatId == chat.Id);

                                if (kpi == null)
                                {
                                    await db.KPIs.AddAsync(new KPI
                                    {
                                        FromUserId = message.FromUserId,
                                        ChatId = chat.Id,
                                        Score = -1.0f
                                    });
                                }
                                else
                                {
                                    kpi.Score -= 1.0f;
                                }

                                try
                                {
                                    //await TelegramExtension.SendMessage(chat.ExternalId, "The message is not toxic.", client, message.ExternalId);
                                    await TelegramExtension.SendMessage(chat.ExternalId, "Ля ты даешь! -KPI за клевету и +балл в ТОП токсиков для профилактики!", client, message.ExternalId);
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Error sending not toxicity detected message for Bot {BotId}, Chat {ChatId}, Message {MessageId}", bot.Id, chat.Id, message.Id);
                                }
                            }

                            replyMessage.VerifiedOnToxics = true;

                            await db.SaveChangesAsync();
                        }
                        else
                        {
                            try
                            {
                                //await TelegramExtension.SendMessage(chat.ExternalId, "The message to check for toxicity has no text.", client, message.ExternalId);
                                await TelegramExtension.SendMessage(chat.ExternalId, "Сообщение для проверки не содержит текст.", client, message.ExternalId);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error sending toxicity check no text message for Bot {BotId}, Chat {ChatId}, Message {MessageId}", bot.Id, chat.Id, message.Id);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            //await TelegramExtension.SendMessage(chat.ExternalId, "The message to check for toxicity was not found.", client, message.ExternalId);
                            await TelegramExtension.SendMessage(chat.ExternalId, "Пу пу пу....", client, message.ExternalId);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error sending toxicity check failure message for Bot {BotId}, Chat {ChatId}, Message {MessageId}", bot.Id, chat.Id, message.Id);
                        }
                    }
                }

                // --Quiz--

                if (handler.Type == "Quiz" && message.Text.Contains(externalid))
                {
                    try
                    {
                        await TelegramExtension.SendQuiz(chat.ExternalId, client);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error sending quiz for Bot {BotId}, Chat {ChatId}, Message {MessageId}", bot.Id, chat.Id, message.Id);
                    }
                }

                // ----
            }

            logger.LogInformation("Testing MessageHandler for Bot {BotId}, Chat {ChatId}, Message {MessageId}", bot.Id, chat.Id, message.Id);
        }

        public static async Task CallbackQueryHandler(string data, long chatId, Bot bot, TelegramBotClient client,TelegramBotEngineDbContext db, ILogger logger)
        {

            var chat = await db.Chats
                .FirstOrDefaultAsync(c => c.ExternalId == chatId);

            if (chat == null)
            {
                logger.LogError("Chat not found. Extermal ID: {ChatId}", chatId);
                return;
            }

            // -- ToxicTop --
            if (data == "ToxicTop")
            {
                var toxics = await db.ToxicUsers
                    .Where(tu => tu.ChatId == chat.Id)
                    .Include(tu => tu.FromUser)
                    .OrderByDescending(tu => tu.Score)
                    .ToListAsync();

                var topList = string.Empty;

                var top = 0;

                foreach (var toxicUser in toxics)
                {
                    if (toxicUser.Score == 0)
                    {
                        continue;
                    }
                    
                    top += 1;

                    var username = toxicUser.FromUser!.Username;

                    if (string.IsNullOrEmpty(username))
                    {
                        username = toxicUser.FromUser!.FirstName ?? "Неопознанный пользователь";
                    }

                    topList = string.Concat(topList, $"{top}. @{username}: {toxicUser.Score}\n");
                }

                try
                {
                    if (string.IsNullOrEmpty(topList))
                    {
                        topList = "Нет токсичных пользователей.";
                    }
                    await TelegramExtension.SendMessage(chat.ExternalId, $"<blockquote expandable>🏆 ТОП токсиков🏆\n\n{topList}</blockquote>", client);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending Toxic Top list for Bot {BotId}, Chat {ChatId}", bot.Id, chat.Id);
                }
            }

            // -- MyKPI --
            if (data == "MyKPI")
            {
                var KPIs = await db.KPIs
                    .Where(tu => tu.ChatId == chat.Id)
                    .Include(tu => tu.FromUser)
                    .OrderByDescending(tu => tu.Score)
                    .ToListAsync();

                var topList = string.Empty;

                var top = 0;

                foreach (var KPI in KPIs)
                {
                    if (KPI.Score == 0)
                    {
                        continue;
                    }

                    top += 1;

                    var username = KPI.FromUser!.Username;

                    if (string.IsNullOrEmpty(username))
                    {
                        username = KPI.FromUser!.FirstName ?? "Неопознанный пользователь";
                    }

                    topList = string.Concat(topList, $"{top}. @{username}: {KPI.Score}\n");
                }

                try
                {
                    if (string.IsNullOrEmpty(topList))
                    {
                        topList = "Нет KPI пользователей.";
                    }
                    await TelegramExtension.SendMessage(chat.ExternalId, $"<blockquote expandable>🏆 KPI 🏆\n\n{topList}</blockquote>", client);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending KPI Top list for Bot {BotId}, Chat {ChatId}", bot.Id, chat.Id);
                }
            }

            // --
        }

    }
}