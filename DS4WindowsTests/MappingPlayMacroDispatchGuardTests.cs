using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xunit;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// 「□ホールドのマウス左クリック連打マクロで、Controls タブ直接割り当て（action == null）の
    /// 場合に PlayMacro の二重実行防止ガードが素通りになり、実行中／クールダウン中でも
    /// 入力ポーリングのたびに新しい Task を生成しようとしていた」バグの暫定対策
    /// （control 単位の macroDispatchInFlight フラグ）を検証する。
    ///
    /// 【暫定対策・恒久対応の申し送り】この対策は Mapping.cs の場当たり的な修正であり、
    /// 根本対応（トリガー判定層とマクロ実行層の分離、Controls/SpecialActions の区別なく
    /// 単一の実行層に一本化）は Phase7（DI-App-Wide-Migration-Plan.md §6.9）で行う。
    /// 詳細は Mapping.cs 内の macroDispatchInFlight 宣言部のコメントを参照。
    /// </summary>
    public class MappingPlayMacroDispatchGuardTests
    {
        // テスト間の副作用を避けるため、他のテストが使わなそうな控えめなボタンを使う。
        private const DS4Controls TestControl = DS4Controls.SwipeLeft;

        private static readonly MethodInfo PlayMacroMethod = typeof(Mapping).GetMethod(
            "PlayMacro", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly FieldInfo DispatchFlagsField = typeof(Mapping).GetField(
            "macroDispatchInFlight", BindingFlags.Static | BindingFlags.NonPublic);

        private static bool[] DispatchFlags => (bool[])DispatchFlagsField.GetValue(null);

        private static void InvokePlayMacro(DS4Controls control, DS4KeyType keyType, SpecialAction action = null)
        {
            Assert.NotNull(PlayMacroMethod); // メソッド名変更等でシグネチャが失われた場合に検出する

            // macroLst を空リストにすることで、PlayMacroCodeValue（実際のキー/マウス送信）を
            // 一切呼ばせずに、PlayMacro/PlayMacroTask のガード・後片付けロジックだけを検証する。
            PlayMacroMethod.Invoke(null, new object[]
            {
                /* device */ 0,
                /* macrocontrol */ null,
                /* macroStr */ string.Empty,
                /* macroLst */ new List<int>(),
                /* macroArr */ null,
                /* control */ control,
                /* keyType */ keyType,
                /* action */ action,
                /* actionDoneState */ null
            });
        }

        [Fact]
        public void PlayMacro_SetsControlDispatchFlag_SynchronouslyBeforeReturning()
        {
            int idx = Mapping.DS4ControltoInt(TestControl);
            DispatchFlags[idx] = false;

            InvokePlayMacro(TestControl, DS4KeyType.HoldMacro);

            // Task はバックグラウンドで動くが、フラグの設定自体は PlayMacro 内で
            // 同期的（Task 生成前）に行われるため、Invoke から戻った直後に true になっている。
            Assert.True(DispatchFlags[idx]);

            // クリーンアップ: 後続のクールダウン（Task.Delay(50)）が明けるまで待ってから片付ける。
            Thread.Sleep(300);
            DispatchFlags[idx] = false;
        }

        [Fact]
        public void PlayMacro_SecondCallWhileFirstStillDispatching_DoesNotResetOrOverlap()
        {
            int idx = Mapping.DS4ControltoInt(TestControl);
            DispatchFlags[idx] = false;

            // 1回目: HoldMacro のクールダウン（50ms）により、しばらく true のままになる。
            InvokePlayMacro(TestControl, DS4KeyType.HoldMacro);
            Assert.True(DispatchFlags[idx]);

            // 2回目（同じ control）: ボタンを押し続けている間、入力ポーリングのたびに
            // PlayMacro が再度呼ばれる状況を模擬。ガードにより新たな実行状態を
            // 作らず、既に true のフラグはそのまま維持されるべき（＝Task を追加生成しない）。
            InvokePlayMacro(TestControl, DS4KeyType.HoldMacro);
            Assert.True(DispatchFlags[idx]);

            // 十分待てば、1回目（で生成された唯一の Task）の完了によりフラグは false に戻る。
            Thread.Sleep(300);
            Assert.False(DispatchFlags[idx]);
        }

        [Fact]
        public void PlayMacro_FlagClearsAfterDispatchCompletes()
        {
            int idx = Mapping.DS4ControltoInt(TestControl);
            DispatchFlags[idx] = false;

            InvokePlayMacro(TestControl, DS4KeyType.None); // HoldMacro なし＝即座に完了する一発マクロ

            Thread.Sleep(300);

            Assert.False(DispatchFlags[idx]);
        }

        [Fact]
        public void PlayMacro_SpecialActionPath_ControlNone_NeverTouchesDispatchFlags()
        {
            // SpecialAction 経由の呼び出しは常に control == DS4Controls.None で行われるため、
            // 本対策のガードは一切関与しない（controlIndexValid が false になり lock ブロックを
            // 通らない）ことを、例外が発生しないこと・配列が変化しないことで確認する。
            var before = (bool[])DispatchFlags.Clone();

            var ex = Record.Exception(() =>
            {
                InvokePlayMacro(DS4Controls.None, DS4KeyType.None);
                InvokePlayMacro(DS4Controls.None, DS4KeyType.None);
            });

            Assert.Null(ex);
            Assert.Equal(before, DispatchFlags);
        }
    }
}