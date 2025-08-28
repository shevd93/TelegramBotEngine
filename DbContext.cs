using Microsoft.EntityFrameworkCore;
using TelegramBotEngine.Models;

namespace TelegramBotEngine
{
    public class TelegramBotEngineDbContext : DbContext
    {
        public TelegramBotEngineDbContext() => Database.EnsureCreated();

        public DbSet<Bot> Bots => Set<Bot>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=TelegramBotEngine;Integrated Security=true;TrustServerCertificate=True; Encrypt=False;");
    }
}
