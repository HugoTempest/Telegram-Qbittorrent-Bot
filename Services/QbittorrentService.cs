using System.Net;
using System.Text;
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

        public async Task AddMagnet(string magnet)
        {
            CookieContainer cookies = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = cookies
            };

            using HttpClient client = new HttpClient(handler);

            FormUrlEncodedContent loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password
            });

            HttpResponseMessage login = await client.PostAsync($"{url}/api/v2/auth/login", loginContent);

            if (!login.IsSuccessStatusCode)
                throw new Exception("Login qBittorrent échoué");

            FormUrlEncodedContent addContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["urls"] = magnet,
                ["savepath"] = "/downloads",
            });

            HttpResponseMessage add = await client.PostAsync($"{url}/api/v2/torrents/add", addContent);

            if (!add.IsSuccessStatusCode)
                throw new Exception("Ajout torrent échoué");
        }
    }
}
