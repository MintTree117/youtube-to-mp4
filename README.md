# YouTube Downloader Projects

This repository contains two YouTube downloader applications:

1. **Avalonia YouTube Downloader**
2. **Blazor YouTube Downloader**

You can also try the Blazor YouTube Downloader application [here](https://coral-app-anp75.ondigitalocean.app/).

## Avalonia YouTube Downloader

A cross-platform desktop application for Windows and Linux, built with .NET 8 and Avalonia (WPF).

### Features

#### Technologies Used

- .NET 8
- C#
- Avalonia (WPF)

#### Architecture

- MVVM (Model-View-ViewModel) Pattern

#### Functionality

- **YouTube Integration:**
  - Integrates the third-party package `YoutubeExplode` for YouTube video processing.
  - Allows users to paste YouTube links and select download options.
  
- **Download Options:**
  - Choose between mixed (video + audio), video only, or audio only.
  - Select desired download quality.

- **Video Editing:**
  - Enables users to cut videos by specifying start and end times.
  - Downloads the video thumbnail and uses `ffmpeg` to embed the thumbnail in the `.mp4` file.
  
- **Bundled Tools:**
  - `ffmpeg` is included directly in the application for seamless video processing.

### Platforms

- Windows
- Linux

## Blazor YouTube Downloader

A web application hosted on Digital Ocean as a Docker container, built with .NET 8 and Blazor.

### Features

#### Technologies Used

- .NET 8
- Blazor
- Docker
- Hosted on Digital Ocean

#### Functionality

- **YouTube Integration:**
  - Integrates the third-party package `YoutubeExplode` for YouTube video processing.
  - Allows users to paste YouTube links and select download options.
  
- **Download Options:**
  - Choose between mixed (video + audio), video only, or audio only.
  - Select desired download quality.

- **Video Editing:**
  - Enables users to cut videos by specifying start and end times.
  - Downloads the video thumbnail and uses `ffmpeg` to embed the thumbnail in the `.mp4` file.
  
- **Bundled Tools:**
  - `ffmpeg` is included directly in the application for seamless video processing.

- **Server-Side Processing:**
  - Performs video processing on the server and returns the downloaded file to the user.


