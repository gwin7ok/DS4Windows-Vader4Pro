# Changelog

All notable changes to this project are recorded in this file.

## [3.11.3] - 2025-11-12
- (7dc4860a) CI: Build/package x64+x86 and attach zips to Release (use Directory.Build.props & CHANGELOG)
- (9a38278c) CI: Add Windows build+release workflow using Directory.Build.props; fix other workflow files formatting
- (88630d2f) Use Directory.Build.props as version source; update tasks and changelog; README points to CHANGELOG
- (d620058a) Change changelog and release management approach
- (ac20f682) Remove unnecessary methods and variables added during fix investigation
- (4c33ea94) Log an info message when an invalid special action is removed from a profile file
- (48430dfb) Iterate profile actions using an array snapshot; add explanatory comment to MapCustomAction
- (cf8ef1dd) Reworded log output messages for clarity
- (86854867) Consolidate special action name check into a common method
- (10e78add) Organize logging for invalid special action detection (emit once at the appropriate time)
- (260c7525) Compatibility check between *.xml files for 3.11.3 and 3.9.9 (result: OK)
- (813efce2) Parse LastChecked using multiple formats to avoid culture-dependent failures
- (c585e4e9) Modify ScpUtil.Save() to treat app_version and config_version as XmlSerializer attributes
- (105575a7) Revert to DTO-based load/save for settings files
- (7f27cfcc) Consolidate LoadOld into Load()
- (576426b8) Keep legacy load/save methods for compatibility with pre-3.9.9 profiles
- (777420ba) Fix saving of column and left/right area widths
- (142c24c8) Change Profile.xml to be compatible with 3.9.9
- (31a85068) Convert Japanese log outputs to English (internationalization)
- (6d6cdff5) Replace hard-coded Japanese strings with localization references
- (136a8e50) Fix display of HidHide settings client launch link
- (b5db9c18) Fix URLs and related references
- (7e78c34c) Update URL reference targets
- (f893622e) Fix changelog display formatting
- (83cbd5b1) Change update-check URI
- (470babcb) Increase log archive count to 1000 (NLog.config)
- (6f04ecf6) Add and organize log output
- (09af1441) Remove SortDescriptions counter check and clear logic
- (5018df78) Remove old leftover logic (cleanup)
- (204ab347) Fix column-name object assignment when closing the profile editor
- (6d739001) Remove more unnecessary processing
- (c9717a75) Remove redundant parts of special action column-name handling
- (5f0a39d7) Increase default window and profile-editor left-area widths
- (bbd837c2) Set checkbox column default width to 32 and make it restorable via init button
- (134544ee) Blank checkbox column header; show ▲/▼ on sort
- (9c9516bd) Place special action list checkboxes in a dedicated left-most column
- (4891588d) When total width exceeds area, move right edge to the left instead of right
- (a4e0ea3c) Avoid unnecessary column object retrieval
- (8d44d589) Run initial pre-display sort only once
- (bc6aedce) Remove unnecessary code (phase 2)
- (5ee0f9dc) Show ▲ on column headers at initial display
- (7624f4cd) Fix perceived sortable state when clicking columns (initial display only)
- (72a99958) Show ▲/▼ on current sort column
- (6ca74dfa) Remove tests
- (f77ccedd) Use the same method for initial display and column-click sorting
- (d83a87a9) Make entire column area clickable for sorting
- (7d790f37) Visually enable column-click sorting (implementation requires clicking header text)
- (232ebd9b) Delete test code
- (3fb26fbe) Make special action list sorting toggle between ascending/descending
- (a024a4ee) Add buttons to column headers to allow sorting
- (ceb2b330) Display "(invalid special action)" in the action column of the special action list
- (40011c12) Gray out invalid special action names in the list
- (fecb41b6) Add note that invalid names are grayed out and can be removed by unchecking and saving
- (56e259b2) Remove unnecessary special-action existence check code
- (0044537c) Small unnecessary code removals
- (a5bb8fb5) Fix bug where debug-level errors were continually logged when invalid special actions existed
- (994b8f28) Remove unused variable `e`
- (bc937107) Adjust profile editor left/right boundary margins and tweak default window width
- (513d4822) Make profile editor left/right boundary lines thicker

## [3.11.2] - 2025-11-06

CI
- (c8cd8a8d) Fix auto-release workflow (`auto-release.yml`).
- (32246d73) Fix auto-release workflow (`auto-release.yml`).
- (d25340f3) Fix auto-release workflow (`auto-release.yml`).
- (18838ffd) CI/Docs: remove version description from auto-release workflow.

Fixes
- (4f237854) Fix profile switching when a Special Action is registered in actions.xml at a position other than the end (could cause profile-switch failure after startup). Also: test fixes and ensure language files are saved to `lang/` at build time.

Docs / Changelog
- (87eb4562) Fix README (typos/formatting).

Chore
- (b0ba181f) Exclude `C:\Program Files\DS4Windows` (packaging/ignore adjustments).

> Note: CI/GitHub Actions will extract the section matching the version in `Directory.Build.props` to populate release notes.

## [3.11.1] - 2025-11-05

CI
- (8f272340) Fix PowerShell syntax in workflow (use proper PS conditional and error handling).
- (846dcc86) Workflow fixes.
- (09671a04) Misc workflow file fixes.
- (72d6d210) Replace deprecated create-release action with `gh` CLI for reliable release creation.
- (f6298837) Workflow fixes.
- (fe09d2ef) Revert to working v3.11.0 workflow structure while applying v3.11.1 content.
- (acc564d0) Fix workflow: create release after build (move create-release step to after successful build/publish).
- (98e07393) Update GitHub Actions to use modern `gh` CLI instead of deprecated actions.

Features / UI
- (0112ced9) Display active profile notifications using a custom top-right window (focus-independent).
- (ddb01d9f) Use custom window to show the active profile name in notifications.

Fixes
- (ef916d53) Ensure notifications display the actual active profile name.

Docs / Changelog
- (1bf98a7f) Update release description for v3.11.1 features.
- (b64bef1a) Update Changelog for v3.11.1 — custom notification system and profile accuracy improvements.

Chore / Misc
- (cd190051) Configure error-suppression settings (attempt to reduce noisy errors).
- (d4cfb1c8) WIP: attempted fixes for an error (investigation/attempted patch).
- (722f7799) Remove obsolete profile-notification code from `Log.cs`.

## [3.11.0]
- Special Action Button Suppression Fix: when a Special Action is triggered, the last pressed button's other assigned function is now prevented from executing alongside the Special Action to avoid duplicate inputs.

