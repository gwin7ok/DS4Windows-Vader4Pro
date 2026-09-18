# サービス別紙 個別参照展開（タスク3）

作成日: 2026-09-18
対象: Service-Reference-Evidence（未検証メモ）
状態: 概数・範囲表記を個別参照へ展開中、コア/UI別紙との照合中

## 展開対象（サービス領域の未検証参照）

| サービス/クラス | 参照元ファイル | 行範囲（概数） | 分類（暫定） | 備考 |
|---|---|---|---|---|
| ProfileApplicationService | ControlService.cs | 2176〜2178 | b（契約差） | ApplyProfileToSlot経路、4スロット固定 |
| SpecialAction編集 | ProfileEditor.xaml.cs / Services | （別紙未確定） | b（要設計） | GetAction/LoadActions正規化差 |
| ConfigurationLocation | App.xaml.cs / Services | （別紙未確定） | b（要設計） | 保存場所とXMLパス境界 |
| StandardActionInit | Services / Mapping | （別紙未確定） | b（要設計） | 複数XML更新・再読込 |
| IKbmBackendLifecycle | DS4Control/Services | （別紙未確定） | b（要設計） | ハンドラ生成・交換・破棄順序 |
| IDisplayCoordinateService | DS4Control/Services | （別紙未確定） | b（要設計） | 座標変換・画面構成変更 |

## 分類矛盾解消（コア/UI別紙との照合）

- コア別紙（ControlService/Mapping/Mouse）とサービス別紙の参照範囲が重複する場合、ファイル+行の一意ID（タスク1）で統合する。
- UI別紙（App/MainWindow/ProfileEditor/SettingsVM）とサービス別紙の参照は、Pre-Host経路（c: 除外）とHost後経路（a/b: 移行対象）を分離して記録する。
- サービス内部（フォルダ名だけで除外しない）の静的呼出しと、意図的な同一実体への委譲を区別する（報告書§5維持）。

## 未完了

- 各サービス参照の個別行番号と一意IDの完全記録
- コア/UI別紙との完全照合（重複ゼロ、漏れゼロの証明）
- 分類別実測集計（タスク4）への統合
