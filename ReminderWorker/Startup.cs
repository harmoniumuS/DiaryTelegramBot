using DiaryTelegramBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReminderWorker.Data;
using ReminderWorker.Services;
using ReminderWorker.Settings;

namespace ReminderWorker
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
            services.AddDbContext<RemindContext>(options=>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
            services.Configure<RemindsSettings>(Configuration.GetSection("Reminds"));

            services.AddScoped<RemindsService>();
            services.AddHostedService<ReminderWorker>();
        }
    }
}