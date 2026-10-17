/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Control.DTOXml;
using DS4WinWPF.DS4Forms.ViewModels;
using Sensorit.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using DS4WinWPF;
using DS4WinWPF.ApiDTO;
using WpfScreenHelper;
using static DS4Windows.Mouse;
using static DS4Windows.Util;
using LightbarMacro = DS4WinWPF.DS4Forms.ViewModels.LightbarMacro;
namespace DS4Windows
{

    public class Changelog
    {
        // Use the GitHub REST API /repos/ path so the app queries releases for the correct repo
        public const string GITHUB_RELEASES_API_URI = "https://api.github.com/repos/gwin7ok/DS4Windows-Vader4Pro/releases";
        public const string GITHUB_LATEST_RELEASE_API_URI = "https://api.github.com/repos/gwin7ok/DS4Windows-Vader4Pro/releases/latest";

        private static bool? _newerVersionAvailable = null;
        private static Version _latestVersion;

        // Much more compact and elegant way of checking if there is a new update available than the
        // shenanigans with fetching newest.txt and using a .txt file as a DTO instead of simply
        // passing a string to the function that displays the updater window.
        // I wanted to make this method async, but these can't have an out parameter and this is the signature i wanted
        // it to have
        public static bool CheckNewerVersionExists(out Version version, bool allowCached = true, bool respectSkip = true)
        {
            version = Version.Parse("0.0.0");

            if (!Version.TryParse(Global.exeversion, out var currentVersion)) return false;

            // attempt to limit HTTP requests for reasons of 1. performance 2. GH API rate limit
            if (allowCached && _newerVersionAvailable is not null)
            {
                version = _latestVersion;
                return (bool)_newerVersionAvailable;
            }

            var request = App.requestClient.GetAsync(GITHUB_LATEST_RELEASE_API_URI);
            request.Wait();
            if (request.Result.IsSuccessStatusCode)
            {
                var task = request.Result.Content.ReadFromJsonAsync<GithubRelease>();
                task.Wait();

                // if can't parse the newest version
                if (!Version.TryParse(task.Result.TagName[1..], out version)) return false;

                // If user previously skipped this (or a newer) version, treat as no newer version
                if (respectSkip)
                {
                    try
                    {
                        // Prefer comparing Version objects parsed from stored string to avoid
                        // ambiguous numeric compile differences when Revision/Build are missing.
                        if (!string.IsNullOrEmpty(Global.LastVersionChecked))
                        {
                            if (Version.TryParse(Global.LastVersionChecked, out var skippedVer))
                            {
                                AppLogger.LogDebug($"[Changelog] SkippedVersion={skippedVer} LatestCandidate={version}");
                                if (skippedVer >= version)
                                {
                                    _newerVersionAvailable = false;
                                    return false;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogDebug($"[Changelog] Skip-check parse error: {ex.Message}");
                        // If anything goes wrong reading skip info, fall back to normal behavior
                    }
                }

                // if there is a newer version available
                if (currentVersion < version)
                {
                    _latestVersion = version;
                    _newerVersionAvailable = true;
                    return true;
                }

                // if current version is the latest
                _newerVersionAvailable = false;
                return false;
            }

            return false;
        }

        public static async Task<Dictionary<Version, string>> GetChangelog(bool allVersions = false)
        {
            Dictionary<Version, string> dict = new();

            if (!Version.TryParse(Global.exeversion, out var currentVersion)) return dict;

            var request = await App.requestClient.GetAsync(GITHUB_RELEASES_API_URI);
            var releases = await request.Content.ReadFromJsonAsync<GithubRelease[]>();

            foreach (var release in releases)
            {
                if (release.PreRelease) continue;

                if (!Version.TryParse(release.TagName[1..], out var parsedVersion)) continue;

                if (!allVersions && parsedVersion <= currentVersion) break;
                dict.Add(parsedVersion, release.Body);
            }

            return dict;
        }

        public static async Task<string> GetChangelogMarkdown(bool allVersions = false)
        {
            var versions = await GetChangelog(allVersions);

            StringBuilder sb = new();
            foreach (var version in versions)
            {
                // if we've already added content, ensure separation before next header
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine();
                }

                // Header + blank line
                sb.Append("## Version ");
                sb.Append(version.Key);
                sb.AppendLine();
                sb.AppendLine();

                var parsedChangelog = ParseChangelogString(version.Value)?.Trim();
                if (!string.IsNullOrEmpty(parsedChangelog))
                {
                    sb.Append(parsedChangelog);
                    // ensure the section ends with a blank line
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static string ParseChangelogString(string changelog)
        {
            var split = changelog.Split("\n").ToList();
            split.RemoveAll(x => x.StartsWith("**Full Changelog**"));
            return string.Join(Environment.NewLine, split);
        }
    }
}

