using ImpulseBot;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

Console.WriteLine("🤖 Запускаю бота...");
Console.WriteLine("Загружаю расписание...");
Config.LoadSchedule();

var botService = new BotService(Config.TelegramToken);
using var cts = new CancellationTokenSource();

var options = new TelegramBotClientOptions(
    Config.TelegramToken,
    baseUrl: "https://late-dust-46f7.balysevasveta000.workers.dev"
);
var botClient = new TelegramBotClient(options);

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

botClient.StartReceiving(
    updateHandler: (bot, update, ct) => botService.HandleUpdateAsync(update, ct),
    errorHandler: (bot, ex, ct) =>
    {
        Console.WriteLine($"❌ Ошибка бота: {ex.Message}");
        return Task.CompletedTask;
    },
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

await botService.StartAsync(cts.Token);
Console.WriteLine("Нажми Ctrl+C чтобы остановить.");

// Фейковый веб-сервер для Render
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
_ = Task.Run(() =>
{
    var listener = new System.Net.HttpListener();
    listener.Prefixes.Add($"http://*:{port}/");
    listener.Start();
    Console.WriteLine($"🌐 Health-сервер на порту {port}");
    while (true)
    {
        var context = listener.GetContext();
        var response = context.Response;
        var buffer = System.Text.Encoding.UTF8.GetBytes("Bot is running");
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();
    }
});

await Task.Delay(-1, cts.Token);