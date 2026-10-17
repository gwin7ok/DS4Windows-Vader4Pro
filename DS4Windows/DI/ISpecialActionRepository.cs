using System;
using System.Collections.Generic;

namespace DS4Windows.DI
{
    public interface ISpecialActionRepository
    {
        string ActionsPath { get; }
        IReadOnlyList<SpecialAction> Actions { get; }
        List<SpecialAction> ActionList { get; }

        bool LoadActions();
        // Phase7-Step2 W7: SaveActions／GetAction／ActionExists は契約から外した（本体の呼び出しが 0 件。受け手が別の型だったため、
        // 名前の衝突で前回の走査から漏れていた）。保存は SaveAction／RemoveActionAndSave、取得は GetActionOrPlaceholder を使う。
        // 実装の GetAction／ActionExists は、RemoveActionAndSave の内部判定とテストの確認で使うため、クラスには残した。

        // Phase7-Step2 W5・W6: AddAction／ReplaceAction／RemoveAction／GetActionIndex は削除した。AddAction／ReplaceAction／RemoveAction は、
        // メモリ上のリストだけを変える並行実装で、本体の保存経路（SaveAction／RemoveActionAndSave＝Actions.xml への保存と読み直し）と別物だった。
        // GetActionIndex は、AddAction などの内部でだけ使われていた。
        // 参照: docs-forDIMG/MadeByAgent/Phase7-Step2-Plan.md §2.4

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消（Global への薄い委譲）----
        /// <summary>
        /// Actions.xml を読み込めなかったときの復旧処理（<c>Global.CreateStdActions</c>）。
        /// 各プロファイルの ProfileActions に「Disconnect Controller」を追加し、アクションを読み込み直す。
        /// </summary>
        void CreateStandardActions();

        // ---- Phase6-Step8-1: ProfileEditor.xaml.cs の Global 直接参照解消（契約差の吸収）----
        /// <summary>
        /// 名前（前後の空白を無視、大文字小文字を無視）でアクションを取得する。見つからないときは
        /// <c>null</c> ではなく placeholder（<c>new SpecialAction("null", "null", "null", "null")</c>）を返す。
        /// <c>Global.GetAction</c> と同じ契約。<c>GetAction</c> は前後の空白を無視せず、見つからないと
        /// <c>null</c> を返すため、置換できない（Phase6-Step8 §0A.1-4）。
        /// </summary>
        SpecialAction GetActionOrPlaceholder(string actionName);

        // ---- Phase6-Step10-3-1: スペシャルアクション編集画面の Global 直接参照解消（Global への薄い委譲）----

        /// <summary>
        /// アクションを保存する（<c>Global.SaveAction</c> と同じ動作）。
        /// 実行中のアクション状態のクリア、<c>Actions.xml</c> への書き込み、読み直しと件数の検証、
        /// キー種別（<paramref name="mode"/>＝4）のランタイム状態のリセットまでを含む。
        /// </summary>
        /// <param name="mode">種別（1:マクロ、2:プログラム、3:プロファイル、4:キー、5:切断、6:バッテリー、7:複数アクション、8:ステアリング校正、9:ジャイロ校正、10:ボタン）</param>
        void SaveAction(string name, string controls, int mode, string details, bool edit,
            double delayTime = 0.0, string extras = "");

        /// <summary>
        /// アクションを削除して <c>Actions.xml</c> へ保存する（<c>Global.RemoveAction</c> と同じ動作）。
        /// 実行中のアクション状態のクリア、ファイルへの保存、読み直しと件数の検証を含む。
        /// </summary>
        /// <returns>削除前に、その名前のアクションが存在していれば true</returns>
        bool RemoveActionAndSave(string actionName);

        event EventHandler ActionsChanged;
    }
}
