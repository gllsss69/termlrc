using System;
using System.Runtime.InteropServices;
using termlrc.Services;
using termlrc.Views;
using termlrc.Presenters;

class Program
{
    static void Main(string[] args)
    {
        IPlayerService player;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            player = CreateWindowsPlayerService();
        }
        else
        {
            player = new LinuxPlayerService();
        }

        var ascii = new AsciiService();
        var lyrics = new LyricsService();
        var view = new ConsoleView();

        var presenter = new MainPresenter(view, player, ascii, lyrics);
        presenter.Run();
    }

    /// <summary>
    /// Isolated into its own method so the JIT never tries to load
    /// Windows-only types when running on Linux.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static IPlayerService CreateWindowsPlayerService()
    {
#if WINDOWS
        return new WindowsPlayerService();
#else
        // Fallback: should never be reached on a correctly-built binary,
        // but guards against misconfigured builds.
        throw new PlatformNotSupportedException(
            "This binary was not compiled with Windows support. " +
            "Please build on Windows with the net10.0-windows10.0.19041.0 target.");
#endif
    }
}