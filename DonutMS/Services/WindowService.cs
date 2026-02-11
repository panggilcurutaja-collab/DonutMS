using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace DonutMS.Services;

public interface IWindowService
{
    void ShowRegisterWindow();
}

public class WindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;

    public WindowService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ShowRegisterWindow()
    {
        var window = _serviceProvider.GetRequiredService<DonutMS.Views.Auth.RegisterWindow>();
        window.Owner = Application.Current?.MainWindow;
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.ShowDialog();
    }
}
