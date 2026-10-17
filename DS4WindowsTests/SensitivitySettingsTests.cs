using System;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    public class SensitivitySettingsTests
    {
        [Fact]
        public void SensitivitySettings_IndividualChange_ShouldUpdateString()
        {
            var settings = new SensitivitySettings();
            settings.LS = 5;
            settings.R2 = 10;

            // 個別の変更がパイプ区切り文字列に正しく反映されること
            Assert.Equal("5|0|0|10|0|0", settings.SensitivityString);
        }

        [Fact]
        public void SensitivitySettings_StringChange_ShouldUpdateIndividualProperties()
        {
            var settings = new SensitivitySettings();
            settings.SensitivityString = "1|2|3|4|5|6";

            // 文字列の入力が各プロパティに正しくパースされること
            Assert.Equal(1, settings.LS);
            Assert.Equal(2, settings.RS);
            Assert.Equal(3, settings.L2);
            Assert.Equal(4, settings.R2);
            Assert.Equal(5, settings.SX);
            Assert.Equal(6, settings.SZ);
        }

        [Fact]
        public void SensitivitySettings_SubPropertyChange_ShouldRaisePropertyChanged()
        {
            var settings = new SensitivitySettings();
            string reportedProperty = null;
            bool eventFired = false;

            // Phase7-Step10b W3: OnSubPropertyChanged（親への中継）は削除した。PropertyChanged で変更の通知を確認する
            settings.PropertyChanged += (s, e) =>
            {
                reportedProperty = e.PropertyName;
                eventFired = true;
            };

            settings.LS = 42;

            Assert.True(eventFired);
            Assert.Equal(nameof(SensitivitySettings.LS), reportedProperty);
        }
    }
}