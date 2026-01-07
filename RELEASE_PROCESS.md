# Release Process

This document describes how to build and release DGScope Profile Manager bundles.

## Automatic Release Builds (GitHub Actions)

The repository includes a GitHub Actions workflow that automatically builds complete release bundles containing:
- **ProfileManager/** - DGScope Profile Manager application
- **scope/** - Prebuilt DGScope from yanjz124/scope releases
- **profiles/** - Empty ARTCC profile directories
- **README.md** - Quick start guide

### Triggering a Release Build

#### Method 1: Manual Dispatch (Any Time)
1. Go to GitHub → Actions → "Build Release Bundle"
2. Click "Run workflow"
3. Enter:
   - **Release Version**: e.g., `1.0.0` (shown in filenames and installer)
   - **DGScope Release**: e.g., `latest` or a specific tag like `v1.2.3`
4. Click "Run workflow"

The workflow will:
- Build Profile Manager Release executable
- Download specified DGScope release from yanjz124/scope
- Create ZIP bundle: `DGScope-Profile-Manager-v1.0.0.zip`
- Create NSIS installer: `DGScope-Profile-Manager-v1.0.0-Setup.exe`
- Upload as workflow artifacts

#### Method 2: Git Tag (Recommended for Releases)
```bash
# Create and push a version tag
git tag v1.0.0
git push origin v1.0.0
```

When a tag is pushed:
- Workflow automatically triggers
- Extracts version from tag (v1.0.0 → 1.0.0)
- Uses `latest` DGScope release
- Creates and uploads Release assets on GitHub

### Workflow Output

**Artifacts** (available for 90 days):
- `DGScope-Profile-Manager-v1.0.0.zip` (~150-200 MB)
  - Contains complete bundle ready to extract and use
  - No installation required, just run ProfileManager.exe
  
- `DGScope-Profile-Manager-v1.0.0-Setup.exe` (~100-150 MB)
  - Windows installer
  - Installs to Program Files
  - Creates Start Menu and desktop shortcuts
  - Includes uninstaller

**Release Assets** (if using git tags):
- Same files automatically attached to GitHub Release
- Permanent storage on GitHub releases page
- Users can download from https://github.com/yanjz124/DGScope-profile-manager/releases

## Local Release Builds

For testing or manual releases:

```bash
cd DGScope-profile-manager

# Using PowerShell script
.\build-release.ps1 -ReleaseVersion "1.0.0" -ScopeRelease "latest"

# Using batch file (interactive)
.\build-release.bat
```

This creates a local `release/DGScope-Profile-Manager/` folder with the bundle structure and `DGScope-Profile-Manager-v1.0.0.zip`.

## Bundle Structure

Both ZIP and installer extract/install to this structure:

```
DGScope-Profile-Manager/
├── ProfileManager/
│   ├── DGScopeProfileManager.exe
│   ├── DGScopeProfileManager.dll
│   └── [dependencies]
├── scope/
│   ├── scope.exe
│   ├── [.NET Framework files]
│   └── [DGScope dependencies]
├── profiles/
│   ├── ZAN/
│   ├── ZAK/
│   ├── ZDV/
│   ├── ZDC/
│   ├── ZID/
│   ├── ZIN/
│   ├── ZJX/
│   ├── ZKC/
│   ├── ZLA/
│   ├── ZLC/
│   ├── ZMA/
│   ├── ZME/
│   ├── ZMP/
│   ├── ZNY/
│   ├── ZOA/
│   ├── ZOB/
│   ├── ZSE/
│   ├── ZTL/
│   └── ZUA/
└── README.md
```

## Auto-Detection

When ProfileManager starts, it automatically:
1. Checks if `DgScopeExePath` is configured
2. Looks for `scope/scope.exe` relative to the app location
3. If found, saves to settings (no re-detection on subsequent launches)
4. User can override in Settings if desired

## Version Numbering

Use semantic versioning: `MAJOR.MINOR.PATCH`

Examples:
- `1.0.0` - Initial release
- `1.1.0` - Feature additions
- `1.0.1` - Bug fixes only
- `1.1.0-beta` - Pre-release

## Pre-Release Checklist

Before building a release:

- [ ] All features tested and working
- [ ] No uncommitted changes in git
- [ ] Update CHANGELOG/version history if maintained
- [ ] Test the auto-detection by extracting bundle
- [ ] Verify DGScope release from yanjz124/scope is current

## Troubleshooting Builds

### DGScope Release Not Found
- Ensure yanjz124/scope has a release with a `.zip` asset
- Check that the specified tag or "latest" exists
- Verify the release contains a `scope/` folder or `scope.exe`

### NSIS Installation Issues
- GitHub Actions handles NSIS installation automatically
- For local builds, ensure NSIS 3.11+ is installed: https://nsis.sourceforge.io/
- Verify `C:\Program Files (x86)\NSIS\makensis.exe` exists

### Build Failures
- Check GitHub Actions logs for detailed error messages
- Ensure .NET 10.0 SDK is available (handled by Actions)
- Verify ProfileManager builds: `dotnet build src/DGScopeProfileManager`

## Distribution

### For Users

**Option 1: ZIP Download**
1. Download `DGScope-Profile-Manager-v1.0.0.zip` from releases
2. Extract anywhere
3. Run `ProfileManager/DGScopeProfileManager.exe`
4. Auto-detects bundled DGScope ✓

**Option 2: Installer**
1. Download `DGScope-Profile-Manager-v1.0.0-Setup.exe`
2. Run installer, accept defaults
3. Launch from Start Menu or desktop shortcut
4. Auto-detects DGScope in installation directory ✓

### For Developers

To include Profile Manager in your own distribution:
1. Download the ZIP bundle
2. Copy the three folders (ProfileManager/, scope/, profiles/)
3. Include with your own application
4. Users automatically see ProfileManager launch option

## FAQ

**Q: Do I need to build locally?**
A: No. The GitHub Actions workflow handles everything. Just push a git tag or use manual dispatch.

**Q: Can I use a custom DGScope build?**
A: Yes. Users can configure a different DGScope path in Settings. Or modify `build-release.ps1` to use your fork.

**Q: How often should I release?**
A: When features are complete and tested. Recommend tagging releases in git for permanent GitHub storage.

**Q: Can I release a beta version?**
A: Yes. Use tags like `v1.0.0-beta` or input `1.0.0-beta` when running workflow manually.

**Q: How do I update the DGScope version in releases?**
A: The workflow uses the latest DGScope release from yanjz124/scope. Update that repository, tag a new release there, then rebuild Profile Manager.
