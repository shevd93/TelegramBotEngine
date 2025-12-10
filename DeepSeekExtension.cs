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



            return false;
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
            
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpResponse = await httpClient.PostAsync("https://api.deepseek.com/chat/completions", content);

            var deepSeekResponse = new DeepSeekResponse
            {
                StatusCode = httpResponse.StatusCode,
                Content = await httpResponse.Content.ReadAsStringAsync()
            };

            return deepSeekResponse;                    
        }
    }
}
