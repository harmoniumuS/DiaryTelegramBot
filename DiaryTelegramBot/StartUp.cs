using DiaryTelegramBot.Options;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using DiaryTelegramBot.Data;
using DiaryTelegramBot.Handlers;
using DiaryTelegramBot.Service;
using DiaryTelegramBot.States;
using DiaryTelegramBot.Wrappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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
            
            services.AddSingleton<IMemoryCache, MemoryCache>();
            services.AddSingleton<UserStateService>();
            services.AddScoped<AddRemindHandler>();
            services.AddScoped<RemoveRemindHandler>();
            services.AddScoped<UserStateHandler>();
            services.AddScoped<UserDataService>();
            services.AddScoped<MessageHandler>();
            services.AddScoped<TelegramBotService>();
            services.AddTransient<CallBackQueryHandler>();
            services.AddScoped<ViewAllRemindersHandler>();
            services.AddScoped<AddRecordHandler>();
            services.AddScoped<RemoveRecordHandler>();
            services.AddScoped<ViewAllRecordsHandler>();
            
            services.Configure<TelegramOptions>(Configuration.GetSection("Telegram"));
            
            services.AddSingleton<ITelegramBotClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
                return new TelegramBotClient(options.Token);
            });
            
            services.AddSingleton<BotClientWrapper>();
            
            services.AddHostedService<TelegramBotService>();
            services.AddHostedService<ReminderService>();
        }
    }
}