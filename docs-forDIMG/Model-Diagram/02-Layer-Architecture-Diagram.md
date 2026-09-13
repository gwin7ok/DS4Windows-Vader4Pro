# レイヤー構成図（スタック図）
**（Layered Stack Architecture Specification）**

本ドキュメントは、実行時における4層構造＋基盤層の積み重なり、および「制御・依存の方向」と「データフロー」の対比関係を定義する。

---

## 1. 実行時スタック構造図

```text
┌───────────────────────────────────────────────────────────────────────────────────┐
│                             第4層: UI層 (Presentation)                            │
│   WPF Views (Window / UserControl) ──[DataBinding]──> ViewModels (Singleton/Trans)│
│                                    └───> IViewModelFactory (Pure DI Parameter注入) │
└────────────────────────────────────────┬──────────────────────────────────────────┘
                                         │ 依存 (要求・設定・バインド)
                                         ▼
┌───────────────────────────────────────────────────────────────────────────────────┐
│                       第2層: 信号変換層 (Domain / Application)                     │
│  【パイプライン統括】 ControlService                                               │
│  【マッピング・補正】 Mapping / MappingActionDispatcher / StickOutCurve            │
│  【プロファイル統括】 ProfileApplicationService / AutoProfileService               │
│  【アクション管理】   DefaultActionManager / KeyButtonActionController             │
└──────────────┬─────────────────────────────────────────────────────┬──────────────┘
               │                                                     │
 依存 (生データ要求)                                                  │ 依存 (変換済み信号送出)
               ▼                                                     ▼
┌──────────────────────────────────────┐    ┌───────────────────────────────────────┐
│       第1層: 入力監視層 (Ingress)      │    │       第3層: 信号出力層 (Egress)       │
│  IDs4DeviceRegistry                  │    │  3-a 仮想パッド: IOutputSlotService   │
│  ├─ DS4Device (HID/BT/USB)           │    │  ├─ ViGEm Client (Xbox360 / DS4)      │
│  ├─ Vader4ProDevice (Dongle/DInput)  │    │  3-b KBM/マクロ: IVirtualKBM / Player │
│  └─ DualSenseDevice / JoyConDevice   │    │  3-c 副作用: ProcessLauncher / UDP   │
└──────────────────────────────────────┘    └───────────────────────────────────────┘
  ▲                                           │
  │ 【データフロー (Data Pipeline)】           │
  └───────── 生HID入力 ─────────> 変換・計算 ───┴─────────> 仮想パッド/OS入力
                                      │
                                      ▼
┌───────────────────────────────────────────────────────────────────────────────────┐
│                  横断基盤・永続化層 (Cross-Cutting Infrastructure)                 │
│  IProfileXmlStore / IAppSettingsService / IOutputSlotStore / XmlIoLock (ファイルIO) │
│  IPathService / IEnvironmentService / INotificationService (OSサービス)           │
└───────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 各層の責務詳細

| 層番号 | レイヤー名 | 主な構成要素 | 責務と役割 |
| :--- | :--- | :--- | :--- |
| **第4層** | **UI層**<br>(Presentation) | Views (XAML), ViewModels, `IViewModelFactory` | ユーザー入力の受付、プロファイル設定の可視化、接続デバイス状態およびリアルタイム入力値（ControllerReadings）の描画。 |
| **第2層** | **信号変換層**<br>(Domain/App) | `ControlService`, `Mapping`, `MappingActionDispatcher`, `ProfileApplicationService` | アプリケーションの頭脳。入力ループの統括、デッドゾーン/感度曲線計算、ボタンリマップ、スペシャルアクション（マクロ/プロファイル切替）の判定と分配。 |
| **第1層** | **入力監視層**<br>(Ingress) | `IDs4DeviceRegistry`, `DS4Device`, `Vader4ProDevice`, `HidDevice` | 物理コントローラーとの通信（USB/Bluetooth HID）。切断検知、生入力レポート（Raw Report）の継続的読み取り。 |
| **第3層** | **信号出力層**<br>(Egress) | `IOutputSlotService`, `IVirtualKBM`, `IMacroPlayer`, `IProcessLauncher` | 変換後データのOS側への反映。ViGEmBus経由の仮想Xbox360/DS4出力、仮想キーボード・マウス（SendInput/FakerInput）、プロセス起動。 |
| **基盤** | **横断基盤・永続化層**<br>(Infrastructure) | `ProfileXmlStore`, `AppSettingsService`, `OutputSlotStore`, `XmlIoLock`, `PathService` | XML設定ファイルのディスク読み書き、プロセス内・スレッド間排他制御、ファイルパス解決、OS環境情報提供。 |

---

## 3. 「データフロー」と「依存関係」の比較対比

設計理解において混同を防ぐための対比表：

```text
【データの流れる方向 (Data Flow)】
  [物理入力] ──> [1. 入力監視層] ──> [2. 信号変換層] ──> [3. 信号出力層] ──> [仮想パッド/OS]
                                           │
                                           └──(状態通知)──> [4. UI層]

【プログラムの依存方向 (Dependency Flow)】
  [4. UI層] ───────────────┐
                           ▼
  [3. 信号出力層] <──── [2. 信号変換層] ────> [1. 入力監視層]
         │                 │                 │
         └─────────────────┼─────────────────┘
                           ▼
              [横断基盤層 (Interfaces & IO)]
```

* **ホットパスの最適化：**
  毎秒250〜1000回実行されるデータパイプライン（1 $\rightarrow$ 2 $\rightarrow$ 3）内では、DIコンテナからの名前解決（`GetService`）を一切行わない。コンストラクタインジェクション時に注入された参照をフィールド変数としてキャッシュすることで、ガベージコレクション（GC）の発生をゼロに抑える。