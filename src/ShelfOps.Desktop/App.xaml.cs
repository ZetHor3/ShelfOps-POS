using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ShelfOps.Desktop.Services;
using ShelfOps.Desktop.Views;

namespace ShelfOps.Desktop;

public partial class App : Application
{
    private readonly ServiceProvider _services;

    public App()
    {
        _services = new ServiceCollection()
            .AddSingleton(new ApiClient("http://localhost:5000"))
            .AddSingleton<ShellView>()
            .BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var shell = _services.GetRequiredService<ShellView>();
        shell.Show();
    }
}