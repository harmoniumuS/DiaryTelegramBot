using DiaryTelegramBot;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Options;
using Microsoft.Extensions.Options;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TelegramBotService>();

builder.Services.AddSingleton<ITelegramBotClient>(sp=>
{
    var options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
    return new TelegramBotClient(options.Token);
});

builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.Telegram));

var host = builder.Build();
host.Run();
