# DGScope Profile Manager Installer

This directory contains the NSIS bundle installer for DGScope Profile Manager.

The release installer is normally built by CI (`.github/workflows/release-bundle.yml`)
when a `v*` tag is pushed. This directory holds the script and assets it uses.

## Files

- **DGScopeProfileManager-Bundle.nsi** — NSIS script for the bundle installer. It installs:
  - the Profile Manager (self-contained .NET publish — no separate .NET runtime needed),
  - DGScope (`scope/`), downloaded from the [yanjz124/scope](https://github.com/yanjz124/scope) release,
  - bundled fonts and an empty `profiles/` folder,
  - Start Menu and Desktop shortcuts, and an uninstaller.
- **fonts/** — fonts packaged into the installer.

## How the release installer is built

On a `v*` tag push, the CI workflow:

1. Publishes the Profile Manager self-contained (`dotnet publish -r win-x64 --self-contained`).
2. Downloads the DGScope portable release and extracts it to `scope/`.
3. Substitutes the version into `DGScopeProfileManager-Bundle.nsi` and runs `makensis`.
4. Uploads `DGScope-Profile-Manager-v<version>-bundle-Setup.exe` to the GitHub release.

See `RELEASE_PROCESS.md` in the repo root for the full flow.

## Building the installer locally

Requires **NSIS 3.x** (`winget install NSIS.NSIS`). The script expects the published
`ProfileManager/`, `scope/`, `profiles/`, and `fonts/` folders next to it (the CI workflow
stages these); `RELEASE_VERSION` in the script is replaced with the target version before compiling:

```powershell
& "C:\Program Files (x86)\NSIS\makensis.exe" installer\DGScopeProfileManager-Bundle.nsi
```

## License

- **DGScope Profile Manager** — license as per source
- **DGScope** — GPLv3 (from the upstream project)
- **Installer (NSIS)** — public domain
