using System.ComponentModel.DataAnnotations;

namespace TelegramBotEngine.Models
{
    public class Bot
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public bool UsePulling { get; set; } = true;
        public string WebhookUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
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