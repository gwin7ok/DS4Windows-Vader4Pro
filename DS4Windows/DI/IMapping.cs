namespace DS4Windows.DI
{
    /// <summary>
    /// Phase7-Step8 W2（Q1＝A）: 入力の評価（<c>Mapping</c>）の窓口。外部の利用者が使うメンバーだけを持つ。
    /// 実装は <c>DS4Windows.Mapping</c>（DI の Singleton。旧: メンバーのほぼすべてが static だった）。
    /// 状態を持たない純粋な関数（<c>getBool*Mapping</c>、<c>remainderCutoff</c> ほか）と定数は、<c>Mapping</c> の static のまま残す（Q2＝A）。
    /// </summary>
    public interface IMapping
    {
        // ---- 入力の評価（ControlService が毎レポート呼ぶ） ----

        /// <summary>入力にカーブ・デッドゾーン・ドリフト補正・ジャイロ→コントロール変換を適用した状態を返す。</summary>
        DS4State SetCurveAndDeadzone(int device, DS4State cState, DS4State dState, IProfileSettingsService settings);

        /// <summary>プロファイルの割り当てを評価して <paramref name="MappedState"/> に書く（通常入力の評価の本体）。</summary>
        void MapCustom(int device, DS4State cState, DS4State MappedState, DS4StateExposed eState, Mouse tp, ControlService ctrl);

        /// <summary>タッチパッドをジョイスティックとして扱うときの補助。</summary>
        void TempMouseJoystick(int device, DS4State MappedState);

        /// <summary>合成キー・マウスの入力を仮想キーボード・マウスへ確定して出力する。</summary>
        void Commit(int device, IProfileSettingsService settings);

        // ---- 画面向け ----

        /// <summary>指定のコントロールが押されているかを、プロファイルの割り当てを考慮して判定する（マクロ記録画面が使う）。</summary>
        bool GetBoolMappingExternal(int device, DS4Controls control, DS4State cState, DS4StateExposed eState, Mouse tp);

        // ---- 切断・マクロ（Actions/* 向け） ----

        /// <summary>切断時の後始末（保持中の合成キーの解放、キュー・タスクのクリア、コントローラーの破棄）。</summary>
        void HandleDeviceDisconnect(int device);

        /// <summary>特殊アクションのマクロを再生する（<c>IMacroPlayer</c> 用）。</summary>
        void PlayMacroDirect(int device, SpecialAction action);

        /// <summary>マクロを停止してキーを解放する（<c>IMacroPlayer</c> 用）。</summary>
        void EndMacroDirect(int device);
    }
}
