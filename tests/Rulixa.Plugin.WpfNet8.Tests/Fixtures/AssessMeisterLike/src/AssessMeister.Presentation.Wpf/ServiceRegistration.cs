using Microsoft.Extensions.DependencyInjection;
using AssessMeister.Presentation.Wpf.Services;
using AssessMeister.Presentation.Wpf.ViewModels;

namespace AssessMeister.Presentation.Wpf;

public static class ServiceRegistration
{
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<ISettingWindowService, SettingWindowService>();
    }
}
