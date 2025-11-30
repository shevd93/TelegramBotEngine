using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TelegramBotEngine.Models;

namespace TelegramBotEngine.Pages
{
    public class CreateBotModel : PageModel
    {
        private readonly ILogger<CreateBotModel> _logger;
        private readonly TelegramBotEngineDbContext _db;
        [BindProperty]
        public BotInputModel Bot { get; set; } = new();

        public CreateBotModel(ILogger<CreateBotModel> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public void OnGet()
        {
            _logger.LogInformation("The bot creation page has been loaded..");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Bot.Name = Bot.Name.Trim();
            Bot.Token = Bot.Token.Trim();
            Bot.WebhookUrl = Bot.WebhookUrl?.Trim();

            bool exists = await _db.Bots
                .AnyAsync(b => b.Name == Bot.Name || b.Token == Bot.Token);

            if (exists)
            {
                ModelState.AddModelError(string.Empty, "A bot with this name or token already exists..");
                return Page();
            }

            var newBot = new Bot
            {
                Id = Guid.NewGuid(),
                Name = Bot.Name,
                Token = Bot.Token,
                WebhookUrl = string.IsNullOrWhiteSpace(Bot.WebhookUrl) ? string.Empty : Bot.WebhookUrl,
                IsActive = false,
                UsePulling = Bot.UsePulling
            };

            _db.Bots.Add(newBot);

            try
            {
                await _db.SaveChangesAsync();
                _logger.LogInformation("New bot created: {BotName} (ID: {BotId})", newBot.Name, newBot.Id);
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save new bot to database");
                ModelState.AddModelError(string.Empty, "Failed to save the bot. Try again later..");
                return Page();
            }
        }
    }
}