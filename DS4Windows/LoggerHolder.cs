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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DS4Windows;
using NLog;
using NLog.Targets.Wrappers;

namespace DS4WinWPF
{
    public class LoggerHolder
    {
        private Logger logger;// = LogManager.GetCurrentClassLogger();
        public Logger Logger { get => logger; }
        private ReaderWriterLockSlim logLock = new ReaderWriterLockSlim();

        public LoggerHolder(DS4Windows.ControlService service)
        {
            var configuration = LogManager.Configuration;
            var wrapTarget = configuration.FindTargetByName<WrapperTargetBase>("logfile") as WrapperTargetBase;
            var fileTarget = wrapTarget.WrappedTarget as NLog.Targets.FileTarget;
            fileTarget.FileName = $@"{DS4Windows.Global.appdatapath}\Logs\ds4windows_log.txt";
            fileTarget.ArchiveFileName = $@"{DS4Windows.Global.appdatapath}\Logs\ds4windows_log_{{#}}.txt";
            
            // Configure archive settings
            fileTarget.ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence;
            fileTarget.ArchiveDateFormat = "yyyyMMdd";
            fileTarget.ArchiveOldFileOnStartup = true; // Keep true as default
            
            // Apply log settings from Profiles.xml (includes MaxArchiveFiles)
            ApplyLogSettings(fileTarget);
            
            LogManager.Configuration = configuration;
            LogManager.ReconfigExistingLoggers();

            logger = LogManager.GetCurrentClassLogger();
            logger.Info("Logger initialized");

            service.Debug += WriteToLog;
            DS4Windows.AppLogger.GuiLog += WriteToLog;
        }

        /// <summary>
        /// Apply log settings from Profiles.xml to NLog configuration
        /// </summary>
        private void ApplyLogSettings(NLog.Targets.FileTarget fileTarget)
        {
            // Set maxArchiveFiles
            fileTarget.MaxArchiveFiles = DS4Windows.Global.LogMaxArchiveFiles;
            
            // Set minlevel for all logging rules that target "logfile"
            var configuration = LogManager.Configuration;
            var minLevel = ParseLogLevel(DS4Windows.Global.LogMinLevel);
            
            foreach (var rule in configuration.LoggingRules)
            {
                if (rule.Targets.Any(t => t.Name == "logfile" || (t is WrapperTargetBase wrapper && wrapper.WrappedTarget?.Name == "logfile")))
                {
                    // Clear existing levels and set new range
                    rule.DisableLoggingForLevels(LogLevel.Trace, LogLevel.Fatal);
                    rule.EnableLoggingForLevels(minLevel, LogLevel.Fatal);
                }
            }
            
            // Log the applied settings for debugging
            System.Diagnostics.Debug.WriteLine($"NLog settings applied: MaxArchiveFiles={fileTarget.MaxArchiveFiles}, MinLevel={minLevel}");
        }

        /// <summary>
        /// Parse log level string to NLog.LogLevel
        /// </summary>
        private LogLevel ParseLogLevel(string level)
        {
            return level?.ToLower() switch
            {
                "trace" => LogLevel.Trace,
                "debug" => LogLevel.Debug,
                "info" => LogLevel.Info,
                "warn" => LogLevel.Warn,
                "error" => LogLevel.Error,
                "fatal" => LogLevel.Fatal,
                _ => LogLevel.Debug
            };
        }

        /// <summary>
        /// Update NLog configuration with new settings (call when settings change)
        /// </summary>
        public void UpdateLogSettings()
        {
            var configuration = LogManager.Configuration;
            var wrapTarget = configuration.FindTargetByName<WrapperTargetBase>("logfile") as WrapperTargetBase;
            var fileTarget = wrapTarget.WrappedTarget as NLog.Targets.FileTarget;
            
            // Temporarily disable archive to prevent rotation during reconfiguration
            fileTarget.ArchiveOldFileOnStartup = false;
            
            ApplyLogSettings(fileTarget);
            
            // Reconfigure loggers to apply new rules
            LogManager.ReconfigExistingLoggers();
            
            // Log the settings change
            logger?.Debug($"Log settings updated: MaxArchiveFiles={DS4Windows.Global.LogMaxArchiveFiles}, MinLevel={DS4Windows.Global.LogMinLevel}");
        }
        
        /// <summary>
        /// Restore archiveOldFileOnStartup after all settings updates are complete
        /// </summary>
        public void RestoreArchiveSetting()
        {
            var configuration = LogManager.Configuration;
            var wrapTarget = configuration.FindTargetByName<WrapperTargetBase>("logfile") as WrapperTargetBase;
            var fileTarget = wrapTarget.WrappedTarget as NLog.Targets.FileTarget;
            fileTarget.ArchiveOldFileOnStartup = true;
        }
        
        /// <summary>
        /// Update NLog.config file with new maxArchiveFiles setting
        /// </summary>
        public void UpdateNLogConfig(int maxArchiveFiles)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NLog.config");
                if (!File.Exists(configPath))
                {
                    logger?.Debug($"NLog.config not found at {configPath}");
                    return;
                }
                
                XDocument doc = XDocument.Load(configPath);
                XNamespace ns = "http://www.nlog-project.org/schemas/NLog.xsd";
                
                var target = doc.Descendants(ns + "target")
                    .FirstOrDefault(t => t.Attribute("name")?.Value == "logfile");
                
                if (target != null)
                {
                    // Update or add maxArchiveFiles attribute
                    var maxArchiveAttr = target.Attribute("maxArchiveFiles");
                    if (maxArchiveAttr != null)
                    {
                        maxArchiveAttr.Value = maxArchiveFiles.ToString();
                    }
                    else
                    {
                        target.Add(new XAttribute("maxArchiveFiles", maxArchiveFiles));
                    }
                    
                    doc.Save(configPath);
                    logger?.Debug($"NLog.config updated: maxArchiveFiles={maxArchiveFiles}");
                }
            }
            catch (Exception ex)
            {
                logger?.Debug($"Failed to update NLog.config: {ex.Message}");
            }
        }

        private void WriteToLog(object sender, DS4Windows.DebugEventArgs e)
        {
            if (e.Temporary)
            {
                return;
            }

            using WriteLocker locker = new WriteLocker(logLock);
            if (!e.Warning)
            {
                logger.Info(e.Data);
            }
            else
            {
                logger.Warn(e.Data);
            }
        }
    }
}
