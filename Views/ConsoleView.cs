using System;
using System.Collections.Generic;
using System.Linq;
using termlrc.Models;

namespace termlrc.Views
{
    public class ConsoleView : IMainView
    {
        public int WindowWidth
        {
            get
            {
                try { return Console.WindowWidth; } catch { return 80; }
            }
        }

        public int WindowHeight
        {
            get
            {
                try { return Console.WindowHeight; } catch { return 24; }
            }
        }

        public bool KeyAvailable => Console.KeyAvailable;

        public ConsoleKeyInfo ReadKey() => Console.ReadKey(true);

        public void Clear() => Console.Clear();

        public void SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);

        public void PrepareSearchScreen(string logo, string currentTrack)
        {
            Clear();
            int winWidth = WindowWidth;
            int winHeight = WindowHeight;
            string[] logoLines = logo.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            int searchBlockHeight = logoLines.Length + 12;
            int topPadding = Math.Max(0, (winHeight - searchBlockHeight) / 2);

            for (int i = 0; i < topPadding; i++)
            {
                Console.WriteLine(new string(' ', winWidth));
            }

            int maxLogoWidth = logoLines.Length > 0 ? logoLines.Max(l => l.Length) : 0;
            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (string line in logoLines)
            {
                int leftPad = Math.Max(0, (winWidth - maxLogoWidth) / 2);
                Console.WriteLine(new string(' ', leftPad) + line);
            }
            Console.ResetColor();

            Console.WriteLine(new string(' ', winWidth));

            string searchHeader = $"Searching lyrics for: {currentTrack}...";
            int pad = Math.Max(0, (winWidth - searchHeader.Length) / 2);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine((new string(' ', pad) + searchHeader).PadRight(winWidth));
            Console.ResetColor();

            Console.WriteLine(new string(' ', winWidth));
        }

        public void DrawSearchStep(string text, ConsoleColor color)
        {
            int winWidth = WindowWidth;
            int pad = Math.Max(0, (winWidth - text.Length) / 2);
            Console.ForegroundColor = color;
            Console.WriteLine((new string(' ', pad) + text).PadRight(winWidth));
            Console.ResetColor();
        }

        public void DrawLyrics(List<string> asciiLines, PlaybackState state)
        {
            int winWidth = WindowWidth;
            int winHeight = WindowHeight;
            int footerHeight = state.HudMode == 2 ? 0 : 3;
            int availableHeight = Math.Max(1, winHeight - footerHeight);
            int printedLines = 0;

            int totalAsciiHeight = 0;
            foreach (var artPart in asciiLines)
            {
                var splitArt = artPart.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                totalAsciiHeight += splitArt.Count(line => !string.IsNullOrWhiteSpace(line));
            }

            int topPadding = Math.Max(0, (availableHeight - totalAsciiHeight) / 2);

            for (int i = 0; i < topPadding; i++)
            {
                Console.WriteLine(new string(' ', winWidth));
            }
            printedLines += topPadding;

            Console.ForegroundColor = state.CurrentColor;
            foreach (var artPart in asciiLines)
            {
                string[] splitArt = artPart.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                int maxArtWidth = splitArt.Length > 0 ? splitArt.Max(l => l.Length) : 0;
                int blockLeftPad = Math.Max(0, (winWidth - maxArtWidth) / 2);
                foreach (var line in splitArt)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string paddedLine = new string(' ', blockLeftPad) + line;
                    Console.WriteLine(paddedLine.PadRight(winWidth));
                    printedLines++;
                }
            }
            Console.ResetColor();

            // Pad remaining lines
            for (int i = printedLines; i < availableHeight; i++)
            {
                Console.WriteLine(new string(' ', winWidth));
            }
        }

        public void DrawNoLyricsMessage(PlaybackState state)
        {
            int winWidth = WindowWidth;
            int winHeight = WindowHeight;
            int footerHeight = state.HudMode == 2 ? 0 : 3;
            int availableHeight = Math.Max(1, winHeight - footerHeight);
            int printedLines = 0;

            string msg = "[Failed to find lyrics for this song]";
            int topPad = Math.Max(0, availableHeight / 2);
            for (int i = 0; i < topPad; i++) 
            {
                Console.WriteLine(new string(' ', winWidth));
            }
            printedLines += topPad;
            
            int leftPad = Math.Max(0, (winWidth - msg.Length) / 2);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine((new string(' ', leftPad) + msg).PadRight(winWidth));
            Console.ResetColor();
            printedLines++;

            for (int i = printedLines; i < availableHeight; i++)
            {
                Console.WriteLine(new string(' ', winWidth));
            }
        }

        public void DrawIdleMessage(string logo)
        {
            int winWidth = WindowWidth;
            int winHeight = WindowHeight;

            string[] logoLines = logo.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string msg = "Open a player and play some music";

            int totalHeight = logoLines.Length + 3;
            int topPadding = Math.Max(0, (winHeight - totalHeight) / 2);

            int printedLines = 0;

            for (int i = 0; i < topPadding; i++)
            {
                Console.WriteLine(new string(' ', winWidth));
            }
            printedLines += topPadding;

            int maxLogoWidth = logoLines.Length > 0 ? logoLines.Max(l => l.Length) : 0;
            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (string line in logoLines)
            {
                int leftPad = Math.Max(0, (winWidth - maxLogoWidth) / 2);
                Console.WriteLine(new string(' ', leftPad) + line);
                printedLines++;
            }
            Console.ResetColor();

            Console.WriteLine(new string(' ', winWidth));
            printedLines++;

            int msgPad = Math.Max(0, (winWidth - msg.Length) / 2);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine((new string(' ', msgPad) + msg).PadRight(winWidth));
            Console.ResetColor();
            printedLines++;

            for (int i = printedLines; i < winHeight; i++)
            {
                Console.WriteLine(new string(' ', winWidth));
            }
        }

        public void DrawAdMessage(string logo)
        {
            int winWidth = WindowWidth;
            int winHeight = WindowHeight;

            string[] logoLines = logo.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string msg = "Ad break — sit tight";

            int totalHeight = logoLines.Length + 3;
            int topPadding = Math.Max(0, (winHeight - totalHeight) / 2);

            int printedLines = 0;

            for (int i = 0; i < topPadding; i++)
            {
                Console.WriteLine(new string(' ', winWidth));
            }
            printedLines += topPadding;

            int maxLogoWidth = logoLines.Length > 0 ? logoLines.Max(l => l.Length) : 0;
            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (string line in logoLines)
            {
                int leftPad = Math.Max(0, (winWidth - maxLogoWidth) / 2);
                Console.WriteLine(new string(' ', leftPad) + line);
                printedLines++;
            }
            Console.ResetColor();

            Console.WriteLine(new string(' ', winWidth));
            printedLines++;

            int msgPad = Math.Max(0, (winWidth - msg.Length) / 2);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine((new string(' ', msgPad) + msg).PadRight(winWidth));
            Console.ResetColor();
            printedLines++;

            for (int i = printedLines; i < winHeight; i++)
            {
                Console.WriteLine(new string(' ', winWidth));
            }
        }

        public void DrawFooter(PlaybackState state, string currentFontName)
        {
            if (state.HudMode == 2) return;

            int winWidth = WindowWidth;
            Console.WriteLine(new string(' ', winWidth));
            
            // Draw track info
            string trackInfo = $"{state.CurrentArtist} - {state.CurrentTitle}";
            int pad = Math.Max(0, (winWidth - trackInfo.Length) / 2);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine((new string(' ', pad) + trackInfo).PadRight(winWidth));
            Console.ResetColor();

            // Draw HUD menu if hudMode is 0
            if (state.HudMode == 0)
            {
                string wordMode = state.WordByWordMode ? "ON" : "OFF";
                string hudText = $"Lyrics: {state.ScrollModeInfo}  |  [F]Font: {currentFontName}  [C]Color  [H]HUD  [W]Word({wordMode})";
                int fPad2 = Math.Max(0, (winWidth - hudText.Length) / 2);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write((new string(' ', fPad2) + hudText).PadRight(winWidth));
                Console.ResetColor();
            }
            else
            {
                Console.Write(new string(' ', winWidth));
            }
        }
    }
}
