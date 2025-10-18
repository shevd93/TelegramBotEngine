using Microsoft.EntityFrameworkCore;
using TelegramBotEngine.Models;

namespace TelegramBotEngine
{
    public class TelegramBotEngineDbContext : DbContext
    {
        public DbSet<Bot> Bots => Set<Bot>();
        public DbSet<UpdateId> UpdateIds => Set<UpdateId>();
        public DbSet<FromUser> FromUsers => Set<FromUser>();
        public DbSet<Chat> Chats => Set<Chat>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Photo> Photos => Set<Photo>();
        public DbSet<Video> Video => Set<Video>();
        public DbSet<Handler> Handlers => Set<Handler>();   

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=TelegramBotEngine;Integrated Security=true;TrustServerCertificate=True; Encrypt=False;");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FromUser>().HasIndex(f => f.ExternalId);
            modelBuilder.Entity<Chat>().HasIndex(c => new{ c.ExternalId, c.BotId});
            modelBuilder.Entity<Message>().HasIndex(m => new { m.ExternalId, m.FromUserId, m.ChatId});
            modelBuilder.Entity<Photo>().HasIndex(p => new { p.FileId, p.FileUniqueId, p.MessageId });
            modelBuilder.Entity<Video>().HasIndex(v => new { v.FileId, v.FileUniqueId, v.MessageId });
            modelBuilder.Entity<Handler>().HasIndex(h => new { h.ExternalId, h.BotId });
        }
    }
}
