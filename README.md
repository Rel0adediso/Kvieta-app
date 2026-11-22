<div align="center">

<img src="assets/branding/kvieta-mark.svg" alt="Kvieta logo" width="132" />

# Kvieta

### All in good time.

A calm, local-first way to understand and manage screen time on Windows.

[**Türkçe**](docs/README.tr.md)

![Windows](https://img.shields.io/badge/Windows-native-87946B?style=flat-square&labelColor=292B26)
![.NET](https://img.shields.io/badge/.NET-10-87946B?style=flat-square&labelColor=292B26)
![WPF](https://img.shields.io/badge/UI-WPF-C9B98E?style=flat-square&labelColor=292B26)
![Privacy](https://img.shields.io/badge/privacy-local--first-87946B?style=flat-square&labelColor=292B26)
![Status](https://img.shields.io/badge/status-v1.0.0-C9B98E?style=flat-square&labelColor=292B26)
![License](https://img.shields.io/badge/license-MIT-87946B?style=flat-square&labelColor=292B26)

</div>

Kvieta helps make computer time visible and intentional without turning it into punishment. Schedules, rules, usage history, credentials, and recovery data stay on the Windows device. No Kvieta account is required.

## Kvieta v1.0.0 — preparing for release

The links below are the planned release locations and become available only after publication.

[**Download Kvieta Setup for Windows x64**](https://github.com/Rel0adediso/kvieta-app/releases/download/v1.0.0/Kvieta-Setup-v1.0.0.exe)

The self-contained setup supports English and Turkish and does not require the
.NET SDK. This release is intentionally unsigned, so Windows
SmartScreen may show an **Unknown publisher** warning.

The standalone MSI, checksum files, release manifest, detailed notes, and known
limitations are on the [Kvieta v1.0.0 release page](https://github.com/Rel0adediso/kvieta-app/releases/tag/v1.0.0). Verify the Setup EXE with the attached `.sha256` file before running it.

> **Upgrade plan:** v1.0.0 uses numeric installer/file version **7.0.0** to remain above Alpha 6.x. The visible product version remains v1.0.0. Validate an in-place upgrade from your deployed Alpha before publishing; preserve settings, history, recovery material and protected policy. Older Alpha updaters may require a manual download.

## Choose the relationship you want with time

| Mode | Designed for | Experience |
|---|---|---|
| **Insights** | Understanding your habits | Records configured app usage locally without restricting anything. |
| **Personal** | Building a routine of your own | Adds schedules, limits, breaks, focus sessions, and optional personal protection. |
| **Family** | A family member's standard Windows account | Protects policy with an administrator PIN and the Kvieta Guardian service. |

## What Kvieta can do

| | |
|---|---|
| **Plan time** | Weekly schedules, daily limits, controlled breaks, temporary allowances, and administrator-approved extra time. |
| **Manage apps** | Blocked, limited, or unlimited rules with daily counters and resilient process identification. |
| **Show the rhythm** | Seven-day insights, a 90-day local history, weekly totals, daily averages, and activity events. |
| **Stay recoverable** | Offline recovery codes plus optional trusted-phone enrollment, QR transfer, revocation, and local PIN-reset approval. |
| **Protect policy** | Guardian-backed supervision, protected policy storage, health checks, repair paths, and verified administrator exit. |
| **Survive real life** | Atomic saves, last-known-good backups, corruption recovery, clock-rollback detection, and concurrent-write protection. |

## What's new in Kvieta v1.0.0

- Zen UI overhaul with Mica glass backdrop, calming sage aesthetic, and intuitive Today donut dashboard.
- Mindful Dopamine Barrier: browser interception for short-form video loops (Shorts/Reels/TikTok) with peaceful home redirect.
- Compressed single-file distribution; the Setup EXE and embedded application have different sizes. Use the final manifest for exact package sizes.
- Recovery and policy ownership: restores Kvieta-owned settings on normal shutdown/startup and uninstall, preserving unrelated policies. Crash and upgrade paths require the final Windows test matrix.
- Windows process access controls and Guardian supervision for standard user accounts. This is not a kernel driver or an administrator-proof boundary.

## Private by design

Kvieta has no required cloud account. Usage history stays on the PC. Optional phone sharing sends an end-to-end encrypted summary through Cloudflare when remote access is enabled; local sharing is also available. Pairing grants the permissions shown in the phone hub. Administrator PIN verifiers and recovery codes are not included in summaries.

Community releases are Authenticode-unsigned. With a configured stable channel, the initial Setup registers the Windows tasks that download and verify updates in the background. An update that is already ready installs when you next open Kvieta. Administrator approval is required during the initial Setup. Older builds can adopt this flow by running the newer Setup once.

## Project status

**Kvieta v1.0.0 is being prepared and has not been published by this change.**

- The source is usable today and the Windows package pipeline is in place.
- Debug and Release builds, smoke tests, documentation checks, and public-build bypass checks run as quality gates.
- Community installers are intentionally unsigned, so Windows SmartScreen may show an **Unknown publisher** warning.
- Rebuild the final Setup EXE, MSI, checksums and manifest from the accepted source commit; earlier artifacts do not contain the readiness fixes.
- The broader installer, DPI, Guardian, escape-path, and Windows lifecycle matrix remains open before final `v1.0.0`.

See the [roadmap](docs/ROADMAP.md) for the remaining validation work and [release notes](docs/RELEASE_NOTES.md) for the detailed history.

## Run from source

Requirements: Windows and the .NET 10 SDK.

```powershell
dotnet run --project src/Kvieta.App/Kvieta.App.csproj
```

Open the session surface directly:

```powershell
dotnet run --project src/Kvieta.App/Kvieta.App.csproj -- --session
```

Run the main quality checks:

```powershell
dotnet build Kvieta.slnx -c Release
dotnet run --project tests/Kvieta.Core.SmokeTests/Kvieta.Core.SmokeTests.csproj -c Release
```

<details>
<summary><strong>Project structure</strong></summary>

| Component | Responsibility |
|---|---|
| `Kvieta.Core` | Scheduling, sessions, policies, models, and resilient local persistence |
| `Kvieta.App` | WPF Control Center, session surface, tray, and Windows integrations |
| Guardian service | Windows service supervising protected sessions and authoritative policy |
| `Kvieta.SetupApp` | Bilingual setup, update, repair, configuration, and removal experience |
| `Kvieta.Core.SmokeTests` | Core behavior, security regressions, and real-process checks |

</details>

## Security boundary

Protected mode is designed primarily for a **standard Windows account** managed through a separate administrator account. No desktop application can guarantee absolute resistance against someone with physical access and Windows administrator privileges. Development-only test gates are not compiled into Public builds. Read the [security model and limitations](docs/SECURITY.md) before relying on protected mode.

## Documentation

- [English user guide](docs/USAGE.md) · [Türkçe kullanım rehberi](docs/KULLANIM.tr.md)
- [Roadmap](docs/ROADMAP.md) · [Release notes](docs/RELEASE_NOTES.md)
- [Support](.github/SUPPORT.md) · [Contributing](.github/CONTRIBUTING.md)

## Development approach

**Human-directed product · AI-assisted development.** Product direction, UX decisions, and hands-on testing are led by [Rel0adediso](https://github.com/Rel0adediso). Architecture, implementation, and test development are carried out with AI assistance.

Kvieta is open-source software released under the [MIT License](LICENSE).

---

<div align="center">

**Kvieta** · *All in good time.*

</div>
