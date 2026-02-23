using Microsoft.Extensions.AI;

namespace BlazorChatDemo.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCopilotSdk(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<CopilotOptions>(configuration.GetSection(CopilotOptions.SectionName));
        services.AddSingleton<CopilotChatService>();
        services.AddSingleton<NorthwindToolsProvider>();

        services.AddChatClient(sp =>
        {
            var service = sp.GetRequiredService<CopilotChatService>();
            var toolsProvider = sp.GetRequiredService<NorthwindToolsProvider>();

            service.Tools = toolsProvider.Tools;
            service.SystemMessage = CopilotChatDefaults.SystemPrompt;

            return new CopilotChatClient(service);
        });

        return services;
    }
}
