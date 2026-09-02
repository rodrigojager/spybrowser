using System.Text.Json;
using System.Text.Json.Serialization;
using SpyBrowser.Core;
using SpyBrowser.Playwright;

namespace SpyBrowser.Cli;

internal static class SpyBrowserCli
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
            {
                PrintHelp();
                return 0;
            }

            return args[0].ToLowerInvariant() switch
            {
                "identity" => await RunIdentityAsync(args[1..]).ConfigureAwait(false),
                "doctor" => await RunDoctorAsync(args[1..]).ConfigureAwait(false),
                "probe" => await RunProbeAsync(args[1..]).ConfigureAwait(false),
                "open" => await RunOpenAsync(args[1..]).ConfigureAwait(false),
                "version" => ShowVersion(),
                _ => throw new ArgumentException($"Unknown command: '{args[0]}'.")
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"SpyBrowser error: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> RunIdentityAsync(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("Identity requires one of: create, list, show, path.");
        }

        return args[0].ToLowerInvariant() switch
        {
            "create" => await CreateIdentityAsync(args[1..]).ConfigureAwait(false),
            "list" => await ListIdentitiesAsync(args[1..]).ConfigureAwait(false),
            "show" => await ShowIdentityAsync(args[1..]).ConfigureAwait(false),
            "path" => ShowIdentityPath(args[1..]),
            _ => throw new ArgumentException($"Unknown identity command: '{args[0]}'.")
        };
    }

    private static async Task<int> CreateIdentityAsync(string[] args)
    {
        var command = new CommandLine(args);
        if (command.Positionals.Count != 1)
        {
            throw new ArgumentException("Usage: spybrowser identity create <id> [options]");
        }

        var id = command.Positionals[0];
        var engine = ParseBrowserEngine(command.Get("engine") ?? "chrome");
        var gpuPolicy = ParseGpuPolicy(command.Get("gpu") ?? "auto");
        var defaultChannel = engine switch
        {
            BrowserEngine.Chrome => "chrome",
            BrowserEngine.Edge => "msedge",
            BrowserEngine.Chromium or BrowserEngine.CustomChromium => null,
            _ => null
        };
        var identity = BrowserIdentity.Create(id, command.Get("name")) with
        {
            Locale = command.Get("locale") ?? "pt-BR",
            TimezoneId = command.Get("timezone") ?? "America/Sao_Paulo",
            Viewport = new ViewportIdentity
            {
                Width = command.GetInt("width", 1440),
                Height = command.GetInt("height", 1000),
                ScreenWidth = command.GetInt("screen-width", 1920),
                ScreenHeight = command.GetInt("screen-height", 1080),
                DeviceScaleFactor = command.GetFloat("scale", 1)
            },
            Browser = new BrowserIdentitySettings
            {
                Engine = engine,
                Channel = command.Get("channel") ?? defaultChannel,
                ExecutablePath = command.Get("executable"),
                UserAgent = command.Get("user-agent")
            },
            Network = new NetworkIdentitySettings
            {
                ProxyServer = command.Get("proxy"),
                ProxyBypass = command.Get("proxy-bypass"),
                ProxyUsernameEnvironment = command.Get("proxy-user-env"),
                ProxyPasswordEnvironment = command.Get("proxy-password-env"),
                DisableNonProxiedWebRtc = !command.Has("allow-non-proxied-webrtc")
            },
            Gpu = new GpuIdentitySettings
            {
                Policy = gpuPolicy,
                MaskVendor = command.Get("gpu-vendor"),
                MaskRenderer = command.Get("gpu-renderer")
            },
            Navigator = new NavigatorIdentitySettings
            {
                EnableExperimentalOverrides = command.Has("experimental-navigator"),
                Platform = command.Get("platform"),
                HardwareConcurrency = command.Get("hardware-concurrency") is { } concurrency
                    ? int.Parse(concurrency, System.Globalization.CultureInfo.InvariantCulture)
                    : null,
                DeviceMemoryGb = command.Get("device-memory") is { } memory
                    ? double.Parse(memory, System.Globalization.CultureInfo.InvariantCulture)
                    : null
            }
        };

        var store = new IdentityStore(command.Get("root"));
        await store.CreateAsync(identity).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(identity, JsonOptions));
        Console.WriteLine($"Profile: {store.GetProfileDirectory(identity.Id)}");
        return 0;
    }

    private static async Task<int> ListIdentitiesAsync(string[] args)
    {
        var command = new CommandLine(args);
        EnsureNoPositionals(command);
        var store = new IdentityStore(command.Get("root"));
        var identities = await store.ListAsync().ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(identities.Select(identity => new
        {
            identity.Id,
            identity.DisplayName,
            identity.Locale,
            identity.TimezoneId,
            identity.Browser.Engine,
            identity.Gpu.Policy
        }), JsonOptions));
        return 0;
    }

    private static async Task<int> ShowIdentityAsync(string[] args)
    {
        var command = new CommandLine(args);
        if (command.Positionals.Count != 1)
        {
            throw new ArgumentException("Usage: spybrowser identity show <id> [--root path]");
        }

        var store = new IdentityStore(command.Get("root"));
        var identity = await store.GetAsync(command.Positionals[0]).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(identity, JsonOptions));
        return 0;
    }

    private static int ShowIdentityPath(string[] args)
    {
        var command = new CommandLine(args);
        if (command.Positionals.Count != 1)
        {
            throw new ArgumentException("Usage: spybrowser identity path <id> [--root path]");
        }

        var store = new IdentityStore(command.Get("root"));
        Console.WriteLine(store.GetProfileDirectory(command.Positionals[0]));
        return 0;
    }

    private static async Task<int> RunDoctorAsync(string[] args)
    {
        var command = new CommandLine(args);
        EnsureNoPositionals(command);
        var report = await EnvironmentDoctor.InspectAsync(command.Get("root")).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
        return report.Warnings.Count == 0 ? 0 : 2;
    }

    private static async Task<int> RunProbeAsync(string[] args)
    {
        var command = new CommandLine(args);
        if (command.Positionals.Count != 1)
        {
            throw new ArgumentException("Usage: spybrowser probe <id> [--headless] [--url URL] [--root path]");
        }

        await using var session = await SpyBrowserLauncher.LaunchAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = command.Positionals[0],
            IdentitiesRoot = command.Get("root"),
            Headless = command.Has("headless"),
            Humanize = command.Has("humanize"),
            RunGpuProbe = true,
            FailOnConsistencyErrors = !command.Has("allow-inconsistent")
        }).ConfigureAwait(false);
        var page = session.Pages.FirstOrDefault() ?? await session.NewPageAsync().ConfigureAwait(false);
        if (command.Get("url") is { } url)
        {
            EnsureNavigableUrl(url);
            await page.GotoAsync(url).ConfigureAwait(false);
        }

        var currentDiagnostics = await GpuProbe.RunAsync(page).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            session.Identity.Id,
            InitialDiagnostics = session.Diagnostics,
            CurrentDiagnostics = currentDiagnostics,
            session.Consistency
        }, JsonOptions));
        return session.Consistency.HasErrors ? 2 : 0;
    }

    private static async Task<int> RunOpenAsync(string[] args)
    {
        var command = new CommandLine(args);
        if (command.Positionals.Count != 1)
        {
            throw new ArgumentException("Usage: spybrowser open <id> [--url URL] [--root path]");
        }

        await using var session = await SpyBrowserLauncher.LaunchAsync(new SpyBrowserLaunchOptions
        {
            IdentityId = command.Positionals[0],
            IdentitiesRoot = command.Get("root"),
            Headless = false,
            Humanize = command.Has("humanize"),
            RunGpuProbe = true
        }).ConfigureAwait(false);
        var page = session.Pages.FirstOrDefault() ?? await session.NewPageAsync().ConfigureAwait(false);
        var url = command.Get("url") ?? "about:blank";
        EnsureNavigableUrl(url);
        await page.GotoAsync(url).ConfigureAwait(false);

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.WriteLine($"SpyBrowser identity '{session.Identity.Id}' is open. Press Ctrl+C to close.");
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }

        return 0;
    }

    private static BrowserEngine ParseBrowserEngine(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "chrome" => BrowserEngine.Chrome,
            "chromium" => BrowserEngine.Chromium,
            "edge" or "msedge" => BrowserEngine.Edge,
            "custom" or "custom-chromium" => BrowserEngine.CustomChromium,
            _ => throw new ArgumentException($"Unknown browser engine: '{value}'.")
        };

    private static int ShowVersion()
    {
        var spyBrowserVersion = typeof(SpyBrowserCli).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .SingleOrDefault()?.InformationalVersion ?? "unknown";
        var playwrightVersion = typeof(Microsoft.Playwright.IPage).Assembly.GetName().Version?.ToString() ?? "unknown";
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            SpyBrowser = spyBrowserVersion,
            Playwright = playwrightVersion,
            OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()
        }, JsonOptions));
        return 0;
    }

    private static GpuPolicy ParseGpuPolicy(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "auto" => GpuPolicy.Auto,
            "require-hardware" => GpuPolicy.RequireHardware,
            "allow-software" => GpuPolicy.AllowSoftware,
            "force-software" => GpuPolicy.ForceSoftware,
            "experimental-mask" => GpuPolicy.ExperimentalMask,
            _ => throw new ArgumentException($"Unknown GPU policy: '{value}'.")
        };

    private static void EnsureNoPositionals(CommandLine command)
    {
        if (command.Positionals.Count > 0)
        {
            throw new ArgumentException($"Unexpected positional argument: '{command.Positionals[0]}'.");
        }
    }

    private static void EnsureNavigableUrl(string url)
    {
        if (string.Equals(url, "about:blank", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("URL must be about:blank or an absolute HTTP/HTTPS address.");
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            SpyBrowser - persistent Playwright identities for authorized automation

            Commands:
              spybrowser identity create <id> [options]
              spybrowser identity list [--root path]
              spybrowser identity show <id> [--root path]
              spybrowser identity path <id> [--root path]
              spybrowser doctor [--root path]
              spybrowser probe <id> [--headless] [--url URL] [--root path]
              spybrowser open <id> [--url URL] [--root path]
              spybrowser version

            Runtime options:
              --humanize       transparently humanize standard Playwright interactions

            Identity create options:
              --name TEXT
              --locale pt-BR
              --timezone America/Sao_Paulo
              --engine chrome|chromium|edge|custom-chromium
              --channel CHANNEL
              --executable PATH
              --width N --height N --screen-width N --screen-height N --scale N
              --gpu auto|require-hardware|allow-software|force-software|experimental-mask
              --gpu-vendor TEXT --gpu-renderer TEXT
              --proxy URI --proxy-bypass TEXT
              --proxy-user-env NAME --proxy-password-env NAME
              --experimental-navigator --platform TEXT
              --hardware-concurrency N --device-memory N

            SPYBROWSER_HOME changes the default identity root. Proxy credentials
            are read from the named environment variables and never stored in JSON.
            """);
    }
}
