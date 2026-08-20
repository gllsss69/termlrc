using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using termlrc.Models;

namespace termlrc.Services
{


public class LyricsService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly string _hmacKey = "IEJ5E8XFaH" + "QvIQNfs7IC";
    private static readonly string _apiBase = "https://apic-desktop.musixmatch.com/ws/1.1/";

    private string? _cachedToken = null;
    private DateTime _tokenExpiry = DateTime.MinValue;

    static LyricsService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
    }


    private string SignRequest(string method, Dictionary<string, string> queryParams, string timestamp)
    {
        string url = _apiBase + method + "?" + string.Join("&",
            queryParams.OrderBy(kv => kv.Key).Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        string dataToSign = url + timestamp;

        byte[] keyBytes = Encoding.UTF8.GetBytes(_hmacKey);
        byte[] dataBytes = Encoding.UTF8.GetBytes(dataToSign);

        using (var hmac = new HMACSHA1(keyBytes))
        {
            byte[] hash = hmac.ComputeHash(dataBytes);

            return Convert.ToBase64String(hash)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }


    private string? GenerateMusixmatchToken()
    {
        try
        {
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string guid = Guid.NewGuid().ToString();

            var queryParams = new Dictionary<string, string>
            {
                { "format", "json" },
                { "guid", guid },
                { "timestamp", timestamp },
                { "build_number", "2017091202" },
                { "lang", "en-GB" },
                { "app_id", "web-desktop-app-v1.0" }
            };

            string signature = SignRequest("token.get", queryParams, timestamp);
            queryParams["signature"] = signature;
            queryParams["signature_protocol"] = "sha1";

            string queryString = string.Join("&",
                queryParams.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            string url = _apiBase + "token.get?" + queryString;

            string response = _httpClient.GetStringAsync(url).GetAwaiter().GetResult();

            using (JsonDocument doc = JsonDocument.Parse(response))
            {
                if (doc.RootElement.TryGetProperty("message", out JsonElement msg) &&
                    msg.TryGetProperty("body", out JsonElement body) &&
                    body.ValueKind == JsonValueKind.Object &&
                    body.TryGetProperty("user_token", out JsonElement tokenEl))
                {
                    string? token = tokenEl.GetString();
                    if (!string.IsNullOrEmpty(token) && token != "MusixmatchUserToken")
                    {
                        return token;
                    }
                }
            }
        }
        catch { }
        return null;
    }

 
    private string? GetMusixmatchToken()
    {
        string? envToken = Environment.GetEnvironmentVariable("MUSIXMATCH_TOKEN");
        if (!string.IsNullOrEmpty(envToken))
            return envToken;

        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        string? newToken = GenerateMusixmatchToken();
        if (!string.IsNullOrEmpty(newToken))
        {
            _cachedToken = newToken;
            _tokenExpiry = DateTime.UtcNow.AddMinutes(10);
            return _cachedToken;
        }

        return null;
    }


    public string? GetLyricsFromMusixmatchBySpotifyId(string spotifyTrackId)
    {
        try
        {
            string? usertoken = GetMusixmatchToken();
            if (string.IsNullOrEmpty(usertoken)) return null;

            string matchUrl = $"{_apiBase}matcher.track.get?app_id=web-desktop-app-v1.0" +
                              $"&usertoken={usertoken}&track_spotify_id=spotify:track:{spotifyTrackId}";

            string matchResponse = _httpClient.GetStringAsync(matchUrl).GetAwaiter().GetResult();

            int trackId = 0;
            using (JsonDocument doc = JsonDocument.Parse(matchResponse))
            {
                if (doc.RootElement.TryGetProperty("message", out JsonElement msg) &&
                    msg.TryGetProperty("body", out JsonElement body) &&
                    body.ValueKind == JsonValueKind.Object &&
                    body.TryGetProperty("track", out JsonElement track))
                {
                    trackId = track.GetProperty("track_id").GetInt32();
                }
            }

            if (trackId == 0) return null;

            return FetchMusixmatchSubtitle(usertoken, trackId);
        }
        catch { }
        return null;
    }


    public string? GetLyricsFromMusixmatch(string artist, string title)
    {
        try
        {
            string? usertoken = GetMusixmatchToken();
            if (string.IsNullOrEmpty(usertoken)) return null;

            string cleanArtist = CleanQuery(artist);
            string cleanTitle = CleanQuery(title);

            string searchUrl = $"{_apiBase}track.search?app_id=web-desktop-app-v1.0" +
                               $"&usertoken={usertoken}&q_artist={Uri.EscapeDataString(cleanArtist)}&q_track={Uri.EscapeDataString(cleanTitle)}&f_has_lyrics=1";

            var response = _httpClient.GetAsync(searchUrl).GetAwaiter().GetResult();
            string searchResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            int trackId = 0;
            using (JsonDocument doc = JsonDocument.Parse(searchResponse))
            {
                if (doc.RootElement.TryGetProperty("message", out JsonElement messageElement) &&
                    messageElement.TryGetProperty("body", out JsonElement bodyElement) &&
                    bodyElement.ValueKind == JsonValueKind.Object &&
                    bodyElement.TryGetProperty("track_list", out JsonElement trackList))
                {
                    if (trackList.GetArrayLength() > 0)
                    {
                        trackId = trackList[0].GetProperty("track").GetProperty("track_id").GetInt32();
                    }
                }
            }

            if (trackId == 0) return null;

            return FetchMusixmatchSubtitle(usertoken, trackId);
        }
        catch { }
        return null;
    }


    private string? FetchMusixmatchSubtitle(string usertoken, int trackId)
    {
        try
        {
            string lyricsUrl = $"{_apiBase}track.subtitle.get?app_id=web-desktop-app-v1.0" +
                               $"&usertoken={usertoken}&track_id={trackId}&subtitle_format=lrc";

            string lyricsResponse = _httpClient.GetStringAsync(lyricsUrl).GetAwaiter().GetResult();

            using (JsonDocument doc = JsonDocument.Parse(lyricsResponse))
            {
                if (doc.RootElement.TryGetProperty("message", out JsonElement msg) &&
                    msg.TryGetProperty("body", out JsonElement body) &&
                    body.ValueKind == JsonValueKind.Object &&
                    body.TryGetProperty("subtitle", out JsonElement subtitle))
                {
                    string subtitleBody = subtitle.GetProperty("subtitle_body").GetString() ?? "";
                    if (!string.IsNullOrEmpty(subtitleBody))
                    {
                        return ConvertMusixmatchJsonToLrc(subtitleBody);
                    }
                }
            }
        }
        catch { }
        return null;
    }


    public string? GetLyricsFromLrcLib(string artist, string title)
    {
        try
        {
            string cleanArtist = CleanQuery(artist);
            string cleanTitle = CleanQuery(title);

            string searchUrl = $"https://lrclib.net/api/get?artist_name={Uri.EscapeDataString(cleanArtist)}&track_name={Uri.EscapeDataString(cleanTitle)}";
            
            using (var request = new HttpRequestMessage(HttpMethod.Get, searchUrl))
            {
                request.Headers.Add("User-Agent", "termlrc v1.0");
                var response = _httpClient.Send(request);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("syncedLyrics", out JsonElement syncedElement) && 
                            syncedElement.ValueKind == JsonValueKind.String)
                        {
                            string syncedText = syncedElement.GetString() ?? "";
                            if (!string.IsNullOrEmpty(syncedText))
                            {
                                return syncedText;
                            }
                        }
                    }
                }
            }
        }
        catch { }
        return null;
    }


    public List<SyncedLine> ParseLrc(string lrcLyrics)
    {
        var lines = new List<SyncedLine>();
        var rawLines = lrcLyrics.Split('\n');

        foreach (var line in rawLines)
        {
            var match = Regex.Match(line, @"^\[(\d+):(\d+)[\.\:](\d+)\](.*)");
            if (match.Success)
            {
                int minutes = int.Parse(match.Groups[1].Value);
                int seconds = int.Parse(match.Groups[2].Value);
                string msStr = match.Groups[3].Value;

                int milliseconds;
                if (msStr.Length <= 2)
                    milliseconds = int.Parse(msStr) * 10;
                else
                    milliseconds = int.Parse(msStr);

                double totalSeconds = minutes * 60 + seconds + (milliseconds / 1000.0);
                string text = match.Groups[4].Value.Trim();

                lines.Add(new SyncedLine { TimeInSeconds = totalSeconds, Text = text });
            }
        }
        return lines;
    }


    private string CleanQuery(string query)
    {
        if (string.IsNullOrEmpty(query)) return query;
        return query.Trim();
    }

    private string ConvertMusixmatchJsonToLrc(string jsonSubtitle)
    {
        try
        {
            var lrcBuilder = new StringBuilder();
            using (JsonDocument doc = JsonDocument.Parse(jsonSubtitle))
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.TryGetProperty("text", out JsonElement textOpt) && item.TryGetProperty("time", out JsonElement timeOpt))
                    {
                        string text = textOpt.GetString() ?? "";
                        double totalSeconds = timeOpt.GetProperty("total").GetDouble();

                        TimeSpan t = TimeSpan.FromSeconds(totalSeconds);
                        string timestamp = $"[{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds / 10:D2}]";

                        lrcBuilder.AppendLine($"{timestamp}{text}");
                    }
                }
            }
            return lrcBuilder.ToString();
        }
        catch
        {
            return jsonSubtitle;
        }
    }
}
}