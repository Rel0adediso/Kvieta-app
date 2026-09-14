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

For the first Kvieta-branded community preview, the public release name is
**Kvieta Alpha 1**. Use `alpha-1` for the Git tag and `Alpha-1` where a
package-safe label is needed; do not present `v1.0.0-alpha.1` as the product name.
The numeric MSI version stays `1.0.0` so Windows Installer can service the
existing product correctly.

The current branded preview is **Kvieta Alpha 4.2**. Its package-safe label is
`Alpha-4.2`; publish it under the `kvieta-alpha-4.2` tag. Use numeric MSI version
`4.2.0`, above both the published Alpha 4 package (`1.0.0`) and the intervening
local Alpha 4.1 test packages (`4.1.0`–`4.1.3`). Do not downgrade this version.
Build community artifacts from the clean release commit and verify that their
manifest names the annotated tag's commit before uploading.

The next field-validation build is **Kvieta Alpha 4.3**. Its package-safe label
is `Alpha-4.3` and its numeric MSI version is `4.3.0`, so Alpha 4.2 is recognized
as an upgrade source. Until its release commit and tag exist, Alpha 4.3 artifacts
must be treated as unsigned local/VM validation builds and must not replace the
published Alpha 4.2 download links.

Final releases must never be published while a documented release blocker remains open. Release candidates and test packages must be labeled clearly and must not be described as signed or production-ready when they are not.

Kvieta currently plans an unsigned, non-commercial community release. It must remain technically distinct from Debug/test packages: no development bypass may be compiled in, manifest and source commit must match, Guardian client identity checks must pass, and the expected Windows SmartScreen warning must be documented. Authenticode can be added later without changing the open-source license.
