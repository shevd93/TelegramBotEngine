using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramBotEngine;
using TelegramBotEngine.Models;

public class CommandHandlersModel : PageModel
{
    private readonly ILogger<CommandHandlersModel> _logger;
    private readonly TelegramBotEngineDbContext _db;
    [BindProperty(SupportsGet = true)]
    public Guid BotId { get; set; }
    [BindProperty]
    public string BotName { get; set; } = string.Empty;
    public List<Handler> Handlers { get; set; } = new();

    public CommandHandlersModel(ILogger<CommandHandlersModel> logger, TelegramBotEngineDbContext db)
    {
        _logger = logger;
        _db = db;
    }
    
    public IActionResult OnGet(Guid id)
    {
        BotId = id;

        if (BotId == Guid.Empty)
            return RedirectToPage("/Index");

        var bot = _db.Bots.Find(BotId);
        if (bot == null)
        {
            var errorMessage = "Bot not found.";
            ModelState.AddModelError(string.Empty, errorMessage);
            _logger.LogWarning("Bot not found: {BotId}", BotId);
            return NotFound();
        }

        BotName = bot.Name;
        Handlers = _db.Handlers
            .Where(h => h.BotId == BotId)
            .OrderBy(h => h.ExternalId)
            .ToList();

        return Page();
    }
    public async Task<IActionResult> OnPostDeleteHandlerAsync(Guid id)
    {
        var handler = await _db.Handlers.FindAsync(id);

        if (handler == null)
        {
            var errorMessage = $"Handler with ID {id} not found.";
            ModelState.AddModelError(string.Empty, errorMessage);
            _logger.LogWarning(errorMessage);
            return Page(); 
        }

        _db.Handlers.Remove(handler);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Handler deleted: {HandlerId} for bot {BotId}", id, BotId);

        return RedirectToPage(new { id = BotId });
    }
}