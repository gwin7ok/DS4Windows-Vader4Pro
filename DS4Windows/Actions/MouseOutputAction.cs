using System;
using DS4Windows.DS4Control;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// マウス出力アクション（IOutputAction 実装）。
    /// IVirtualKBM 経由でマウスイベントを出力する。
    /// </summary>
    /// <remarks>
    /// TODO(技術的負債・未完成のため DI に登録しない／Phase8): このクラスの Execute／Stop は、IVirtualKBM を取得するだけで
    /// マウスイベントを何も出力しない（Phase1-C2 で作られたが、出力の実装は未接続のまま）。
    /// 現在、ServiceRegistration にも DefaultActionFactory／ActionFactory にも登録されていないため、
    /// Mapping.cs の「C2 MouseOutputAction integration」（2 箇所）の sp.GetService(typeof(MouseOutputAction)) は
    /// 常に null になり、従来の VirtualKBM.PerformMouseButtonEvent の経路で動いている（実害なし）。
    /// ★登録してはならない。登録すると mouseHandled=true となり、左クリックなどが出なくなる。
    /// 完成させる時期: Phase8（Controls／SpecialActions 統一トリガーディスパッチ。Phase8-Unified-Trigger-Dispatch-Plan.md）で、
    /// マウスボタン・ホイール・移動の出力を統一経路へ移すときに、PerformMouseButtonEvent などの呼び出しを移植して完成させる。
    /// 経緯と判断: Phase6-Step9b-Plan.md F03（Step9b-0 の検証で分類B）。
    /// </remarks>
    public class MouseOutputAction : IOutputAction
    {
        private readonly SpecialAction sa;
        private readonly IVirtualKBM _virtualKBM;

        public MouseOutputAction(SpecialAction sa, IVirtualKBM virtualKBM)
        {
            this.sa = sa;
            this._virtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM)); // Phase6-Step10-4-4: 必須の依存
        }

        public string Id => sa?.name ?? "MouseOutput";

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;

                var kbm = _virtualKBM ?? (ctx?.OutputHandler as IVirtualKBM);
                if (kbm == null) return;

                try { AppLogger.LogTrace($"MouseOutputAction.Execute: id={Id} device={ctx?.Device}"); } catch { }
            }
            catch { }
        }

        public void Stop(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;

                var kbm = _virtualKBM ?? (ctx?.OutputHandler as IVirtualKBM);
                if (kbm == null) return;

                try { AppLogger.LogTrace($"MouseOutputAction.Stop: id={Id} device={ctx?.Device}"); } catch { }
            }
            catch { }
        }
    }
}
