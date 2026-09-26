# バグ修正記録: ProfileSwitchAction.cs の CS1501 エラー

## 発生日
2026-08-28（Step 2-6 テスト実行中に発見）

## 症状
dotnet publish / dotnet build の際、以下のコンパイルエラーが発生。

    DS4Windows\Actions\ProfileSwitchAction.cs(48,34): error CS1501:
    引数 2 を指定するメソッド 'RestoreProfile' のオーバーロードはありません

## 原因
IProfileSwitcher インターフェース（DS4Windows/Actions/IProfileSwitcher.cs）および
その標準実装 DefaultProfileSwitcher（DS4Windows/Actions/DefaultProfileSwitcher.cs）は
RestoreProfile(int deviceIndex) という1引数のみのシグネチャで定義・実装されている。

    public interface IProfileSwitcher
    {
        void SwitchProfile(int deviceIndex, SpecialAction action);
        void RestoreProfile(int deviceIndex);
    }

しかし ProfileSwitchAction.Stop() メソッド内の呼び出しのみが、誤って2引数
（dev, sa）で RestoreProfile を呼び出しており、インターフェース定義と不整合な状態だった。

    修正前（誤り）:
      _profileSwitcher.RestoreProfile(dev, sa);

    修正後（正しい）:
      _profileSwitcher.RestoreProfile(dev);

## 修正内容
DS4Windows/Actions/ProfileSwitchAction.cs の Stop() メソッド内、該当1箇所のみを
ピンポイント置換（.github/copilot-instructions.md §3.2準拠）。
インターフェース定義・実装（IProfileSwitcher.cs / DefaultProfileSwitcher.cs）は変更していない。

## 確認事項
- [ ] dotnet build ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj -c Debug /p:platform=x64 -o ./DS4WindowsTests/bin/x64/Debug がエラーなく完了すること
- [ ] dotnet publish ./DS4Windows/DS4WinWPF.csproj -c Release /p:platform=x64 ... がエラーなく完了すること
- [ ] dotnet test が全件成功すること（Step 2-6完了条件の再検証）