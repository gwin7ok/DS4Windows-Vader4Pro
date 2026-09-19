using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-2 (PR-2): ControlService のコンストラクタ注入拡張（IOutputSlotService の遅延解決〔決定D1〕、
    /// IProfileRepository / IDeviceStateService / IProfileXmlStore / IProfileSlotApplier〔決定O1=B・O4=B-2〕）が
    /// Composition Root から正しく配線されていることと、必須引数が null で受け付けられないことを検証する。
    /// </summary>
    public class ControlServicePr2DiWiringTests
    {
        private static ControlService CreateWith(
            Func<IOutputSlotService> outputSlotFactory = null,
            IProfileRepository profileRepository = null,
            IDeviceStateService deviceStateService = null,
            IProfileXmlStore profileXmlStore = null,
            IProfileSlotApplier profileSlotApplier = null,
            bool useDefaults = true)
        {
            DS4WinWPF.AppHost.CreateHost();
            return new ControlService(
                new DS4WinWPF.ArgumentParser(),
                DS4WinWPF.AppHost.GetService<IDs4DeviceRegistry>(),
                DS4WinWPF.AppHost.GetService<IProfileSettingsService>(),
                DS4WinWPF.AppHost.GetService<IAppSettingsService>(),
                DS4WinWPF.AppHost.GetService<IEnvironmentService>(),
                DS4WinWPF.AppHost.GetService<IPathService>(),
                outputSlotFactory ?? (useDefaults ? (() => DS4WinWPF.AppHost.GetService<IOutputSlotService>()) : null),
                profileRepository ?? (useDefaults ? DS4WinWPF.AppHost.GetService<IProfileRepository>() : null),
                deviceStateService ?? (useDefaults ? DS4WinWPF.AppHost.GetService<IDeviceStateService>() : null),
                profileXmlStore ?? (useDefaults ? DS4WinWPF.AppHost.GetService<IProfileXmlStore>() : null),
                profileSlotApplier ?? (useDefaults ? DS4WinWPF.AppHost.GetService<IProfileSlotApplier>() : null));
        }

        [Fact]
        public void AppHost_ResolvesProfileSlotApplier_AsDefaultImplementation()
        {
            DS4WinWPF.AppHost.CreateHost();

            Assert.IsType<ProfileSlotApplier>(DS4WinWPF.AppHost.GetService<IProfileSlotApplier>());
        }

        [Fact]
        public void AppHost_ResolvesControlService_WithPr2Dependencies()
        {
            DS4WinWPF.AppHost.CreateHost();

            ControlService controlService = null;
            var ex = Record.Exception(() => controlService = DS4WinWPF.AppHost.GetService<ControlService>());

            Assert.Null(ex);
            Assert.NotNull(controlService);
        }

        [Fact]
        public void Constructor_DoesNotInvokeOutputSlotServiceFactory()
        {
            // 決定D1: OutputSlotService → ControlService の逆依存があるため、
            // コンストラクタ内で IOutputSlotService を解決してはならない（生成中の解決は二重実体化の恐れがある）。
            int factoryCalls = 0;

            var controlService = CreateWith(outputSlotFactory: () =>
            {
                factoryCalls++;
                return DS4WinWPF.AppHost.GetService<IOutputSlotService>();
            });

            Assert.NotNull(controlService);
            Assert.Equal(0, factoryCalls);
        }

        [Fact]
        public void Constructor_NullOutputSlotServiceFactory_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => CreateWith(useDefaults: false));

            Assert.Equal("outputSlotServiceFactory", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullProfileRepository_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new ControlService(
                new DS4WinWPF.ArgumentParser(),
                DS4WinWPF.AppHost.GetService<IDs4DeviceRegistry>(),
                DS4WinWPF.AppHost.GetService<IProfileSettingsService>(),
                DS4WinWPF.AppHost.GetService<IAppSettingsService>(),
                DS4WinWPF.AppHost.GetService<IEnvironmentService>(),
                DS4WinWPF.AppHost.GetService<IPathService>(),
                () => DS4WinWPF.AppHost.GetService<IOutputSlotService>(),
                null,
                DS4WinWPF.AppHost.GetService<IDeviceStateService>(),
                DS4WinWPF.AppHost.GetService<IProfileXmlStore>(),
                DS4WinWPF.AppHost.GetService<IProfileSlotApplier>()));

            Assert.Equal("profileRepository", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullDeviceStateService_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new ControlService(
                new DS4WinWPF.ArgumentParser(),
                DS4WinWPF.AppHost.GetService<IDs4DeviceRegistry>(),
                DS4WinWPF.AppHost.GetService<IProfileSettingsService>(),
                DS4WinWPF.AppHost.GetService<IAppSettingsService>(),
                DS4WinWPF.AppHost.GetService<IEnvironmentService>(),
                DS4WinWPF.AppHost.GetService<IPathService>(),
                () => DS4WinWPF.AppHost.GetService<IOutputSlotService>(),
                DS4WinWPF.AppHost.GetService<IProfileRepository>(),
                null,
                DS4WinWPF.AppHost.GetService<IProfileXmlStore>(),
                DS4WinWPF.AppHost.GetService<IProfileSlotApplier>()));

            Assert.Equal("deviceStateService", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullProfileXmlStore_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new ControlService(
                new DS4WinWPF.ArgumentParser(),
                DS4WinWPF.AppHost.GetService<IDs4DeviceRegistry>(),
                DS4WinWPF.AppHost.GetService<IProfileSettingsService>(),
                DS4WinWPF.AppHost.GetService<IAppSettingsService>(),
                DS4WinWPF.AppHost.GetService<IEnvironmentService>(),
                DS4WinWPF.AppHost.GetService<IPathService>(),
                () => DS4WinWPF.AppHost.GetService<IOutputSlotService>(),
                DS4WinWPF.AppHost.GetService<IProfileRepository>(),
                DS4WinWPF.AppHost.GetService<IDeviceStateService>(),
                null,
                DS4WinWPF.AppHost.GetService<IProfileSlotApplier>()));

            Assert.Equal("profileXmlStore", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullProfileSlotApplier_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new ControlService(
                new DS4WinWPF.ArgumentParser(),
                DS4WinWPF.AppHost.GetService<IDs4DeviceRegistry>(),
                DS4WinWPF.AppHost.GetService<IProfileSettingsService>(),
                DS4WinWPF.AppHost.GetService<IAppSettingsService>(),
                DS4WinWPF.AppHost.GetService<IEnvironmentService>(),
                DS4WinWPF.AppHost.GetService<IPathService>(),
                () => DS4WinWPF.AppHost.GetService<IOutputSlotService>(),
                DS4WinWPF.AppHost.GetService<IProfileRepository>(),
                DS4WinWPF.AppHost.GetService<IDeviceStateService>(),
                DS4WinWPF.AppHost.GetService<IProfileXmlStore>(),
                null));

            Assert.Equal("profileSlotApplier", ex.ParamName);
        }
    }
}