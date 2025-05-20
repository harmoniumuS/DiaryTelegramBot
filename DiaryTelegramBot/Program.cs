using DiaryTelegramBot;
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
        
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var startup = new StartUp(hostContext.Configuration);
                startup.ConfigureServices(services);
            });
}