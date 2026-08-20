# termlrc

> ⚠️ **Note:** This project is currently in active development (Work In Progress). It is not the final version, and features, UI, or stability may change as development continues.

**termlrc** is a stylish, terminal-based synchronized lyrics viewer that works with **any media player** on **Linux** and **Windows**. It detects the currently playing song, fetches synchronized lyrics from popular APIs (Musixmatch, LRCLIB), and renders them in beautiful ASCII art using `Figgle`.

## ✨ Features

- **Cross-Platform**: Works natively on both Linux and Windows — no extra configuration required.
- **Universal Player Support**: Detects the currently playing song from Spotify, VLC, Rhythmbox, browser media, and any other media player that reports to the system.
- **Synced Lyrics in ASCII**: Experience your music like never before with dynamic, terminal-based ASCII art lyrics.
- **Cyrillic Support**: Automatically transliterates Ukrainian, Russian, and other Cyrillic text into Latin characters on the fly, ensuring all lyrics render flawlessly in ASCII fonts.
- **Word-by-word Mode**: Words appear one at a time with character-length-weighted timing — longer words stay on screen proportionally longer for natural synchronization.
- **Multiple Lyric Sources**: Automatically searches Musixmatch and LRCLIB to find the most accurate synced lyrics.
- **Real-time Customization**: Change ASCII fonts, text colors, HUD visibility, and Word-by-word mode on the fly using keyboard hotkeys.
- **Smart Playback Tracking**: Seamlessly handles pausing, resuming, ad breaks, and track changes.

## 📋 Prerequisites

### All Platforms
- **.NET SDK 10.0** (or later)

### Linux
- **`playerctl`**: A command-line utility for controlling media players. Install via your package manager (e.g. `sudo apt install playerctl`).
- Any **MPRIS-compatible media player** (Spotify, VLC, Rhythmbox, Audacious, etc.) must be running.

### Windows
- **Windows 10** (Build 17763 / version 1809) or later.
- Any media player that reports to the **Windows System Media Transport Controls** (most modern players do — Spotify, web browsers, Groove Music, foobar2000, etc.).

## 🚀 Getting Started

1. Clone or download the repository to your local machine.
2. Ensure you have a media player open and playing music.
3. Open a terminal in the project directory and run:

```bash
dotnet run
```

## ⌨️ Hotkeys / Controls

While the application is running, you can use the following keys to customize your experience:

- `F` - **Change Font**: Cycle through various available ASCII fonts.
- `C` - **Change Color**: Cycle through the available terminal text colors.
- `H` - **Toggle HUD**: Change the visibility of the bottom HUD menu (Full, Minimal, or Hidden).
- `W` - **Toggle Word-by-word Mode**: Switches between displaying whole lines or interpolating words one at a time.

## 🛠️ Built With

- **C# / .NET 10.0**
- [**Figgle**](https://github.com/drewnoakes/figgle) - For generating ASCII art from text.
- **playerctl** (Linux) / **Windows SMTC API** (Windows) - For retrieving metadata and playback status.
- **Musixmatch & LRCLIB APIs** - For fetching synced `.lrc` lyrics.