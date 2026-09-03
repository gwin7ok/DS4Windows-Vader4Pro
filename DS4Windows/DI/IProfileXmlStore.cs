using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// プロファイルXMLの純粋な読込・保存(実I/O)のみを表す契約。
    /// 状態調整ロジック(一時プロファイルフラグ等)は含まない。
    /// 実装は BackingStore への薄い委譲ラッパーとする(Phase5-Step2)。
    /// </summary>
    public interface IProfileXmlStore
    {
        bool LoadProfileXml(int deviceIndex, bool launchProgram, ControlService control,
            string overridePath = "", bool xinputChange = true, bool postLoad = true);

        bool SaveProfileXml(int deviceIndex, string profileName);
    }
}