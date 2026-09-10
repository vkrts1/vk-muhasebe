using Avalonia;
using Avalonia.Headless;
using ErmayMuhasebe.Avalonia;
using ErmayMuhasebe.Avalonia.Tests;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]

namespace ErmayMuhasebe.Avalonia.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false
            });
}
