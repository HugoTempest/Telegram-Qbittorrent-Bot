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

        public async Task AddTorrent(string tUrl, bool Url)
        {
            CookieContainer cookies = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = cookies
            };
            HttpClient client = new HttpClient(handler);
            FormUrlEncodedContent loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password
            });

            HttpResponseMessage login = await client.PostAsync($"{url}/api/v2/auth/login", loginContent);

            if (!login.IsSuccessStatusCode)
                throw new Exception("Login qBittorrent échoué");

            if (login.IsSuccessStatusCode)
            {
                if (Url)
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
                else
                {
                    FileStream fs = File.OpenRead(tUrl);
                    StreamContent torrentContent = new StreamContent(fs);

                    MultipartFormDataContent addContent = new MultipartFormDataContent();

                    addContent.Add(torrentContent, "torrents", tUrl);
                    addContent.Add(new StringContent("/downloads"), "savepath");

                    HttpResponseMessage add = await client.PostAsync($"{url}/api/v2/torrents/add", addContent);

                    if (!add.IsSuccessStatusCode)
                        throw new Exception("Ajout torrent échoué");
                }
            }
        }

        public async Task<Dictionary<string, string>> ListTorrent()
        {
            CookieContainer cookies = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = cookies
            };
            HttpClient client = new HttpClient(handler);
            FormUrlEncodedContent loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password
            });

            HttpResponseMessage login = await client.PostAsync($"{url}/api/v2/auth/login", loginContent);

            if (!login.IsSuccessStatusCode)
                throw new Exception("Login qBittorrent échoué");

            if (login.IsSuccessStatusCode)
            {
                HttpResponseMessage list = await client.GetAsync($"{url}/api/v2/torrents/info");

                if (!list.IsSuccessStatusCode)
                {
                    string errorContent = await list.Content.ReadAsStringAsync();

                    throw new Exception(
                        $"Récupération de la liste échouée. " +
                        $"StatusCode: {(int)list.StatusCode} ({list.StatusCode}) " +
                        $"Body: {errorContent}");
                }

                string listStr = await list.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(listStr))
                {
                    return new Dictionary<string, string>();
                }

                using JsonDocument json = JsonDocument.Parse(listStr);
                if (json.RootElement.ValueKind != JsonValueKind.Array || json.RootElement.GetArrayLength() == 0)
                {
                    return new Dictionary<string, string>();
                }

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
