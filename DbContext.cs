using Microsoft.EntityFrameworkCore;
using TelegramBotEngine.Models;

namespace TelegramBotEngine
{
    public class TelegramBotEngineDbContext : DbContext
    {
        public TelegramBotEngineDbContext(DbContextOptions<TelegramBotEngineDbContext> options) : base(options) { }

        public DbSet<Bot> Bots => Set<Bot>();
        public DbSet<UpdateId> UpdateIds => Set<UpdateId>();
        public DbSet<FromUser> FromUsers => Set<FromUser>();
        public DbSet<Chat> Chats => Set<Chat>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Photo> Photos => Set<Photo>();
        public DbSet<Video> Video => Set<Video>();
        public DbSet<Handler> Handlers => Set<Handler>();
        public DbSet<ToxicUser> ToxicUsers => Set<ToxicUser>();
        public DbSet<KPI> KPIs => Set<KPI>();
        public DbSet<Mem> Mems => Set<Mem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FromUser>().HasIndex(f => f.ExternalId);
            modelBuilder.Entity<Chat>().HasIndex(c => new { c.ExternalId, c.BotId });
            modelBuilder.Entity<Message>().HasIndex(m => new { m.ExternalId, m.ReplyToMessageExternalId, m.FromUserId, m.ChatId});
            modelBuilder.Entity<Photo>().HasIndex(p => new { p.FileId, p.FileUniqueId, p.MessageId });
            modelBuilder.Entity<Video>().HasIndex(v => new { v.FileId, v.FileUniqueId, v.MessageId });
            modelBuilder.Entity<Handler>().HasIndex(h => new { h.ExternalId, h.BotId });
            modelBuilder.Entity<ToxicUser>().HasIndex(tu => new { tu.FromUserId, tu.ChatId });
            modelBuilder.Entity<KPI>().HasIndex(k => new { k.FromUserId, k.ChatId });

            base.OnModelCreating(modelBuilder);
        }
    }
}
