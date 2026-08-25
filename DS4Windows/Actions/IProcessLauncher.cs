using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// 外部プロセス起動の抽象化（分類① SpecialAction起動、分類② 権限昇格、分類⑥ 多重起動チェック共通）
    /// §2.1修正版: 新経路（DI経由）とフォールバック（直接 Process.Start）を共存させる
    /// </summary>
    public interface IProcessLauncher
    {
        /// <summary>
        /// 指定されたパスのプロセスを起動する（分類①・⑥共通）
        /// </summary>
        void Launch(string filePath);
    }
}
