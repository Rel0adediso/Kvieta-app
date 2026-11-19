# Release process

Every Kvieta version must follow this process:

1. Update the application version and public build metadata.
2. Run formatting verification, Debug and Release builds, and both smoke-test configurations.
3. Update `docs/RELEASE_NOTES.md` in detailed English.
4. Document highlights, behavior changes, security changes, migration notes, validation performed, and known issues.
5. Create an annotated Git tag with an English release summary.
6. Push the release commit and tag to GitHub.
7. Publish a GitHub Release with the same English notes when release tooling is available.
8. Attach the matching installer, verification manifest, and SHA-256 values. State clearly whether the package is Authenticode-signed.

Current target: **Kvieta v1.0.0**, not yet published. Public tag/label: `v1.0.0`.
Numeric MSI/FileVersion: **7.0.0**; keep this separate from the product label.
Previously distributed Alpha 4.x–6.x packages have higher numeric versions than
1.0.0. Never lower the installer version or enable arbitrary downgrades.

Build unsigned community artifacts with:

```powershell
./scripts/build-test-installer.ps1 -Version 7.0.0 -ReleaseLabel v1.0.0 -CommunityRelease
```

Output: `artifacts/installer-community/7.0.0`. Rebuild after every accepted source
change; never attach the previous 1.0.0 artifacts. Verify the exact candidate commit
and hash, then complete Windows installation, upgrade, recovery and uninstall acceptance tests. Community updates use
the official manual download page; signed automatic updates require the pinned publisher.
Editing source does not publish a release or validate physical device behavior.

Final releases must never be published while a documented release blocker remains open. Release candidates and test packages must be labeled clearly and must not be described as signed or production-ready when they are not.

Kvieta currently plans an unsigned, non-commercial community release. It must remain technically distinct from Debug/test packages: no development bypass may be compiled in, manifest and source commit must match, Guardian client identity checks must pass, and the expected Windows SmartScreen warning must be documented. Authenticode can be added later without changing the open-source license.
