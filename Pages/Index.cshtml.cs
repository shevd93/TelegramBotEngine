using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramBotEngine.Models;

namespace TelegramBotEngine.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly TelegramBotEngineDbContext _db;
        private string ErrorMessage { get; set; } = string.Empty;
        public List<Bot> Bots { get; set; } = new List<Bot>();

        public IndexModel(ILogger<IndexModel> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public void OnGet()
        {
            Bots = _db.Bots.ToList();

            _logger.LogInformation("Index page loaded");
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var bot = await _db.Bots.FindAsync(id);

            if (bot != null)
            {
                if (bot.IsActive)
                {
                    ErrorMessage = string.Concat("Cannot delete an active bot. Please stop the bot before deletion. Id: ", bot.Id);
                }
                else
                {
                    _db.Bots.Remove(bot);
                    
                    try
                    {
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("Bot deleted. Id: {Id}", bot.Id);
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = string.Concat("Bot deletion logging failed. Id: ", bot.Id, ". Error: ", ex.Message);
                    }
                }
            }
            else
            {
                ErrorMessage = string.Concat("Bot deletion failed. Bot not found. Id: ", id);
            }

            return RedirectToNextPage();
        }

        public async Task<IActionResult> OnPostStart(Guid id)
        {
            var bot = await _db.Bots.FindAsync(id);

            if (bot != null)
            {
                if (!bot.IsActive)
                {
                    bot.IsActive = true;

                    try
                    {
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = string.Concat("Bot start failed due to an internal error. Id: ", bot.Id, ". Error: ", ex.Message);
                        bot.IsActive = false;
                    }
                }
            }
            else
            {
                ErrorMessage = string.Concat("Bot start failed. Bot not found. Id: ", id);
            }

            return RedirectToNextPage();
        }

        public async Task<IActionResult> OnPostStop(Guid id)
        {
            var bot = await _db.Bots.FindAsync(id);

            if (bot != null)
            {
                if (bot.IsActive)
                {
                    bot.IsActive = false;

                    try
                    {                         
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = string.Concat("Bot stop failed due to an internal error. Id: ", bot.Id, ". Error: ", ex.Message);
                        bot.IsActive = true;
                    }
                }
            }
            else
            {
                ErrorMessage = string.Concat("Bot stop failed. Bot not found. Id: ", id);
            }

            return RedirectToNextPage();
        }

        private IActionResult RedirectToNextPage()
        {
            if (ErrorMessage == string.Empty)
            {
                return RedirectToPage("/index");
            }
            else
            {
                _logger.LogError(ErrorMessage);
                return RedirectToPage("/error", new { errorMessage = ErrorMessage });
            }
        }
    }
}
