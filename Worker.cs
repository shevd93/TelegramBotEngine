namespace TelegramBotEngine
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TelegramBotEngineDbContext _db;

        public Worker(ILogger<Worker> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
