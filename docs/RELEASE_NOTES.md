# Kvieta release notes

## Kvieta v1.0.0 (release preparation)

- Redesigned Windows dashboard with English and Turkish support, app usage insights, and a short-video pause surface.
- Improved Web Guard policy ownership and cleanup, including preservation of unrelated browser settings.
- Family remote locks persist across restarts and day rollover. Personal web restrictions follow the configured change delay.
- More reliable phone commands, pairing, recovery, and handling of network timeouts. The Android companion requires an update to support the new [Family policy authorization protocol](mobile-parent-policy-v1.md).
- Updated the release checker and installer for community updates. The visible version is v1.0.0. Windows MSI and file version 7.0.1 supports upgrades from Alpha 6.x and earlier 7.0.0 preview packages.
- Improved bilingual feedback when protection or cleanup cannot complete.

### Validation and known limitations

- Automated Debug/Release builds, smoke tests, public-package checks, and installer metadata/manifest checks have passed. Physical Windows installation, upgrade, recovery and uninstall tests remain pending.
- Community packages are Authenticode-unsigned. With a configured stable feed and startup update tasks registered by Setup, Kvieta verifies and prepares the next release in the background. Ready updates install on the next normal launch.
- Previously altered browser policies may require manual restoration if their original values were never backed up. The uninstaller preserves policies it cannot safely identify as its own.
- The Family protection boundary is documented in the [security model](SECURITY.md).

## Earlier releases

- **Alpha 6.5** (16 November 2026): updater, Windows protection, recovery and stability improvements.
- **Alpha 6.4** (5 October 2026): optional phone companion and remote supervision.
- **Alpha 5** (15 September 2026): interface polish, startup reliability and policy management.
- **Alpha 4.x and earlier**: initial community previews, setup, scheduling, local usage tracking and recovery.

For complete historical release descriptions and packages, see the [GitHub releases](https://github.com/Rel0adediso/Kvieta-app/releases) and the [release-notes history](https://github.com/Rel0adediso/Kvieta-app/commits/main/docs/RELEASE_NOTES.md).
