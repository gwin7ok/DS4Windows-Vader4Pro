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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            
            // Apply log settings from Profiles.xml
            ApplyLogSettings(fileTarget);
            
            LogManager.Configuration = configuration;
            LogManager.ReconfigExistingLoggers();

            logger = LogManager.GetCurrentClassLogger();

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
            
            // Set minlevel
            var configuration = LogManager.Configuration;
            foreach (var rule in configuration.LoggingRules)
            {
                if (rule.LoggerNamePattern == "*" && rule.Targets.Any(t => t.Name == "logfile"))
                {
                    rule.SetLoggingLevels(ParseLogLevel(DS4Windows.Global.LogMinLevel), LogLevel.Fatal);
                }
            }
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
            
            ApplyLogSettings(fileTarget);
            
            LogManager.Configuration = configuration;
            LogManager.ReconfigExistingLoggers();
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
