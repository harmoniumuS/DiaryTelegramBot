using DiaryTelegramBot.Options;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Handlers;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ReminderWorker.Data;
using ReminderWorker.Services;
using ReminderWorker.Settings;

namespace DiaryTelegramBot
{
    public class StartUp
    {
        public IConfiguration Configuration { get; }

        public StartUp(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
            
            services.AddScoped<RemindsService>();

            services.AddDbContext<RemindContext>(options=>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
            services.Configure<RemindsSettings>(Configuration.GetSection("Reminds"));
            
            
            services.AddScoped<AddRemindState>();
            services.AddScoped<RemoveRemindState>();
            services.AddScoped<UserStateHandler>();
            services.AddScoped<UserContext>();
            services.AddScoped<MessageHandler>();
            services.AddTransient<PressedButtonHandler>();
            services.AddScoped<ViewAllRemindersState>();
            services.AddScoped<AwaitingAddRecordState>();
            services.AddScoped<AwaitingRemoveRecordState>();
            services.AddScoped<ViewAllRecordsState>();
            services.AddScoped<AwaitingMainMenuState>();
            services.AddScoped<AwaitingContentState>();
            services.AddScoped<AwaitingDateState>();
            services.AddScoped<AwaitingTimeState>();
            services.AddScoped<AwaitingDeleteRecordSelection>();
            services.AddScoped<AwaitingRemoveRemindState>();
            services.AddMemoryCache(); 
            services.Configure<TelegramOptions>(Configuration.GetSection("Telegram"));

            services.AddSingleton<ITelegramBotClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
                return new TelegramBotClient(options.Token);
            });

            services.AddSingleton<BotClientWrapper>();
            
            services.AddHostedService<TelegramBotService>();
            services.AddHostedService<ReminderWorker.ReminderWorker>();
        }

    }
}