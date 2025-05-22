using DiaryTelegramBot.Data;
using DiaryTelegramBot.Service;
using DiaryTelegramBot.Service.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;


namespace DiaryTelegramBot
{
    public class StartUp
    {
        public IConfiguration Configuration { get; }

        public StartUp(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<RemindContext>(options=>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
            services.Configure<RemindsSettings>(Configuration.GetSection("Reminds"));

            services.AddScoped<RemindsService>();
            services.AddHostedService<Service.ReminderWorker>();
            return services;
        }
    }
}