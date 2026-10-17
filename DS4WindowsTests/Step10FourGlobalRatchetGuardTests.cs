using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-4-6: 全域の退行ガード（ラチェット）。<c>DS4Windows</c> 配下の全 <c>.cs</c> について、
    /// ファイルごとの <c>Program.rootHub</c>／<c>AppHost.GetService|Services</c>／<c>Global.*</c> の件数（コメントを除く）が、
    /// Step10-4 の完了時点（2026-10-15）の基準値以下であることを固定する。
    /// - 件数が減るのは許可する（基準値を更新して、さらに固定すること）。
    /// - 件数が増える、または基準値に無いファイルに現れる場合は失敗する（新しい直接参照を持ち込まない）。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// 基準値の内訳と経緯: <c>docs-forDIMG/MadeByAgent/Phase6-Step10-4-Plan.md</c> §34。
    /// </summary>
    public class Step10FourGlobalRatchetGuardTests
    {
        // (Program.rootHub, AppHost.GetService|Services, Global.*)
        private static readonly Dictionary<string, (int RootHub, int AppHost, int Global)> Baseline =
            new Dictionary<string, (int RootHub, int AppHost, int Global)>
        {
            // Phase6-Step10b W4: ActionManager.cs の Global 1 件は ActionEntry.cs へ移った（合計は不変）
            ["DS4Windows/Actions/ActionEntry.cs"] = (0, 0, 0),
            ["DS4Windows/Actions/DefaultActionManager.cs"] = (0, 0, 0),
            ["DS4Windows/Actions/DefaultMacroPlayer.cs"] = (0, 0, 0),
            // Phase7-Step5 W2: Program.rootHub を App のフィールド _controlService に置き換え（12 → 0）
            // Phase7-Step5 W4: App の AppHost.GetService は ResolvePostHostService の 1 件だけ（4 → 1）
            ["DS4Windows/App.xaml.cs"] = (0, 1, 0),
            // Phase7-Step10a W4: Global.Clamp の残りの呼び出し元を NumericUtil.Clamp に替えた（BezierCurve 1、DS4State 4、Mouse 2、MouseCursor 2 → すべて 0）
            ["DS4Windows/BezierCurveEditor/BezierCurve.cs"] = (0, 0, 0),
            // Phase6-Step10b W2: DS4Windows.AppHost（互換ラッパー）を DI/Compat/AppHost.cs へ分離。件数を引き継いだ（4＝2＋2。合計は不変）
            // Phase7-Step9 W1: 互換ラッパー（DI/Compat/AppHost.cs）を削除（呼び出し元 0 件。AppHost 合計 5 → 4）
            ["DS4Windows/DI/AppHost.cs"] = (0, 2, 0),
            // Phase7-Step5 W2: Program.rootHub ?? の削除（1 → 0）
            ["DS4Windows/DI/ServiceRegistration.cs"] = (0, 0, 1),
            // Phase7-Step5 W4: IElevatedProcessLauncher をコンストラクター注入にし、AppHost.GetService を削除（1 → 0）
            ["DS4Windows/DS4Control/ControlService.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Control/DS4LightBar.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceBasic.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceExt.cs"] = (0, 0, 1),
            // Phase6-Step10b W1: 1 ファイル 1 型の分割に伴い、件数を新設ファイルへ引き継いだ（合計は不変: ActionsDTO 6＝4＋2、AutoProfilesDTO 4＝0＋4、ProfileDTO 10＝2＋8）
            // Phase6-Step12 Wave 6: プロファイル用 DTO（ProfileDTO と、ProfileDTO 専用の型 19 ファイル）の削除に伴い、DS4Control*AssignmentSerializer（6 ファイル）と ProfileDTO の基準値（計 10）を除いた
            ["DS4Windows/DS4Control/DTOXml/ActionsDTO.cs"] = (0, 0, 4),
            ["DS4Windows/DS4Control/DTOXml/AppSettingsDTO.cs"] = (0, 0, 2),
            ["DS4Windows/DS4Control/DTOXml/AutoProfileEntrySerializer.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Control/DTOXml/OutputSlotPersistDTO.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/DTOXml/SpecialActionSerializer.cs"] = (0, 0, 2),
            // Phase6-Step10b W4: Log.cs（AppLogger／ProfileChangeSource／ProfileChangedEventArgs）は型ごとに分割。Global 1 件は AppLogger.cs
            ["DS4Windows/DS4Control/AppLogger.cs"] = (0, 0, 1),
            // Phase7-Step5 W2: Program.rootHub のフォールバックを削除（1 → 0）
            // Phase7-Step7 W2: 合成入力の状態の宣言（Global.MAX_DS4_CONTROLLER_COUNT）を SyntheticInputState へ移した（52 → 51）
            // Phase7-Step7 W3: macroTaskQueue の宣言（Global.MAX_DS4_CONTROLLER_COUNT）を MacroRuntimeState へ移した（51 → 50）
            // Phase7-Step7 W4: スティック・ジャイロ・トリガーの処理状態の宣言（Global.MAX_DS4_CONTROLLER_COUNT 7 件）を StickMotionState へ移した（50 → 43。StickMotionState は 1 件）
            // Phase7-Step8 W2: AppHost 6 件をコンストラクター注入へ（6 → 0）、フォールバックの Global.ProfileSettingsServiceInstance／Global.outputKBMHandler を削除（43 → 41）
            // Phase7-Step9 W2: 定数 18 件と Clamp 23 件を ControllerLimits／NumericUtil へ切り替え（41 → 0）
            ["DS4Windows/DS4Control/Mapping.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Control/StickMotionState.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Control/MacroRuntimeState.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Control/SyntheticInputState.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Control/Mouse.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Control/MouseCursor.cs"] = (0, 0, 0),
            // Phase7-Step4 W1: fakerInputInstalled を EnvironmentService の状態に切り替え（1 → 0）
            ["DS4Windows/DS4Control/OutputKBM/VirtualKBMFactory.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Control/OutputSlotPersist.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/ProfileMigration.cs"] = (0, 0, 4),
            // Phase6-Step10b W5: ScpUtil.cs（29 型）を型ごとに分割。件数を引き継いだ（Global 233＝Global 47＋BackingStore 182＋Changelog 4、AppHost 12＝11＋1。rootHub 9 は Global。合計は不変）
            // Phase7-Step1 W1: 孤立メソッド 10 名の削除で Global.* 14 件が減った（182 → 168）
            // Phase7-Step5 W4: IProcessInspector をプロパティ注入にし、AppHost.GetService を削除（1 → 0）
            // Phase7-Step6 W2: activeOutDevType の読み取り 8 件を ActiveOutDevType プロパティへ置換し、既定配列の長さの定数 1 件を追加（166 → 159）
            // Phase7-Step10a W1: 定数 132 件（TEST_PROFILE_ITEM_COUNT 114、MAX_DS4_CONTROLLER_COUNT 18）を ControllerLimits へ切り替え（159 → 27）
            // Phase7-Step10c W3: アプリ設定の保存・読込（Load／Save／LoadFakeExeName）を AppSettingsXmlSerializer へ移した（27 → 18）
            // Phase7-Step10c W4: プロファイルの読込（LoadProfile ほか）を LegacyProfileXmlSerializer へ移した（18 → 14）
            // Phase7-Step10c W5: プロファイルの保存（SaveProfile ほか）を LegacyProfileXmlSerializer へ移した（14 → 7）
            ["DS4Windows/DS4Control/BackingStore.cs"] = (0, 0, 7),
            ["DS4Windows/DS4Control/Changelog.cs"] = (0, 0, 4),
            // Phase6-Step12 Wave 1: 孤立コード 24 件を削除（Global 47 → 45）。Wave 2: 互換アクセサ 152 件を削除（Global 45 → 38）。Wave 3: *Instance のフォールバック群 19 件を削除（Global 38 → 26、AppHost 11 → 5、rootHub 9 → 6）。Wave 4: BackingStore 直転送ラッパー 58 件を削除（AppHost 5 → 4。(6, 4, 26)）
            // Phase7-Step4 W1: IsUsingMinViGEm117333 の移管で Global.* 2 件を削除（24 → 22）
            // Phase7-Step4 W2: Global.Load の削除で AppHost.GetService を 1 件削除（4 → 3）
            // Phase7-Step4 W3: FindConfigLocation／SaveWhere の移管で Global.* 6 件を削除（22 → 16）
            // Phase7-Step4 W5: Program.rootHub の実数は 5（基準値 6 は過大だった）。合計 22 → 21
            // Phase7-Step5 W2: Program.rootHub の参照 5 件を削除（フォールバックのラムダと、Save/LoadControllerConfigs）
            // Phase7-Step5 W3: ProfileXmlStoreInstance／NotificationServiceInstance の削除で AppHost.GetService を 2 件、Global.* を 4 件削除（(0, 3, 16) → (0, 1, 12)）
            // Phase7-Step10a W3: iconChoiceResources と ASSEMBLY／RESOURCES の自己参照 4 件を ResourcePaths の定数に替えた（12 → 8）
            ["DS4Windows/DS4Control/Global.cs"] = (0, 1, 8),
            // Phase7-Step2 W4: INotificationService.NotificationsEnabled／FlashTaskbar の削除で Global.* 5 件を削除（6 → 1）
            ["DS4Windows/DS4Control/Services/AppNotificationService.cs"] = (0, 0, 0),
            // Phase7-Step4 W3: FirstRun は PathService の状態、RunHotPlug はインスタンスの状態にした（8 → 2）
            // Phase7-Step10c W3: CompileVersionNumberFromString を VersionUtil へ移した（2 → 1）
            ["DS4Windows/DS4Control/Services/AppSettingsService.cs"] = (0, 0, 1),
            // Phase7-Step10b W2: iconChoiceResources を TrayIconResources へ移した（4 → 3）
            ["DS4Windows/DS4Control/Services/AppearanceSettingsService.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Control/Services/AutoProfileService.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Services/DefaultElevatedProcessLauncher.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Services/DeviceStateService.cs"] = (0, 0, 1),
            // Phase7-Step2 W7: IDs4DeviceRegistry.IsHidHideInstalled の削除で Global.IsHidHideInstalled の参照 1 件を削除（1 → 0）
            ["DS4Windows/DS4Control/Services/Ds4DeviceRegistryAdapter.cs"] = (0, 0, 0),
            // Phase7-Step4 W1: 調べ物と状態を EnvironmentService が持つ形に移管（21 → 7: IsAdministrator／exeversion／GetInstanceIdFromDevicePath／AdminNeeded と、スロット上限の定数 3）
            // Phase7-Step10a W2: 定数参照（Global.MAX_DS4_CONTROLLER_COUNT ほか）を ControllerLimits へ切り替え（Actions 3、App 1、ControlService 2、DS4LightBar 7、AppSettingsDTO 14、AutoProfileEntrySerializer 4、DeviceStateService 1、EnvironmentService 3、OutputSlotService 1、MainWindow 1、ProfileEditor 3）
            ["DS4Windows/DS4Control/Services/EnvironmentService.cs"] = (0, 0, 4),
            // Phase7-Step2 W4: IVirtualKBM.ErrorMessage の削除で Global.outputKBMHandler の参照 1 件を削除（21 → 20）。W7: Version の削除でさらに 1 件削除（20 → 19）
            // Phase7-Step8 W3（Q6＝A）: OutputKBMHandlerAdapter.cs の Global.outputKBMHandler／outputKBMMapping 参照 19 件を削除（出力ハンドラの実体を OutputKBMHandlerAdapter へ）
            // Phase7-Step8 W3（Q6＝A）: OutputKBMHandlerLifecycle.cs の Global.outputKBMHandler／outputKBMMapping 参照 8 件を削除（出力ハンドラの実体を OutputKBMHandlerAdapter へ）
            // Phase7-Step6 W2: 配列の実体を持ち、Global の参照は配列の長さの定数 1 件だけ（2 → 1）
            ["DS4Windows/DS4Control/Services/OutputSlotService.cs"] = (0, 0, 0),
            // Phase7-Step4 W3: 実行ファイル・保存場所の状態を PathService が持つ形に移管（8 → 1: 省略時の Global.store）
            ["DS4Windows/DS4Control/Services/PathService.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Services/ProfileApplicationService.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Control/Services/ProfilePresetLoader.cs"] = (0, 0, 0),
            // 26: 名前変更の伝播（ReplaceProfileSwitchTarget。Phase6-Step10-4-5d）で、Global.store への委譲を 2 件追加（薄いシムの性質。24 → 26）
            ["DS4Windows/DS4Control/Services/ProfileRepository.cs"] = (0, 0, 0),
            // Phase7-Step2 W1: コピー版 GetReverseX360ButtonMapping の削除で Global.reverseX360ButtonMapping の参照 1 件を削除（5 → 4）
            // Phase7-Step10b W1: 表と変換（defaultButtonMapping／reverseX360ButtonMapping／getX360ControlString ほか）を X360ControlTables／DS4ControlNames へ移した（ProfileSettingsService 3 → 1、MappedControl 3 → 0、RecordBoxViewModel 1 → 0、PressKeyViewModel 7 → 0）
            ["DS4Windows/DS4Control/Services/ProfileSettingsService.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Services/ProfileSlotApplier.cs"] = (0, 0, 0),
            // Phase7-Step5 W2: Global.Load/SaveControllerConfigs の削除に伴い、BackingStore を直接呼ぶ形に（2 → 0）
            ["DS4Windows/DS4Control/Services/ProfileXmlStore.cs"] = (0, 0, 0),
            // Phase7-Step2 W7: SaveActions の削除で Global.SaveActions の参照 1 件を削除（4 → 3）
            // Phase7-Step10c W2: Global.SaveAction／RemoveAction／CreateStdActions の呼び出しを SpecialActionRepository のメソッドに替えた（3 → 0。基準値の更新漏れを W7 で直した）
            ["DS4Windows/DS4Control/Services/SpecialActionRepository.cs"] = (0, 0, 0),
            // Phase7-Step8 W3（Q6＝A）: SyntheticDispatcher.cs の Global.outputKBMHandler／outputKBMMapping 参照 4 件を削除（出力ハンドラの実体を OutputKBMHandlerAdapter へ）
            ["DS4Windows/DS4Control/Util.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Control/Xbox360OutDevice.cs"] = (0, 0, 1),
            // Phase6-Step10b W3: 1 ファイル 1 型の分割に伴い、件数を新設ファイルへ引き継いだ（合計は不変）
            //   MainWindow 7＝1＋ImageLocationPaths 6、ProfileEditor 37＝32＋ResourcePaths 5、ControllerListViewModel 4＝0＋CompositeDeviceModel 4、
            //   MappingListViewModel 3＝0＋MappedControl 3、RecordBoxViewModel 7＝1＋MacroStepItem 6
            // Phase7-Step10a W3: 資源パスの定数 Global.RESOURCES_PREFIX／ASSEMBLY_RESOURCE_PREFIX を ResourcePaths へ移した（ImageLocationPaths 6、ProfileEditor 29、ResourcePaths 5、CompositeDeviceModel 4、MacroStepItem 6、ProfileSettingsViewModel 1、TrayIconViewModel 12 → すべて 0）
            ["DS4Windows/DS4Forms/ImageLocationPaths.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/LanguageSelectDialog.xaml.cs"] = (0, 0, 1),
            // Phase7-Step5 W5: AppHost.GetService の 17 件をコンストラクター引数に（IMainWindowFactory）。17 → 0
            ["DS4Windows/DS4Forms/MainWindow.xaml.cs"] = (0, 0, 0),
            // Phase7-Step5 W2: Program.rootHub のフォールバックを削除（1 → 0）
            // Phase7-Step5 W5: AppHost.GetService の 4 件をコンストラクター引数に（IEditorWindowFactory.CreateProfileEditor）。4 → 0
            ["DS4Windows/DS4Forms/ProfileEditor.xaml.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/ResourcePaths.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/ViewModels/CompositeDeviceModel.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/ViewModels/LanguagePackViewModel.cs"] = (0, 0, 3),
            ["DS4Windows/DS4Forms/ViewModels/MacroStepItem.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/ViewModels/MappedControl.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/ViewModels/MappingListViewModel.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/ViewModels/RecordBoxViewModel.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/CheckBatteryViewModel.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/LaunchProgramViewModel.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/PressKeyViewModel.cs"] = (0, 0, 0),
            ["DS4Windows/DS4Forms/ViewModels/SpecialActions/SpecialActionViewModel.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Forms/ViewModels/TrayIconViewModel.cs"] = (0, 0, 0),
            // Phase6-Step12: デバウンス時間の通知経路の削除で、Global.DebouncingMsChanged／DebouncingMs の参照 2 件を削除（3 → 1）
            ["DS4Windows/DS4Library/DS4Device.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Library/DS4Devices.cs"] = (0, 0, 6),
            // Phase6-Step10b W4: DS4Sixaxis.cs は DS4SixAxis.cs へ改名（型名に合わせた大文字・小文字の変更。Dictionary は大文字・小文字を区別する）
            // Phase7-Step5 W1: DS3 のジャイロトリガーの判定を関数の注入にし、Program.rootHub の参照を削除（1 → 0）
            ["DS4Windows/DS4Library/DS4SixAxis.cs"] = (0, 0, 1),
            ["DS4Windows/DS4Library/DS4State.cs"] = (0, 0, 0),
            ["DS4Windows/HidLibrary/HidDevices.cs"] = (0, 0, 2),
        };

        private static readonly Regex RootHubPattern = new Regex(@"Program\.rootHub", RegexOptions.Compiled);
        private static readonly Regex AppHostPattern = new Regex(@"AppHost\.(?:GetService|Services)", RegexOptions.Compiled);
        private static readonly Regex GlobalPattern = new Regex(@"(?<![\w.])Global\.\w+", RegexOptions.Compiled);

        private readonly ITestOutputHelper _output;

        public Step10FourGlobalRatchetGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void DirectReferences_DoNotExceedStep10FourBaseline()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            // <root>/DS4Windows/DS4Control/Global.cs → <root>
            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            string sourceDir = Path.Combine(root, "DS4Windows");

            var violations = new List<string>();
            foreach (string file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/") || normalized.Contains("/bin/")) continue;

                string relative = "DS4Windows/" + Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
                string text = StripComments(File.ReadAllText(file));

                int rootHub = RootHubPattern.Matches(text).Count;
                int appHost = AppHostPattern.Matches(text).Count;
                int global = GlobalPattern.Matches(text).Count;
                if (rootHub == 0 && appHost == 0 && global == 0) continue;

                Baseline.TryGetValue(relative, out var allowed);
                if (rootHub > allowed.RootHub)
                    violations.Add($"{relative}: Program.rootHub {rootHub} 件（基準 {allowed.RootHub}）");
                if (appHost > allowed.AppHost)
                    violations.Add($"{relative}: AppHost.GetService|Services {appHost} 件（基準 {allowed.AppHost}）");
                if (global > allowed.Global)
                    violations.Add($"{relative}: Global.* {global} 件（基準 {allowed.Global}）");
            }

            foreach (string v in violations) _output.WriteLine(v);
            Assert.True(violations.Count == 0,
                "基準値を超える直接参照がある（新しい Global／AppHost／Program.rootHub の参照を持ち込まず、依存を注入すること）:\n" +
                string.Join("\n", violations));
        }
    }
}
