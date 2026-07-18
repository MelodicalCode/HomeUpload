# Pre-flight disclaimer, this entire project was vibe coded so use it at your own risk and take what it says below with a grain of salt. I will eventually unvibe the repo into a more maintainable state.

# HomeUpload

HomeUpload is a lightweight .NET 10 web app for quickly transferring files from your phone to your computer over your local network, and vise versa. You can use it to transer files/images/videos between any device with access.

This is really more of a proof of concept, I'm wanting to make this in assembly I just needing a good working prototype.

It is designed for simple self-hosting on your LAN:
- Open the app URL on your phone browser
- Upload files from your device
- Store uploads on an attached USB drive when available
- Automatically fall back to local project storage when no USB drive is mounted
- View, download, and delete uploaded files from the web UI

## Why This Project Is Useful

- Fast ad-hoc file transfer without cloud accounts
- Keeps files local on your own network and hardware
- Supports large uploads (including large videos)
- Organizes uploads automatically:
  - `storage/photos`
  - `storage/videos`
  - `storage/files`
- Works from any phone browser on the same Wi-Fi network

## Tech Stack

- ASP.NET Core Minimal API (.NET 10)
- Static frontend (HTML/CSS/JavaScript)

## Prerequisites

1. Linux machine (current setup target)
2. .NET 10 SDK installed
3. Phone and host machine on the same local network
4. Optional: A mounted removable drive (USB thumbdrive) if you want external-media storage

## Install Dependencies

This project has no third-party package dependencies beyond .NET.

### Check .NET installation

```bash
dotnet --version
```

You should see a `10.x` version.

### If .NET is not installed (Fedora example)

```bash
sudo dnf install dotnet-sdk-10.0
```

For other distributions, install the .NET 10 SDK using Microsoft/Linux distro instructions.

## Setup

1. Clone or open this repository.
2. Optional: Attach and mount your USB drive if you want storage on external media.
3. Restore and build:

```bash
dotnet restore
dotnet build
```

## Run

```bash
dotnet run
```

On startup, the app prints:
- Local URL (for the host machine)
- Phone upload URL (LAN IP)
- Storage root path (USB drive if found, otherwise project `storage` folder)

Example:
- `http://localhost:3000`
- `http://192.168.x.x:3000`

Use the LAN URL on your phone (not `localhost`).

## USB Storage Behavior

The app automatically finds the first mounted removable drive under:
- `/run/media/<username>` or
- `/media/<username>`

If a removable drive is found, it creates and uses:
- `storage/photos`
- `storage/videos`
- `storage/files`

If no removable drive is found, it falls back to local project storage at:
- `./storage/photos`
- `./storage/videos`
- `./storage/files`

## Web UI Features

- Upload one or many files
- View uploaded files
- Download selected files (ZIP)
- Delete selected files (with confirmation modal)

## API Endpoints

- `GET /api/health` - Health check
- `POST /api/upload` - Upload files
- `GET /api/files` - List uploaded items
- `GET /download/{type}/{fileName}` - Download single file
- `POST /api/download-zip` - Download selected files as ZIP
- `POST /api/delete-files` - Delete selected files

## Troubleshooting

### 1) Phone cannot reach the site

- Ensure host and phone are on the same Wi-Fi
- Use the printed LAN URL (for example `http://192.168.1.244:3000`)
- Do not use `localhost` on your phone
- Check firewall rules for port `3000`

### 2) Address already in use on port 3000

Find the process:

```bash
ss -ltnp '( sport = :3000 )'
```

Stop it (replace `<pid>`):

```bash
kill <pid>
```

Then rerun `dotnet run`.

### 3) Removable drive not detected

- Confirm it is mounted under `/run/media/<username>` or `/media/<username>`
- Replug the drive and rerun the app
- If no USB drive is mounted, the app will still run and store files in the project `storage` folder

## Development Notes

- Project file: `HomeUpload.csproj`
- Entry point and API logic: `Program.cs`
- Frontend page: `wwwroot/index.html`


## Features to add before first product creation
[x] Gallery feature that plays all photos/videos in rotation
[ ] Video player that displays all videos uploaded and lets you select one to play next
[x] Icon to show/connect to wifi or direct internet connection is being used
[x] Icon to show available memory on each connected USB
[ ] Settings icon that sets user settings
    [ ] Ability to select a few RAID options for data storage
    [ ] Ability to check repo for updates and download them to update then restart
    [ ] Add setting for how long videos should play during gallery, (5s, 10s, 15, 20s, 25s, 30s, 1m, full video)
    [ ] Add setting for how long photos display for during gallery showing
    [ ] Add setting to change the upload batch size, be sure to note that 5 is recommended
[x] Displays url and has a QR code to open to the url on the center screen
[ ] Show USB ports and what's attached to them - will allow you to setup storage options and can show current storage metrics
[ ] Quick transfer feature to transfer all data from one USB to another
[ ] Take away ability to have internal storage, so a USB with adequate storage must be present on the device
[ ] Weather button - Opens weatherwise at the last set location
[ ] internet website button - allows any website to be displayed on the device
