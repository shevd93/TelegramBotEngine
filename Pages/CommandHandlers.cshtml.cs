using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramBotEngine.Models;

namespace TelegramBotEngine.Pages
{
    public class CommandHandlersModel : PageModel
    {
        private readonly ILogger<CreateBotModel> _logger;
        private readonly TelegramBotEngineDbContext _db;

        [BindProperty]
        public string ErrorMessage { get; set; } = string.Empty;
        public Guid BotId { get; set; } = Guid.Empty;
        public string BotName { get; set; } = string.Empty;
        public List<Handler> Handlers { get; set; } = new List<Handler>();

        public CommandHandlersModel(ILogger<CreateBotModel> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult OnGet(Guid? id)
        {
            if (id == null)
            {
                return RedirectToPage("/index");
            }
            else
            {
                BotId = id.Value;
                var existingBot = _db.Bots.Find(BotId);

                if (existingBot == null)
                {
                    ErrorMessage = "Bot not found.";
                    _logger.LogError("Bot with ID: {BotId} not found.", id);
                    return NotFound();
                }

                BotName = existingBot.Name;

                Handlers = _db.Handlers
                    .Where(h => h.BotId == BotId)
                    .OrderBy(h => h.ExternalId)
                    .ToList();

                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteHandler(Guid id)
        {
            var handler = await _db.Handlers.FindAsync(id);

            if (handler != null)
            {
                _db.Handlers.Remove(handler);
                try
                {
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Handler deleted. Id: {Id}", handler.Id);
                }
                catch (Exception ex)
                {
                    ErrorMessage = string.Concat("Handler deletion logging failed. Id: ", handler.Id, ". Error: ", ex.Message);
                    _logger.LogError(ErrorMessage);
                }
            }
            else
            {
                ErrorMessage = string.Concat("Handler deletion failed. Handler not found. Id: ", id);
                _logger.LogError(ErrorMessage);
            }

            if (ErrorMessage == string.Empty)
            {
                return RedirectToPage("/CommandHandlers",  new { id = handler!.BotId });
            }
            else
            {
                return Page();
            }

        }
    }
}
