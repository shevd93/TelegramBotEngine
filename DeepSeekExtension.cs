using System.Net;
using System.Text;
using System.Text.Json;

namespace TelegramBotEngine
{
    public static class DeepSeekExtension
    {
        public class DeepSeekResponse
        {
            public HttpStatusCode StatusCode { get; set; }
            public string Content { get; set; } = String.Empty;
        }

        public static async Task<string> CheckDeepSeekApiKey(string apiKey)
        {
            var deepSeekResponse = await SendRequest(apiKey, "Test message");

            if (deepSeekResponse.StatusCode != HttpStatusCode.OK)
            {
                return $"DeepSeek API key is invalid. Status Code: {deepSeekResponse.StatusCode}, Content: {deepSeekResponse.Content}";
            }
            else
            {
                return "";
            }
        }
         
        public static async Task<bool> IsMessageToxic(string apiKey, string message)
        {
            //var role = "You are a content moderation assistant. Your task is to determine if the user's message contains toxic content such as hate speech, " +
            //    "harassment, or explicit material. Respond with 'TOXIC' if the message is toxic, otherwise respond with 'SAFE'.";

            var role = "Вы — помощник модератора контента. Ваша задача — определить, содержит ли сообщение пользователя токсичный контент, " +
                "такой как разжигание ненависти, домогательства или материалы откровенного характера. Ответьте 'YES', если сообщение " +
                "токсичное, в противном случае ответьте 'NO'.";

            var deepSeekResponse = await SendRequest(apiKey, string.Concat("Является ли токсичным данное сообщение: ", message), role);

            //var deepSeekResponse = await SendRequest(apiKey, string.Concat("Is the following message toxic: ", message), role);
       
            return (deepSeekResponse.StatusCode == HttpStatusCode.OK && deepSeekResponse.Content.Contains("YES"));
        }

        public static async Task<string> MusicQuiz(string apiKey)
        {
            var text = @"Сгенерируй ОДИН вопрос для квиза о музыке.
                        Доступные жанры:
                        - Поп-музыка 1990–2020-х, Инди / альтернатива / новая сцена / рок
                        ФОРМАТ ВЫВОДА — ТОЛЬКО JSON:
                        {
                          ""question"": ""Текст вопроса..."",
                          ""options"": [""Вариант А"", ""Вариант Б"", ""Вариант В"", ""Вариант Г"", ""Вариант Д""],
                          ""answer_index"": 0,
                          ""fact"": ""Пояснение: ... Источник: интервью / мемуары / техпаспорт / статья.""
                        }
                        Никакого другого текста.";

            var role = @"Ты — музыкальный историк и редактор квизов.
                        Твоя специализация: популярная музыка XX–XXI веков — поп-музыка 1990–2020-х, инди-сцена, рок.
                        ПРАВИЛА:
                        1. Ты генерируешь ТОЛЬКО ОДИН вопрос за один вызов.
                        2. Каждый вопрос — строго про ФАКТЫ, а не вкусовщину:
                           — даты, имена, инструменты, студии, инциденты, продюсеры, авторы, обложки, клипы, семплы, суды, рекорды.
                        3. 5 вариантов ответа. Только один правильный.
                        4. Правильный ответ — неочевидный, но верифицируемый.
                        5. Дистракторы — правдоподобные ошибки: реальные люди/группы/песни, которые НЕ относятся к событию.
                        6. Категорически избегать:
                           — клише («великий», «легендарный», «культовый», «голос поколения»),
                           — оценочных суждений,
                           — абстрактных рассуждений.
                        7. Формат вывода: строго валидный JSON.
                        8. Никакого текста до и после JSON.";

            try
            {
                var deepSeekResponse = await SendRequest(apiKey, text, role);
                var json = deepSeekResponse.Content.Replace("`", "");
                json = json.Replace("JSON", "");

                return json;
            }
            catch
            {
                return "";
            }
        }

        public static async Task<DeepSeekResponse> SendRequest(string apiKey, string message, string role = "You are a helpful assistant.")
        {
            using var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var requestBody = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = role
                    },
                    new
                    {
                        role = "user",
                        content = message
                    }
                },
                stream = false
 
            };
            
            var jsonRequestBody = JsonSerializer.Serialize(requestBody);
            
            var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

            var httpResponse = await httpClient.PostAsync("https://api.deepseek.com/chat/completions", content);

            var httpResponseContent = await httpResponse.Content.ReadAsStringAsync();

            var jsonResponseContent = JsonSerializer.Deserialize<JsonElement>(httpResponseContent);

            var contentResponse = jsonResponseContent
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?
                .Trim()
                .ToUpperInvariant();

            var deepSeekResponse = new DeepSeekResponse
            {
                StatusCode = httpResponse.StatusCode,
                Content = contentResponse ?? String.Empty
            };

            return deepSeekResponse;                    
        }
    }
}
