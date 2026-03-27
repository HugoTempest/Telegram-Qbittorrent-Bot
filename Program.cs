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
            default: await bot.SendMessage(msg.Chat.Id, "Commande non reconnu"); break;
            case string s when s.StartsWith("/add"):

                string[] texts = msg.Text.Split(' ');
                if (texts.Any(text => text.StartsWith("magnet:?")))
                {
                    try
                    {
                        await qbService.AddMagnet(texts.FirstOrDefault(text => text.StartsWith("magnet:?")));
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
        }
    }
    else
    {
        await bot.SendMessage(msg.Chat.Id, "Vous n'êtes pas autorisé à m'utiliser");
    }
}