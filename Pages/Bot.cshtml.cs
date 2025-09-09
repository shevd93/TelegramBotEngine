using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TelegramBotEngine.Models;
using Telegram.BotAPI;

namespace TelegramBotEngine.Pages
{
    public class BotModel : PageModel
    {
        private readonly ILogger<Worker> _logger;
        private readonly TelegramBotEngineDbContext _db;
        [BindProperty]
        public Guid BotId { get; set; }
        public string BotName { get; set; } = string.Empty;
        public List<Chat> Chats { get; set; } = new List<Chat>();
        [BindProperty]
        public string TextMessage { get; set; } = string.Empty;
        [BindProperty]
        public Guid SelectedChat { get; set; }
        
        public BotModel(ILogger<Worker> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public void OnGet(Guid id, Guid? selectedChat)
        {
            BotId = id;

            if (selectedChat != null)
            {
                SelectedChat = selectedChat.Value;
            }

            var bot = _db.Bots.Find(id);

            if (bot is null)
            {
                RedirectToPage("/Error", new { errorMessage = "Bot not found" });
            }
            else
            {
                BotName = bot.Name;

                var chats = _db.Chats.Where(c => c.BotId == id).ToList();

                if (chats.Count > 0)
                {
                    Chats = chats;
                }
            }
        }

        async public Task<IActionResult> OnPostSendMessage()
        {
            if (SelectedChat == Guid.Empty)
            {
                return RedirectToPage("/Error", new { errorMessage = "Chat not selected" });
            }
            else if (TextMessage?.Trim().Length == 0 || TextMessage == null)
            {
                return RedirectToPage("/Error", new { errorMessage = "Message is empty" });
            }

            var chat = _db.Chats.FirstOrDefault(c => c.Id == SelectedChat);

            if (chat is null)
            {
                return RedirectToPage("/Error", new { errorMessage = "Chat not found" });
            }

            var bot = _db.Bots.FirstOrDefault(b => b.Id == BotId);

            if (bot is null)
            {
                return RedirectToPage("/Error", new { errorMessage = "Bot not found" });
            }

            var telegramBotClient = new TelegramBotClient(bot.Token);
            
            var args = new Dictionary<string, object?>
            {
                { "chat_id", chat.ExternalId },
                { "parse_mode", "HTML" },
                { "text", TextMessage }
            };

            try
            {
                await telegramBotClient.CallMethodAsync<Message>("sendMessage", args);
            }
            catch (Exception ex)
            {
                return RedirectToPage("/Error", new { errorMessage = ex.Message });
            }

            return RedirectToPage("/Bot", new { id = BotId, selectedChat = SelectedChat });
        }
    }
}
