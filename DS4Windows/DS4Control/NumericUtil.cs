using System.Runtime.CompilerServices;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step9 W2（Q2＝A）: 数値の純粋な計算（旧: <c>Global.Clamp</c>）。<c>Global.Clamp</c> は、この関数へ委譲する薄いシムとして残してある。
    /// 入力 1 回ごとの処理（<c>Mapping</c>）から呼ばれるため、割り当てを行わず、本体は旧実装と同一に保つ。
    /// </summary>
    public static class NumericUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double min, double value, double max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
    }
}
