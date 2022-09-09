Param(
    [Parameter(Mandatory=$true)]
    [string]
    $newVersionName,
    [Parameter(Mandatory=$true)]
    [string]
    $signingKeyPass
)

Function ChangeVersion {
    Param(
        [Parameter(Mandatory=$true)]
        [string]
        $newVersionName
    )

    # Get current version code and increment
    $manifestPath = Resolve-Path '.\AndroidYouTubeDownloader\AndroidManifest.xml'
    [xml]$manifest = Get-Content -Path $manifestPath
    $newVersionCode = ([int]$manifest.manifest.versionCode + 1).ToString()

    $csprojPath = Resolve-Path '.\AndroidYouTubeDownloader\AndroidYouTubeDownloader.csproj'

    # Update manifest
    (Get-Content $manifestPath) -Replace 'android:versionCode="\d+"', "android:versionCode=`"${newVersionCode}`"" | Set-Content $manifestPath
    (Get-Content $manifestPath) -Replace 'android:versionName="[\w\d.-]+"', "android:versionName=`"$newVersionName`"" | Set-Content $manifestPath

    # Update project
    (Get-Content $csprojPath) -Replace '<ApplicationVersion>\d+</ApplicationVersion>', "<ApplicationVersion>$newVersionCode</ApplicationVersion>" | Set-Content $csprojPath
    (Get-Content $csprojPath) -Replace '<ApplicationDisplayVersion>[\w\d.-]+</ApplicationDisplayVersion>', "<ApplicationDisplayVersion>$newVersionName</ApplicationDisplayVersion>" | Set-Content $csprojPath

    Write-Host "New versionName: $newVersionName, new versionCode: $newVersionCode"
}

Function Build {
    Param(
        [Parameter(Mandatory=$true)]
        [string]
        $signingKeyPass
    )

    dotnet publish -f:net6.0-android -c:Release /p:AndroidSigningKeyPass=$signingKeyPass /p:AndroidSigningStorePass=$signingKeyPass
}

Function CopyToLocalAppStore {
    Param(
        [Parameter(Mandatory=$true)]
        [string]
        $versionName
    )

    $publishDirectory = '.\AndroidYouTubeDownloader\bin\Release\net6.0-android\publish'
    $appStoreDirectory = 'C:\Source\AppStore'
    $appName = 'AndroidYouTubeDownloader'

    $package = 'com.tmk907.androidyoutubedownloader'
    $architectures = @('arm64-v8a','armeabi-v7a','x86_64')

    foreach($arch in $architectures){
        $apkName = "$package-$arch-Signed.apk"
        $publishedApkName = "$appName-v$versionName-$arch.apk"

        Copy-Item -Path "$publishDirectory\$apkName" -Destination "$appStoreDirectory\$publishedApkName"
        Write-Host "$publishedApkName copied to  $publishDirectory"
    }
}

ChangeVersion -newVersionName $newVersionName

Build -signingKeyPass $signingKeyPass

CopyToLocalAppStore -versionName $newVersionName