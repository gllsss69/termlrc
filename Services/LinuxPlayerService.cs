using System;
using System.Diagnostics;
using System.IO;

namespace termlrc.Services
{

public class LinuxPlayerService : IPlayerService
{
    public string GetStatus()
    {
        try
        {
            string? status = RunPlayerctl("status");
            return string.IsNullOrEmpty(status) ? "Stopped" : status;
        }
        catch { }
        return "Stopped";
    }

    public string GetCurrentTrack()
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "playerctl",
                Arguments = "--player=%any metadata --format \"{{ artist }} - {{ title }}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process? process = Process.Start(psi))
            {
                if (process == null) return "The music isn't playing.";
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd().Trim();
                    return string.IsNullOrEmpty(result) ? "The music isn't playing." : result;
                }
            }
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
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "playerctl",
                Arguments = "--player=%any position",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process? process = Process.Start(psi))
            {
                if (process == null) return 0;
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd().Trim().Replace(',', '.');
                    if (double.TryParse(result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double position))
                    {
                        return position;
                    }
                }
            }
        }
        catch { }
        return 0;
    }



    public bool IsAd()
    {
        try
        {
            string? trackId = RunPlayerctl("metadata mpris:trackid");
            if (!string.IsNullOrEmpty(trackId))
            {
                return trackId.Contains("spotify:ad:") ||
                       trackId.Contains("/com/spotify/ad/") ||
                       trackId.Contains(":ad:");
            }
        }
        catch { }
        return false;
    }

    public string? GetSpotifyTrackId()
    {
        try
        {
            string? trackId = RunPlayerctl("metadata mpris:trackid");
            if (!string.IsNullOrEmpty(trackId))
            {
                if (trackId.Contains("spotify:track:"))
                {
                    return trackId.Replace("spotify:track:", "").Trim();
                }
                else if (trackId.Contains("/com/spotify/track/"))
                {
                    return trackId.Replace("/com/spotify/track/", "").Trim();
                }
            }
        }
        catch { }
        return null;
    }


    public double GetTrackDuration()
    {
        try
        {
            string? lengthStr = RunPlayerctl("metadata mpris:length");
            if (!string.IsNullOrEmpty(lengthStr))
            {
                if (long.TryParse(lengthStr.Trim(), out long microseconds))
                {
                    return microseconds / 1_000_000.0;
                }
            }
        }
        catch { }
        return 0;
    }


    public string? GetAlbumName()
    {
        try
        {
            return RunPlayerctl("metadata xesam:album");
        }
        catch { }
        return null;
    }

    private string? RunPlayerctl(string arguments)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "playerctl",
                Arguments = $"--player=%any {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process? process = Process.Start(psi))
            {
                if (process == null) return null;
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd().Trim();
                    return string.IsNullOrEmpty(result) ? null : result;
                }
            }
        }
        catch
        {
            return null;
        }
    }
}
}