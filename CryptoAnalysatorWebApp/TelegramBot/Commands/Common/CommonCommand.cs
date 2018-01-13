using Telegram.Bot;
using Telegram.Bot.Types;
using System.Net.Http;


namespace CryptoAnalysatorWebApp.TelegramBot.Commands.Common
{
    public abstract class CommonCommand {
        public abstract string Name { get; }

        public abstract void Execute(Message message, TelegramBotClient client, string channelId = null);

        protected string GetResponse(int port) {
            using (HttpClient httpClient = new HttpClient()) {
                using (HttpResponseMessage respMessage = httpClient.GetAsync($"http://localhost:{port}/api/actualpairs").Result) {
                    using (HttpContent httpContent = respMessage.Content) {
                        string responseStr = httpContent.ReadAsStringAsync().Result;
                        return responseStr;
                    }
                }
            }
        }
    }
}
