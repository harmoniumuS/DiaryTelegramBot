using DiaryTelegramBot;
using DiaryTelegramBot.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TelegramBotService>();

builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.Telegram));

var host = builder.Build();
host.Run();
