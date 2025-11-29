using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using TelegramBotEngine.Models;

namespace TelegramBotEngine.Pages;

public class BotModel(
    ILogger<BotModel> logger,
    TelegramBotEngineDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid BotId { get; set; }
    [BindProperty(SupportsGet = true)]
    public Guid? SelectedChat { get; set; }
    [BindProperty]
    public string TextMessage { get; set; } = string.Empty;
    public string BotName { get; private set; } = string.Empty;
    public IReadOnlyList<Chat> Chats { get; private set; } = [];

    public IActionResult OnGet(Guid id)
    {
        BotId = id;

        var bot = db.Bots.Find(BotId);

        if (bot is null)
            return RedirectToError("Bot not found");

        BotName = bot.Name;
        Chats = db.Chats
            .Where(c => c.BotId == BotId)
            .OrderBy(c => c.Title)
            .AsNoTracking()
            .ToList();

        return Page();
    }
    public async Task<IActionResult> OnPostSendMessageAsync()
    {
        if (SelectedChat is null || SelectedChat == Guid.Empty)
            return RedirectToError("No chat selected");

        if (string.IsNullOrWhiteSpace(TextMessage))
            return RedirectToError("Message text is empty");

        var chat = await db.Chats
            .FirstOrDefaultAsync(c => c.Id == SelectedChat && c.BotId == BotId);

        if (chat is null)
            return RedirectToError("Chat not found");

        var bot = await db.Bots
            .Where(b => b.Id == BotId)
            .Select(b => new { b.Token, b.Name })
            .FirstOrDefaultAsync();

        if (bot is null)
            return RedirectToError("Bot not found");

        var telegramClient = new TelegramBotClient(bot.Token);

        try
        {
            await telegramClient.SendMessageAsync(
                chatId: chat.ExternalId,
                text: TextMessage,
                parseMode: "HTML");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message to chat {ChatId} from bot {BotName}", chat.ExternalId, bot.Name);

            return RedirectToError($"Failed to send message: {ex.Message}");
        }

        TempData["Success"] = "Message sent successfully!";

        return RedirectToPage(new { id = BotId, selectedChat = SelectedChat });
    }
    private IActionResult RedirectToError(string message) =>
        RedirectToPage("/Error", new { errorMessage = message });
}