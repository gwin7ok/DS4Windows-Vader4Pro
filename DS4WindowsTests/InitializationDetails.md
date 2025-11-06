## 強制初期化の詳細説明

### 💾 「強制初期化」で初期化されるデータ構造

#### 1. actionDone配列の構造
```
actionDone = List<ActionState>

各ActionStateオブジェクト:
┌─────────────────────────────────┐
│ ActionState                     │
│ ├── dev: bool[8]                │
│     ├── dev[0] = false (Device0)│
│     ├── dev[1] = false (Device1)│
│     ├── dev[2] = false (Device2)│
│     ├── dev[3] = false (Device3)│
│     ├── dev[4] = false (Device4)│
│     ├── dev[5] = false (Device5)│
│     ├── dev[6] = false (Device6)│
│     └── dev[7] = false (Device7)│
└─────────────────────────────────┘
```

#### 2. 初期化前後の状態

**【初期化前】（エラーの原因）**
```
// Special Actions: 5個定義済み
actions[0] = "Disconnect Controller"
actions[1] = "Profile Switch Gaming" 
actions[2] = "Volume Up"
actions[3] = "Macro Alt+Tab"
actions[4] = "Battery Check"

// actionDone: 空！
actionDone.Count = 0

// 実行時エラー
actionDone[2].dev[0] ← IndexOutOfRangeException!
```

**【初期化後】（正常動作）**
```
// Special Actions: 5個定義済み
actions[0] = "Disconnect Controller"
actions[1] = "Profile Switch Gaming"
actions[2] = "Volume Up" 
actions[3] = "Macro Alt+Tab"
actions[4] = "Battery Check"

// actionDone: 5個作成済み
actionDone[0] = ActionState { dev: [false×8] }
actionDone[1] = ActionState { dev: [false×8] }
actionDone[2] = ActionState { dev: [false×8] } ← 安全にアクセス可能
actionDone[3] = ActionState { dev: [false×8] }
actionDone[4] = ActionState { dev: [false×8] }

// 正常実行
actionDone[2].dev[0] = true ← 正常動作！
```

#### 3. 初期化処理の詳細ステップ

```csharp
public static void InitializeActionDoneList()
{
    lock (actionDoneLock) // スレッドセーフ
    {
        // Step 1: 二重初期化防止
        if (actionDoneInitialized) return;

        try 
        {
            // Step 2: Special Actions数を取得
            var actions = GetActions();
            int totalActionCount = actions.Count; // 例: 5個

            // Step 3: 既存配列をクリア
            actionDone.Clear(); // Count = 0

            // Step 4: Actions数分のActionStateを作成
            for (int i = 0; i < totalActionCount; i++)
            {
                actionDone.Add(new ActionState());
                // ↓ 内部で以下が実行される
                // new ActionState() {
                //     dev = new bool[8] { false, false, false, false, false, false, false, false }
                // }
            }

            // Step 5: 初期化完了フラグ設定
            actionDoneInitialized = true;

            // Step 6: ログ出力
            AppLogger.LogToGui($"ActionDone list initialized with {totalActionCount} entries", false);
        }
        catch (Exception ex) 
        {
            // Step 7: エラーハンドリング
            AppLogger.LogToGui($"Failed to initialize actionDone list: {ex.Message}", true);
            actionDoneInitialized = false;
        }
    }
}
```

#### 4. メモリ使用量

```
1個のActionState = 8 bytes (bool[8])
Special Actions数 × 8 bytes = 総メモリ使用量

例:
- 10個のSpecial Actions = 80 bytes
- 50個のSpecial Actions = 400 bytes  
- 200個のSpecial Actions = 1,600 bytes

軽量でメモリ効率的！
```

#### 5. 実際の使用例

```csharp
// Special Action実行時
public void ExecuteSpecialAction(int actionIndex, int deviceIndex)
{
    // actionDone[actionIndex].dev[deviceIndex] でアクセス
    
    // 例: Action #2 (Volume Up) をDevice #0 で実行
    if (!actionDone[2].dev[0]) // まだ実行されていない？
    {
        actionDone[2].dev[0] = true; // 実行済みマーク
        
        // Volume Up実行...
        SendKey(VK_VOLUME_UP);
        
        // 後で actionDone[2].dev[0] = false でリセット
    }
}
```