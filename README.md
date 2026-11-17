# VentIt

A personal life-organiser app built with **.NET MAUI** for Android and Windows. It brings planning, journaling, songwriting and an AI chat companion together in one place.

![.NET MAUI](https://img.shields.io/badge/.NET_MAUI-.NET_9-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Claude API](https://img.shields.io/badge/Claude_API-191919?style=flat-square&logo=anthropic&logoColor=white)

## Features

| Page | What it does |
|---|---|
| **Chat** | AI companion through the Anthropic Messages API, with saved chat sessions |
| **Journal** | Dated journal entries |
| **Planner / Timetable / Routine** | Tasks, time blocks and recurring routines |
| **Notes** | Free-form notes |
| **Mind Map** | Node-based idea mapping |
| **Songwriting** | Draft and organise songs |
| **Settings** | API key and preferences; Google Drive backup and restore |

## Architecture

```
Models/      TaskItem, JournalEntry, ChatSession, Song, MindMapNode, TimeBlock …
Services/    ClaudeService, DatabaseService, GoogleDriveService, JournalService …
ViewModels/  SettingsViewModel
Views/       one page per feature
```

Data is stored locally. Google Drive is used for optional backup and sync.

## Build

Open the project in Visual Studio 2022 with the .NET MAUI workload (.NET 9) installed. Run on Android (API 33+) or Windows. Enter your Anthropic API key on the Settings page at runtime; it isn't stored in the source.

## License

AGPL-3.0
