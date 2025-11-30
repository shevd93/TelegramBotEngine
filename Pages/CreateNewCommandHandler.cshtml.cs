using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using TelegramBotEngine.Models;

namespace TelegramBotEngine.Pages
{
    public class CreateNewCommandHandlerModel : PageModel
    {
        private readonly ILogger<CreateBotModel> _logger;
        private readonly TelegramBotEngineDbContext _db;

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;
        [BindProperty (SupportsGet = true)]
        public Guid BotId { get; set; }
        public string BotName { get; set; } = string.Empty;
        [BindProperty]
        public Handler Handler { get; set; } = new Handler();
        [BindProperty]
        public List<string> BotCommands { get; set; } = new List<string>();

        public CreateNewCommandHandlerModel(ILogger<CreateBotModel> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public void OnGet(Guid id)
        {
            BotId = id;
            var existingBot = _db.Bots.Find(BotId);

            if (existingBot == null)
            {
                ErrorMessage = "Bot not found.";
            }
            else
            {
                BotName = existingBot.Name;
                var telegramBotClient = new TelegramBotClient(existingBot.Token);

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
        }

        public IActionResult OnPostCreateANewCommandHandler()
        { 
            Handler.Code = Handler.Code ?? "";

            _db.Handlers.Add(Handler);

            try
            {
                _db.SaveChanges();
                _logger.LogInformation("New command handler created with ID: {HandlerId} for Bot ID: {BotId}", Handler.Id, Handler.BotId);
            }
            catch (Exception ex)
            {
                ErrorMessage = string.Concat("Error creating new command handler: ", ex.Message);
                _logger.LogError(ex, "Error creating new command handler for Bot ID: {BotId}", Handler.BotId);
                return Page();
            }

            return RedirectToPage("/CommandHandlers", new {id = Handler.BotId});
        }
    }
}
