using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramBotEngine.Models;

namespace TelegramBotEngine.Pages
{
    public class EditBotModel : PageModel
    {
        private readonly ILogger<CreateBotModel> _logger;
        private readonly TelegramBotEngineDbContext _db;

        [BindProperty]
        public string ErrorMessage { get; set; } = string.Empty;

        [BindProperty]
        public Bot Bot { get; set; } = new Bot();

        public EditBotModel(ILogger<CreateBotModel> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("/index");
        }

        public IActionResult OnPostEdit(Guid id)
        {
            var existingBot = _db.Bots.Find(id);

            if (existingBot == null)
            {
                return NotFound();
            }
            Bot = existingBot;
            return Page();
        }

        public async Task<IActionResult> OnPostSave()
        {
            Bot.WebhookUrl = Bot.WebhookUrl ?? "";

            var existingBot = _db.Bots.Find(Bot.Id);

            if (existingBot == null)
            {
                ErrorMessage = string.Concat("Bot not found. Id: ", Bot.Id);
            }
            else
            {
                var bots = (_db.Bots.Where(b => (b.Name == Bot.Name || b.Token == Bot.Token) && b.Id != Bot.Id).ToList());

                if (bots.Count > 0)
                {
                    ErrorMessage = "A bot with the same name or token already exists.";
                }
                else
                {
                    existingBot.Name = Bot.Name;
                    existingBot.Token = Bot.Token;
                    existingBot.UsePulling = Bot.UsePulling;
                    existingBot.WebhookUrl = Bot.WebhookUrl;

                    try
                    {
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("Bot updated. Id: {Id}", Bot.Id);
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = string.Concat("Bot update failed. Id: ", Bot.Id, ". Error: ", ex.Message);
                    }
                }

            }

            if (ErrorMessage == string.Empty)
            {
                return RedirectToPage("/index");
            }
            else
            {
                _logger.LogError(ErrorMessage);
                return Page();
            }

        }
    }
}
