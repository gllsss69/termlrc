using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using termlrc.Models;
using termlrc.Services;
using termlrc.Views;

namespace termlrc.Presenters
{
    public class MainPresenter
    {
        private readonly IMainView _view;
        private readonly IPlayerService _player;
        private readonly AsciiService _ascii;
        private readonly LyricsService _lyrics;
        private readonly PlaybackState _state;
        private bool _idleDrawn;
        private string _lastRenderState = string.Empty;

        public MainPresenter(IMainView view, IPlayerService player, AsciiService ascii, LyricsService lyrics)
        {
            _view = view;
            _player = player;
            _ascii = ascii;
            _lyrics = lyrics;
            _state = new PlaybackState();
        }

        public async Task RunAsync()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible = false;

            CancellationTokenSource? searchCts = null;
            Task? searchTask = null;

            while (true)
            {
                // 1. Process keys
                while (_view.KeyAvailable)
                {
                    var key = _view.ReadKey();
                    switch (key.Key)
                    {
                        case ConsoleKey.F:
                            _ascii.NextFont();
                            ForceRedraw();
                            break;
                        case ConsoleKey.C:
                            _state.ColorIndex = (_state.ColorIndex + 1) % _state.Colors.Length;
                            break;
                        case ConsoleKey.H:
                            _state.HudMode = (_state.HudMode + 1) % 3;
                            ForceRedraw();
                            break;
                        case ConsoleKey.W:
                            _state.WordByWordMode = !_state.WordByWordMode;
                            ForceRedraw();
                            break;
                    }
                }

                // 2. Poll player (metadata every 2s to reduce process spawns)
                bool shouldPoll = (DateTime.UtcNow - _state.LastPollTime).TotalMilliseconds >= 2000;
                if (shouldPoll)
                {
                    _state.LastPollTime = DateTime.UtcNow;
                    _state.CurrentTrack = _player.GetCurrentTrack();
                    string status = _player.GetStatus();
                    _state.IsPlaying = status.Trim().Equals("Playing", StringComparison.OrdinalIgnoreCase);
                    _state.IsAd = _player.IsAd();

                    if (_state.IsPlaying)
                    {
                        _state.LastPolledPosition = _player.GetCurrentPosition();
                        _state.LocalTimer.Restart();
                    }
                    else
                    {
                        _state.LocalTimer.Stop();
                    }
                }

                // 2b. Detect idle state (no player or no music)
                bool isIdle = !_state.IsPlaying &&
                    (string.IsNullOrEmpty(_state.CurrentTrack) ||
                     _state.CurrentTrack == "The music isn't playing." ||
                     _state.CurrentTrack == "Failed to retrieve the song");

                if (isIdle)
                {
                    if (!_idleDrawn)
                    {
                        _view.Clear();
                        _view.SetCursorPosition(0, 0);
                        _view.DrawIdleMessage(_ascii.Logo);
                        _state.LastTrack = string.Empty;
                        _idleDrawn = true;
                        _lastRenderState = "IDLE";
                    }
                    await Task.Delay(300);
                    continue;
                }

                _idleDrawn = false;

                // 3. Ad playing
                if (_state.IsAd)
                {
                    if (_state.LastTrack != "__ad__")
                    {
                        _state.IsSynced = false;
                        _state.SyncedLyrics.Clear();
                        _state.LastTrack = "__ad__";
                        _view.Clear();
                        _lastRenderState = "AD_INIT";
                        
                        _view.SetCursorPosition(0, 0);
                        _view.DrawAdMessage(_ascii.Logo);
                    }
                    await Task.Delay(200);
                    continue;
                }

                // 4. Track changed - fetch lyrics asynchronously
                if (_state.CurrentTrack != _state.LastTrack && 
                    _state.CurrentTrack != "The music isn't playing." && 
                    _state.CurrentTrack != "Failed to retrieve the song")
                {
                    _state.LastTrack = _state.CurrentTrack;
                    
                    searchCts?.Cancel();
                    searchCts = new CancellationTokenSource();
                    
                    string[] trackParts = _state.CurrentTrack.Split(new string[] { " - " }, StringSplitOptions.None);
                    _state.CurrentArtist = trackParts.Length >= 2 ? trackParts[0] : "";
                    _state.CurrentTitle = trackParts.Length >= 2 ? trackParts[1] : _state.CurrentTrack;

                    _state.IsSynced = false;
                    _state.SyncedLyrics.Clear();
                    _state.ScrollModeInfo = "Searching...";

                    _view.PrepareSearchScreen(_ascii.Logo, _state.CurrentTrack);
                    _lastRenderState = string.Empty;
                    
                    string spotifyId = _player.GetSpotifyTrackId() ?? "";
                    
                    searchTask = PerformSearchAsync(spotifyId, _state.CurrentArtist, _state.CurrentTitle, searchCts.Token);
                }

                // If currently searching, don't draw lyrics yet
                if (searchTask != null && !searchTask.IsCompleted)
                {
                    await Task.Delay(50);
                    continue;
                }

                // 5. Render active screen (only if changed)
                string activeText = "~ ~ ~";
                if (_state.IsSynced && _state.SyncedLyrics.Count > 0)
                {
                    double currentPosition = _state.GetCurrentPosition();
                    int activeIndex = 0;

                    for (int i = 0; i < _state.SyncedLyrics.Count; i++)
                    {
                        if (_state.SyncedLyrics[i].TimeInSeconds <= currentPosition)
                        {
                            activeIndex = i;
                        }
                        else
                        {
                            break;
                        }
                    }

                    activeText = _state.SyncedLyrics[activeIndex].Text;
                    if (string.IsNullOrWhiteSpace(activeText) || activeText == "♪" || activeText == "♫")
                    {
                        activeText = "~ ~ ~";
                    }

                    // Word-by-word mode
                    if (_state.WordByWordMode && activeText != "~ ~ ~")
                    {
                        string[] words = activeText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Length > 0)
                        {
                            double lineStart = _state.SyncedLyrics[activeIndex].TimeInSeconds;
                            double lineEnd = (activeIndex + 1 < _state.SyncedLyrics.Count)
                                ? _state.SyncedLyrics[activeIndex + 1].TimeInSeconds
                                : lineStart + 5.0;

                            double rawDuration = lineEnd - lineStart;
                            double tailGap = Math.Min(rawDuration * 0.30, 0.8);
                            double activeDuration = Math.Max(0.5, rawDuration - tailGap);

                            double elapsed = currentPosition - lineStart;
                            double progress = Math.Max(0, Math.Min(1, elapsed / activeDuration));

                            int totalChars = 0;
                            foreach (var w in words) totalChars += Math.Max(w.Length, 1);

                            int wordIndex = 0;
                            double cumulative = 0;
                            for (int wi = 0; wi < words.Length; wi++)
                            {
                                cumulative += (double)Math.Max(words[wi].Length, 1) / totalChars;
                                if (progress < cumulative)
                                {
                                    wordIndex = wi;
                                    break;
                                }
                                wordIndex = wi;
                            }

                            activeText = words[wordIndex];
                        }
                    }
                }

                string currentRenderState = $"{activeText}_{_view.WindowWidth}_{_view.WindowHeight}_{_state.HudMode}_{_state.ColorIndex}_{_ascii.CurrentFontName}_{_state.WordByWordMode}_{_state.IsSynced}";

                if (currentRenderState != _lastRenderState)
                {
                    _view.SetCursorPosition(0, 0);

                    if (_state.IsSynced && _state.SyncedLyrics.Count > 0)
                    {
                        int maxChars = Math.Max(10, _view.WindowWidth / 7);
                        List<string> asciiLines = _ascii.RenderAsciiWrapped(activeText, maxChars);
                        _view.DrawLyrics(asciiLines, _state);
                    }
                    else
                    {
                        _view.DrawNoLyricsMessage(_state);
                    }

                    _view.DrawFooter(_state, _ascii.CurrentFontName);
                    _lastRenderState = currentRenderState;
                }

                await Task.Delay(50);
            }
        }
        
        private void ForceRedraw()
        {
            _view.Clear();
            _lastRenderState = string.Empty;
        }

        private async Task PerformSearchAsync(string spotifyId, string artist, string title, CancellationToken token)
        {
            string? syncedText = null;

            if (!token.IsCancellationRequested)
            {
                _view.DrawSearchStep("LRCLIB...", ConsoleColor.DarkGray);
                syncedText = await _lyrics.GetLyricsFromLrcLibAsync(artist, title);

                if (token.IsCancellationRequested) return;

                if (!string.IsNullOrEmpty(syncedText) && syncedText.Contains("["))
                {
                    var parsed = _lyrics.ParseLrc(syncedText);
                    if (parsed.Count > 0)
                    {
                        _state.SyncedLyrics = parsed;
                        _state.IsSynced = true;
                        _state.ScrollModeInfo = "LRCLIB";
                        ForceRedraw();
                        return;
                    }
                }
            }
            
            if (!token.IsCancellationRequested && !string.IsNullOrEmpty(spotifyId))
            {
                _view.DrawSearchStep($"Spotify ID: {spotifyId}", ConsoleColor.DarkGray);
                _view.DrawSearchStep("Musixmatch (Spotify ID)...", ConsoleColor.DarkGray);
                syncedText = await _lyrics.GetLyricsFromMusixmatchBySpotifyIdAsync(spotifyId);

                if (token.IsCancellationRequested) return;

                if (!string.IsNullOrEmpty(syncedText) && syncedText.Contains("["))
                {
                    var parsed = _lyrics.ParseLrc(syncedText);
                    if (parsed.Count > 0)
                    {
                        _state.SyncedLyrics = parsed;
                        _state.IsSynced = true;
                        _state.ScrollModeInfo = "Musixmatch";
                        ForceRedraw();
                        return;
                    }
                }
            }

            if (!token.IsCancellationRequested)
            {
                _view.DrawSearchStep("Musixmatch (search)...", ConsoleColor.DarkGray);
                syncedText = await _lyrics.GetLyricsFromMusixmatchAsync(artist, title);

                if (token.IsCancellationRequested) return;

                if (!string.IsNullOrEmpty(syncedText) && syncedText.Contains("["))
                {
                    var parsed = _lyrics.ParseLrc(syncedText);
                    if (parsed.Count > 0)
                    {
                        _state.SyncedLyrics = parsed;
                        _state.IsSynced = true;
                        _state.ScrollModeInfo = "Musixmatch";
                        ForceRedraw();
                        return;
                    }
                }
            }
            
            if (token.IsCancellationRequested) return;
            
            // Search failed
            ForceRedraw();
        }
    }
}
