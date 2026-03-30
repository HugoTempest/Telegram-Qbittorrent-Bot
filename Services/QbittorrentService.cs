using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace RedskyBot.Services
{
    public class QbittorrentService
    {
        private readonly string url;
        private readonly string username;
        private readonly string password;

        public QbittorrentService(IConfiguration config)
        {
            url = config["Qbittorrent:Url"];
            username = config["Qbittorrent:Username"];
            password = config["Qbittorrent:Password"];
        }

        public HttpClient Client()
        {
            CookieContainer cookies = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = cookies
            };
            using HttpClient client = new HttpClient(handler);
            return client;
        }
        public async Task<bool> Login(HttpClient client)
        {
            FormUrlEncodedContent loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password
            });

            HttpResponseMessage login = await client.PostAsync($"{url}/api/v2/auth/login", loginContent);

            if (!login.IsSuccessStatusCode)
                throw new Exception("Login qBittorrent échoué");
            return login.IsSuccessStatusCode;
        }

        public async Task AddTorrent(string tUrl)
        {
            HttpClient client = Client();
            if (Login(client).Result)
            {
                FormUrlEncodedContent addContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["urls"] = tUrl,
                    ["savepath"] = "/downloads",
                });

                HttpResponseMessage add = await client.PostAsync($"{url}/api/v2/torrents/add", addContent);

                if (!add.IsSuccessStatusCode)
                    throw new Exception("Ajout torrent échoué");
            }
        }

        public async Task<Dictionary<string, string>> ListTorrent()
        {
            HttpClient client = Client();
            if (Login(client).Result)
            {
                HttpResponseMessage list = await client.GetAsync($"{url}/api/v2/torrents/info");

                if (!list.IsSuccessStatusCode)
                    throw new Exception("Récupération de la liste échoué");

                string listStr = await list.Content.ReadAsStringAsync();
                using JsonDocument json = JsonDocument.Parse(listStr);
                Dictionary<string, string> dico = new Dictionary<string, string>();

                foreach (JsonElement element in json.RootElement.EnumerateArray())
                {
                    string hash = element.GetProperty("hash").GetString();
                    string name = element.GetProperty("name").GetString();
                    dico.Add(hash, name);
                }
                return dico;
            }
            throw new Exception("Login to QbitTorrent failed");
        }
    }
}
