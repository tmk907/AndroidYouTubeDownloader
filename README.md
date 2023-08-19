# AndroidYouTubeDownloader

Android app for downloading audio and videos from Youtube.

## Screenshots

<img src="https://github.com/tmk907/AndroidYouTubeDownloader/blob/master/images/download1.jpg" alt="Download" width=50% height=50%>
<img src="https://github.com/tmk907/AndroidYouTubeDownloader/blob/master/images/download2.jpg" alt="Download" width=50% height=50%>
<img src="https://github.com/tmk907/AndroidYouTubeDownloader/blob/master/images/settings.jpg" alt="Settings" width=50% height=50%>

## Libraries used

- [YouTubeStreamsExtractor](https://github.com/tmk907/YouTubeStreamsExtractor)
- [FFmpegKitSlim](https://github.com/tmk907/FFmpegKitSlim)
- [ATL .NET](https://github.com/Zeugma440/atldotnet)
- [Xamarin Essentials](https://github.com/xamarin/Essentials)
- [Square.OkIO](https://www.nuget.org/packages/Square.OkIO)
- [Glide](https://www.nuget.org/packages/Xamarin.Android.Glide/)
- [CurrentActivityPlugin](https://github.com/jamesmontemagno/CurrentActivityPlugin)

## Build 

Run build script
```
.\CreateApk.ps1 -newVersionName 'x.y' -signingKeyPass 'mypassword'
```

## Build ffmpeg-kit

```
./android.sh --enable-gpl --enable-x264 --enable-libwebp --enable-opus --enable-libvorbis --disable-arm-v7a --disable-x86 --no-ffmpeg-kit-protocols
```
