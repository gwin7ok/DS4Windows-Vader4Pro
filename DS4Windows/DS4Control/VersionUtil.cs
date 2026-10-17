using System;

namespace DS4Windows
{
    /// <summary>
    /// Phase7-Step10c W3（Q1＝A）: バージョン文字列の数値化（旧: <c>Global.CompileVersionNumberFromString</c>／<c>CompileVersionNumber</c>。本体は同一）。
    /// スキップしたバージョンの比較（<c>BackingStore.PostProcessLoad</c>、<c>AppSettingsXmlSerializer.Load</c>、<c>AppSettingsService</c>）が使う。
    /// </summary>
    public static class VersionUtil
    {
        public static ulong CompileVersionNumberFromString(string versionStr)
        {
            ulong result = 0;
            try
            {
                Version tmpVersion = new Version(versionStr);
                result = CompileVersionNumber(tmpVersion.Major, tmpVersion.Minor,
                    tmpVersion.Build, tmpVersion.Revision);
            }
            catch (Exception) { }

            return result;
        }

        public static ulong CompileVersionNumber(int majorPart, int minorPart,
            int buildPart, int privatePart)
        {
            ulong result = (ulong)majorPart << 48 | (ulong)minorPart << 32 |
                (ulong)buildPart << 16 | (ushort)privatePart;
            return result;
        }
    }
}
