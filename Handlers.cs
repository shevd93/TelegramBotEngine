using Microsoft.EntityFrameworkCore;
using Telegram.BotAPI.GettingUpdates;
using TelegramBotEngine.Models;

namespace TelegramBotEngine
{
    public static class Handlers
    {
        async public static Task<int> UpdateHandler(IEnumerable<Update> updates, Bot bot, TelegramBotEngineDbContext _db, ILogger _logger)
        {
            var updateId = 0;
            List<Chat> chats = [];
            List<FromUser> fromUsers = [];

            foreach (var update in updates)
            {
               updateId = update.UpdateId;

               var isEditing = false;

               if (update.Message == null && update.EditedMessage == null)
               {
                   continue;
               }

               isEditing = (update.EditedMessage != null);

               var message = isEditing ? update.EditedMessage : update.Message;

               Chat? chat = null;

                if (message.Chat != null)
                {
                    var messageChat = message.Chat;

                    chat = chats.FirstOrDefault(c => c.ExternalId == messageChat.Id && c.BotId == bot.Id);

                    if (chat == null)
                    {
                        chat = await _db.Chats.FirstOrDefaultAsync(c => c.ExternalId == messageChat.Id && c.BotId == bot.Id);

                        if (chat == null)
                        {
                            chat = new Chat()
                            {
                                ExternalId = messageChat.Id,
                                BotId = bot.Id,
                                Type = messageChat.Type.ToString(),
                                Title = messageChat.Title ?? string.Empty,
                                Username = messageChat.Username ?? string.Empty,
                                FirstName = messageChat.FirstName ?? string.Empty,
                                LastName = messageChat.LastName ?? string.Empty
                            };

                            await _db.Chats.AddAsync(chat);

                            await _db.SaveChangesAsync();
                        }
                        else
                        {
                            if (chat.Type != messageChat.Type.ToString() ||
                                chat.Title != (messageChat.Title ?? string.Empty) ||
                                chat.Username != (messageChat.Username ?? string.Empty) ||
                                chat.FirstName != (messageChat.FirstName ?? string.Empty) ||
                                chat.LastName != (messageChat.LastName ?? string.Empty))
                            {
                                chat.Type = messageChat.Type.ToString();
                                chat.Title = messageChat.Title ?? string.Empty;
                                chat.Username = messageChat.Username ?? string.Empty;
                                chat.FirstName = messageChat.FirstName ?? string.Empty;
                                chat.LastName = messageChat.LastName ?? string.Empty;
                                
                                _db.Chats.Update(chat);
                               
                                await _db.SaveChangesAsync();
                            }
                        }

                        chats.Add(chat);
                    }
                }

                FromUser? fromUser = null;

                if (message.From != null)
                {
                    var from = message.From;

                    fromUser = fromUsers.FirstOrDefault(f => f.ExternalId == from.Id);

                    if (fromUser == null)
                    {
                        fromUser = await _db.FromUsers.FirstOrDefaultAsync(f => f.ExternalId == message.From.Id);

                        if (fromUser == null)
                        {
                            fromUser = new FromUser()
                            {
                                ExternalId = from.Id,
                                FirstName = from.FirstName ?? string.Empty,
                                LastName = from.LastName ?? string.Empty,
                                Username = from.Username ?? string.Empty,
                                IsBot = from.IsBot
                            };

                            await _db.FromUsers.AddAsync(fromUser);

                            await _db.SaveChangesAsync();
                        }
                        else
                        {
                            if (fromUser.FirstName != (from.FirstName ?? string.Empty) ||
                                fromUser.LastName != (from.LastName ?? string.Empty) ||
                                fromUser.Username != (from.Username ?? string.Empty) ||
                                fromUser.IsBot != from.IsBot)
                            {
                                fromUser.FirstName = from.FirstName ?? string.Empty;
                                fromUser.LastName = from.LastName ?? string.Empty;
                                fromUser.Username = from.Username ?? string.Empty;
                                fromUser.IsBot = from.IsBot;
                                
                                _db.FromUsers.Update(fromUser);

                                await _db.SaveChangesAsync();
                            }
                        }

                        fromUsers.Add(fromUser);
                    }
                }

                if (chat == null || fromUser == null)
                {
                    _logger.LogWarning("Bot {botId}: {bot.Name}. The chat or user is null. MessageId: {messageId}", bot.Id, bot.Name, message.MessageId);
                    continue;
                }

                var dbMessage = await _db.Messages.FirstOrDefaultAsync(m => m.ExternalId == message.MessageId && m.ChatId == chat.Id);

                if (dbMessage == null)
                {
                    dbMessage = new Message()
                    {
                        ExternalId = message.MessageId,
                        FromUserId = fromUser.Id,
                        Date = DateTimeOffset.FromUnixTimeSeconds(message.Date).UtcDateTime,
                        ChatId = chat.Id,
                        Text = message.Text ?? string.Empty,
                        Caption = message.Caption ?? string.Empty,
                        Processed = false
                    };
                    
                    await _db.Messages.AddAsync(dbMessage);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Bot {botId}: {bot.Name}. New message. MessageId: {messageId}", bot.Id, bot.Name, message.MessageId);

                }
                else
                {
                    dbMessage.FromUserId = fromUser.Id;
                    dbMessage.Date = DateTimeOffset.FromUnixTimeSeconds(message.Date).UtcDateTime;
                    dbMessage.Text = message.Text ?? string.Empty;
                    dbMessage.Caption = message.Caption ?? string.Empty;
                    
                    _db.Messages.Update(dbMessage);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Bot {botId}: {bot.Name}. Updated message. MessageId: {messageId}", bot.Id, bot.Name, message.MessageId);

                }
            }

            _logger.LogInformation("Bot {botId}: {bot.Name}. The changes have been written to the database.", bot.Id, bot.Name);

            return updateId;
        }
    }
}

    
