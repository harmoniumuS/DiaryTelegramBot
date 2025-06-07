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
            ConfigureDatabase(services);
            ConfigureTelegram(services);
            ConfigureStates(services);
            ConfigureHandlers(services);
            ConfigureOtherServices(services);
            
            services.AddMemoryCache();

            services.AddHostedService<TelegramBotService>();
            services.AddHostedService<ReminderWorker.ReminderWorker>();
        }

        private void ConfigureDatabase(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));

            services.AddDbContext<RemindContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
        }

        private void ConfigureTelegram(IServiceCollection services)
        {
            services.Configure<TelegramOptions>(Configuration.GetSection("Telegram"));

            services.AddSingleton<ITelegramBotClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
                return new TelegramBotClient(options.Token);
            });

            services.AddSingleton<BotClientWrapper>();
        }

        private void ConfigureStates(IServiceCollection services)
        {
            services.AddScoped<AwaitingAddRemindState>();
            services.AddScoped<AwaitingSelectedRemoveRemind>();
            services.AddScoped<AwaitingViewAllRemindersState>();
            services.AddScoped<AwaitingAddRecordState>();
            services.AddScoped<AwaitingRemoveRecordState>();
            services.AddScoped<AwaitingViewAllRecordsState>();
            services.AddScoped<AwaitingMainMenuState>();
            services.AddScoped<AwaitingContentState>();
            services.AddScoped<AwaitingDateState>();
            services.AddScoped<AwaitingTimeState>();
            services.AddScoped<AwaitingDeleteRecordSelection>();
            services.AddScoped<AwaitingRemoveRemindState>();
        }

        private void ConfigureHandlers(IServiceCollection services)
        {
            services.AddScoped<UserContext>();
            services.AddTransient<PressedButtonHandler>();
            services.AddScoped<MessageHandler>();
            services.AddScoped<UserStateHandler>(sp =>
            {
                var botClient = sp.GetRequiredService<ITelegramBotClient>();
                var userContext = sp.GetRequiredService<UserContext>();
                
                var awaitingMainMenuState = sp.GetRequiredService<AwaitingMainMenuState>();
                var awaitingAddRecordState = sp.GetRequiredService<AwaitingAddRecordState>();
                var awaitingRemoveRecordState = sp.GetRequiredService<AwaitingRemoveRecordState>();
                var awaitingViewAllRecordsState = sp.GetRequiredService<AwaitingViewAllRecordsState>();
                var awaitingDeleteRecordSelection = sp.GetRequiredService<AwaitingDeleteRecordSelection>();
                var awaitingRemoveRemindState = sp.GetRequiredService<AwaitingRemoveRemindState>();
                var awaitingAddRemindState = sp.GetRequiredService<AwaitingAddRemindState>();
                var awaitingViewAllRemindersState = sp.GetRequiredService<AwaitingViewAllRemindersState>();
                var awaitingSelectedRemoveRemind = sp.GetRequiredService<AwaitingSelectedRemoveRemind>();
                var awaitingContentState = sp.GetRequiredService<AwaitingContentState>();
                var awaitingDateState = sp.GetRequiredService<AwaitingDateState>();
                var awaitingTimeState = sp.GetRequiredService<AwaitingTimeState>();
                
                var statesByStatus = new Dictionary<UserStatus, IState>
                {
                    [UserStatus.None] = awaitingMainMenuState,
                    [UserStatus.AwaitingAddRecord] = awaitingAddRecordState,
                    [UserStatus.AwaitingRemoveRecord] = awaitingRemoveRecordState,
                    [UserStatus.AwaitingGetAllRecords] = awaitingViewAllRecordsState,
                    [UserStatus.AwaitingRemoveSelectedRecord] = awaitingRemoveRecordState,
                    [UserStatus.AwaitingRemoveRecord] = awaitingDeleteRecordSelection,
                    [UserStatus.AwaitingRemoveRemind] = awaitingRemoveRemindState,
                    [UserStatus.AwaitingRemind] = awaitingAddRemindState,
                    [UserStatus.AwaitingGetAllReminds] = awaitingViewAllRemindersState,
                    [UserStatus.AwaitingRemoveChoiceRemind] = awaitingSelectedRemoveRemind,
                    [UserStatus.AwaitingContent] = awaitingContentState,
                    [UserStatus.AwaitingDate] = awaitingDateState,
                    [UserStatus.AwaitingTime] = awaitingTimeState
                };
                
                var allStates = statesByStatus.Values.Distinct().ToList();

                return new UserStateHandler(allStates, botClient, userContext, statesByStatus);
            });
        }

        private void ConfigureOtherServices(IServiceCollection services)
        {
            services.AddScoped<RemindsService>();
        }
    }
}
