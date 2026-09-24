# Phase6-Step4 完了報告書: マウスエミュレーション系（`Mouse`／`MouseCursor`／`MouseWheel`）のGlobal直参照解消

作成日: 2026-09-24  
状態: **完了確定**（ビルド・テストビルド・テスト実行成功、実機確認は一部を Step11 へ先送り）  
対象ブランチ: `For-DI-migration-work`  
計画書: `docs-forDIMG/MadeByAgent/Phase6-Step4-Plan.md`  
台帳作成時の HEAD: `c6eb492f`

---

## 1. 結果の要約
- `Mouse.cs`（56行）、`MouseCursor.cs`（19行）、`MouseWheel.cs`（6行）の、計81行の `Global.` 直接参照を、コンストラクタ注入した `IProfileSettingsService`／`IVirtualKBM` 経由に置換した。
- 3ファイルに残る `Global.` 参照は、除外とした `Global.Clamp` の4行のみ（Mouse.cs 2行、MouseCursor.cs 2行。状態を持たない純粋計算）。コメントアウトされた既存コードの `Global.` 参照5行は温存した。
- **フォールバックなしの必須引数**（`AppHost.GetService` や null 既定は採用せず、null なら `ArgumentNullException`）。生成元は `ControlService.cs` の1箇所のみで、そこは既に Pure DI 済みのため。
- 契約（インターフェース）の追加はゼロ。`ServiceRegistration.cs`、`ControlService` のコンストラクタ、モデル図の変更もなし。

## 2. 計画書からの主な変更点（Step4-0 の台帳再作成）
旧計画書（2026-09-18）は、現行コードと次の点で異なっていたため、台帳を現行コードから作り直した。
- 参照メンバーの名前が別物だった（`Global.TouchpadButton[..]` 等の想定に対し、実際は `GetGyroOutMode`、`GyroMouseStickInf`、`TouchOutMode`、`GetDS4CSetting` 等）。
- Abs Mouse 関連（旧 C4-C17〜C19）は、Step3-3 の機能削除で消滅していた。関連シムの追加は不要になった。
- 生成元は `DS4Device.cs` ではなく `ControlService.cs`（旧記載は誤り）。
- 件数は 73 → 81行（Mouse 56／MouseCursor 19／MouseWheel 6）。
- 契約は追加不要（`GetTouchActive` は `GetTouchpadActive`、`GetTouchMouseStickInfo` は `TouchMouseStickInf[..]`、`getTapSensitivity` は `TapSensitivity[..]` で代替）。

## 3. 実施した変更（マイクロステップ）
- **Step4-0**: 台帳の再作成、計画書の全面改訂（コミット `35b5e8ff`／`f78e9a77`）。
- **Step4-1**: `MouseWheel.cs` のコンストラクタを `(int, IProfileSettingsService, IVirtualKBM)` にし、6行を置換。テスト `MouseWheelShimTests`。
- **Step4-2**: `MouseCursor.cs` のコンストラクタを `(int, GyroMouseSens, IProfileSettingsService, IVirtualKBM)` にし、19行を置換。テスト `MouseCursorMovementTests`、共有スタブ `RecordingVirtualKbm`。
- **Step4-3**: `Mouse.cs` のコンストラクタを `(int, DS4Device, IProfileSettingsService, IVirtualKBM)` にし、56行を置換して `cursor`／`wheel` へ伝搬。
- **Step4-4**: `ControlService.cs` の生成箇所を `new Mouse(index, device, _profileSettings, _virtualKBM)` に変更。
- **Step4-5**: テスト `MouseDiWiringTests`、`MouseGlobalReferenceGuardTests`、`MouseCursorHotPathAllocationTests` を新設。

## 4. 挙動の同一性
- 置換前後で、読み取り元は同じ `BackingStore`（`Global.store`）の同じ配列・オブジェクトである。`Global` 側の各 `get*` メソッドがサービスへ委譲、または `m_Config` の同じ配列を返す実装であることを、1件ずつ確認した。
- `Mouse.cs:475` の `GetGyroMouseStickHorizontalAxis(0)`（引数が 0 固定）は、既存挙動を維持するためそのまま移した（持ち越し K4-1）。
- `IVirtualKBM` の実装 `OutputKBMHandlerAdapter` は、`Global.outputKBMHandler` へ呼び出しごとに遅延委譲する。ハンドラの差し替えや null 化があっても、`Mouse` が古い実体を保持し続ける問題は起きない。
- ホットパスへのログ・サービス解決・オブジェクト生成の追加はない。

## 5. 追加したテスト（`DS4WindowsTests`）
- `MouseWheelShimTests`（6件）: null 引数の例外、上下スクロールでの `WHEEL_TICK_*` の乗算、生成後の設定差し替えの反映、感度0で送出なし。
- `MouseCursorMovementTests`（7件）: null 引数、`TouchMoveCursor` の移動出力、感度・反転が渡した設定から読まれること、`disableInvert`、移動量0で送出なし。
- `MouseDiWiringTests`（3件）: null 引数、`Mouse` から `cursor`／`wheel` へ同一インスタンスが伝搬すること。
- `MouseGlobalReferenceGuardTests`（3件）: 3ファイルに `Global.Clamp` 以外の `Global.` 参照と `using static` が再混入しないこと、`Global.Clamp` がちょうど4行であること。
- `MouseCursorHotPathAllocationTests`（1件）: `TouchMoveCursor` の割り当てが0バイトであること。
- `RecordingVirtualKbm`: テスト用スタブ（記録の有無を `Recording` で切替）。
- `Mouse.sixaxisMoved`／`touchesMoved` は、デバイス状態の組み立てが大きいため直接駆動していない。ガードテストと実機確認で担保した。

## 6. 検証結果
- **ビルド・テスト（ユーザー確認、2026-09-24）**: ビルド、テストビルド、テスト実行のすべて成功。コミットしてリモートリポジトリへ反映済み。
- **実機確認（2026-09-24、問題なし）**: タッチパッドでのマウス移動とタップ・クリック、ジャイロマウス、2 本指のスクロール。
- **実機確認の先送り**（`Phase6-Step11-Plan.md` §3.3 に登録済み）: ①タッチパッドのスティック・ホイール等の出力モード、②タッチボタンとクリックパススルー、③ジャイロ→スティック・スワイプ・コントロール出力・トリガー条件、④コントローラーの接続・切断の繰り返し。

## 7. 持ち越し事項
- **K4-1**: `Mouse.cs:475` の `GetGyroMouseStickHorizontalAxis(0)` の引数が 0 固定。他のデバイスでは設定と異なる軸を読む可能性がある既存の挙動。修正要否はユーザー判断。
- **K4-2**: `Global.Clamp`（4行）は除外のまま残る。Step12 の呼出元0件判定でも削除対象にしない。
- **K4-3（新規・Step4 の変更が原因ではない既存仕様の問題）**: コントローラー横の Edit ボタンでプロファイルを開くと、編集内容が保存前に接続中のコントローラーへ即時反映される（Output Mode を Mouse にした瞬間にポインタが動く等）。新設の **Step7b**（`Phase6-Step7b-Plan.md`）で対応する。
- **テストの重複**: 既存の `MockVirtualKBM.cs` と、新設の `RecordingVirtualKbm.cs` は役割が重なる。統合は Step11 以降のテスト整理で検討できる（必須ではない）。

## 8. 完了判定チェックリストの結果
- [x] `Mouse`／`MouseCursor`／`MouseWheel` の全コンストラクタが必須引数の Pure DI 注入になっている
- [x] 3ファイルの `Global.` 参照が、コメントと `Global.Clamp`（4行）を除いて0件
- [x] `ControlService.cs` から `_profileSettings`／`_virtualKBM` が配線されている
- [x] ホットパスの割り当て0バイトがテストで固定されている（`MouseCursor.TouchMoveCursor`）
- [x] ビルド・テストビルド・テスト実行が成功している
- [x] 実機確認が完了、または Step11 の先送り台帳に登録済み
- [x] `Phase6-Status.md`／`Phase6-Step4-Plan.md`／`Phase6-Step11-Plan.md` を更新、完了報告書を作成
