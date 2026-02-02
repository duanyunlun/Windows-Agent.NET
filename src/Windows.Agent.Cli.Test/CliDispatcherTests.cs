using System.Text.Json;
using Windows.Agent.Interface;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Windows.Agent.Cli;
using Windows.Agent.Tools.Desktop;
using Windows.Agent.Tools.FileSystem;
using Windows.Agent.Tools.OCR;
using Windows.Agent.Tools.SystemControl;

namespace Windows.Agent.Cli.Test;

public class CliDispatcherTests
{
    [Fact]
    public async Task DesktopClick_ShouldInvokeDesktopService()
    {
        var desktop = new Mock<IDesktopService>(MockBehavior.Strict);
        desktop
            .Setup(s => s.ClickAsync(1, 2, "left", 1))
            .ReturnsAsync("ok");

        var sp = BuildServiceProvider(services =>
        {
            services.AddSingleton(desktop.Object);
            services.AddTransient<ClickTool>();
        });

        var output = new StringWriter();
        var exit = await CliDispatcher.RunAsync(
            new[] { "desktop", "click", "--x", "1", "--y", "2", "--button", "left", "--clicks", "1", "--pretty" },
            sp,
            output);

        Assert.Equal(0, exit);
        desktop.VerifyAll();

        using var doc = JsonDocument.Parse(output.ToString());
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Windows.Agent.Tools.Desktop.ClickTool.ClickAsync", doc.RootElement.GetProperty("tool").GetString());
        Assert.Equal("ok", doc.RootElement.GetProperty("result").GetProperty("raw").GetString());
    }

    [Fact]
    public async Task FileSystemRead_ShouldInvokeFileSystemService_AndExposeParsedJson()
    {
        var fs = new Mock<IFileSystemService>(MockBehavior.Strict);
        fs
            .Setup(s => s.ReadFileAsync(@"C:\temp\a.txt"))
            .ReturnsAsync(("hello", 0));

        var sp = BuildServiceProvider(services =>
        {
            services.AddSingleton(fs.Object);
            services.AddTransient<ReadFileTool>();
        });

        var output = new StringWriter();
        var exit = await CliDispatcher.RunAsync(
            new[] { "fs", "read", "--path", @"C:\temp\a.txt", "--pretty" },
            sp,
            output);

        Assert.Equal(0, exit);
        fs.VerifyAll();

        using var doc = JsonDocument.Parse(output.ToString());
        Assert.Equal("Windows.Agent.Tools.FileSystem.ReadFileTool.ReadFileAsync", doc.RootElement.GetProperty("tool").GetString());

        var parsed = doc.RootElement.GetProperty("result").GetProperty("parsed");
        Assert.True(parsed.GetProperty("success").GetBoolean());
        Assert.Equal("hello", parsed.GetProperty("content").GetString());
    }

    [Fact]
    public async Task OcrScreen_ShouldInvokeOcrService()
    {
        var ocr = new Mock<IOcrService>(MockBehavior.Strict);
        ocr
            .Setup(s => s.ExtractTextFromScreenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(("abc", 0));

        var sp = BuildServiceProvider(services =>
        {
            services.AddSingleton(ocr.Object);
            services.AddTransient<ExtractTextFromScreenTool>();
        });

        var output = new StringWriter();
        var exit = await CliDispatcher.RunAsync(
            new[] { "ocr", "screen", "--pretty" },
            sp,
            output);

        Assert.Equal(0, exit);
        ocr.VerifyAll();

        using var doc = JsonDocument.Parse(output.ToString());
        Assert.Equal(
            "Windows.Agent.Tools.OCR.ExtractTextFromScreenTool.ExtractTextFromScreenAsync",
            doc.RootElement.GetProperty("tool").GetString());

        var parsed = doc.RootElement.GetProperty("result").GetProperty("parsed");
        Assert.True(parsed.GetProperty("success").GetBoolean());
        Assert.Equal("abc", parsed.GetProperty("text").GetString());
    }

    [Fact]
    public async Task SystemVolumePercent_ShouldInvokeSystemControlService()
    {
        var sys = new Mock<ISystemControlService>(MockBehavior.Strict);
        sys
            .Setup(s => s.SetVolumePercentAsync(30))
            .ReturnsAsync("done");

        var sp = BuildServiceProvider(services =>
        {
            services.AddSingleton(sys.Object);
            services.AddTransient<VolumeTool>();
        });

        var output = new StringWriter();
        var exit = await CliDispatcher.RunAsync(
            new[] { "sys", "volume", "--percent", "30", "--pretty" },
            sp,
            output);

        Assert.Equal(0, exit);
        sys.VerifyAll();

        using var doc = JsonDocument.Parse(output.ToString());
        Assert.Equal("Windows.Agent.Tools.SystemControl.VolumeTool.SetVolumePercentAsync", doc.RootElement.GetProperty("tool").GetString());
        Assert.Equal("done", doc.RootElement.GetProperty("result").GetProperty("raw").GetString());
    }

    [Fact]
    public async Task DesktopShortcut_ShouldSplitKeysAndInvokeService()
    {
        var desktop = new Mock<IDesktopService>(MockBehavior.Strict);
        desktop
            .Setup(s => s.ShortcutAsync(It.Is<string[]>(k => k.SequenceEqual(new[] { "ctrl", "shift", "esc" }))))
            .ReturnsAsync("ok");

        var sp = BuildServiceProvider(services =>
        {
            services.AddSingleton(desktop.Object);
            services.AddTransient<ShortcutTool>();
        });

        var output = new StringWriter();
        var exit = await CliDispatcher.RunAsync(
            new[] { "desktop", "shortcut", "--keys", "ctrl+shift+esc", "--pretty" },
            sp,
            output);

        Assert.Equal(0, exit);
        desktop.VerifyAll();

        using var doc = JsonDocument.Parse(output.ToString());
        Assert.Equal("Windows.Agent.Tools.Desktop.ShortcutTool.ShortcutAsync", doc.RootElement.GetProperty("tool").GetString());
        Assert.Equal("ok", doc.RootElement.GetProperty("result").GetProperty("raw").GetString());
    }

    private static ServiceProvider BuildServiceProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        configure(services);
        return services.BuildServiceProvider();
    }
}
