using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Playwright;
using SpyBrowser.Cursory;

namespace SpyBrowser.Tests;

[Collection(TimedInputCollection.Name)]
public sealed class SDKDatasetAdmissionFaultTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Corrupt_or_missing_dataset_is_rejected_before_unknown_mouse_anchor_dispatch(bool removeResourceName)
    {
        byte[] damaged = MutateCursoryImage(removeResourceName);
        using var sdk = new IsolatedSdk(damaged);
        var page = DispatchProxy.Create<IPage, RecordingPage>();
        var actions = sdk.CreateHumanActions("Cursory");

        InvalidDataException failure = await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await sdk.MoveAsync(actions, page));

        Assert.Contains("Cursory", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, RecordingPage.Get(page).MoveCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Bad_dataset_precedes_locator_wait_trial_and_scroll(bool playwrightCompatible)
    {
        using var sdk = new IsolatedSdk(MutateCursoryImage(removeResourceName: false));
        var page = DispatchProxy.Create<IPage, RecordingPage>();
        var locator = DispatchProxy.Create<ILocator, RecordingLocator>();
        RecordingLocator.Get(locator).PageValue = page;
        var actions = sdk.CreateHumanActions("Cursory", playwrightCompatible);

        await Assert.ThrowsAsync<InvalidDataException>(() => sdk.HoverAsync(actions, locator));

        Assert.Equal(0, RecordingLocator.Get(locator).WaitCalls);
        Assert.Equal(0, RecordingLocator.Get(locator).TrialCalls);
        Assert.Equal(0, RecordingLocator.Get(locator).ScrollCalls);
        Assert.Equal(0, RecordingPage.Get(page).MoveCalls);
    }

    [Fact]
    public async Task Healthy_selected_dataset_keeps_native_unknown_anchor_then_generates_later_motion()
    {
        using var sdk = new IsolatedSdk(File.ReadAllBytes(typeof(CursoryTrajectoryGenerator).Assembly.Location));
        var page = DispatchProxy.Create<IPage, RecordingPage>();
        var actions = sdk.CreateHumanActions("Cursory");

        await sdk.MoveAsync(actions, page);
        Assert.Equal(1, RecordingPage.Get(page).MoveCalls);
        await sdk.MoveAsync(actions, page);
        Assert.True(RecordingPage.Get(page).MoveCalls > 1);
    }

    [Fact]
    public async Task Non_cursory_algorithm_does_not_admit_or_load_selected_dataset()
    {
        using var sdk = new IsolatedSdk(MutateCursoryImage(removeResourceName: false));
        var page = DispatchProxy.Create<IPage, RecordingPage>();
        var actions = sdk.CreateHumanActions("Bezier");

        await sdk.MoveAsync(actions, page);

        Assert.True(RecordingPage.Get(page).MoveCalls > 1);
    }

    private static byte[] MutateCursoryImage(bool removeResourceName)
    {
        Assembly source = typeof(CursoryTrajectoryGenerator).Assembly;
        string resourceName = source.GetManifestResourceNames().Single(name => name.EndsWith("trajectories.json.gz", StringComparison.Ordinal));
        byte[] image = File.ReadAllBytes(source.Location);
        if (removeResourceName)
        {
            byte[] name = System.Text.Encoding.UTF8.GetBytes(resourceName);
            int offset = IndexOf(image, name);
            Assert.True(offset >= 0);
            image[offset] = (byte)'X';
        }
        else
        {
            using var resource = source.GetManifestResourceStream(resourceName)!;
            byte[] gzip = new byte[resource.Length];
            resource.ReadExactly(gzip);
            int offset = IndexOf(image, gzip);
            Assert.True(offset >= 0);
            image[offset + gzip.Length / 2] ^= 1;
        }
        return image;
    }

    private static int IndexOf(byte[] source, byte[] value)
    {
        for (int i = 0; i <= source.Length - value.Length; i++)
            if (source.AsSpan(i, value.Length).SequenceEqual(value)) return i;
        return -1;
    }

    public class RecordingLocator : DispatchProxy
    {
        internal IPage? PageValue;
        internal int WaitCalls;
        internal int TrialCalls;
        internal int ScrollCalls;
        internal static RecordingLocator Get(ILocator locator) => (RecordingLocator)(object)locator;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_Page") return PageValue;
            if (method?.Name == nameof(ILocator.WaitForAsync)) Interlocked.Increment(ref WaitCalls);
            if (method?.Name is nameof(ILocator.ClickAsync) or nameof(ILocator.HoverAsync) or nameof(ILocator.DblClickAsync) &&
                args?.FirstOrDefault() is object options && options.GetType().GetProperty("Trial")?.GetValue(options) is true)
                Interlocked.Increment(ref TrialCalls);
            if (method?.Name == nameof(ILocator.ScrollIntoViewIfNeededAsync)) Interlocked.Increment(ref ScrollCalls);
            return RecordingPage.DefaultValue(method?.ReturnType);
        }
    }

    public class RecordingPage : DispatchProxy
    {
        private int _moveCalls;
        internal int MoveCalls => Volatile.Read(ref _moveCalls);
        private readonly IMouse _mouse = DispatchProxy.Create<IMouse, RecordingMouse>();
        private readonly IBrowserContext _context = DispatchProxy.Create<IBrowserContext, RecordingContext>();
        internal static RecordingPage Get(IPage page) => (RecordingPage)(object)page;

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_Mouse")
            {
                ((RecordingMouse)(object)_mouse).Owner = this;
                return _mouse;
            }
            if (method?.Name == "get_Context") return _context;
            if (method?.Name == "get_IsClosed") return false;
            return DefaultValue(method?.ReturnType);
        }

        public class RecordingContext : DispatchProxy
        {
            protected override object? Invoke(MethodInfo? method, object?[]? args) => DefaultValue(method?.ReturnType);
        }

        public class RecordingMouse : DispatchProxy
        {
            internal RecordingPage? Owner;
            protected override object? Invoke(MethodInfo? method, object?[]? args)
            {
                if (method?.Name == nameof(IMouse.MoveAsync))
                {
                    Interlocked.Increment(ref Owner!._moveCalls);
                    return Task.CompletedTask;
                }
                return DefaultValue(method?.ReturnType);
            }
        }

        internal static object? DefaultValue(Type? type) => type == typeof(void) || type is null ? null :
            type == typeof(Task) ? Task.CompletedTask :
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>) ?
                typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(type.GetGenericArguments()[0]).Invoke(null, [type.GetGenericArguments()[0].IsValueType ? Activator.CreateInstance(type.GetGenericArguments()[0]) : null]) :
            type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private sealed class IsolatedSdk : IDisposable
    {
        private readonly FaultLoadContext _context;
        private readonly Assembly _sdk;
        private readonly Type _actionsType;
        internal IsolatedSdk(byte[] cursoryImage)
        {
            string sdkPath = typeof(SpyBrowser.Playwright.HumanActions).Assembly.Location;
            _context = new FaultLoadContext(cursoryImage);
            using var sdkStream = File.OpenRead(sdkPath);
            _sdk = _context.LoadFromStream(sdkStream);
            _actionsType = _sdk.GetType("SpyBrowser.Playwright.HumanActions", throwOnError: true)!;
        }

        internal object CreateHumanActions(string algorithm, bool playwrightCompatible = false)
        {
            Type optionsType = _sdk.GetType("SpyBrowser.Playwright.HumanInteractionOptions", throwOnError: true)!;
            object options = Activator.CreateInstance(optionsType)!;
            // These are admission/path tests, not wall-clock cadence tests. Keep actual
            // generated points and dispatch assertions while eliminating nominal pacing;
            // allow the real Cursory path room under a loaded runner.
            optionsType.GetProperty("MouseMinimumDurationMilliseconds")!.SetValue(options, 0);
            optionsType.GetProperty("MouseMaximumDurationMilliseconds")!.SetValue(options, 0);
            optionsType.GetProperty("CursoryMovementDeadlineMilliseconds")!.SetValue(options, 10_000);
            Type enumType = _sdk.GetType("SpyBrowser.Playwright.MouseTrajectoryAlgorithm", throwOnError: true)!;
            optionsType.GetProperty("MouseAlgorithm")!.SetValue(options, Enum.Parse(enumType, algorithm));
            if (playwrightCompatible)
            {
                Type compatibilityType = _sdk.GetType("SpyBrowser.Playwright.HumanizationCompatibilityMode", throwOnError: true)!;
                optionsType.GetProperty("CompatibilityMode")!.SetValue(options, Enum.Parse(compatibilityType, "PlaywrightCompatible"));
            }
            return Activator.CreateInstance(_actionsType, options)!;
        }

        internal Task MoveAsync(object actions, IPage page) =>
            (Task)_actionsType.GetMethod("MoveAsync")!.Invoke(actions, [page, 300d, 200d, CancellationToken.None])!;

        internal Task HoverAsync(object actions, ILocator locator) =>
            (Task)_actionsType.GetMethod("HoverAsync")!.Invoke(actions, [locator, CancellationToken.None])!;

        public void Dispose() => _context.Unload();
    }

    private sealed class FaultLoadContext(byte[] cursoryImage) : AssemblyLoadContext(isCollectible: true)
    {
        private readonly byte[] _cursoryImage = cursoryImage;
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == "SpyBrowser.Cursory")
                return LoadFromStream(new MemoryStream(_cursoryImage, writable: false));
            return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
        }
    }
}
