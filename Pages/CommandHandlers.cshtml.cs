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

        [BindProperty]
        public Bot Bot { get; set; } = new Bot();

        public CommandHandlersModel(ILogger<CreateBotModel> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("/index");
        }

        public IActionResult OnPostCommandHandlers(Guid id)
        {
            var existingBot = _db.Bots.Find(id);

            if (existingBot == null)
            {
                return NotFound();
            }

            Bot = existingBot;

            return Page();

        }
    }
}
