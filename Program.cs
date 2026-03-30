using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using RedskyBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CancellationTokenSource cts = new CancellationTokenSource();

var config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddEnvironmentVariables().Build();

long[] allowedChatIds = config
    .GetSection("Telegram:AllowedChatIds")
    .Get<long[]>() ?? [];

string token = config["Telegram:Token"]
    ?? throw new Exception("Le token Telegram est introuvable.");

TelegramBotClient bot = new TelegramBotClient(token, cancellationToken: cts.Token);

QbittorrentService qbService = new QbittorrentService(config);

User me = await bot.GetMe();
bot.OnError += OnError;
bot.OnMessage += OnMessage;

Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");

await Task.Delay(Timeout.Infinite, cts.Token);

async Task OnError(Exception exception, HandleErrorSource source)
{
    Console.WriteLine(exception);
}
async Task OnMessage(Message msg, UpdateType type)
{
    Console.WriteLine(msg.Chat.Id);
    if (allowedChatIds.Contains(msg.Chat.Id))
    {
        switch (msg.Text)
        {
            default: await bot.SendMessage(msg.Chat.Id, "Commande non reconnu, faire /help pour plus de commande"); break;
            case string s when s.StartsWith("/add"):

                string[] texts = msg.Text.Split(' ');
                if (texts.Any(text => text.StartsWith("magnet:?") || text.EndsWith(".torrent")))
                {
                    try
                    {
                        await qbService.AddTorrent(texts.FirstOrDefault(text => text.StartsWith("magnet:?") || text.EndsWith(".torrent")));
                        await bot.SendMessage(msg.Chat.Id, "Torrent ajouté à qBittorrent");
                        break;
                    }
                    catch (Exception ex)
                    {
                        await bot.SendMessage(msg.Chat.Id, $"Erreur: {ex.Message}");
                        break;
                    }
                }
                else
                {
                    await bot.SendMessage(msg.Chat.Id, $"Contenu de la commande non valide \"{msg.Text.Replace("/add", "")}\"");
                    break;
                }
            case string s when s.StartsWith("/help"):
                await bot.SendMessage(msg.Chat.Id, "/add <magnet> ou fichier .torrent pour ajouter un torrent\n/list renvoie les torrents en cours et leurs états" +
                    "/remove <hashe|hashe|...> ou all pour tous les supprimer, ajouter true à la fin pour suppression de la data déjà téléchargé\n");
                break;
            case string s when s.StartsWith("/list"):
                string text = "List des torrents \n";
                Dictionary<string, string> dico = await qbService.ListTorrent();
                foreach (KeyValuePair<string, string> dic in dico)
                {
                    text = text + $"--------------------\nHash : ${dic.Key}\nName : ${dic.Value}\n";
                }
                await bot.SendMessage(msg.Chat.Id, text);
                break;
        }
    }
    else
    {
        await bot.SendMessage(msg.Chat.Id, "Vous n'êtes pas autorisé à m'utiliser");
    }
}