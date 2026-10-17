using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step6: 出力デバイス種別の配列（OutDevTypeTemp／ActiveOutDevType）の所有権を、Global から OutputSlotService へ移したことの回帰ガード。
    /// - Global に配列がない。BackingStore は Global から読まず、自身の ActiveOutDevType を読む。
    /// - 配列の実体は OutputSlotService の readonly フィールド（参照は再代入されない。ControlService のキャッシュの前提）。
    /// - BackingStore への配列の受け渡しは IOutputSlotService の登録内で行い、BackingStore の登録内で IOutputSlotService を解決しない
    ///   （IOutputSlotStore → IPathService → BackingStore と循環し、スタックオーバーフローになる）。
    /// ソースが見つからない環境では、検査を行わず合格とする。判断の根拠は Phase7-Step6-Plan.md。
    /// </summary>
    public class Phase7Step6OutputTypeOwnershipGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step6OutputTypeOwnershipGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string Read(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した: " + relativePath);
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        [Fact]
        public void Global_NoLongerHoldsTheArrays_AndNoOneReadsThemFromGlobal()
        {
            string global = Read("DS4Windows/DS4Control/Global.cs");
            string store = Read("DS4Windows/DS4Control/BackingStore.cs");
            string control = Read("DS4Windows/DS4Control/ControlService.cs");
            string service = Read("DS4Windows/DS4Control/Services/OutputSlotService.cs");
            if (global == null || store == null || control == null || service == null) return;

            Assert.DoesNotContain("outDevTypeTemp", global);
            Assert.DoesNotContain("activeOutDevType", global);
            Assert.DoesNotContain("Global.activeOutDevType", store);
            Assert.DoesNotContain("Global.outDevTypeTemp", store);
            Assert.DoesNotContain("Global.activeOutDevType", service);
            Assert.DoesNotContain("Global.outDevTypeTemp", service);
            Assert.DoesNotContain("Global.activeOutDevType", control);
        }

        [Fact]
        public void BackingStore_ReadsItsOwnActiveOutDevType()
        {
            string store = Read("DS4Windows/DS4Control/BackingStore.cs");
            if (store == null) return;

            Assert.Matches(@"public\s+OutContType\[\]\s+ActiveOutDevType\s*\{\s*get;\s*set;\s*\}", store);
            // Phase7-Step10c W4: プロファイルの読込（1 件）は LegacyProfileXmlSerializer へ移した（store.ActiveOutDevType）。合計は変わらない
            // Phase7-Step10c W6: プリセットの読込（7 件）も ProfilePresets へ移した。3 つのソースを合わせて 8 件
            string serializer = Read("DS4Windows/DS4Control/Services/LegacyProfileXmlSerializer.cs");
            string presets = Read("DS4Windows/DS4Control/Services/ProfilePresets.cs");
            if (serializer == null || presets == null) return;
            Assert.Equal(8, Regex.Matches(store + serializer + presets, @"OutContType oldContType = (store\.)?ActiveOutDevType\[device\];").Count);
        }

        [Fact]
        public void OutputSlotService_OwnsTheArrays_AsReadonlyFields()
        {
            string service = Read("DS4Windows/DS4Control/Services/OutputSlotService.cs");
            if (service == null) return;

            Assert.Matches(@"private\s+readonly\s+OutContType\[\]\s+_outDevTypeTemp\s*=\s*CreateFilled\(OutContType\.X360\)", service);
            Assert.Matches(@"private\s+readonly\s+OutContType\[\]\s+_activeOutDevType\s*=\s*CreateFilled\(OutContType\.None\)", service);
            Assert.Contains("OutContType[] OutDevTypeTemp => _outDevTypeTemp;", service);
            Assert.Contains("OutContType[] ActiveOutDevType => _activeOutDevType;", service);
        }

        [Fact]
        public void Registration_HandsTheArrayToBackingStore_WithoutResolvingTheServiceFromTheStoreRegistration()
        {
            string registration = Read("DS4Windows/DI/ServiceRegistration.cs");
            if (registration == null) return;

            Assert.Contains("sp.GetRequiredService<BackingStore>().ActiveOutDevType = service.ActiveOutDevType;", registration);

            // BackingStore の登録のラムダの中で IOutputSlotService を解決しない
            int start = registration.IndexOf("services.AddSingleton<BackingStore>(sp =>");
            Assert.True(start >= 0);
            int end = registration.IndexOf("});", start);
            Assert.True(end > start);
            Assert.DoesNotContain("IOutputSlotService", registration.Substring(start, end - start));
        }
    }
}
