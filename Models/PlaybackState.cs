using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace termlrc.Models
{
    public class PlaybackState
    {
        public string CurrentTrack { get; set; } = string.Empty;
        public string LastTrack { get; set; } = string.Empty;
        public string CurrentArtist { get; set; } = string.Empty;
        public string CurrentTitle { get; set; } = string.Empty;
        public bool IsPlaying { get; set; }
        public bool IsAd { get; set; }
        
        public double LastPolledPosition { get; set; }
        public Stopwatch LocalTimer { get; } = new Stopwatch();
        public DateTime LastPollTime { get; set; } = DateTime.MinValue;

        public List<SyncedLine> SyncedLyrics { get; set; } = new List<SyncedLine>();
        public bool IsSynced { get; set; }
        public string ScrollModeInfo { get; set; } = string.Empty;

        // UI settings
        public int HudMode { get; set; } // 0 = full, 1 = hide menu, 2 = only lyrics
        public int ColorIndex { get; set; }
        public bool WordByWordMode { get; set; } // true = show one word at a time
        public ConsoleColor[] Colors { get; } = { ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.White, ConsoleColor.Red };
        
        public double GetCurrentPosition()
        {
            return LastPolledPosition + LocalTimer.Elapsed.TotalSeconds;
        }

        public ConsoleColor CurrentColor => Colors[ColorIndex];
    }
}
