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

                if (msg.Document != null)
                {
                    string path = Path.Combine("downloads/", msg.Document.FileName);
                    string fileId = msg.Document.FileId;
                    await using FileStream stream = File.Create(path);
                    await bot.GetInfoAndDownloadFile(fileId, stream);
                    await qbService.AddTorrent(path, false);
                    break;
                }

                string[] texts = msg.Text.Split(' ');
                foreach (string text in texts)
                {
                    if (text.StartsWith("magnet:?") || text.EndsWith(".torrent"))
                    {
                        try
                        {
                            await qbService.AddTorrent(text, true);
                            await bot.SendMessage(msg.Chat.Id, "Torrent ajouté à qBittorrent");
                        }
                        catch (Exception ex)
                        {
                            await bot.SendMessage(msg.Chat.Id, $"Erreur: {ex.Message}");
                        }
                    }
                    else
                    {
                        await bot.SendMessage(msg.Chat.Id, $"Contenu de la commande non valide \"{msg.Text.Replace("/add", "")}\"");
                    }
                }
                break;
            case string s when s.StartsWith("/help"):
                await bot.SendMessage(msg.Chat.Id, "/add <magnet> ou fichier .torrent pour ajouter un torrent\n\n/list renvoie les torrents en cours et leurs états" +
                    "\n\n/remove <hashe|hashe|...> ou all pour tous les supprimer, ajouter true à la fin pour suppression de la data déjà téléchargé\n");
                break;
            case string s when s.StartsWith("/list"):
                Dictionary<string, string> dico = await qbService.ListTorrent();
                if (dico.Count == 0)
                {
                    await bot.SendMessage(msg.Chat.Id, "Aucun torrent");
                    break;
                }
                string textList = "List des torrents :\n";
                foreach (KeyValuePair<string, string> dic in dico)
                {
                    textList = textList + $"--------------------\nHash : {dic.Key}\nName : {dic.Value}\n";
                }
                await bot.SendMessage(msg.Chat.Id, textList);
                break;
        }
    }
    else
    {
        await bot.SendMessage(msg.Chat.Id, "Vous n'êtes pas autorisé à m'utiliser");
    }
}