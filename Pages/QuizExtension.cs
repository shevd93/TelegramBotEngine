using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TelegramBotEngine.Models;
using static System.Net.Mime.MediaTypeNames;

namespace TelegramBotEngine.Pages
{
    public class QuizExtension
    {
        public static async Task<MusicQuizQuestion> MusicQuiz(TelegramBotEngineDbContext db)
        {
            MusicQuizQuestion musicQuizQuestion = new MusicQuizQuestion();

            var LyricsCount = await db.Lyrics.CountAsync();

            var random = new Random();

            var songNumber = random.Next(0, LyricsCount);

            var song = await db.Lyrics
                 .Skip(songNumber)
                 .FirstOrDefaultAsync();
            
            if (song == null)
            {
                return musicQuizQuestion;
            }

            var firstFourLines = string.Join(
                Environment.NewLine,
                song.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .Take(8));

            if (firstFourLines.Length > 250)
            {
                firstFourLines = firstFourLines.Substring(0, 250);
            }

            var performer = await db.PerformersOfSongs.FirstOrDefaultAsync(pe => pe.Id == song.PerformerId);

            if (performer == null)
            {
                return musicQuizQuestion;
            }

            var answer = performer.Name;

            musicQuizQuestion.Options.Add(performer.Name);

            var perfomerCount = await db.PerformersOfSongs.CountAsync();

            while (musicQuizQuestion.Options.Count() < 5)
            {
                var perfomerNumber = random.Next(0, perfomerCount);

                var randomPerformer = db.PerformersOfSongs.Skip(perfomerNumber).FirstOrDefault();

                if (randomPerformer == null)
                {
                    continue;
                }

                if (randomPerformer.Name == answer)
                {
                    continue;
                }

                if (musicQuizQuestion.Options.FirstOrDefault(op => op == randomPerformer.Name) == null)
                {
                    musicQuizQuestion.Options.Add(randomPerformer.Name);
                }
            }

            for (int i = musicQuizQuestion.Options.Count - 1; i > 0; i--)
            {                
                int j = random.Next(i + 1);
                (musicQuizQuestion.Options[i], musicQuizQuestion.Options[j]) = (musicQuizQuestion.Options[j], musicQuizQuestion.Options[i]);
            }

            var answerIndex = musicQuizQuestion.Options.FindIndex(op => op == answer);
            
            musicQuizQuestion.AnswerIndex = answerIndex;
            musicQuizQuestion.Question = string.Concat("Угадай исполнителя (это может быть кавер): \n", firstFourLines);
            //musicQuizQuestion.Question = string.Concat("Guess the artist (it could be a cover): \n", firstFourLines);

            return musicQuizQuestion;

        }
    }
}
