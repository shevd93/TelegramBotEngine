using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using TelegramBotEngine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TelegramBotEngineDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<Worker>();
builder.Services.AddRazorPages();
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = 10000;
    //options.MultipartBodyLengthLimit = 
});

var app = builder.Build();

using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<TelegramBotEngineDbContext>().Database.EnsureCreated();

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

