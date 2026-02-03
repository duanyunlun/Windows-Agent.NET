using Microsoft.Extensions.Logging;
using System.ComponentModel;
using Windows.Agent.Interface;
using Windows.Agent.Uia;

namespace Windows.Agent.Tools.Desktop;

/// <summary>
/// UIA 自动化工具（控件树/查找/Invoke/Value）。
/// </summary>
public class UiaTool
{
    private readonly IUiaService _uiaService;
    private readonly ILogger<UiaTool> _logger;

    public UiaTool(IUiaService uiaService, ILogger<UiaTool> logger)
    {
        _uiaService = uiaService;
        _logger = logger;
    }

    [Description("Dump UIA element tree for a matched window")]
    public async Task<string> GetTreeAsync(
        [Description("Regex to match the target window title")] string windowTitleRegex,
        [Description("Max depth of the element tree")] int depth = 3)
    {
        _logger.LogInformation("UIA tree: windowTitleRegex={WindowTitleRegex}, depth={Depth}", windowTitleRegex, depth);
        return await _uiaService.GetTreeAsync(windowTitleRegex, depth);
    }

    [Description("Find UIA element by selector in a matched window")]
    public async Task<string> FindAsync(
        [Description("Regex to match the target window title")] string windowTitleRegex,
        [Description("Selector (key=value;...) e.g. automationId=btnSend;controlType=Button")] string selector,
        [Description("Max number of matches to return")] int limit = 5)
    {
        _logger.LogInformation("UIA find: windowTitleRegex={WindowTitleRegex}, selector={Selector}", windowTitleRegex, selector);

        if (!UiaSelector.TryParse(selector, out var parsed, out var error) || parsed == null)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { success = false, windowTitleRegex, selector, message = error ?? "invalid selector" }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        return await _uiaService.FindAsync(windowTitleRegex, parsed, limit);
    }

    [Description("Invoke UIA element by selector in a matched window")]
    public async Task<string> InvokeAsync(
        [Description("Regex to match the target window title")] string windowTitleRegex,
        [Description("Selector (key=value;...) e.g. automationId=btnSend;controlType=Button")] string selector)
    {
        _logger.LogInformation("UIA invoke: windowTitleRegex={WindowTitleRegex}, selector={Selector}", windowTitleRegex, selector);

        if (!UiaSelector.TryParse(selector, out var parsed, out var error) || parsed == null)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { success = false, windowTitleRegex, selector, message = error ?? "invalid selector" }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        return await _uiaService.InvokeAsync(windowTitleRegex, parsed);
    }

    [Description("Set value on UIA element by selector in a matched window")]
    public async Task<string> SetValueAsync(
        [Description("Regex to match the target window title")] string windowTitleRegex,
        [Description("Selector (key=value;...) e.g. automationId=txtUser;controlType=Edit")] string selector,
        [Description("Value to set")] string value)
    {
        _logger.LogInformation("UIA setvalue: windowTitleRegex={WindowTitleRegex}, selector={Selector}", windowTitleRegex, selector);

        if (!UiaSelector.TryParse(selector, out var parsed, out var error) || parsed == null)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { success = false, windowTitleRegex, selector, message = error ?? "invalid selector" }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        return await _uiaService.SetValueAsync(windowTitleRegex, parsed, value);
    }
}

