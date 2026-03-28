using Microsoft.Extensions.DependencyInjection;
using AssessMeister.Presentation.Wpf.Services;
using AssessMeister.Presentation.Wpf.ViewModels;

namespace AssessMeister.Presentation.Wpf;

public static class ServiceRegistration
{
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton<
            ShellViewModel
        >();
        services.AddSingleton<IProjectWorkspaceService, ProjectWorkspaceService>();
        services.AddScoped<IProjectWorkspaceFlowService, ProjectWorkspaceFlowService>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddTransient<ISettingsQuery, SettingsQuery>();
        services.AddTransient<ISettingWindowService, SettingWindowService>();
    }
}
