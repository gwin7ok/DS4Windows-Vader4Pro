using DS4WinWPF;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase6-Step7-4（決定7＝案H）: アプリの起動引数（<see cref="ArgumentParser"/>）を、DI コンテナ経由で
    /// 利用側（現在は <see cref="ControlService"/> の生成）へ受け渡すための保持役。
    ///
    /// DI コンテナは一度構築すると登録を追加できない。一方、現在の起動順序では、ホストは
    /// <c>Global</c> の静的初期化（<c>ScpUtil.cs</c> の <c>fallbackProfileRepository</c>）の中で、起動引数なしで先に
    /// 暗黙に構築される（<c>Phase6-Step7-Plan.md</c> §0.4 の訂正）。そのため起動引数は「登録」ではなく、
    /// 登録済みのこの保持役へ <c>AppHost.CreateHost(config, parser)</c> が値を設定する形で渡す。
    /// </summary>
    public interface IStartupArguments
    {
        /// <summary>設定済みの起動引数。未設定なら null（起動引数なしのホスト、テストなど）。</summary>
        ArgumentParser Parser { get; }

        /// <summary>起動引数を設定する。<c>AppHost.CreateHost(config, parser)</c> から呼ぶ。</summary>
        void SetParser(ArgumentParser parser);
    }
}
