using Microsoft.Extensions.DependencyInjection;
using ReferenceWorkspace.Presentation.Wpf.Services;
using ReferenceWorkspace.Presentation.Wpf.ViewModels;

namespace ReferenceWorkspace.Presentation.Wpf;

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

