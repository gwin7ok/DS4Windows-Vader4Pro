# Direct callsites inventory

作成日: 2025-12-20

目的: リポジトリ内で「DIを通さず直接外部副作用を発生させる」呼び出しを列挙する（例: `PerformKeyPress*`, `Process.Start`, `Global.ApplyProfile`, `PlayMacro`, 直接 `outputKBMHandler` 呼び出しなど）。

検索ワード: `PerformKeyPress`, `PerformKeyPressAlt`, `PerformMouseButtonEvent`, `PerformMouseWheelEvent`, `PerformKeyRelease`, `PlayMacro`, `Global.ApplyProfile`, `outputKBMHandler` など。

## 要約
- 大量の直接呼び出しが `DS4Windows/DS4Control/Mapping.cs` に存在します。多くは `outputKBMHandler` 経由のキーボード/マウス操作や `PlayMacro`、`Global.ApplyProfile` 呼び出しです。ログにも `ApplyProfile` 呼び出しの痕跡があります。
- `Process.Start` の明示的なヒットは今回の検索結果では見つかりませんでした（必要なら追加の検索を行います）。

## 見つかった主な呼び出し箇所（カテゴリ別）

**outputKBMHandler を使った直接出力（キーボード/マウス）**
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1272) — `outputKBMHandler.PerformMouseButtonEvent(outputKBMMapping.MOUSEEVENTF_LEFTDOWN);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1278) — `outputKBMHandler.PerformMouseButtonEvent(outputKBMMapping.MOUSEEVENTF_RIGHTDOWN);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1284) — `outputKBMHandler.PerformMouseButtonEvent(outputKBMMapping.MOUSEEVENTF_MIDDLEDOWN);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1290) — `outputKBMHandler.PerformMouseButtonEventAlt(..., 1);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1296) — `outputKBMHandler.PerformMouseButtonEventAlt(..., 2);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1302) — `outputKBMHandler.PerformMouseButtonEvent(outputKBMMapping.MOUSEEVENTF_LEFTUP);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1304) — `outputKBMHandler.PerformMouseButtonEvent(outputKBMMapping.MOUSEEVENTF_RIGHTUP);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1306) — `outputKBMHandler.PerformMouseButtonEvent(outputKBMMapping.MOUSEEVENTF_MIDDLEUP);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1310) — `outputKBMHandler.PerformMouseButtonEventAlt(..., 2);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1384) — `outputKBMHandler.PerformMouseWheelEvent(...);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1410) — `outputKBMHandler.PerformMouseWheelEvent(wheel, 0);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1725) — `outputKBMHandler.PerformKeyPressAlt(nativeKey);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1743) — `outputKBMHandler.PerformKeyPress(nativeKey);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1776) — `outputKBMHandler.PerformKeyPressAlt(nativeKey);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1796) — `outputKBMHandler.PerformKeyPress(nativeKey);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1817) — `outputKBMHandler.PerformKeyReleaseAlt(nativeKey);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1831) — `outputKBMHandler.PerformKeyRelease(nativeKey);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1853) — `outputKBMHandler.Sync();`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L3944) — `outputKBMHandler.MoveRelativeMouse(mouseDeltaX, mouseDeltaY);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L3979) — `outputKBMHandler.MoveAbsoluteMouse(outX, outY);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L3995) — `outputKBMHandler.MoveAbsoluteMouse(releaseX, releaseY);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L6514) — macro 内での `PerformMouseButtonEvent` など
- ほか同ファイル内に多数の `PerformKeyPress`, `PerformKeyRelease`, `PerformMouseButtonEvent`, `PerformMouseWheelEvent` 呼び出しがあります（Mapping.cs 全域）。

**直接の ActionManager 呼び出し周り（DI 経由での Dispatch は存在するが、fallback としての直接呼び出しも確認）**
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1551) — `handled = ActionManager.DispatchTriggerReleased(..., outputKBMHandler);`（Mapping 内で OutputHandler を渡している箇所）
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L1639) — `handled = ActionManager.DispatchTriggerReleased(..., outputKBMHandler);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L4239) — `ActionManager.DispatchTriggerEstablished(...)`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L4323) — `ActionManager.DispatchTriggerReleased(...)`

**PlayMacro（マクロ再生）の直接呼び出し / マクロタスク**
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L4867) — `PlayMacro(device, macroControl, string.Empty, null, action.actionMacro, dcs.control, keyType);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L5605) — `PlayMacro(device, macroControl, String.Empty, action.macro, null, DS4Controls.None, keyType, action, null);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L5631) — `PlayMacro(...)`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L6125) — `PlayMacro(...)`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L6151) — `PlayMacro(...)`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L6175) — `PlayMacro(...)`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L6361) — `private static void PlayMacro(...)` 実装
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L6367) — `Task.Factory.StartNew(() => PlayMacroTask(...));`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L6374) — `Task.Factory.StartNew(() => PlayMacroTask(...));`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L6378) — `PlayMacroTask` 呼び出し周り
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L6430) — `PlayMacroCodeValue` 呼び出し

**Global.ApplyProfile（プロファイル切替）の直接呼び出し**
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L5564) — `Global.ApplyProfile(device, action.details, false, true, ctrl,` などの呼び出しが存在
- ログ（Program Files）にも `ApplyProfile` 呼び出しの痕跡が多数あります（例: `c:/Program Files/DS4Windows/Logs/ds4windows_log_20251219_9.txt`）
  - [Logs/ds4windows_log_20251219_9.txt](Logs/ds4windows_log_20251219_9.txt#L67)

**その他（キーボード関連の release/sync 等）**
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L6258) — `outputKBMHandler.PerformKeyRelease((ushort)dcs.action.actionKey);`
- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L6369) — `macroTaskQueue[...] = prevTask.ContinueWith((x) => PlayMacroTask(...));`
- さらに Mapping.cs 内のマクロ処理で `PerformKeyPress/Release` 系が多く出現します。

## メモ / 次のステップ提案
- 最重要: `DS4Windows/DS4Control/Mapping.cs` に集中しているため、まずこのファイル内の各直接呼び出しを1件ずつラップ（ActionManager/TriggerContext 経由に置換）する計画を作るのが効率的です。
- `Process.Start` のような別呼び出しが見つからないか追加検索が必要なら実行します。
- 望むなら、上記の各リンクについて「DI経由に置き換えるための小さなパッチ案」を順次作成します。

---

ファイル保存先: `g:/Cursor_Folder/DS4Windows-Vader4Pro/docs/Direct-Callsites-Inventory.md`

もしさらに深掘り（例えば `Process.Start`、`System.Diagnostics.Process`、あるいは追加ファイルの完全列挙）を行う場合、次にどのパターンを優先して走査しますか？

## `Process.Start` / `System.Diagnostics.Process` 検出結果
以下はリポジトリ内で見つかった `Process.Start` / `System.Diagnostics.Process` 関連の主要な出現箇所です。これらは外部プロセスを直接起動またはプロセス情報を参照するため、移行の観点で重要です。

- [DS4Windows/DS4Control/Mapping.cs](DS4Windows/DS4Control/Mapping.cs#L5459) — `System.Diagnostics.Process specActionLaunchProc = new System.Diagnostics.Process();`
- [DS4Windows/DS4Forms/MainWindow.xaml.cs](DS4Windows/DS4Forms/MainWindow.xaml.cs#L1551) — `using (Process temp = Process.Start(startInfo))`
- [DS4Windows/DS4Forms/MainWindow.xaml.cs](DS4Windows/DS4Forms/MainWindow.xaml.cs#L1560) — `Process.Start("control", "joy.cpl");`
- [DS4Windows/DS4Forms/MainWindow.xaml.cs](DS4Windows/DS4Forms/MainWindow.xaml.cs#L1580) — `using (Process temp = Process.Start(startInfo))`
- [DS4Windows/DS4Forms/MainWindow.xaml.cs](DS4Windows/DS4Forms/MainWindow.xaml.cs#L1982) — `using (Process proc = Process.Start(startInfo)) { }`
- [DS4Windows/DS4Forms/MainWindow.xaml.cs](DS4Windows/DS4Forms/MainWindow.xaml.cs#L2004) — `using (Process proc = Process.Start(path)) { }`
- [DS4Windows/UpdaterInstaller.cs](DS4Windows/UpdaterInstaller.cs#L77) — `var proc = Process.Start(psi);`
- [DS4Windows/UpdaterInstaller.cs](DS4Windows/UpdaterInstaller.cs#L171) — `var proc = Process.Start(psi);`
- [DS4Windows/DS4Forms/WelcomeDialog.xaml.cs](DS4Windows/DS4Forms/WelcomeDialog.xaml.cs#L209) — `monitorProc = Process.Start(startInfo);`
- [DS4Windows/DS4Forms/WelcomeDialog.xaml.cs](DS4Windows/DS4Forms/WelcomeDialog.xaml.cs#L268) — `Process.Start("http://www.microsoft.com/accessories/en-gb/d/xbox-360-controller-for-windows");`
- [DS4Windows/DS4Forms/WelcomeDialog.xaml.cs](DS4Windows/DS4Forms/WelcomeDialog.xaml.cs#L273) — `Process.Start("control", "bthprops.cpl");`
- [DS4Windows/DS4Forms/WelcomeDialog.xaml.cs](DS4Windows/DS4Forms/WelcomeDialog.xaml.cs#L327) — `monitorProc = Process.Start(startInfo);`
- [DS4Windows/DS4Forms/WelcomeDialog.xaml.cs](DS4Windows/DS4Forms/WelcomeDialog.xaml.cs#L433) — `monitorProc = Process.Start(startInfo);`
- [externals/DS4Updater/Updater2/Util.cs](externals/DS4Updater/Updater2/Util.cs#L49) — `using (Process p = Process.Start(psi)) { }`（フォールバックでの `Process.Start(path)` 呼び出しもあり）
- [externals/DS4Updater/Updater2/Util.cs](externals/DS4Updater/Updater2/Util.cs#L65) — `try { Process.Start(path); } catch { }`
- [DS4Windows/DS4Forms/ViewModels/MainWindowsViewModel.cs](DS4Windows/DS4Forms/ViewModels/MainWindowsViewModel.cs#L133) — `using (Process temp = Process.Start(startInfo))`
- [DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs](DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs#L3698) — `System.Diagnostics.Process.Start(defaultBrowserCmd, ...)`
- [DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs](DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs#L3703) — `using (System.Diagnostics.Process temp = System.Diagnostics.Process.Start(startInfo))`
- [DS4Windows/DS4Forms/ViewModels/TrayIconViewModel.cs](DS4Windows/DS4Forms/ViewModels/TrayIconViewModel.cs#L257) — `using (Process temp = Process.Start(startInfo))`
- [externals/DS4Updater/Updater2/MainWindow.xaml.cs](externals/DS4Updater/Updater2/MainWindow.xaml.cs#L192) — `string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;`
- [externals/DS4Updater/Updater2/MainWindow.xaml.cs](externals/DS4Updater/Updater2/MainWindow.xaml.cs#L199) — `var psi = new System.Diagnostics.ProcessStartInfo(exePath, arguments)`
- [externals/DS4Updater/Updater2/MainWindow.xaml.cs](externals/DS4Updater/Updater2/MainWindow.xaml.cs#L209) — `System.Diagnostics.Process.Start(psi);`
- [externals/DS4Updater/Updater2/MainWindow.xaml.cs](externals/DS4Updater/Updater2/MainWindow.xaml.cs#L1044) — `using (Process tempProc = Process.Start(startInfo)) { }`
- [externals/DS4Updater/Updater2/MainWindow.xaml.cs](externals/DS4Updater/Updater2/MainWindow.xaml.cs#L1096) — `try { Process.Start(Path.Combine(ds4WindowsDir, "DS4Windows.exe")); } catch { }`
- [externals/DS4Updater/Updater2/App.xaml.cs](externals/DS4Updater/Updater2/App.xaml.cs#L250) — `using (Process tempProc = Process.Start(startInfo))`
- [externals/DS4Updater/Updater2/App.xaml.cs](externals/DS4Updater/Updater2/App.xaml.cs#L259) — `try { Process.Start(finalLaunchExePath); } catch { }`
- [externals/DS4Updater/Updater2/App.xaml.cs](externals/DS4Updater/Updater2/App.xaml.cs#L516) — `Process.Start(psi);`
- [DS4Windows/DS4Forms/UpdaterWindow.xaml.cs](DS4Windows/DS4Forms/UpdaterWindow.xaml.cs#L93) — `string ds4WindowsExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;`
- [DS4Windows/DS4Forms/UpdaterWindow.xaml.cs](DS4Windows/DS4Forms/UpdaterWindow.xaml.cs#L102) — `var psi = new System.Diagnostics.ProcessStartInfo(ds4UpdaterExe)`
- [DS4Windows/DS4Forms/UpdaterWindow.xaml.cs](DS4Windows/DS4Forms/UpdaterWindow.xaml.cs#L102) — `var proc = System.Diagnostics.Process.Start(psi);`
- [DS4Windows/DS4Forms/UpdaterWindow.xaml.cs](DS4Windows/DS4Forms/UpdaterWindow.xaml.cs#L236) — `var proc2 = System.Diagnostics.Process.Start(psi2);`
- [DS4Windows/DS4Control/ScpUtil.cs](DS4Windows/DS4Control/ScpUtil.cs#L6694) — `System.Diagnostics.Process[] localAll = System.Diagnostics.Process.GetProcesses();`
- [DS4Windows/DS4Control/ScpUtil.cs](DS4Windows/DS4Control/ScpUtil.cs#L6716) — `System.Diagnostics.Process tempProcess = new System.Diagnostics.Process();` / `tempProcess.Start();`
- [DS4Windows/DS4Control/Util.cs](DS4Windows/DS4Control/Util.cs#L245) — `using (Process temp = Process.Start(startInfo))`
- [DS4Windows/DS4Control/Util.cs](DS4Windows/DS4Control/Util.cs#L282) — `using (Process temp = Process.Start(startInfo)) { }`
- [DS4Windows/DS4Control/Util.cs](DS4Windows/DS4Control/Util.cs#L329) — `using (Process temp = Process.Start(startInfo))`
- [DS4Windows/DS4Control/ControlService.cs](DS4Windows/DS4Control/ControlService.cs#L637) — `Process child = Process.Start(startInfo);`
- [DS4Windows/DS4Control/ControlService.cs](DS4Windows/DS4Control/ControlService.cs#L2335) — `try { tempProcess.Start(); }`

### メモ
- 多数は Updater / Updater2（`externals/DS4Updater`）や UI 系（`DS4Forms`）での起動処理です。これらはアプリの起動・再起動・外部ツール起動を目的としているため、単純に DI 経由へ置換するかはコンテキスト次第です。
- ただし `Mapping.cs` の `specActionLaunchProc`（SA に紐づく外部起動）は SpecialAction の副作用なので、Action/Controller 層で扱うべき候補です。

---

（追記完了）
