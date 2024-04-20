# YoutubeToMp4

YoutubeToMp4 is a free tool for desktop, Android, and the web that enables you to download YouTube videos to your device.

## Shared Features

- **FFmpeg Thumbnail Generation:** YoutubeToMp4 can generate thumbnails for all types of downloaded videos.

- **Quality Selection:** Choose the download quality, offering flexibility between saving data or enjoying high-definition videos.
- **Video Cutting:** Choose a custom start and end time to cut out of the video and download it, instead of the full video.

### Desktop/Android Features

- **Client-Side Download:** All video conversion is done on the client. For most hardware, this should be much faster than the web, but very low-end/powered devices may slow down.

### Web Features

- **Server-Side Download:** All video conversion is done through a public server (my server), which returns a download to your browser. YouTube currently does not allow browsers to access their API.
- **Private Access Keys:** The public web server is accessed through a private key.

# Web Prerequisites

- **.NET 8:** Required to run the application. [Download and install from here](https://dotnet.microsoft.com/download).
- **FFmpeg:** Required to run the application. [Download and install from here](https://ffmpeg.org/).

# Desktop Prerequisites

- **.NET 8:** Required to run the application. [Download and install from here](https://dotnet.microsoft.com/download).
- **FFmpeg:** Required to run the application. [Download and install from here](https://ffmpeg.org/).

# Android Prerequisites

- **.NET 8:** Required to run the application. [Download and install from here](https://dotnet.microsoft.com/download).
- **FFmpeg:** Required to run the application. [Download and install from here](https://ffmpeg.org/).
- **Android SDK:** Required to run the application on Android. [Download and install from here](https://docs.avaloniaui.net/docs/guides/platforms/android/setting-up-your-developer-environment-for-android).

## Shared Installation Instructions

1. Install prerequisites.
2. Clone the repository.
3. Open a terminal in the application's directory appropriate to your platform: ex - YoutubeToMp4Avalonia/YoutubeToMp4.Desktop.

## Web Installation Instructions

4. Publish to your system: `dotnet publish -c Release`
   - Blazor WASM currently does not allow publishing trimmed.
5. The content in `bin/Release/publish` is your app, and the executable is an exe file that's the name of the app.

## Desktop Installation Instructions

4. Publish to your system: `dotnet publish -c Release -p:PublishTrimmed=true /p:AndroidSdkDirectory=/path/to/sdk`
5. The content in `bin/Release/publish` is your app, and the executable is an exe file that's the name of the app.

## Android Installation Instructions

4. Publish to your system: `dotnet publish -c Release -p:PublishTrimmed=true /p:AndroidSdkDirectory=/path/to/sdk`
   - Note publishing trimmed may take very long, like 15-20 minutes, so if you are testing I suggest omitting that part. It is kind of out of my hands... The frameworks I am using are slow to trim.
5. Build ffmpeg for Android and include it in your publish folder.
6. The content in `bin/Release/publish` is your app, and the executable is an apk file that's the name of the app.

## Useful Commands

```
JAVA ANDROID GENERATE KEY STORE:
    keytool -genkey -v -keystore my-release-key.keystore -keyalg RSA -keysize 2048 -validity 10000 -alias my-alias
    dotnet publish -f net8.0-android -c Release -p:AndroidKeyStore=true -p:AndroidSigningKeyStore=myapp.keystore -p:AndroidSigningKeyAlias=myapp -p:AndroidSigningKeyPass=mypassword -p:AndroidSigningStorePass=mypassword
    apksigner sign --ks my-release-key.jks --out my_app_signed.apk my_app.apk
    apksigner sign --ks /home/martin/Desktop/my-release-key.keystore --out my_app_signed.apk com.CompanyName.dlTubeAvaloniaCrossPlatform.apk
```

## Todo

### macOS

Support for macOS will be available in the future. The macOS build process is quite long and requires a Mac, which I do not currently have.


