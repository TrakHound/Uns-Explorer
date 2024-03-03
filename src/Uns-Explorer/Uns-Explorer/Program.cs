using Radzen;
using Uns.Explorer.Pages;
using Uns.Explorer.Services;

namespace Uns.Explorer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add ConnectionService (used for connection configurations)
            var connectionService = new ConnectionService();
            connectionService.Load();
            builder.Services.AddSingleton<ConnectionService>(connectionService);

            builder.Services.AddScoped<ClientService>();
            builder.Services.AddScoped<ConnectionListService>();
            builder.Services.AddScoped<FormatService>();

            builder.Services.AddRadzenComponents();
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseAntiforgery();
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
            app.Run();
        }
    }
}
