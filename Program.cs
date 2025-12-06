using Microsoft.EntityFrameworkCore;
using TelegramBotEngine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TelegramBotEngineDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<Worker>();
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();
app.UseStaticFiles();

app.Run();

