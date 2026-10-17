# バグ修正記録: PlayMacroDirect の引数誤り（通常マクロが実行されない）

## 発生日
2026-08-28（実機回帰テスト チェックリスト項目 5-1, 5-2 で発見）

## 症状
Record a macro メニューで作成した通常マクロが実行されない。ログには対象マクロ名やMacroAction.Executeの実行ログは出力されるが、実際のキー/マウス出力信号が一切送信されない。マルチアクションボタンメニューで作成したマクロは正常に動作する。

## 原因
Mapping.PlayMacroDirect が PlayMacro 本体を呼び出す際、macroStr に action.name（マクロの名前文字列、例: TestMacro1）を渡し、同時に macroArr にも空配列（null ではない new int[0]）を渡していた。
PlayMacro のコメントには macroStr / macroLst / macroArr のうちどれか1つだけが有効値を持つべきと明記されているが、この呼び出しはmacroStr, macroLst, macroArr の3つに同時に値らしきものを渡してしまっていた。

実際の再生処理 PlayMacroTask 内では、macroStr が空でない場合、それを最優先して '/' 区切りで int.Parse しようとする。
    if (!String.IsNullOrEmpty(macroStr))
    {
        skeys = macroStr.Split('/');
        macroArr = new int[skeys.Length];
        for (int i = 0; i < macroArr.Length; i++)
            macroArr[i] = int.Parse(skeys[i]);
    }

macroStr に action.name（例: TestMacro1）が渡されるため、数字以外の文字を含む通常のマクロ名では int.Parse が確実に FormatException を送出する。この処理はバックグラウンドタスク内かつ try/catch の外で実行されるため、例外は未観測のままサイレントに消え、後続のキー送出コードに到達しない。

## 修正内容
DS4Windows/DS4Control/Mapping.cs の PlayMacroDirect 内、PlayMacro 呼び出し1箇所のみをピンポイント置換。
修正前: PlayMacro(device, new bool[4], action.name, action.macro, new int[0], DS4Controls.None, action.keyType, action, null);
修正後: PlayMacro(device, new bool[4], String.Empty, action.macro, null, DS4Controls.None, action.keyType, action, null);

既存の直接呼び出し（MultiAction経路、修正前のフォールバックコード）と同じ引数の渡し方（macroStr=String.Empty, macroArr=null）に揃えることで、macroLst（action.macro）のみが有効なマクロ定義source として扱われるようにした。

## 確認事項
- [ ] dotnet build ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj -c Debug /p:platform=x64 -o ./DS4WindowsTests/bin/x64/Debug がエラーなく完了すること
- [ ] dotnet publish ./DS4Windows/DS4WinWPF.csproj -c Release /p:platform=x64 ... がエラーなく完了すること
- [ ] 実機でチェックリスト項目 5-1（通常マクロ再生）が Pass になること
- [ ] 実機でチェックリスト項目 5-2（ホールドマクロ/中断）が Pass になること
