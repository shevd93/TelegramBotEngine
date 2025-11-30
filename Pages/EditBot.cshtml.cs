using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TelegramBotEngine.Models;

namespace TelegramBotEngine.Pages
{
    public class EditBotModel : PageModel
    {
        private readonly ILogger<EditBotModel> _logger;
        private readonly TelegramBotEngineDbContext _db;

        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }
        [BindProperty]
        public BotInputModel Bot { get; set; } = new();

        public EditBotModel(ILogger<EditBotModel> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id == Guid.Empty)
                return NotFound();

            var bot = await _db.Bots.FindAsync(Id);
            if (bot == null)
                return NotFound();

            Bot = new BotInputModel
            {
                Name = bot.Name,
                Token = bot.Token,
                UsePulling = bot.UsePulling,
                WebhookUrl = bot.WebhookUrl ?? ""
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingBot = await _db.Bots.FindAsync(Id);
            if (existingBot == null)
            {
                ModelState.AddModelError(string.Empty, "Bot not found.");
                return Page();
            }

            var duplicate = await _db.Bots
                    .AnyAsync(b => b.Id != Id && (b.Name == Bot.Name.Trim() || b.Token == Bot.Token.Trim()));

            if (duplicate)
            {
                ModelState.AddModelError(string.Empty, "A bot with the same name or token already exists.");
                return Page();
            }

            existingBot.Name = Bot.Name.Trim();
            existingBot.Token = Bot.Token.Trim();
            existingBot.UsePulling = Bot.UsePulling;
            existingBot.WebhookUrl = string.IsNullOrWhiteSpace(Bot.WebhookUrl) ? string.Empty : Bot.WebhookUrl.Trim();

            try
            {
                await _db.SaveChangesAsync();
                _logger.LogInformation("Bot updated successfully. Id: {Id}, Name: {Name}", Id, Bot.Name);
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update bot. Id: {Id}", Id);
                ModelState.AddModelError(string.Empty, "Failed to save changes. Please try again.");
                return Page();
            }
        }
    }
}