# Kvieta release notes

## Kvieta Alpha 4.3.3 — Today redesign and application insights

- Today now uses a spacious editorial heading, the actual Kvieta K watermark,
  a unified application-usage surface, and a prominent next-action strip.
- The large usage total and distribution ring use the same application records;
  session allowance remains a separately labelled value. The top applications
  include icons, matching chart colors, durations, and a remainder legend.
- A 24-hour chart displays existing local hourly measurements on a fixed
  0–60-minute scale, with per-hour tooltips and accessible labels. A factual
  observation identifies the busiest measured hour; empty days show an explicit
  empty state instead of fabricated bars.
- The next action opens History in Insights mode, starts a 25-minute focus
  session in Personal mode, or opens the session controls when appropriate.
  Other focus durations remain available in a compact expandable section.
- The layout reflows for narrow windows and supports light and dark themes.
  Local WPF render previews can be generated with the smoke test executable's
  `--today-preview` argument; preview data is isolated from user settings.

- The compact session widget and Weekly Plan return to their earlier, quieter
  visual treatment while retaining the recent stability fixes.
- Applications now opens with three readable summaries: the most-used category,
  the most-used application, and the application with the largest measured
  increase since yesterday.
- Seven-day category totals filter a compact Android-inspired application list.
  Each row includes measured duration, category, trend, and direct timer/rule
  actions without oversized repeated cards.
- Regression checks cover summary calculation, rising-app selection, category
  filtering, and the new WPF surfaces.

Validation includes Debug/Release smoke tests for the hourly projection, empty
states, focus-aware action, category filtering and prior stability fixes, plus
wide/narrow WPF rendering in both themes. No new collection or network service
is introduced, and existing settings, rules and stored history remain compatible.

This unsigned local field-validation package retains version `4.3.3` at the
user's request. Earlier Alpha 4.3 versions are upgrade sources; an existing
4.3.3 installation is recognized as repair/same-version installation. Hyper-V
installation and lifecycle checks remain required. No public upload is included.

## Kvieta Alpha 4.3.2 — Today dashboard and stability fixes

- The existing-installation setup screen now keeps Cancel at a normal size in
  the bottom-right corner. It no longer stretches through the remaining page.
- Opening Create temporary allowance no longer crashes. The dialog now owns the
  label style it uses instead of depending on a MainWindow-only resource.
- Today adopts a digital-wellbeing dashboard hierarchy: measured time is the
  primary number, a real usage ring shows the top application distribution,
  the top three applications list their durations, and remaining time stays
  visible with the daily-limit progress.
- Regression checks create the temporary-allowance dialog, render the usage
  ring, and retain the responsive categorized-application coverage.

This unsigned local field-validation package is version `4.3.2` and upgrades
Alpha 4.3 and Alpha 4.3.1. Hyper-V visual and lifecycle checks remain required.

## Kvieta Alpha 4.3.1 — layout and contrast fixes

- Application categories now span the available width, with their application
  cards arranged in up to three responsive columns. Previously the category
  groups themselves received column slots and their cards stacked vertically.
- Today uses a prominent olive time summary with contrasting text, a larger
  timer and clearer separation from the status and next-plan area.
- The light theme has a brighter warm background, near-white cards and a softer
  green sidebar to reduce the grey cast observed during Hyper-V testing.
- A WPF regression check covers grouped application cards at wide and narrow
  widths. The smoke executable passes in Release configuration.

This is an unsigned local field-validation package, version `4.3.1`, intended
to update Alpha 4.3 (`4.3.0`). VM visual and installer checks remain required.

## Kvieta Alpha 4.3 — field-validation build

Alpha 4.3 brings the pre-v1 desktop interface into the same warmer, calmer
visual language as the Kvieta website. This package is an unsigned Windows x64
build intended for local and Hyper-V validation before public publication.

### Interface renewal

- Today now gives current status, the next plan and remaining/measured time a
  clearer hierarchy. Quick Focus is visually distinct without changing its
  authorization, timer or session rules.
- Weekly plans and application rules use responsive, independent cards. History
  emphasizes the weekly total, daily average and most-used application before
  the detailed rhythm data.
- Settings and guided setup share the same softer surfaces, spacing and corner
  system. The focus surface and compact session widget use the refreshed Kvieta
  identity while preserving all existing actions.
- Navigation rows are larger, the selected page has a Kvieta accent, and hard
  divider lines between the title bar, sidebar and content have been reduced.
  Empty application and history states are now deliberate product surfaces.

### Accessibility and motion

- Collapsed navigation retains accessible names for every page. The sidebar
  toggle name follows both its current state and the selected language.
- The hidden content-tab host no longer creates an invisible keyboard stop.
  Existing focus rings, modal focus cycling and Escape handling remain intact.
- Setup card motion and forced popup fades that did not follow Windows Reduce
  Motion were removed. Main application transitions continue to require both
  the local motion preference and the Windows animation preference.

### Installation, validation and limitations

Use `Kvieta-Setup-Alpha-4.3.exe` for guided VM testing. Numeric MSI version
`4.3.0` upgrades Alpha 4 (`1.0.0`), local Alpha 4.1 packages and Alpha 4.2
(`4.2.0`). The update path is intended to preserve settings, usage history,
recovery material and protected policy.

Automated validation covers Debug/Release builds, smoke tests, public-build
bypass checks, package metadata, single-file publish output and the release
manifest. The package is **not Authenticode-signed**, so Windows SmartScreen may
show an unknown-publisher warning. Hyper-V still needs to cover clean install,
4.2 upgrade, repair, uninstall, restart/sleep, Guardian recovery, DPI, themes,
keyboard navigation and multi-monitor behavior before Alpha 4.3 is published.

Attached locally: Setup EXE, standalone MSI, SHA-256 files and
`release-manifest.json`.

## Kvieta Alpha 4.2

Alpha 4.2 improves setup, onboarding and everyday navigation. It is an unsigned
Windows x64 community prerelease, not a final production release.

### Highlights

- Setup starts at 960×620 and can shrink to 800×500. Smaller work areas reduce
  the initial size further. Native resize borders, narrower spacing and vertical
  scrolling keep long steps accessible. The close control now fits its icon.
- Language selection uses TR/EN badges instead of flags. Recovery codes are
  presented as readable tiles while copy, file export and acknowledgement remain.
- The first Control Center launch opens a guided tour that highlights controls
  and navigates through pages appropriate to the selected mode. Back/Next, Esc,
  and the top-right skip button are supported; ? and F1 reopen it.
- Today emphasizes remaining time, measured usage and the next plan. Rule-count
  and blocked-app-count tiles are removed from this overview.
- Applications contains most-used apps and all of today's measured usage grouped
  by application name into browsers, communication, productivity, entertainment
  and other applications. Insights mode can view usage without rule editing.
- Settings is grouped into appearance/general, protection, privacy and maintenance.
  Rhythm details have a prominent expandable header. Cards reflow in narrow
  windows, and persistent 100–150% zoom includes the sidebar and header.

### Behavior and recovery fixes

- Setup and the application share a time picker with hour/minute selection and
  direct keyboard input. Invalid plan times remain subject to validation.
- Application selection accepts multiple EXEs and performs identity capture away
  from the UI thread; rule changes still require Save.
- Phone enrollment no longer shows the recovery-code replacement warning.
  That warning remains attached to actual code replacement. The post-install
  enrollment offer is recorded as shown even when cancelled, and waits for the
  guided tour to finish. Manual enrollment remains available in Settings.
- Flexible Personal mode avoids unnecessary relaxation delays and extra-time
  requests. Delays when relaxing an existing stronger policy remain intact.
- Device controls use an available theme brush; session thoughts rotate locally.

### Installation and migration

Use `Kvieta-Setup-Alpha-4.2.exe` for guided setup. Numeric MSI version `4.2.0`
supersedes published Alpha 4 (`1.0.0`) and local Alpha 4.1 builds (`4.1.x`). The
existing update path is intended to preserve settings, history, recovery material
and protected policy. Phone pairing itself does not invalidate recovery codes.
No account or hosted service is introduced by this release.

### Validation and known limitations

Local validation includes formatting, Debug/Release builds and smoke tests,
public-build bypass checks, package metadata and source-manifest checks. WPF
renders cover compact setup and main-window layouts, including 840×540 at 150%
zoom with synthetic usage. Regression checks cover guided navigation/skip,
responsive layout, typed times and durable first-run preferences.

The package is **not Authenticode-signed**; Windows SmartScreen may show an unknown
publisher warning. Verify the attached SHA-256 files before installation.
Application categories are inferred from names and may fall under Other.
Live phone enrollment and the complete Windows/VM upgrade, multi-monitor, DPI,
sleep and protection lifecycle matrix still require field validation. Offscreen
render checks do not replace that testing. See the repository's security and
release-readiness documentation for the remaining alpha limitations.

Attached: Setup EXE, standalone MSI, their SHA-256 files and `release-manifest.json`.


## Kvieta Alpha 4 — Previous community preview

Kvieta Alpha 4 completes the planned pre-V1 product software packages around
honest measurement, daily Rhythm goals, focus closure, policy explanation,
runtime health, notice priority, and local-data control. The numeric MSI version
remains `1.0.0` for in-place servicing; the public package label is `Alpha-4`
and the GitHub tag is `kvieta-alpha-4`.

### Notice priority and My data

- Adds a shared notice priority, event identity, merge key, and expiration model.
  Current time warnings replace stale ones, duplicate announcements are suppressed,
  expired notices do not flood the user after sleep, and low-priority celebration
  UI does not take focus from authorization dialogs.
- Adds **Settings > Privacy and data > My data**, showing local categories,
  retention and date range before export or deletion. JSON/CSV previews explicitly
  disclose application-name inclusion and excluded secrets.
- Separates deletion of detailed usage, reset of Rhythm Streak/Protectors, and
  deletion of all usage plus rhythm. Confirmation is required and none of these
  scopes removes plans, PIN state, clock safety, security identity, or Guardian policy.

### Safe preview, visible health, and coherent endings

- Adds a clearly labelled, synthetic time-expiry preview that never starts a
  session, schedules notifications, changes local data, or invokes protection.
  Example actions only explain behavior, and protected modes refuse the preview
  while Guardian health needs attention.
- Separates measurement, Guardian, and latest local-save health. Disabled,
  not-required, checking, stale, recovered, and error states no longer collapse
  into one reassuring indicator; retry, repair, and diagnostics keep their
  existing local authorization boundaries.
- Models focus completion/early ending separately from daily limit, plan end,
  and application limit. Simultaneous focus success and an access boundary now
  retain the success while suppressing an invalid continue action, with stable
  event identities preventing duplicate outcomes.

### Protection outcome and recovery review

- Adds a shared policy-derived outcome model for setup and in-app transitions
  into Family or Protected Personal use. It explains the actual time-expiry
  action, active plan days, immediate protection increase, Guardian requirement,
  and the standard-user/separate-administrator boundary.
- Keeps acknowledgement informational: it cannot replace PIN verification,
  recovery preparation, Guardian provisioning, or Windows administrator consent.
- Requires first-time Family transitions to prepare and explicitly acknowledge
  one-time recovery codes before the policy can be saved. Plain codes are shown
  only for delivery; settings retain verifier records rather than code content.
- Rejects a Family transition atomically when its PIN or recovery preparation is
  missing, while update/repair cancellation leaves an existing protected policy
  unchanged.

### Honest first-week measurement

- Separates measurement disabled, no observation yet, confirmed measured zero,
  collecting baseline, ready comparison, backup recovery, and unreadable local
  data instead of presenting every missing state as zero usage or improvement.
- Adds one mode-appropriate first action for incomplete weeks: enable local
  measurement, start a 25-minute focus, review today's plan, or review today's
  summary. Measurement remains optional and does not weaken plans or protection.
- Shows the exact current and previous seven-day periods with their valid-day
  counts, explains awareness, app-rule, and focus counters as separate metrics,
  and withholds reduction suggestions until both periods are comparable.
- Migrates usage data to schema 9 with an explicit awareness-observation marker;
  positive legacy records remain measured while unknown legacy zeroes stay neutral.
- Keeps setup templates editable and verifies that update/repair flows preserving
  existing settings do not reapply a selected template over user choices.

### Focus closure and explainable policy state

- Adds an optional, 80-character focus intention that stays only in the live
  session view model and is never persisted to usage history, diagnostics, or
  sharing. Quick Focus remains immediate and does not require an intention.
- Distinguishes completed and early-ended focus sessions, shows actual active
  time and daily-goal progress, and offers same-duration continuation only
  through the current schedule and limit checks.
- Introduces one shared explanation model for Today, session/block surfaces,
  and application-rule previews: what happened, which rule caused it, when it
  changes if known, and which action is available.
- Uses the real application enforcement predicate for read-only rule previews,
  identifies clock, schedule, limit, pending-policy, temporary-allowance, and
  Guardian states, and avoids inventing an end time when none is known.
- Serializes extra-time prompts on the session surface so repeated clicks cannot
  open parallel approval flows; all grants still use the existing local PIN path.

### Daily goals, suggestions, and the seven-day rhythm

- Adds one mode-appropriate daily goal. Flexible Personal mode can use a focus
  minute or completed-session target, Insights requires an explicit summary
  review action, and protected modes measure balance without granting authority.
- Snapshots goal type and amount per day, shows real progress, and applies a
  changed goal from the next day instead of rewriting today's result.
- Makes suggestions explain their seven-day basis and exact old-to-new change,
  then updates only that setting after confirmation. Reminder/hide failures keep
  the card visible, reminders return while the app is open, and hidden cards can
  be restored from Settings.
- Adds a bilingual, icon-and-text seven-day rhythm strip with selectable goal,
  progress, and outcome details. The explicit share action renders the same
  seven-day results, and milestone celebrations persist so they appear once.
- Starts Quick Focus without publishing unrelated unsaved Settings edits.
- Migrates settings to schema 10 and Rhythm preferences to schema 2.

### Rhythm correctness and persistence

- Keeps an unfinished current-day focus or summary goal pending until day
  close, so merely starting to use the PC cannot spend a Rhythm Protector early.
- Snapshots the daily Rhythm goal, balance limit, planned-rest state, approved
  allowance, and final outcome in the local usage record. Later mode or schedule
  changes no longer reinterpret finalized days.
- Migrates local usage data to schema 9. Legacy days without a trustworthy goal
  snapshot remain neutral instead of being rewarded or penalized using today's settings.
- Preserves current streak, best streak, Protector balance, and successful-day
  count in a compact checkpoint when detailed usage history is trimmed.
- Records temporary allowances as their approved minute amount instead of
  excusing the entire day. Focus and summary goals still require their own behavior.
- Adds regressions for pending current-day goals, schedule isolation, mode
  changes, legacy neutral days, approved allowances, and retention checkpoints.

### Focus timing

- Decouples Quick Focus progress from the daily usage counter and advances it
  only by active seconds actually accepted by the session engine.
- Splits active time at local midnight before rolling the daily ledger, keeping
  the focus countdown continuous while attributing usage to the correct day.
- Persists an active focus session ID, target, and elapsed active time so a
  recorded partial session can return after a process restart without counting
  the unknown time while Kvieta was closed.
- Makes focus completion repeat-safe across stale concurrent saves and clears
  persisted focus state when the user explicitly ends the session.
- Clarifies in both languages that deleting usage data also resets the current
  and best Rhythm Streak and Rhythm Protectors while leaving plans and protection unchanged.

### Alpha 4 correctness and security fixes

- Derives in-app protection-transition copy from the actual target mode, expiry
  action, Guardian requirement, and recovery state instead of claiming Windows
  lock or increased protection for every transition.
- Counts only completed focus sessions of at least five minutes toward a
  session-count Rhythm goal; shorter custom sessions still retain their elapsed
  focus time without inflating the streak target.
- Prevents a Rhythm-only reset from immediately recreating today's streak from
  activity recorded before the reset while preserving raw local usage.
- Keeps measurement health in the waiting state until a real local observation
  exists, even when another ledger write has a recent timestamp.
- Resolves historical rule application names for JSON and CSV exports and uses
  an explicit deleted-application fallback instead of exporting blank names.

### Alpha 4 validation and known limits

- Debug and Release builds and smoke-test configurations pass with zero warnings
  and zero errors. Documentation, public-build bypass, installer metadata,
  embedded MSI, checksum, and manifest gates are required for the tagged package.
- This remains an unsigned community prerelease. Windows SmartScreen may show an
  unknown-publisher warning; the package is not Authenticode-signed.
- Final V1 still requires the documented real-device Guardian, lifecycle,
  upgrade, accessibility, multi-monitor/DPI, and long-running usage matrix.

## Kvieta Alpha 3 — Previous community preview

Kvieta Alpha 3 brings the pre-V1 product experience into one coherent,
purpose-led flow. It keeps the numeric MSI version at `1.0.0` so existing Alpha
installations can be serviced in place. The public package label is `Alpha-3`
and the GitHub tag is `kvieta-alpha-3`.

### Highlights

- Added five editable setup templates for usage awareness, focus, gaming,
  winding down, and family routines; setup now previews the first Rhythm goal.
- Reworked Today around the user's next useful action: top applications, a
  comparison with yesterday, the current or next plan, custom Quick Focus, and
  repeat-last-focus.
- Added direct rule creation from measured application cards for daily limits,
  plan-only access, focus blocking, unrestricted use, and permanent blocking.
- Added calm 15, 5, and 1-minute wrap-up prompts with work-saved, controlled
  break, permitted extra-time, and direct tomorrow-planning actions.

### Rhythm Streak and weekly review

- Added a local Rhythm Streak with one mode-appropriate daily goal, rest days,
  best-streak tracking, milestones, and up to two earned Rhythm Protectors.
- Made Protector use visible and kept approved temporary allowances, extra time,
  Guardian failures, and recovery actions from unfairly breaking the streak.
- Expanded the weekly review with completed focus time and the most increased
  and decreased application trends.
- Added persistent apply, remind-tomorrow, and hide choices for local
  suggestions.
- Replaced plain-text sharing with an on-device 1200×630 image card that excludes
  application names and is never uploaded automatically.

### Behavior, security, and migration

- Extra time can be requested before the daily allowance reaches zero, while
  Family and protected flows retain their existing administrator checks.
- Session warnings now appear on the active session surface; warning actions
  cannot bypass permission checks and tomorrow planning opens the Plan page.
- Application identity and enforcement cover the new rule behaviors without
  storing full executable paths in usage history.
- Existing settings and protected policy remain compatible. Usage history is
  migrated locally to schema 7 to preserve focus completion and Rhythm fairness
  markers; separate focus and Rhythm preference files contain convenience state
  only.

### Validation and known limitations

- Formatting verification, Debug and Release builds, Debug and Release smoke
  tests, bilingual documentation checks, public-build bypass inspection, package
  metadata checks, release-manifest verification, and SHA-256 generation are
  required for this package.
- This is an intentionally unsigned community prerelease. Windows SmartScreen
  may display an unknown-publisher warning.
- Broad physical-device coverage for clean install and Alpha upgrade, Guardian
  lifecycle, multi-monitor/DPI, sleep/hibernate, repair/uninstall, and extended
  real-world use remains open before final `v1.0.0`.

## Kvieta Alpha 2.1 — Previous community preview

Kvieta Alpha 2.1 is a usability and reliability update built from the Alpha 2
field-feedback branch. It keeps the numeric MSI version at `1.0.0` so existing
Alpha installations can be serviced in place. The public package label is
`Alpha-2.1` and the GitHub tag is `kvieta-alpha-2.1`.

### Setup and lifecycle

- Added mode-aware weekly planning directly to setup. Tracking-only users no
  longer choose a daily limit, while managed modes can finish their initial plan
  before the first launch.
- Rounded and aligned setup inputs and refined the bilingual onboarding layout.
- Replaced the immediate in-app uninstall action with a confirmation window,
  optional local-data cleanup, progress feedback, and a clear completion result.

### Dashboard and interaction polish

- Increased the useful space for application usage cards and restored their
  visual hierarchy.
- Improved the seven-day chart's label contrast, spacing, progress visibility,
  and selected-day state.
- Refined administrator PIN, bonus-time, session, tray, and manager-device
  approval surfaces across light and dark themes.

### Reliability and validation

- Hardened trusted-device enrollment and transfer validation, Guardian client
  identity handling, and schedule evaluation edge cases.
- Updated the local companion experience and its generated distribution bundle.
- Expanded smoke coverage for scheduling, managed-device verification, and the
  new lifecycle behavior.
- The package remains an intentionally unsigned community prerelease; Windows
  SmartScreen may display an unknown-publisher warning.

## Kvieta Alpha 2 — Previous community preview

Kvieta Alpha 2 turns the Alpha 1 field fixes into a single recommended preview.
It preserves existing settings, usage history, recovery material, and protected
policy when installed over an earlier Alpha package.

The unsigned package was built from clean source commit `0444bd2`. Setup EXE
SHA-256: `44444d2c07add93b5b44e374b4de5a426e7b8759efced119c5252485edf0dd2b`.
The package label is `Alpha-2`; the future GitHub release uses
`kvieta-alpha-2` because the pre-rename Otium history already contains an
`alpha-2` tag.

### Scheduling and recovery

- Replaced the four separate schedule selectors with a compact time picker whose
  hour and minute columns scroll inside one popup.
- Refined the Recovery Center layout, version presentation, and repair paths.
- Recovery codes remain stable when reopened and can be copied or downloaded
  again; opening the window no longer silently invalidates the previous set.
- Clock protection can be cleared directly after Windows time is corrected and
  now recovers automatically when trusted wall time catches up.

### Protected session reliability

- Administrator and Control Center time no longer consumes the managed user's
  daily allowance; accounting resumes only after returning to the session surface.
- Kept administrator PIN and bonus-time dialogs above the protected session
  surface and retained the Guardian identity correction from Alpha 1 hotfixes.
- Improved Guardian health reporting and recovery behavior observed during
  physical-device installation and upgrade testing.

### Presentation and packaging

- Continued the crisp vector Kvieta identity across the application, setup,
  Windows executable, and documentation.
- Passed Release build, formatting, smoke, package metadata, embedded MSI,
  manifest, and SHA-256 verification gates.
- Remains an intentionally unsigned community prerelease, so Windows SmartScreen
  may display an unknown-publisher warning.

## Kvieta Alpha 1 Hotfix 2 — Visible administrator dialogs

This hotfix keeps administrator PIN and bonus-time dialogs visibly above the
protected full-screen session surface.

The unsigned package was built from clean source commit `56c2462` and published
as the `alpha-1-hotfix-2` GitHub prerelease. Setup EXE SHA-256:
`0c9a974072929e47369efdd951bdc42341a836814aa447f48d8299fbf70e5f72`.

- Session-owned WPF dialogs now inherit the topmost z-order required by the
  protected surface instead of relying on ownership alone.
- Administrator PIN dialogs explicitly activate and focus the PIN field after
  loading while preserving modal-dialog focus recovery protections.
- The change covers administrator exit and extra-time approval without weakening
  the full-screen session surface or exposing the desktop.

## Kvieta Alpha 1 Hotfix 1 — Guardian PIN authorization

This hotfix corrects a public-package identity check that could make every valid
administrator PIN appear incorrect in Protected mode.

The unsigned package was built from clean source commit `4ed3210` and published
as the `alpha-1-hotfix-1` GitHub prerelease. Setup EXE SHA-256:
`e13297d9713acdcae13ec67473749aebb03b864c9f27544a3d5cfb3ac0b8f13b`.

- Public builds use a human-readable product version such as
  `Alpha-1+<source-commit>`, while Windows Installer registers the numeric version
  `1.0.0` and the executable carries numeric file version `1.0.0.0`.
- Guardian previously attempted to parse the human-readable product version as a
  numeric `Version`. Parsing failed, so the otherwise valid, installer-managed,
  SHA-256-matched client was rejected before PIN verification.
- Guardian now reads the numeric file version and normalizes three- and four-part
  Windows versions before comparing them.
- Regression coverage now exercises real Kvieta assembly metadata and the
  `1.0.0` / `1.0.0.0` equivalence used by community-package authorization.

Existing Alpha 1 installations require this hotfix package; waiting, repairing
the original package, or resetting the PIN cannot correct the affected binary.

## Kvieta Alpha 1 — First Kvieta-branded community preview

Kvieta Alpha 1 is the first community preview under the Kvieta identity. The
unsigned package was built from clean source commit `9e68876` and published as
the `alpha-1` GitHub prerelease.

### Kvieta identity

- Renamed the solution, projects, assemblies, namespaces, services, installer,
  storage locations, protocol identifiers, documentation, and repository identity.
- Added the asymmetric Kvieta mark across the application, setup flow, tray,
  executable, MSI registration, and project documentation.
- Replaced scaled bitmap branding in WPF surfaces with vector templates and added
  a multi-resolution Windows icon for crisp rendering at common system sizes.

### Product baseline

- Carries forward the three usage modes, schedules, limits, breaks, focus sessions,
  app rules, local insights, trusted-phone recovery, and Guardian protection.
- Includes the setup, update, repair, policy handoff, administrator-exit, session,
  recovery, and power-action reliability work completed during the earlier preview cycle.
- Remains local-first and account-free; trusted-phone approval runs over the local
  network and never sends the administrator PIN or recovery codes to the phone.

### Release gate

- Local Debug/Release builds, smoke tests, documentation validation, public-build
  bypass checks, and package verification are required before publication.
- The community installer is intentionally unsigned. The published Setup EXE
  SHA-256 is `a29169986307434d819e1d7dbb11c30d768058548ce1ef1a166978b93fc87871`;
  the MSI SHA-256 is `29334604fec9f6f577bb830951446c7533bf6e7cb5493fa799ac075e18838fdb`.
- The pre-rename Otium Alpha 2 entry below is retained as historical development
  context from before the Kvieta-branded release line.

## Otium Alpha 2 — Pre-rename historical preview

Otium Alpha 2 was the second community prerelease before the Kvieta rename. Its
historical public name deliberately
does not use `v1.0.0-alpha.2`; `alpha-2` is used only where a URL, Git tag, or
filename-safe identifier is required. The internal MSI product version remains
`1.0.0` so Windows Installer can service existing Alpha installations.

### Trusted phone and PIN recovery

- Added optional trusted-phone enrollment during setup and from the Control Center.
- Added a mobile companion page with QR enrollment, short-lived challenges,
  comparison codes, signed approvals, replay prevention, expiry, and one active
  trusted device at a time.
- Added trusted-phone authorization for administrator PIN reset while keeping the
  new PIN and recovery codes on the Windows device.
- Added QR-based transfer to replace the trusted phone and explicit device revocation.
- Kept recovery codes as an offline fallback and clarified every PIN prompt and
  “forgot PIN” path.
- Normalized browser user-agent names into a friendly device label such as
  **Android phone** instead of exposing raw platform strings.

### Setup, update, repair, and removal

- Reworked first-run PIN handling, confirmation, visibility controls, optional
  phone enrollment, and protected-mode handoff.
- Added safer handling for existing Alpha installations, including update/repair,
  settings reconfiguration, stale registration recovery, and in-app removal.
- Hardened elevated MSI staging, Guardian policy transfer, rollback, lock cleanup,
  and error reporting so failed setup no longer leaves ambiguous state.
- Added package checks for Setup metadata, embedded MSI identity, Guardian service
  registration, public-build configuration, manifest hashes, and release labels.

### Guardian and administrator lifecycle

- Rebuilt administrator exit as a verified transition instead of closing the
  session surface first and letting Guardian immediately reopen it.
- Prevented duplicate Control Centers, duplicate session surfaces, and competing
  management transitions.
- Added Guardian start/repair recovery from the Control Center and clearer health
  reporting when installation repair is required.
- Fixed protected-policy lock ownership, credential synchronization, recovery-code
  consumption, and service handoff failures observed during physical-machine tests.
- Prevented automatic plan/session enforcement from taking over while a management
  window or setup flow is active.

### Session and control experience

- Restored application-rule management and stabilized the protected session after
  install, administrator exit, cancellation, Windows lock, and restart scenarios.
- Made trusted-phone enrollment optional without reporting a cancelled enrollment
  as successful.
- Improved PIN dialogs, recovery screens, compact-window layouts, error messages,
  progress states, icons, and the Kvieta light/dark visual language.
- Fixed session-surface power actions and confirmation-dialog focus handling.

### Local-first boundary

- The Alpha 2 companion flow is served by the Windows device and stores no Kvieta
  usage data in a cloud account.
- Enrollment and approval payloads are signed, short-lived, origin-checked, and
  rate-limited; PINs and recovery codes are never sent to the phone.
- Internet relay support remains a post-v1 roadmap item after the planned product
  rename. Alpha 2 phone enrollment therefore requires reachability to the Windows
  device on the local network.

### Release gate

- Hands-on setup, Guardian, trusted-phone, administrator-exit, session, repair,
  uninstall, and power-action regressions have been exercised on Windows devices.
- The final Alpha 2 community package was rebuilt from clean release commit
  `ca2181c` and passed the complete build, format, smoke, documentation,
  public-bypass, package metadata, manifest, and SHA-256 gates.
- Alpha 2 remains an intentionally unsigned prerelease; Windows SmartScreen may
  display an unknown-publisher warning.

## v1.0.0-alpha.1 — First community prerelease

`v1.0.0-alpha.1` is the first public test package for real-device feedback. It is an unsigned Windows community prerelease, not the final V1 release. It consolidates installer, recovery, diagnostics, usage-mode, personal-protection, Guardian, and security work completed after v0.16.1.

The final `v1.0.0` tag will only be created after the remaining Windows escape-path, public community package integrity, installer lifecycle, and real-device test requirements have been completed.

### Highlights

- Replaced the old technical mode names with three user-facing usage modes: **Tracking only**, **For myself**, and **For someone I manage**.
- Added Flexible, Balanced, and Guarded personal-protection levels.
- Added Guardian-backed personal protection without turning a self-selected PIN into an instant escape mechanism.
- Converted Flexible mode into a user-controlled manual focus session.
- Hardened the Control Center, session surface, tray, and single-instance window lifecycle.

### Flexible personal mode

- Removed the weekly schedule page and allowed-hour enforcement.
- Disabled forced daily-limit enforcement.
- Sessions now start only after an explicit user action.
- Added a stopwatch that starts from `00:00`, pauses during a break, resumes correctly, and resets when the session ends.
- Application rules apply only while a Flexible focus session is active.
- Guardian is never required, while local usage history continues to work.

### Balanced personal mode

- Retains weekly schedules, daily limits, application rules, and delayed relaxation of restrictions.
- Keeps Guardian disabled while preserving the personal change-delay policy.
- Reuses a single background session controller instead of creating new windows during navigation.
- Hides the Control Center from the taskbar before presenting the session surface.

### Guarded personal mode

- Added **Guarded · Guardian** as the strictest personal-protection level.
- Uses an internal cryptographic Guardian credential instead of a user-created administrator PIN.
- Lowering protection is queued using the configured personal change delay.
- Guardian applies an expired relaxation even when the Control Center is closed.
- Opening the Control Center does not silently disable enforcement.
- Windows administrator recovery remains the emergency uninstall and repair path.

### Managed and protected use

- Preserved administrator-PIN verification for **For someone I manage**.
- Fixed protected administrator exit and Control Center transitions.
- Strengthened Guardian enrollment, protected-policy synchronization, recovery, and version compatibility checks.
- Restricted test-only unlock behavior to Development builds.

### Security hardening completed for Alpha.1

- Removed the offline administrator-PIN verifier from user-readable protected policy copies. Public policy files now contain a non-verifying marker while the real verifier remains restricted to Guardian and administrator storage.
- Routed protected-mode PIN checks through authenticated Guardian IPC with persistent throttling, replay protection, and migration of older policy files.
- Serialized PIN-dialog verification so parallel clicks or Enter presses cannot submit multiple attempts concurrently.
- Removed the installer elevation time-of-check/time-of-use gap. The elevated setup process now extracts and verifies its embedded MSI inside an ACL-restricted ProgramData staging directory before invoking Windows Installer.
- Added process-start observation and verified parent ancestry for launcher and child-process application rules.
- Preserved protected credential redaction when settings or pending policy targets are copied back to the user profile.

### Alpha.1 behavior and reliability fixes

- Removed the **Sign out** limit action after a real-use test exposed a repeated Windows sign-out loop. Existing settings using it migrate to the safer **Windows lock** action.
- Changed usage-mode selection so Protected mode and Guardian policy changes are staged in the interface and applied only after **Save** succeeds.
- Prevented duplicate PIN submissions while Guardian verification is in progress.
- Replaced frequent system-theme registry polling with Windows preference-change notifications.
- Consolidated Guardian IPC request handling and reduced repeated application-rule matching work.
- Improved Recovery Center sizing and scrolling on constrained displays.
- Confirmed the basic protected surface and secondary-display shields on a physical two-monitor setup.

### Community package

- Added an unsigned `community` package kind that uses the Release configuration and excludes every development/test bypass.
- Kept community artifacts technically separate from Debug test installers.
- Produces a self-contained Setup EXE, standalone MSI, SHA-256 files, and a schema-v2 release manifest tied to the exact source commit.
- Verifies setup/MSI metadata, embedded package integrity, release configuration, artifact sizes, hashes, and the absence of public-build test unlock markers.
- Windows SmartScreen may show an unknown-publisher warning because Alpha.1 is intentionally unsigned.

### Window and startup reliability

- Fixed a Tracking-only startup crash caused by an invisible maximized WPF window.
- Prevented rapid Session Screen clicks from creating duplicate session surfaces.
- Prevented repeated Control Center requests from creating duplicate management windows.
- Added transition guards around session and Control Center navigation.
- Replaced the stale `Awareness` label with `Tracking only`.
- Normalized irrelevant personal-protection fields outside personal mode.

### Recovery, diagnostics, and privacy

- Added Guardian health and version compatibility reporting.
- Added privacy-safe diagnostic export without PINs, recovery secrets, window titles, or document content.
- Added last-known-good settings recovery and installer repair flows.
- Added delayed policy-change details and recovery audit events.
- Kept all usage data local to the device.
- Expanded the Recovery Center into a System Health view with separate application, installer, Guardian, and local-data status.
- Added one-click privacy-safe diagnostic export from the health surface.

### Windows lifecycle

- Added a serialized lock, unlock, suspend, and resume state policy.
- Atomically pauses active timing before suspend and resumes only when the system was not locked.
- Preserves the user-controlled Break state after Win+L and rebuilds the protected display topology after resume.
- Records bounded, content-free lifecycle audit events for diagnostics.

### Open-source project foundation

- Added the MIT License, a support policy, and matching English/Turkish usage guides.
- Corrected stale RC, two-mode, and planned Application Identity claims in both READMEs.
- Documented the deliberate unsigned community-release direction without treating Development bypasses as release security.
- Added a CI documentation gate for bilingual status, modes, install/update, uninstall, support, and license coverage.

### Removed or deferred

- Removed unstable one-click application suggestions from this release.
- Moved reliable live suggestion refresh to the post-v1 roadmap.
- Kept AppLocker and WDAC integration as a future optional protection level because availability depends on Windows edition and policy.

### Known Alpha.1 limitations and final V1 blockers

- Balanced session-surface recovery has been hardened and passed the initial single-monitor manual test; Explorer restart, virtual desktops, and the wider repeatable Windows matrix remain open.
- The unsigned community packaging path is ready, but the published Alpha.1 artifact must be built from and matched to its release commit.
- Clean install, Protected Guardian enrollment, expiry behavior, repair, and uninstall are the first post-publication Alpha.1 field tests on a separate Windows device. Upgrade and rollback remain part of the broader final V1 matrix.
- Reboot, Win+L, sleep, hibernation, Explorer restart, user switching, Remote Desktop, multiple monitors, and standard-user scenarios remain in the final Windows matrix.

### Validation completed

- Debug and Release builds complete with zero warnings and zero errors.
- Core smoke tests pass in both configurations.
- Settings migration, delayed relaxation, Guardian credentials, protected-policy synchronization, and manual Flexible sessions have automated regression coverage.
- A real-process startup diagnostic confirmed that the Tracking-only startup crash no longer occurs.
- Protected PIN redaction, legacy Sign-out migration, and save-gated Protected mode transitions have automated regression coverage.
- The unsigned Release community package passed metadata, embedded MSI, manifest, SHA-256, and public-build bypass verification.

## v0.19.0 — Diagnostics and Guardian reliability

- Added Guardian health, service-state, protected-policy, and version compatibility checks.
- Added privacy-safe diagnostics export and security audit coverage.
- Restricted development unlock behavior to Development packages.
- Improved Guardian installation recovery, protected-policy restoration, and crash recovery.

## v0.18.0 — Recovery and security hardening

- Added Recovery Center tools for clock validation, settings restore, and installation repair.
- Added recovery-code-based administrator PIN reset and safer confirmations.
- Extended app identity with publisher trust, original filename, product metadata, SHA-256, package family, and process relationships.
- Improved recovery layout, explanations, temporary allowances, empty states, rule removal, and dashboard readability.

## v0.17.0 — Secure installer lifecycle

- Added Windows Installer support for install, upgrade, repair, and uninstall.
- Added Program Files installation, Start menu integration, and Guardian service lifecycle support.
- Added downgrade prevention and rollback validation.
- Added release-manifest verification for package name, version, architecture, size, SHA-256, and Authenticode signer identity.
- Added automated clean-install, repair, removal, upgrade, and rollback verification.
