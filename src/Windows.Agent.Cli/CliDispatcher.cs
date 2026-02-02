using Microsoft.Extensions.DependencyInjection;
using Windows.Agent.Tools.Desktop;
using Windows.Agent.Tools.FileSystem;
using Windows.Agent.Tools.SystemControl;
using Windows.Agent.Tools.OCR;

namespace Windows.Agent.Cli;

internal static class CliDispatcher
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
        => await RunAsync(args, services, Console.Out);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services, TextWriter output)
    {
        try
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                PrintHelp(output);
                return 0;
            }

            var group = args[0].ToLowerInvariant();
            var action = args.Length > 1 ? args[1].ToLowerInvariant() : string.Empty;
            var optionArgs = args.Skip(2).ToArray();
            var options = CliOptions.Parse(optionArgs);

            var pretty = options.GetBool("pretty", false);

            switch (group)
            {
                case "desktop":
                    return await RunDesktopAsync(action, options, services, pretty, output);
                case "fs":
                case "filesystem":
                    return await RunFileSystemAsync(action, options, services, pretty, output);
                case "ocr":
                    return await RunOcrAsync(action, options, services, pretty, output);
                case "sys":
                case "system":
                    return await RunSystemAsync(action, options, services, pretty, output);
                default:
                    throw new ArgumentException($"Unknown command group: {group}");
            }
        }
        catch (Exception ex)
        {
            var pretty = args.Any(a => a.Equals("--pretty", StringComparison.OrdinalIgnoreCase));
            var payload = new { success = false, error = ex.Message };
            output.WriteLine(CliJson.Serialize(payload, pretty));
            return 1;
        }
    }

    private static bool IsHelp(string token)
        => token.Equals("help", StringComparison.OrdinalIgnoreCase) ||
           token.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
           token.Equals("--help", StringComparison.OrdinalIgnoreCase);

    private static void PrintHelp(TextWriter output)
    {
        output.WriteLine(
@"Windows Agent CLI (non-MCP)

用法：
  windows-agent <group> <action> [options]

通用 options：
  --pretty             JSON 美化输出

示例：
  windows-agent desktop click --x 100 --y 200
  windows-agent fs read --path ""C:\temp\a.txt""

Desktop：
  desktop state [--vision true|false]
  desktop launch --name <app>
  desktop switch --name <app>
  desktop windowinfo --name <app>
  desktop click --x <int> --y <int> [--button left|right|middle] [--clicks <int>]
  desktop move --x <int> --y <int>
  desktop drag --startx <int> --starty <int> --endx <int> --endy <int>
  desktop scroll [--x <int>] [--y <int>] [--type vertical|horizontal] [--direction up|down|left|right] [--wheel <int>]
  desktop type --x <int> --y <int> --text <string> [--clear] [--enter] [--interpretSpecialCharacters]
  desktop key --key <string>
  desktop shortcut --keys <string> 例：""ctrl+shift+esc""
  desktop clipboard --mode copy|paste [--text <string>]
  desktop wait --seconds <int>
  desktop screenshot
  desktop openbrowser [--url <string>] [--search <string>]
  desktop scrape --url <string>
  desktop powershell --command <string>
  desktop ui-find --type text|className|automationId --value <string>
  desktop ui-props --x <int> --y <int>
  desktop ui-wait --type text|className|automationId --value <string> [--timeoutMs <int>]

FileSystem：
  fs read --path <string>
  fs write --path <string> --content <string> [--append]
  fs create --path <string> --content <string>
  fs delete --path <string>
  fs copy --source <string> --destination <string> [--overwrite]
  fs move --source <string> --destination <string> [--overwrite]
  fs info --path <string>
  fs list --path <string> [--files true|false] [--dirs true|false] [--recursive]
  fs mkdir --path <string> [--parents]
  fs rmdir --path <string> [--recursive]
  fs search-name --directory <string> --pattern <string> [--recursive]
  fs search-ext --directory <string> --ext <string> [--recursive]

OCR：
  ocr screen
  ocr region --x <int> --y <int> --width <int> --height <int>
  ocr find --text <string>
  ocr coords --text <string>
  ocr file --path <string>

System：
  sys volume [--inc|--dec] [--percent <int>] [--mute true|false]
  sys brightness [--inc|--dec] [--percent <int>]
  sys resolution --type high|medium|low

说明：
  CLI 内部调用现存 Windows.Agent.Tools.* 类（而不是直接调用 Service）。
");
    }

    private static int WriteToolResult(string tool, bool pretty, string raw, TextWriter output)
    {
        object payload;
        if (CliJson.TryParse(raw, out var parsed))
        {
            payload = new { success = true, tool, result = new { raw, parsed } };
        }
        else
        {
            payload = new { success = true, tool, result = new { raw } };
        }

        output.WriteLine(CliJson.Serialize(payload, pretty));
        return 0;
    }

    private static async Task<int> RunDesktopAsync(string action, CliOptions options, IServiceProvider services, bool pretty, TextWriter output)
    {
        switch (action)
        {
            case "click":
            {
                var tool = services.GetRequiredService<ClickTool>();
                var x = options.RequireInt("x");
                var y = options.RequireInt("y");
                var button = options.GetString("button", "left") ?? "left";
                var clicks = options.GetInt("clicks", 1);
                var raw = await tool.ClickAsync(x, y, button, clicks);
                return WriteToolResult("Windows.Agent.Tools.Desktop.ClickTool.ClickAsync", pretty, raw, output);
            }
            case "move":
            {
                var tool = services.GetRequiredService<MoveTool>();
                var x = options.RequireInt("x");
                var y = options.RequireInt("y");
                var raw = await tool.MoveAsync(x, y);
                return WriteToolResult("Windows.Agent.Tools.Desktop.MoveTool.MoveAsync", pretty, raw, output);
            }
            case "scroll":
            {
                var tool = services.GetRequiredService<ScrollTool>();
                var x = options.Has("x") ? options.RequireInt("x") : (int?)null;
                var y = options.Has("y") ? options.RequireInt("y") : (int?)null;
                var type = options.GetString("type", "vertical") ?? "vertical";
                var direction = options.GetString("direction", "down") ?? "down";
                var wheelTimes = options.GetInt("wheel", 1);
                var raw = await tool.ScrollAsync(x, y, type, direction, wheelTimes);
                return WriteToolResult("Windows.Agent.Tools.Desktop.ScrollTool.ScrollAsync", pretty, raw, output);
            }
            case "type":
            {
                var tool = services.GetRequiredService<TypeTool>();
                var x = options.RequireInt("x");
                var y = options.RequireInt("y");
                var text = options.RequireString("text");
                var clear = options.GetBool("clear", false);
                var pressEnter = options.GetBool("enter", false);
                var interpret = options.GetBool("interpretSpecialCharacters", false);
                var raw = await tool.TypeAsync(x, y, text, clear, pressEnter, interpret);
                return WriteToolResult("Windows.Agent.Tools.Desktop.TypeTool.TypeAsync", pretty, raw, output);
            }
            case "state":
            {
                var tool = services.GetRequiredService<StateTool>();
                var useVision = options.GetBool("vision", false);
                var raw = await tool.GetDesktopStateAsync(useVision);
                return WriteToolResult("Windows.Agent.Tools.Desktop.StateTool.GetDesktopStateAsync", pretty, raw, output);
            }
            case "launch":
            {
                var tool = services.GetRequiredService<LaunchTool>();
                var name = options.RequireString("name");
                var raw = await tool.LaunchAppAsync(name);
                return WriteToolResult("Windows.Agent.Tools.Desktop.LaunchTool.LaunchAppAsync", pretty, raw, output);
            }
            case "switch":
            {
                var tool = services.GetRequiredService<SwitchTool>();
                var name = options.RequireString("name");
                var raw = await tool.SwitchAppAsync(name);
                return WriteToolResult("Windows.Agent.Tools.Desktop.SwitchTool.SwitchAppAsync", pretty, raw, output);
            }
            case "windowinfo":
            {
                var tool = services.GetRequiredService<GetWindowInfoTool>();
                var name = options.RequireString("name");
                var raw = await tool.GetWindowInfoAsync(name);
                return WriteToolResult("Windows.Agent.Tools.Desktop.GetWindowInfoTool.GetWindowInfoAsync", pretty, raw, output);
            }
            case "powershell":
            {
                var tool = services.GetRequiredService<PowershellTool>();
                var command = options.RequireString("command");
                var raw = await tool.ExecuteCommandAsync(command);
                return WriteToolResult("Windows.Agent.Tools.Desktop.PowershellTool.ExecuteCommandAsync", pretty, raw, output);
            }
            case "screenshot":
            {
                var tool = services.GetRequiredService<ScreenshotTool>();
                var raw = await tool.TakeScreenshotAsync();
                return WriteToolResult("Windows.Agent.Tools.Desktop.ScreenshotTool.TakeScreenshotAsync", pretty, raw, output);
            }
            case "openbrowser":
            {
                var tool = services.GetRequiredService<OpenBrowserTool>();
                var url = options.GetString("url");
                var search = options.GetString("search");
                var raw = await tool.OpenBrowserAsync(url, search);
                return WriteToolResult("Windows.Agent.Tools.Desktop.OpenBrowserTool.OpenBrowserAsync", pretty, raw, output);
            }
            case "scrape":
            {
                var tool = services.GetRequiredService<ScrapeTool>();
                var url = options.RequireString("url");
                var raw = await tool.ScrapeAsync(url);
                return WriteToolResult("Windows.Agent.Tools.Desktop.ScrapeTool.ScrapeAsync", pretty, raw, output);
            }
            case "wait":
            {
                var tool = services.GetRequiredService<WaitTool>();
                var seconds = options.RequireInt("seconds");
                var raw = await tool.WaitAsync(seconds);
                return WriteToolResult("Windows.Agent.Tools.Desktop.WaitTool.WaitAsync", pretty, raw, output);
            }
            case "clipboard":
            {
                var tool = services.GetRequiredService<ClipboardTool>();
                var mode = options.RequireString("mode");
                var text = options.GetString("text");
                var raw = await tool.ClipboardAsync(mode, text);
                return WriteToolResult("Windows.Agent.Tools.Desktop.ClipboardTool.ClipboardAsync", pretty, raw, output);
            }
            case "drag":
            {
                var tool = services.GetRequiredService<DragTool>();
                var startX = options.RequireInt("startx");
                var startY = options.RequireInt("starty");
                var endX = options.RequireInt("endx");
                var endY = options.RequireInt("endy");
                var raw = await tool.DragAsync(startX, startY, endX, endY);
                return WriteToolResult("Windows.Agent.Tools.Desktop.DragTool.DragAsync", pretty, raw, output);
            }
            case "resize":
            {
                var tool = services.GetRequiredService<ResizeTool>();
                var name = options.RequireString("name");
                var width = options.Has("width") ? options.RequireInt("width") : (int?)null;
                var height = options.Has("height") ? options.RequireInt("height") : (int?)null;
                var x = options.Has("x") ? options.RequireInt("x") : (int?)null;
                var y = options.Has("y") ? options.RequireInt("y") : (int?)null;
                var raw = await tool.ResizeAppAsync(name, width, height, x, y);
                return WriteToolResult("Windows.Agent.Tools.Desktop.ResizeTool.ResizeAppAsync", pretty, raw, output);
            }
            case "key":
            {
                var tool = services.GetRequiredService<KeyTool>();
                var key = options.RequireString("key");
                var raw = await tool.KeyAsync(key);
                return WriteToolResult("Windows.Agent.Tools.Desktop.KeyTool.KeyAsync", pretty, raw, output);
            }
            case "shortcut":
            {
                var tool = services.GetRequiredService<ShortcutTool>();
                var keysRaw = options.RequireString("keys");
                var keys = SplitKeys(keysRaw);
                var raw = await tool.ShortcutAsync(keys);
                return WriteToolResult("Windows.Agent.Tools.Desktop.ShortcutTool.ShortcutAsync", pretty, raw, output);
            }
            case "ui-find":
            {
                var tool = services.GetRequiredService<UIElementTool>();
                var type = options.RequireString("type");
                var value = options.RequireString("value");
                var raw = type.ToLowerInvariant() switch
                {
                    "text" => await tool.FindElementByTextAsync(value),
                    "classname" => await tool.FindElementByClassNameAsync(value),
                    "automationid" => await tool.FindElementByAutomationIdAsync(value),
                    _ => throw new ArgumentException("Invalid --type. Allowed: text, className, automationId")
                };
                return WriteToolResult("Windows.Agent.Tools.Desktop.UIElementTool.(Find*)", pretty, raw, output);
            }
            case "ui-props":
            {
                var tool = services.GetRequiredService<UIElementTool>();
                var x = options.RequireInt("x");
                var y = options.RequireInt("y");
                var raw = await tool.GetElementPropertiesAsync(x, y);
                return WriteToolResult("Windows.Agent.Tools.Desktop.UIElementTool.GetElementPropertiesAsync", pretty, raw, output);
            }
            case "ui-wait":
            {
                var tool = services.GetRequiredService<UIElementTool>();
                var type = options.RequireString("type");
                var value = options.RequireString("value");
                var timeoutMs = options.GetInt("timeoutMs", 5000);
                var raw = await tool.WaitForElementAsync(value, type, timeoutMs);
                return WriteToolResult("Windows.Agent.Tools.Desktop.UIElementTool.WaitForElementAsync", pretty, raw, output);
            }
            default:
                throw new ArgumentException($"Unknown desktop action: {action}");
        }
    }

    private static async Task<int> RunFileSystemAsync(string action, CliOptions options, IServiceProvider services, bool pretty, TextWriter output)
    {
        switch (action)
        {
            case "read":
            {
                var tool = services.GetRequiredService<ReadFileTool>();
                var path = options.RequireString("path");
                var raw = await tool.ReadFileAsync(path);
                return WriteToolResult("Windows.Agent.Tools.FileSystem.ReadFileTool.ReadFileAsync", pretty, raw, output);
            }
            case "write":
            {
                var tool = services.GetRequiredService<WriteFileTool>();
                var path = options.RequireString("path");
                var content = options.RequireString("content");
                var append = options.GetBool("append", false);
                var raw = await tool.WriteFileAsync(path, content, append);
                return WriteToolResult("Windows.Agent.Tools.FileSystem.WriteFileTool.WriteFileAsync", pretty, raw, output);
            }
            case "create":
            {
                var tool = services.GetRequiredService<CreateFileTool>();
                var path = options.RequireString("path");
                var content = options.RequireString("content");
                var raw = await tool.CreateFileAsync(path, content);
                return WriteToolResult("Windows.Agent.Tools.FileSystem.CreateFileTool.CreateFileAsync", pretty, raw, output);
            }
            case "delete":
            {
                var tool = services.GetRequiredService<DeleteFileTool>();
                var path = options.RequireString("path");
                var raw = await tool.DeleteFileAsync(path);
                return WriteToolResult("Windows.Agent.Tools.FileSystem.DeleteFileTool.DeleteFileAsync", pretty, raw, output);
            }
            case "copy":
            {
                var tool = services.GetRequiredService<CopyFileTool>();
                var source = options.RequireString("source");
                var destination = options.RequireString("destination");
                var overwrite = options.GetBool("overwrite", false);
                var raw = await tool.CopyFileAsync(source, destination, overwrite);
                return WriteToolResult("Windows.Agent.Tools.FileSystem.CopyFileTool.CopyFileAsync", pretty, raw, output);
            }
            case "move":
            {
                var tool = services.GetRequiredService<MoveFileTool>();
                var source = options.RequireString("source");
                var destination = options.RequireString("destination");
                var overwrite = options.GetBool("overwrite", false);
                var raw = await tool.MoveFileAsync(source, destination, overwrite);
                return WriteToolResult("Windows.Agent.Tools.FileSystem.MoveFileTool.MoveFileAsync", pretty, raw, output);
            }
            case "info":
            {
                var tool = services.GetRequiredService<GetFileInfoTool>();
                var path = options.RequireString("path");
                var raw = await tool.GetFileInfoAsync(path);
                return WriteToolResult("Windows.Agent.Tools.FileSystem.GetFileInfoTool.GetFileInfoAsync", pretty, raw, output);
            }
            case "list":
            {
                var tool = services.GetRequiredService<ListDirectoryTool>();
                var path = options.RequireString("path");
                var includeFiles = options.GetBool("files", true);
                var includeDirs = options.GetBool("dirs", true);
                var recursive = options.GetBool("recursive", false);
                var raw = await tool.ListDirectoryAsync(path, includeFiles, includeDirs, recursive);
                return WriteToolResult("Windows.Agent.Tools.FileSystem.ListDirectoryTool.ListDirectoryAsync", pretty, raw, output);
            }
            case "mkdir":
            {
                var tool = services.GetRequiredService<CreateDirectoryTool>();
                var path = options.RequireString("path");
                var parents = options.GetBool("parents", true);
                var raw = await tool.CreateDirectoryAsync(path, parents);
                return WriteToolResult("Windows.Agent.Tools.FileSystem.CreateDirectoryTool.CreateDirectoryAsync", pretty, raw, output);
            }
            case "rmdir":
            {
                var tool = services.GetRequiredService<DeleteDirectoryTool>();
                var path = options.RequireString("path");
                var recursive = options.GetBool("recursive", false);
                var raw = await tool.DeleteDirectoryAsync(path, recursive);
                return WriteToolResult("Windows.Agent.Tools.FileSystem.DeleteDirectoryTool.DeleteDirectoryAsync", pretty, raw, output);
            }
            case "search-name":
            {
                var tool = services.GetRequiredService<SearchFilesTool>();
                var directory = options.RequireString("directory");
                var pattern = options.RequireString("pattern");
                var recursive = options.GetBool("recursive", false);
                var raw = await tool.SearchFilesByNameAsync(directory, pattern, recursive);
                return WriteToolResult("Windows.Agent.Tools.FileSystem.SearchFilesTool.SearchFilesByNameAsync", pretty, raw, output);
            }
            case "search-ext":
            {
                var tool = services.GetRequiredService<SearchFilesTool>();
                var directory = options.RequireString("directory");
                var ext = options.RequireString("ext");
                var recursive = options.GetBool("recursive", false);
                var raw = await tool.SearchFilesByExtensionAsync(directory, ext, recursive);
                return WriteToolResult("Windows.Agent.Tools.FileSystem.SearchFilesTool.SearchFilesByExtensionAsync", pretty, raw, output);
            }
            default:
                throw new ArgumentException($"Unknown filesystem action: {action}");
        }
    }

    private static async Task<int> RunOcrAsync(string action, CliOptions options, IServiceProvider services, bool pretty, TextWriter output)
    {
        switch (action)
        {
            case "screen":
            {
                var tool = services.GetRequiredService<ExtractTextFromScreenTool>();
                var raw = await tool.ExtractTextFromScreenAsync();
                return WriteToolResult("Windows.Agent.Tools.OCR.ExtractTextFromScreenTool.ExtractTextFromScreenAsync", pretty, raw, output);
            }
            case "region":
            {
                var tool = services.GetRequiredService<ExtractTextFromRegionTool>();
                var x = options.RequireInt("x");
                var y = options.RequireInt("y");
                var width = options.RequireInt("width");
                var height = options.RequireInt("height");
                var raw = await tool.ExtractTextFromRegionAsync(x, y, width, height);
                return WriteToolResult("Windows.Agent.Tools.OCR.ExtractTextFromRegionTool.ExtractTextFromRegionAsync", pretty, raw, output);
            }
            case "find":
            {
                var tool = services.GetRequiredService<FindTextOnScreenTool>();
                var text = options.RequireString("text");
                var raw = await tool.FindTextOnScreenAsync(text);
                return WriteToolResult("Windows.Agent.Tools.OCR.FindTextOnScreenTool.FindTextOnScreenAsync", pretty, raw, output);
            }
            case "coords":
            {
                var tool = services.GetRequiredService<GetTextCoordinatesTool>();
                var text = options.RequireString("text");
                var raw = await tool.GetTextCoordinatesAsync(text);
                return WriteToolResult("Windows.Agent.Tools.OCR.GetTextCoordinatesTool.GetTextCoordinatesAsync", pretty, raw, output);
            }
            case "file":
            {
                var tool = services.GetRequiredService<ExtractTextFromFileTool>();
                var path = options.RequireString("path");
                var raw = await tool.ExtractTextFromFileAsync(path);
                return WriteToolResult("Windows.Agent.Tools.OCR.ExtractTextFromFileTool.ExtractTextFromFileAsync", pretty, raw, output);
            }
            default:
                throw new ArgumentException($"Unknown ocr action: {action}");
        }
    }

    private static async Task<int> RunSystemAsync(string action, CliOptions options, IServiceProvider services, bool pretty, TextWriter output)
    {
        switch (action)
        {
            case "volume":
            {
                var tool = services.GetRequiredService<VolumeTool>();
                if (options.Has("mute"))
                {
                    var mute = options.GetBool("mute", true);
                    var rawMute = await tool.SetMuteStateAsync(mute);
                    return WriteToolResult("Windows.Agent.Tools.SystemControl.VolumeTool.SetMuteStateAsync", pretty, rawMute, output);
                }
                if (options.Has("percent"))
                {
                    var percent = options.RequireInt("percent");
                    var raw = await tool.SetVolumePercentAsync(percent);
                    return WriteToolResult("Windows.Agent.Tools.SystemControl.VolumeTool.SetVolumePercentAsync", pretty, raw, output);
                }
                if (options.Has("inc"))
                {
                    var raw = await tool.SetVolumeAsync(true);
                    return WriteToolResult("Windows.Agent.Tools.SystemControl.VolumeTool.SetVolumeAsync", pretty, raw, output);
                }
                if (options.Has("dec"))
                {
                    var raw = await tool.SetVolumeAsync(false);
                    return WriteToolResult("Windows.Agent.Tools.SystemControl.VolumeTool.SetVolumeAsync", pretty, raw, output);
                }
                var rawGet = await tool.GetCurrentVolumeAsync();
                return WriteToolResult("Windows.Agent.Tools.SystemControl.VolumeTool.GetCurrentVolumeAsync", pretty, rawGet, output);
            }
            case "brightness":
            {
                var tool = services.GetRequiredService<BrightnessTool>();
                if (options.Has("percent"))
                {
                    var percent = options.RequireInt("percent");
                    var raw = await tool.SetBrightnessPercentAsync(percent);
                    return WriteToolResult("Windows.Agent.Tools.SystemControl.BrightnessTool.SetBrightnessPercentAsync", pretty, raw, output);
                }
                if (options.Has("inc"))
                {
                    var raw = await tool.SetBrightnessAsync(true);
                    return WriteToolResult("Windows.Agent.Tools.SystemControl.BrightnessTool.SetBrightnessAsync", pretty, raw, output);
                }
                if (options.Has("dec"))
                {
                    var raw = await tool.SetBrightnessAsync(false);
                    return WriteToolResult("Windows.Agent.Tools.SystemControl.BrightnessTool.SetBrightnessAsync", pretty, raw, output);
                }
                var rawGet = await tool.GetCurrentBrightnessAsync();
                return WriteToolResult("Windows.Agent.Tools.SystemControl.BrightnessTool.GetCurrentBrightnessAsync", pretty, rawGet, output);
            }
            case "resolution":
            {
                var tool = services.GetRequiredService<ResolutionTool>();
                var type = options.RequireString("type");
                var raw = await tool.SetResolutionAsync(type);
                return WriteToolResult("Windows.Agent.Tools.SystemControl.ResolutionTool.SetResolutionAsync", pretty, raw, output);
            }
            default:
                throw new ArgumentException($"Unknown system action: {action}");
        }
    }

    private static string[] SplitKeys(string keys)
    {
        return keys
            .Split(new[] { '+', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }
}
