using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using TelegramBotEngine.Models;

namespace TelegramBotEngine.Pages
{
    public class EditCommandHandlerModel : PageModel
    {

        private readonly ILogger<EditCommandHandlerModel> _logger;
        private readonly TelegramBotEngineDbContext _db;

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;
        [BindProperty(SupportsGet = true)]
        public Guid HandlerId { get; set; }
        public string BotName { get; set; } = string.Empty;
        [BindProperty]
        public Handler Handler { get; set; } = new Handler();
        [BindProperty]
        public List<string> BotCommands { get; set; } = new List<string>();

        public EditCommandHandlerModel(ILogger<EditCommandHandlerModel> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public void OnGet(Guid id)
        {
            HandlerId = id;

            var existingHandler = _db.Handlers.Find(HandlerId);

            if (existingHandler == null)
            {
                ErrorMessage = "Handler not found.";
                return;
            }

            Handler = existingHandler;

            var bot = _db.Bots.Find(Handler.BotId);

            if (bot != null)
            {
                BotName = bot.Name;

                var telegramBotClient = new TelegramBotClient(bot.Token);

                try
                {
                    var botCommands = telegramBotClient.GetMyCommands();

                    foreach (var element in botCommands)
                    {
                        BotCommands.Add(element.Command);
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = "Failed to retrieve bot commands. Please ensure the bot token is correct.";
                    _logger.LogError(ex, "Error retrieving bot commands for Bot ID: {BotId}", id);
                }
            }
            else
            {
                ErrorMessage = "Associated bot not found.";
                _logger.LogError("Associated bot not found for Handler ID: {HandlerId}", id);
            }
        }

        public async Task<IActionResult> OnPostEditCommandHandler()
        {
            var existingHandler = await _db.Handlers.FindAsync(Handler.Id);

            if (existingHandler == null)
            {
                ErrorMessage = "Handler not found.";
                return Page();
                
            }

            existingHandler.Name = Handler.Name;
            existingHandler.Type = Handler.Type;
            existingHandler.Code = Handler.Code ?? "";
            existingHandler.Text = Handler.Text ?? "";
            existingHandler.IsActive = Handler.IsActive;

            try
            {
                await _db.SaveChangesAsync();
                return RedirectToPage("/CommandHandlers", new { id = existingHandler.BotId });
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while updating the handler.";
                _logger.LogError(ex, "Error updating handler with ID: {HandlerId}", Handler.Id);
                return Page();
            }
        }
    }
}
