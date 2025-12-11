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
