#if WINDOWS
using System;
using Windows.Media.Control;

namespace termlrc.Services
{
    /// <summary>
    /// Windows implementation of IPlayerService.
    /// Uses the Windows 10/11 System Media Transport Controls (SMTC) API
    /// to read "now playing" metadata from any media player.
    /// </summary>
    public class WindowsPlayerService : IPlayerService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
        private bool _initialized;

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                try
                {
                    _sessionManager = GlobalSystemMediaTransportControlsSessionManager
                        .RequestAsync()
                        .AsTask()
                        .GetAwaiter()
                        .GetResult();
                }
                catch { }
                _initialized = true;
            }
        }

        private GlobalSystemMediaTransportControlsSession? GetCurrentSession()
        {
            EnsureInitialized();
            return _sessionManager?.GetCurrentSession();
        }

        public string GetStatus()
        {
            try
            {
                var session = GetCurrentSession();
                if (session == null)
                    return "Stopped";

                var info = session.GetPlaybackInfo();
                if (info == null)
                    return "Stopped";

                return info.PlaybackStatus switch
                {
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => "Playing",
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => "Paused",
                    _ => "Stopped"
                };
            }
            catch { }
            return "Stopped";
        }

        public string GetCurrentTrack()
        {
            try
            {
                var session = GetCurrentSession();
                if (session == null)
                    return "The music isn't playing.";

                var mediaProperties = session.TryGetMediaPropertiesAsync()
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();

                if (mediaProperties == null)
                    return "The music isn't playing.";

                string artist = mediaProperties.Artist ?? "";
                string title = mediaProperties.Title ?? "";

                if (string.IsNullOrWhiteSpace(artist) && string.IsNullOrWhiteSpace(title))
                    return "The music isn't playing.";

                if (string.IsNullOrWhiteSpace(artist))
                    return title;

                return $"{artist} - {title}";
            }
            catch
            {
                return "Failed to retrieve the song";
            }
        }

        public double GetCurrentPosition()
        {
            try
            {
                var session = GetCurrentSession();
                if (session == null)
                    return 0;

                var timeline = session.GetTimelineProperties();
                if (timeline == null)
                    return 0;

                return timeline.Position.TotalSeconds;
            }
            catch { }
            return 0;
        }

        public bool IsAd()
        {
            try
            {
                var session = GetCurrentSession();
                if (session == null)
                    return false;

                // Check if the source app is Spotify
                string sourceApp = session.SourceAppUserModelId ?? "";
                if (!sourceApp.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
                    return false;

                // For Spotify on Windows, ads typically have no artist or have
                // a very short duration. We check the media properties for clues.
                var mediaProperties = session.TryGetMediaPropertiesAsync()
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();

                if (mediaProperties == null)
                    return false;

                string artist = mediaProperties.Artist ?? "";
                string title = mediaProperties.Title ?? "";

                // Spotify ads often have an empty artist or "Spotify" as the artist
                if (string.IsNullOrWhiteSpace(artist) && !string.IsNullOrWhiteSpace(title))
                    return true;

                if (artist.Equals("Spotify", StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }
            catch { }
            return false;
        }

        public string? GetSpotifyTrackId()
        {
            // The Windows SMTC API does not expose Spotify track IDs.
            // The app will fall back to Artist + Title search for lyrics.
            return null;
        }

        public double GetTrackDuration()
        {
            try
            {
                var session = GetCurrentSession();
                if (session == null)
                    return 0;

                var timeline = session.GetTimelineProperties();
                if (timeline == null)
                    return 0;

                return timeline.EndTime.TotalSeconds;
            }
            catch { }
            return 0;
        }

        public string? GetAlbumName()
        {
            try
            {
                var session = GetCurrentSession();
                if (session == null)
                    return null;

                var mediaProperties = session.TryGetMediaPropertiesAsync()
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();

                return mediaProperties?.AlbumTitle;
            }
            catch { }
            return null;
        }
    }
}
#endif
