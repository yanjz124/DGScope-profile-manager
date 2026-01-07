#[OK] Build[OK] Release[OK] Package
#[OK] This[OK] script[OK] creates[OK] a[OK] complete[OK] release[OK] package[OK] with[OK] DGScope,[OK] Profiles,[OK] and[OK] Profile[OK] Manager

param(
[OK] [OK] [OK] [OK] [string]$ReleaseVersion[OK] =[OK] "1.0.0",
[OK] [OK] [OK] [OK] [string]$ScopeRelease[OK] =[OK] "latest"[OK] [OK] #[OK] GitHub[OK] release[OK] tag[OK] or[OK] 'latest'
)

$ErrorActionPreference[OK] =[OK] "Stop"
$ProgressPreference[OK] =[OK] "SilentlyContinue"

$ReleaseDir[OK] =[OK] ".\release"
$PublishDir[OK] =[OK] "src\DGScopeProfileManager\bin\Release\net10.0-windows\win-x64\publish"

Write-Host[OK] "Building[OK] DGScope[OK] Profile[OK] Manager[OK] Release[OK] v$ReleaseVersion"[OK] -ForegroundColor[OK] Green
Write-Host[OK] "========================================"[OK] -ForegroundColor[OK] Green

#[OK] Step[OK] 1:[OK] Build[OK] the[OK] application
Write-Host[OK] "`n[1/4][OK] Building[OK] application..."[OK] -ForegroundColor[OK] Cyan
dotnet[OK] publish[OK] src/DGScopeProfileManager/DGScopeProfileManager.csproj[OK] -c[OK] Release[OK] -r[OK] win-x64[OK] --self-contained[OK] -p:PublishSingleFile=true[OK] -o[OK] $PublishDir[OK] |[OK] Out-Null
if[OK] ($LASTEXITCODE[OK] -ne[OK] 0)[OK] {[OK] throw[OK] "Build[OK] failed"[OK] }
Write-Host[OK] "�?Application[OK] built"[OK] -ForegroundColor[OK] Green

#[OK] Step[OK] 2:[OK] Create[OK] release[OK] directory[OK] structure
Write-Host[OK] "`n[2/4][OK] Creating[OK] release[OK] directory..."[OK] -ForegroundColor[OK] Cyan
if[OK] (Test-Path[OK] $ReleaseDir)[OK] {[OK] Remove-Item[OK] $ReleaseDir[OK] -Recurse[OK] -Force[OK] }
$null[OK] =[OK] mkdir[OK] "$ReleaseDir\DGScope-Profile-Manager"
$ReleaseRoot[OK] =[OK] "$ReleaseDir\DGScope-Profile-Manager"

Write-Host[OK] "�?Release[OK] directory[OK] created[OK] at[OK] $ReleaseRoot"[OK] -ForegroundColor[OK] Green

#[OK] Step[OK] 3:[OK] Copy[OK] Profile[OK] Manager
Write-Host[OK] "`n[3/4][OK] Copying[OK] Profile[OK] Manager..."[OK] -ForegroundColor[OK] Cyan
$null[OK] =[OK] mkdir[OK] "$ReleaseRoot\ProfileManager"
Copy-Item[OK] "$PublishDir\*"[OK] "$ReleaseRoot\ProfileManager\"[OK] -Recurse
Write-Host[OK] "�?Profile[OK] Manager[OK] copied"[OK] -ForegroundColor[OK] Green

#[OK] Step[OK] 4:[OK] Download[OK] DGScope
Write-Host[OK] "`n[4/4][OK] Downloading[OK] DGScope[OK] from[OK] GitHub..."[OK] -ForegroundColor[OK] Cyan

$Owner[OK] =[OK] "yanjz124"
$Repo[OK] =[OK] "scope"

#[OK] Get[OK] the[OK] latest[OK] release
$ReleasesUrl[OK] =[OK] "https://api.github.com/repos/$Owner/$Repo/releases"
$Release[OK] =[OK] if[OK] ($ScopeRelease[OK] -eq[OK] "latest")[OK] {
[OK] [OK] [OK] [OK] (Invoke-WebRequest[OK] -Uri[OK] $ReleasesUrl[OK] |[OK] ConvertFrom-Json)[0]
}[OK] else[OK] {
[OK] [OK] [OK] [OK] Invoke-WebRequest[OK] -Uri[OK] "$ReleasesUrl/tags/$ScopeRelease"[OK] |[OK] ConvertFrom-Json
}

if[OK] (-not[OK] $Release)[OK] {
[OK] [OK] [OK] [OK] throw[OK] "Could[OK] not[OK] find[OK] GitHub[OK] release"
}

$ZipAsset[OK] =[OK] $Release.assets[OK] |[OK] Where-Object[OK] {[OK] $_.name[OK] -match[OK] "\.zip$"[OK] }[OK] |[OK] Select-Object[OK] -First[OK] 1
if[OK] (-not[OK] $ZipAsset)[OK] {
[OK] [OK] [OK] [OK] throw[OK] "No[OK] ZIP[OK] asset[OK] found[OK] in[OK] release:[OK] $($Release.name)"
}

$DownloadUrl[OK] =[OK] $ZipAsset.browser_download_url
$ZipFile[OK] =[OK] Join-Path[OK] $ReleaseDir[OK] "scope.zip"

Write-Host[OK] "Downloading[OK] $($ZipAsset.name)[OK] ($([math]::Round($ZipAsset.size[OK] /[OK] 1MB,[OK] 2))[OK] MB)..."
Invoke-WebRequest[OK] -Uri[OK] $DownloadUrl[OK] -OutFile[OK] $ZipFile
Write-Host[OK] "�?Downloaded:[OK] $($ZipAsset.name)"[OK] -ForegroundColor[OK] Green

#[OK] Extract[OK] and[OK] organize
Write-Host[OK] "Extracting[OK] and[OK] organizing[OK] DGScope[OK] files..."
$ExtractDir[OK] =[OK] Join-Path[OK] $ReleaseDir[OK] "scope-extract"
$null[OK] =[OK] mkdir[OK] $ExtractDir
Expand-Archive[OK] -Path[OK] $ZipFile[OK] -DestinationPath[OK] $ExtractDir

#[OK] Find[OK] the[OK] scope[OK] folder[OK] (it[OK] might[OK] be[OK] nested[OK] in[OK] a[OK] release[OK] folder)
$ScopeFolder[OK] =[OK] Get-ChildItem[OK] $ExtractDir[OK] -Directory[OK] -Recurse[OK] -Filter[OK] "scope"[OK] |[OK] Select-Object[OK] -First[OK] 1
if[OK] (-not[OK] $ScopeFolder)[OK] {
[OK] [OK] [OK] [OK] #[OK] Try[OK] to[OK] find[OK] scope.exe
[OK] [OK] [OK] [OK] $ScopeExe[OK] =[OK] Get-ChildItem[OK] $ExtractDir[OK] -File[OK] -Recurse[OK] -Filter[OK] "scope.exe"[OK] |[OK] Select-Object[OK] -First[OK] 1
[OK] [OK] [OK] [OK] if[OK] ($ScopeExe)[OK] {
[OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] $ScopeFolder[OK] =[OK] $ScopeExe.Directory
[OK] [OK] [OK] [OK] }[OK] else[OK] {
[OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] throw[OK] "Could[OK] not[OK] find[OK] scope.exe[OK] or[OK] scope[OK] folder[OK] in[OK] release"
[OK] [OK] [OK] [OK] }
}

#[OK] Copy[OK] entire[OK] scope[OK] folder[OK] to[OK] release
Copy-Item[OK] $ScopeFolder.FullName[OK] "$ReleaseRoot\scope"[OK] -Recurse
Write-Host[OK] "�?DGScope[OK] extracted[OK] and[OK] copied"[OK] -ForegroundColor[OK] Green

#[OK] Verify[OK] scope.exe[OK] exists
$ScopeExePath[OK] =[OK] "$ReleaseRoot\scope\scope.exe"
if[OK] (-not[OK] (Test-Path[OK] $ScopeExePath))[OK] {
[OK] [OK] [OK] [OK] throw[OK] "scope.exe[OK] not[OK] found[OK] at[OK] expected[OK] location:[OK] $ScopeExePath"
}
Write-Host[OK] "�?Verified[OK] scope.exe[OK] exists"[OK] -ForegroundColor[OK] Green

#[OK] Step[OK] 5:[OK] Create[OK] profiles[OK] folder[OK] structure
Write-Host[OK] "`nCreating[OK] profiles[OK] folder[OK] structure..."[OK] -ForegroundColor[OK] Cyan
$null[OK] =[OK] mkdir[OK] "$ReleaseRoot\profiles"
@("ZAN",[OK] "ZAK",[OK] "ZDV",[OK] "ZDC",[OK] "ZID",[OK] "ZIN",[OK] "ZJX",[OK] "ZKC",[OK] "ZLA",[OK] "ZLC",[OK] "ZMA",[OK] "ZME",[OK] "ZMP",[OK] "ZNY",[OK] "ZOA",[OK] "ZOB",[OK] "ZSE",[OK] "ZTL",[OK] "ZUA")[OK] |[OK] ForEach-Object[OK] {
[OK] [OK] [OK] [OK] $null[OK] =[OK] mkdir[OK] "$ReleaseRoot\profiles\$_"
}
Write-Host[OK] "�?Profiles[OK] folder[OK] structure[OK] created"[OK] -ForegroundColor[OK] Green

#[OK] Step[OK] 6:[OK] Create[OK] package[OK] files
Write-Host[OK] "`nCreating[OK] package[OK] files..."[OK] -ForegroundColor[OK] Cyan

#[OK] Create[OK] ZIP[OK] for[OK] distribution
$ZipOutput[OK] =[OK] "DGScope-Profile-Manager-v$ReleaseVersion.zip"
Compress-Archive[OK] -Path[OK] $ReleaseRoot[OK] -DestinationPath[OK] $ZipOutput[OK] -Force
Write-Host[OK] "�?Created[OK] ZIP:[OK] $ZipOutput"[OK] -ForegroundColor[OK] Green

#[OK] Create[OK] README
$ReadmeContent[OK] =[OK] @"
#[OK] DGScope[OK] Profile[OK] Manager[OK] v$ReleaseVersion

Complete[OK] bundle[OK] with[OK] DGScope[OK] and[OK] Profile[OK] Manager

##[OK] Contents

-[OK] **ProfileManager/**:[OK] DGScope[OK] Profile[OK] Manager[OK] application
-[OK] **scope/**:[OK] DGScope[OK] radar[OK] simulation[OK] (prebuilt,[OK] ready[OK] to[OK] run)
-[OK] **profiles/**:[OK] Empty[OK] ARTCC[OK] profile[OK] folders[OK] (auto-detected[OK] by[OK] Profile[OK] Manager)

##[OK] Quick[OK] Start

1.[OK] Extract[OK] this[OK] folder[OK] anywhere[OK] on[OK] your[OK] computer
2.[OK] Run[OK] `ProfileManager\DGScopeProfileManager.exe`
3.[OK] (Optional)[OK] In[OK] Settings,[OK] configure[OK] your[OK] CRC[OK] root[OK] folder[OK] path
4.[OK] The[OK] app[OK] will[OK] auto-detect[OK] the[OK] bundled[OK] DGScope
5.[OK] Generate[OK] profiles[OK] or[OK] select[OK] existing[OK] ones
6.[OK] Click[OK] "Launch[OK] DGScope"[OK] to[OK] open[OK] profiles

##[OK] Folder[OK] Structure

```
DGScope-Profile-Manager/
├──[OK] ProfileManager/[OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] #[OK] Profile[OK] Manager[OK] executable[OK] and[OK] dependencies
├──[OK] scope/[OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] #[OK] DGScope[OK] radar[OK] simulation
�?[OK] [OK] ├──[OK] scope.exe[OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] #[OK] Main[OK] application
�?[OK] [OK] ├──[OK] dependencies/[OK] [OK] [OK] [OK] [OK] [OK] [OK] #[OK] .NET[OK] and[OK] runtime[OK] files
�?[OK] [OK] └──[OK] ...[OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] #[OK] Other[OK] DGScope[OK] files
├──[OK] profiles/[OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] #[OK] DGScope[OK] profile[OK] storage
�?[OK] [OK] ├──[OK] ZAN/
�?[OK] [OK] ├──[OK] ZAK/
�?[OK] [OK] ├──[OK] ZDV/
�?[OK] [OK] ├──[OK] ...[OK] (all[OK] ARTCCs)
�?[OK] [OK] └──[OK] ZTL/
└──[OK] README.md[OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] [OK] #[OK] This[OK] file
```

##[OK] Auto-Detection

The[OK] Profile[OK] Manager[OK] automatically[OK] detects[OK] `scope/scope.exe`[OK] in[OK] the[OK] same[OK] directory.
No[OK] manual[OK] configuration[OK] needed!

##[OK] Manual[OK] Configuration

If[OK] you[OK] want[OK] to[OK] use[OK] a[OK] different[OK] DGScope[OK] installation:
1.[OK] Open[OK] Settings[OK] in[OK] Profile[OK] Manager[OK] (gear[OK] icon)
2.[OK] Browse[OK] for[OK] DGScope[OK] Executable
3.[OK] Select[OK] the[OK] desired[OK] scope.exe[OK] location
4.[OK] Click[OK] OK[OK] to[OK] save

##[OK] Requirements

-[OK] Windows[OK] 10/11
-[OK] .NET[OK] 10.0[OK] Runtime[OK] (included[OK] in[OK] scope[OK] folder)
-[OK] Optional:[OK] CRC[OK] (vERAM/vSTARS)[OK] for[OK] importing[OK] profiles

##[OK] Documentation

For[OK] detailed[OK] usage[OK] and[OK] features,[OK] see:
https://github.com/yanjz124/DGScope-profile-manager

##[OK] Support

Issues[OK] and[OK] feature[OK] requests:
https://github.com/yanjz124/DGScope-profile-manager/issues
"@

$ReadmeContent[OK] |[OK] Out-File[OK] "$ReleaseRoot\README.md"[OK] -Encoding[OK] UTF8
Write-Host[OK] "�?Created[OK] README.md"[OK] -ForegroundColor[OK] Green

#[OK] Cleanup[OK] temporary[OK] files
Write-Host[OK] "`nCleaning[OK] up[OK] temporary[OK] files..."[OK] -ForegroundColor[OK] Cyan
Remove-Item[OK] $ZipFile[OK] -Force
Remove-Item[OK] $ExtractDir[OK] -Recurse[OK] -Force
Write-Host[OK] "�?Cleanup[OK] complete"[OK] -ForegroundColor[OK] Green

#[OK] Summary
Write-Host[OK] "`n========================================"[OK] -ForegroundColor[OK] Green
Write-Host[OK] "Release[OK] Build[OK] Complete!"[OK] -ForegroundColor[OK] Green
Write-Host[OK] "========================================"[OK] -ForegroundColor[OK] Green
Write-Host[OK] "`nPackage:[OK] $ZipOutput"
Write-Host[OK] "Size:[OK] $('{0:N2}'[OK] -f[OK] ((Get-Item[OK] $ZipOutput).Length[OK] /[OK] 1MB))[OK] MB"
Write-Host[OK] "DGScope[OK] Version:[OK] $($Release.tag_name)"
Write-Host[OK] "Profile[OK] Manager[OK] Version:[OK] $ReleaseVersion"
Write-Host[OK] "`nReadiness:[OK] Ready[OK] for[OK] distribution!"[OK] -ForegroundColor[OK] Green
