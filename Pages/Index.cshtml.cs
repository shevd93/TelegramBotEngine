using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using TelegramBotEngine.Models;

namespace TelegramBotEngine.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly TelegramBotEngineDbContext _db;
        private string ErrorMessage { get; set; } = string.Empty;
        public List<Bot> Bots { get; set; } = new List<Bot>();

        [Display(Name = "ImagesInfList.json path")]
        [BindProperty]
        public IFormFile? MemsInfoFile { get; set; }
        
        [BindProperty]
        public List<IFormFile> Lyrics {  get; set; } = new List<IFormFile>();

        public IndexModel(ILogger<IndexModel> logger, TelegramBotEngineDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public void OnGet()
        {
            Bots = _db.Bots.ToList();

            _logger.LogInformation("Index page loaded");
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var bot = await _db.Bots.FindAsync(id);

            if (bot != null)
            {
                if (bot.IsActive)
                {
                    ErrorMessage = string.Concat("Cannot delete an active bot. Please stop the bot before deletion. Id: ", bot.Id);
                }
                else
                {
                    _db.Bots.Remove(bot);
                    
                    try
                    {
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("Bot deleted. Id: {Id}", bot.Id);
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = string.Concat("Bot deletion logging failed. Id: ", bot.Id, ". Error: ", ex.Message);
                    }
                }
            }
            else
            {
                ErrorMessage = string.Concat("Bot deletion failed. Bot not found. Id: ", id);
            }

            return RedirectToNextPage();
        }

        public async Task<IActionResult> OnPostStart(Guid id)
        {
            var bot = await _db.Bots.FindAsync(id);

            if (bot != null)
            {
                if (!bot.IsActive)
                {
                    bot.IsActive = true;

                    try
                    {
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = string.Concat("Bot start failed due to an internal error. Id: ", bot.Id, ". Error: ", ex.Message);
                        bot.IsActive = false;
                    }
                }
            }
            else
            {
                ErrorMessage = string.Concat("Bot start failed. Bot not found. Id: ", id);
            }

            return RedirectToNextPage();
        }

        public async Task<IActionResult> OnPostStop(Guid id)
        {
            var bot = await _db.Bots.FindAsync(id);

            if (bot != null)
            {
                if (bot.IsActive)
                {
                    bot.IsActive = false;

                    try
                    {                         
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = string.Concat("Bot stop failed due to an internal error. Id: ", bot.Id, ". Error: ", ex.Message);
                        bot.IsActive = true;
                    }
                }
            }
            else
            {
                ErrorMessage = string.Concat("Bot stop failed. Bot not found. Id: ", id);
            }

            return RedirectToNextPage();
        }

        public async Task<IActionResult> OnPostUploadMemsInfo()
        {
            if (MemsInfoFile == null)
            {
                ErrorMessage = "File not selected.";
                return RedirectToNextPage();
            }
            
            var extension = Path.GetExtension(MemsInfoFile.FileName).ToLowerInvariant();

            if (extension != ".json")
            {
                ErrorMessage = "The file must be in .json format.";
                return RedirectToNextPage();
            }

            try
            {
                using var stream = new MemoryStream();

                await MemsInfoFile.CopyToAsync(stream);
                stream.Position = 0;

                var jsonContent = Encoding.UTF8.GetString(stream.ToArray());
                var imagesInfList = JsonSerializer.Deserialize<List<Mem>>(jsonContent);

                if (imagesInfList != null && imagesInfList.Count > 0)
                {
                    foreach (Mem imageInf in imagesInfList)
                    {
                        var imageInfInDb = await _db.Mems.FirstOrDefaultAsync(im => im.Url == imageInf.Url);

                        if (imageInfInDb == null)
                        {
                            _db.Mems.Add(new Mem{Url = imageInf.Url});
                        }
                    }

                    await _db.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "File uploaded successfully!";
            }
            catch (JsonException ex)
            {
                ErrorMessage = $"Error reading JSON: {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Loading error: {ex.Message}";
            }
            
            return RedirectToNextPage();
        }

        public async Task<IActionResult> OnPostUploadLyricsAsync()
        {
            if (Lyrics == null || Lyrics.Count == 0)
            {
                ErrorMessage = "Please select a folder with lyrics.";
                return RedirectToNextPage();
            }

            var root = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "UploadedLyrics");

            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            Directory.CreateDirectory(root);

            foreach (var file in Lyrics)
            {
                var relativePath = file.FileName.Replace('\\', '/');

                relativePath = relativePath.TrimStart('/');

                if (relativePath.Contains(".."))
                {
                    continue;
                }

                var savePath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                
                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

                await using var fs = System.IO.File.Create(savePath);
                await file.CopyToAsync(fs);
            }

            foreach (var patch in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, patch);
                var ext = Path.GetExtension(patch).ToLowerInvariant();
                var performer = new DirectoryInfo(Path.GetDirectoryName(relative)!).Name;
                var title = Path.GetFileNameWithoutExtension(relative);

                var performerInDb = await _db.PerformersOfSongs.FirstOrDefaultAsync(ps => ps.Name == performer);
              
                Guid performerId;

                if (performerInDb == null)
                {
                    performerId = Guid.NewGuid();
                    _db.PerformersOfSongs.Add(new SongPerformer {Name = performer, Id = performerId});
                    await _db.SaveChangesAsync();
                }
                else
                {
                    performerId = performerInDb.Id;
                }

                if (ext != ".txt")
                {
                    continue;
                }

                var songInDb = await _db.Lyrics.FirstOrDefaultAsync(s => s.PerformerId == performerId && s.Title == title);

                if (songInDb != null)
                {
                    continue;
                }

                var text = await System.IO.File.ReadAllTextAsync(patch);

                text = string.Join(
                    Environment.NewLine,
                    text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .Where(line => !string.IsNullOrWhiteSpace(line)));

                _db.Lyrics.Add(new Lyrics { PerformerId = performerId, Text = text, Title = title});
                await _db.SaveChangesAsync();
            }

            Directory.Delete(root, true);

            TempData["SuccessMessage"] = "File uploaded successfully!";

            return RedirectToNextPage();
        }

        private IActionResult RedirectToNextPage()
        {
            if (ErrorMessage == string.Empty)
            {
                return RedirectToPage("/index");
            }
            else
            {
                _logger.LogError(ErrorMessage);
                return RedirectToPage("/error", new { errorMessage = ErrorMessage });
            }
        }
    }
}
