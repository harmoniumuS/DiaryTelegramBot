using DiaryTelegramBot.Options;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Handlers;
using DiaryTelegramBot.Wrappers;
using Microsoft.EntityFrameworkCore;
using ReminderWorker.Data;

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
            services.AddScoped<AddRemindState>();
            services.AddScoped<RemoveRemindState>();
            services.AddScoped<UserStateHandler>();
            services.AddScoped<UserContext>();
            services.AddScoped<MessageHandler>();
            services.AddScoped<TelegramBotService>();
            services.AddTransient<PressedButtonHandler>();
            services.AddScoped<ViewAllRemindersState>();
            services.AddScoped<AddRecordState>();
            services.AddScoped<RemoveRecordState>();
            services.AddScoped<ViewAllRecordsState>();
            
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