using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        private async Task<string?> GenerateMusixmatchTokenAsync()
        {
            // Retry up to 3 times — sometimes the first attempt returns a dummy token
            for (int attempt = 0; attempt < 3; attempt++)
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

                    string response = await _httpClient.GetStringAsync(url);

                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        if (doc.RootElement.TryGetProperty("message", out JsonElement msg) &&
                            msg.TryGetProperty("body", out JsonElement body) &&
                            body.ValueKind == JsonValueKind.Object &&
                            body.TryGetProperty("user_token", out JsonElement tokenEl))
                        {
                            string? token = tokenEl.GetString();
                            if (!string.IsNullOrEmpty(token) &&
                                token != "MusixmatchUserToken" &&
                                !token.All(c => c == '0'))
                            {
                                return token;
                            }
                        }
                    }
                }
                catch { }

                // Small delay before retrying
                if (attempt < 2)
                    await Task.Delay(500);
            }
            return null;
        }

        private async Task<string?> GetMusixmatchTokenAsync()
        {
            string? envToken = Environment.GetEnvironmentVariable("MUSIXMATCH_TOKEN");
            if (!string.IsNullOrEmpty(envToken))
                return envToken;

            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
                return _cachedToken;

            string? newToken = await GenerateMusixmatchTokenAsync();
            if (!string.IsNullOrEmpty(newToken))
            {
                _cachedToken = newToken;
                _tokenExpiry = DateTime.UtcNow.AddMinutes(10);
                return _cachedToken;
            }

            return null;
        }

        public async Task<string?> GetLyricsFromMusixmatchBySpotifyIdAsync(string spotifyTrackId)
        {
            try
            {
                string? usertoken = await GetMusixmatchTokenAsync();
                if (string.IsNullOrEmpty(usertoken)) return null;

                string url = $"{_apiBase}macro.subtitles.get?app_id=web-desktop-app-v1.0" +
                             $"&usertoken={usertoken}&track_spotify_id=spotify:track:{spotifyTrackId}&format=json";

                string response = await _httpClient.GetStringAsync(url);
                return ExtractSubtitleFromMacroResponse(response);
            }
            catch { }
            return null;
        }

        public async Task<string?> GetLyricsFromMusixmatchAsync(string artist, string title)
        {
            try
            {
                string? usertoken = await GetMusixmatchTokenAsync();
                if (string.IsNullOrEmpty(usertoken)) return null;

                string cleanArtist = CleanQuery(artist);
                string cleanTitle = CleanQuery(title);

                string url = $"{_apiBase}macro.subtitles.get?app_id=web-desktop-app-v1.0" +
                             $"&usertoken={usertoken}&q_artist={Uri.EscapeDataString(cleanArtist)}" +
                             $"&q_track={Uri.EscapeDataString(cleanTitle)}&format=json";

                string response = await _httpClient.GetStringAsync(url);
                return ExtractSubtitleFromMacroResponse(response);
            }
            catch { }
            return null;
        }

        private string? ExtractSubtitleFromMacroResponse(string jsonResponse)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    // Navigate: message.body.macro_calls["track.subtitles.get"].message.body.subtitle_list[0].subtitle.subtitle_body
                    if (doc.RootElement.TryGetProperty("message", out JsonElement msg) &&
                        msg.TryGetProperty("body", out JsonElement body) &&
                        body.TryGetProperty("macro_calls", out JsonElement macroCalls) &&
                        macroCalls.TryGetProperty("track.subtitles.get", out JsonElement subtitlesGet) &&
                        subtitlesGet.TryGetProperty("message", out JsonElement subMsg) &&
                        subMsg.TryGetProperty("body", out JsonElement subBody) &&
                        subBody.ValueKind == JsonValueKind.Object &&
                        subBody.TryGetProperty("subtitle_list", out JsonElement subtitleList) &&
                        subtitleList.GetArrayLength() > 0)
                    {
                        var firstSubtitle = subtitleList[0];
                        if (firstSubtitle.TryGetProperty("subtitle", out JsonElement subtitle) &&
                            subtitle.TryGetProperty("subtitle_body", out JsonElement subtitleBodyEl))
                        {
                            string subtitleBody = subtitleBodyEl.GetString() ?? "";
                            if (!string.IsNullOrEmpty(subtitleBody))
                            {
                                // subtitle_body is already in LRC format from this endpoint
                                return subtitleBody;
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public async Task<string?> GetLyricsFromLrcLibAsync(string artist, string title)
        {
            try
            {
                string cleanArtist = CleanQuery(artist);
                string cleanTitle = CleanQuery(title);

                string searchUrl = $"https://lrclib.net/api/get?artist_name={Uri.EscapeDataString(cleanArtist)}&track_name={Uri.EscapeDataString(cleanTitle)}";
                
                using (var request = new HttpRequestMessage(HttpMethod.Get, searchUrl))
                {
                    request.Headers.Add("User-Agent", "termlrc v1.0");
                    var response = await _httpClient.SendAsync(request);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
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
    }
}