# AndroidYouTubeDownloader

## Build 

Run build script
```
.\CreateApk.ps1 -newVersionName 'x.y' -signingKeyPass 'mypassword'
```

Or:  
    Update `ApplicationVersion` and `ApplicationDisplayVersion` in csproj file and project properties.  
    Clear bin and obj folders.  
    ```
    dotnet publish -f:net6.0-android -c:Release /p:AndroidSigningKeyPass=mypassword /p:AndroidSigningStorePass=mypassword
    ```  
    Published apk folder: `\AndroidYouTubeDownloader\AndroidYouTubeDownloader\bin\Release\net6.0-android\publish`

## Build ffmpeg-kit

```
./android.sh --enable-gpl --enable-x264 --enable-libwebp --enable-opus --enable-libvorbis --disable-arm-v7a --disable-x86 --no-ffmpeg-kit-protocols
```
