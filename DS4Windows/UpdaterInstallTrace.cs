// UpdaterInstallTrace.cs
// NOTE: This file was previously used to add a bespoke tracing helper for the Updater
// install/elevation flow. Per project guidelines, prefer the existing AppLogger / NLog
// methods instead of introducing separate helper wrappers. The callsites were updated
// to use `DS4Windows.AppLogger` directly; this file is intentionally left as a placeholder
// to avoid reintroducing duplicate logging helpers.

