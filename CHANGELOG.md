# Changelog

All notable changes to this project are recorded in this file.

## [3.11.3] - 2025-11-12
- Added: Post-save info logging when an invalid special action is removed from a profile. (ProfileEditor)
- Improved: Special action list UI — gray-out for missing actions and ability to remove by unchecking/save.
- Fix: ScpUtil and Mapping robustness fixes, DTO/serialization improvements for profile compatibility.
- UI: ProfileEditor column/width persistence and sorting improvements.

## [3.11.2] - (previous)
- See repository tag `v3.11.2` for previous release details.

> Note: CI/GitHub Actions will extract the section matching the version in `Directory.Build.props` to populate release notes.

## [3.11.2]
- Fixed Profile Switching Failure: Resolved an issue where Special Actions for profile switching would fail to work if the action was not positioned at the end of the actions.xml file after DS4Windows startup.

## [3.11.1]
- Custom Notification Window System: profile changes use an independent notification window instead of Windows Action Center. Notifications appear at top-right, are focus-independent, and include system beep.
- Profile Notification Accuracy: controller connection notifications now display the actual active profile name.

## [3.11.0]
- Special Action Button Suppression Fix: when a Special Action is triggered, the last pressed button's other assigned function is now prevented from executing alongside the Special Action to avoid duplicate inputs.

