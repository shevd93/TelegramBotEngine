using TelegramBotEngine;

Task Worker = new Task(() => WorkerRun(args));
Worker.Start();

Task WebApp = new Task(() => WebApplicationRun(args));
WebApp.Start();

Task.WaitAll(Worker, WebApp);

static void WebApplicationRun(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddDbContext<TelegramBotEngineDbContext>();
    builder.Services.AddRazorPages();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseRouting();

    app.UseAuthorization();

    app.MapStaticAssets();
    app.MapRazorPages()
       .WithStaticAssets();

    app.Run();
}

static void WorkerRun(string[] args)
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddHostedService<Worker>();
    builder.Services.AddDbContext<TelegramBotEngineDbContext>();

    var host = builder.Build();

    host.Run();
}


                