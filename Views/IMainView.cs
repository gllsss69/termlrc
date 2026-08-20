using System;
using System.Collections.Generic;
using termlrc.Models;

namespace termlrc.Views
{
    public interface IMainView
    {
        int WindowWidth { get; }
        int WindowHeight { get; }
        bool KeyAvailable { get; }
        ConsoleKeyInfo ReadKey();
        void Clear();
        void SetCursorPosition(int left, int top);
        void PrepareSearchScreen(string logo, string currentTrack);
        void DrawSearchStep(string text, ConsoleColor color);
        void DrawLyrics(List<string> asciiLines, PlaybackState state);
        void DrawNoLyricsMessage(PlaybackState state);
        void DrawIdleMessage(string logo);
        void DrawAdMessage(string logo);
        void DrawFooter(PlaybackState state, string currentFontName);
    }
}
