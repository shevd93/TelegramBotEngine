using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using TelegramBotEngine.Models;

namespace TelegramBotEngine.Pages
{
    public class CreateBotModel : PageModel
    {
        private readonly ILogger<CreateBotModel> _logger;
        private readonly TelegramBotEngineDbContext _db;

        [BindProperty]
        public BotInputModel Bot { get; set; } = new();
        [BindProperty]
        public string? ErrorMessage { get; set; }

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
                WebhookUrl = string.IsNullOrWhiteSpace(Bot.WebhookUrl) ? null : Bot.WebhookUrl,
                IsActive = false,
                UsePulling = Bot.UsePulling
            };

            _db.Bots.Add(newBot);

            try
            {
                await _db.SaveChangesAsync();
                _logger.LogInformation("New bot created: {BotName} (ID: {BotId})", newBot.Name, newBot.Id);
                TempData["SuccessMessage"] = $"Bot «{newBot.Name}» successfully created!";
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save new bot to database");
                ModelState.AddModelError(string.Empty, "Failed to save the bot. Try again later..");
                return Page();
            }
        }

        public class BotInputModel
        {
            [Required(ErrorMessage = "Bot name is required")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "The name must contain between 2 and 100 characters.")]
            [Display(Name = "Bot name")]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Token required")]
            [Display(Name = "Bot token")]
            public string Token { get; set; } = string.Empty;

            [Url(ErrorMessage = "Please enter a valid UR")]
            [Display(Name = "Webhook URL (optional)")]
            public string? WebhookUrl { get; set; }

            [Display(Name = "Use polling instead of webhook")]
            public bool UsePulling { get; set; } = true;
        }
    }
}