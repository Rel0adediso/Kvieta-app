# Kvieta v1.0.0 acceptance matrix

Public label: v1.0.0. MSI/FileVersion: 7.0.0. Status: **unpublished**.
Use the final rebuilt candidate, record its commit and hashes, and retain logs.
Unchecked items are not validated by compilation or unit checks.

Scope decision (2026-11-17): PC v1 first; Android implementation and physical
phone acceptance are deferred to [POST-V1.md](POST-V1.md). This does not relax
the PC's authorization checks for unsupported phone commands.

## Automated

- [x] Final Debug/Release builds, zero warnings/errors.
- [x] `dotnet run --project tests/Kvieta.Core.SmokeTests -c Debug -- --v1-checks`
- [x] Same V1 regression command in Release.
- [x] Full Debug/Release smoke suites, including real network endpoints.
- [x] Documentation, format and public-build checks.
- [x] Fresh self-contained win-x64 application publish and output metadata/size verification.
- [ ] Fresh installer metadata, embedded payload, manifest/commit and SHA-256 validation.
- [ ] Web/relay build and tests if those components change.

## Windows lifecycle — standard user with a separate administrator

- [ ] Clean install in Insights, Personal Flexible/Balanced/Protected, and Family.
- [ ] Upgrade distributed Alpha 4.x and Alpha 6.5; preserve PIN, recovery, usage and policy.
- [ ] Upgrade/repair when Guardian is running; failed install and rollback.
- [ ] Family phone lock survives process restart, reboot and midnight; explicit unlock works.
- [ ] Sleep/resume, Windows lock/unlock, user switching and Safe Mode recovery.
- [ ] Clock rollback/forward and normal day rollover without extra time or unwanted lockout.
- [ ] Offline recovery code, revoked phone, lost phone and PIN reset.
- [ ] Protected uninstall with a different administrator; report any cleanup failure.

## Web Guard and UI

- [ ] Pre-existing browser blocklists, forced extensions and SafeSearch policies survive enable/disable/uninstall.
- [ ] No third-party extension is silently installed; inspect legacy policies separately.
- [ ] Chrome/Edge/Brave/Firefox: real URL vs query text, child feature toggles, master off, and browser restart.
- [ ] Navigation failure leaves clear feedback; clipboard text/image/files unchanged; never types into another app.
- [ ] Both languages, keyboard/screen reader, 100/150/200% DPI and multiple monitors.
- [ ] Personal relaxations wait, tightening applies immediately; cancellation/restart preserves pending state.

## Phone and network

- [ ] PIN-authorize current phone relay in the phone hub after upgrade; revoke and replace.
- [ ] Standard user can apply authorized schedule/app/web changes via Guardian; raw/unpaired policy requests fail.
- [ ] Command failure/retry, process crash, duplicate/replayed/expired decision and room rotation.
- [ ] No PIN hash/recovery material in a snapshot; public salt/iteration challenge only; verify Android compatibility with this payload.
- [ ] Partial/slow LAN request times out without stopping pairing/recovery or blocking other clients.
- [ ] PC offline/online, Wi-Fi/mobile data transition, revoked credentials and stale summary indication.

## Publication

- [ ] GitHub CI green for the exact release commit.
- [ ] Final installer rebuilt; no previous artifacts attached.
- [ ] Release notes match actual limitations, signing mode, package sizes and permissions.
- [ ] Website/download links work on mobile and desktop after publication.

## Current environment limits

The fresh application publish succeeded (82,137,658 bytes). MSI ICE validation
cannot access Windows Installer Service in this session (WIX0217); validation
was not suppressed. Hyper-V inventory is also denied by the session's CIM
permissions, so no VM acceptance items were marked complete.

Source staging/commit/push is requested but blocked here: .git/index.lock cannot
be created (Permission denied), and the GitHub remote fails DNS resolution.
