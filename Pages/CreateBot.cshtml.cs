using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramBotEngine.Models;

namespace TelegramBotEngine.Pages
{
    public class CreateBotModel : PageModel
    {
        private readonly ILogger<CreateBotModel> _logger;
        private readonly TelegramBotEngineDbContext _db;
        public string ErrorMessage { get; set; } = string.Empty;

        [BindProperty]
        public Bot Bot { get; set; } = new Bot();

        public CreateBotModel(ILogger<CreateBotModel> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public void OnGet()
        {
            _logger.LogInformation("The new bot creation page has been loaded");
        }

        public async Task<IActionResult> OnPostCreateANewBot()
        {
            Bot.Name = Bot.Name ?? "";
            Bot.Token = Bot.Token ?? "";
            Bot.WebhookUrl = Bot.WebhookUrl ?? "";
            Bot.IsActive = false;

            var bots = (_db.Bots.Where(b => b.Name == Bot.Name || b.Token == Bot.Token).ToList());

            if (bots.Count > 0)
            {
                ErrorMessage = "A bot with the same name or token already exists.";
            }
            else
            {
                _db.Bots.Add(Bot);

                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ErrorMessage = string.Concat("Error creating new bot: ", ex.Message);
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
