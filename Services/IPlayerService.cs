namespace termlrc.Services
{
    /// <summary>
    /// Abstraction for retrieving playback metadata from any media player
    /// on any supported operating system.
    /// </summary>
    public interface IPlayerService
    {
        /// <summary>Returns the playback status string (e.g. "Playing", "Paused", "Stopped").</summary>
        string GetStatus();

        /// <summary>Returns a formatted "Artist - Title" string for the current track.</summary>
        string GetCurrentTrack();

        /// <summary>Returns the current playback position in seconds.</summary>
        double GetCurrentPosition();

        /// <summary>Returns true when the player is showing an advertisement.</summary>
        bool IsAd();

        /// <summary>
        /// Returns the Spotify track ID (e.g. "4uLU6hMCjMI75M1A2tKUQC") if the
        /// active player is Spotify; otherwise returns null.
        /// </summary>
        string? GetSpotifyTrackId();

        /// <summary>Returns the duration of the current track in seconds.</summary>
        double GetTrackDuration();

        /// <summary>Returns the album name of the current track, or null.</summary>
        string? GetAlbumName();
    }
}
