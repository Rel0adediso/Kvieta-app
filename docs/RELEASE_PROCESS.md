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

`main` is the development branch; `stable` is the approved release branch.
Both run the same quality gates. CI reads the package and release versions
from `Directory.Build.props`. For each approved update, increment both the
release label and the numeric MSI version (for example `v1.0.1` and `7.0.1`),
merge the tested change into `stable`, and complete the acceptance checks
before publishing a matching release tag and verified package. Updates
offered in the app must have a published release tag whose commit is in the
history of `stable`. Unpublished branch changes are not installable updates.
Never publish an unsigned package as a silent executable update.

Build unsigned community artifacts with:

```powershell
./scripts/build-test-installer.ps1 -Version 7.0.0 -ReleaseLabel v1.0.0 -CommunityRelease
```

Output: `artifacts/installer-community/7.0.0`. Rebuild after every accepted source
change; never attach the previous 1.0.0 artifacts. Verify the exact candidate commit
and hash, then complete Windows installation, upgrade, recovery and uninstall acceptance tests. The stable feed signs
the exact community Setup executable and records its block hashes. Kvieta can reuse matching blocks from an earlier cached
Setup and verifies the complete file. Initial Setup registers the machine's update tasks after administrator approval.
These tasks apply a verified, ready update at the next normal Kvieta launch. Setup packages for older or unmanaged
installations remain available through the normal download and install flow.

Increment `KvietaGuardianRevision` in `Directory.Build.props` whenever a release changes Guardian behavior or its protection
protocol. Keep that revision in subsequent desktop releases. The signed feed carries the revision, so clients that
have skipped a Guardian update still open the official Setup release page and install it manually. Desktop-only
changes keep the current revision and can install silently at startup. The MSI still deploys the desktop and Guardian
from the same executable and restarts a running Guardian during the upgrade.
Editing source does not publish a release or validate physical device behavior.

Final releases must never be published while a documented release blocker remains open. Release candidates and test packages must be labeled clearly and must not be described as signed or production-ready when they are not.

Kvieta currently plans an unsigned, non-commercial community release. It must remain technically distinct from Debug/test packages: no development bypass may be compiled in, manifest and source commit must match, Guardian client identity checks must pass, and the expected Windows SmartScreen warning must be documented. Authenticode can be added later without changing the open-source license.
