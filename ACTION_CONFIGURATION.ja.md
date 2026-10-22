# Inspector で行動をボタンに割り当てる

## 設定済みアセット

Assets/Settings/BattleActions に Attack / Defend / StrongAttack の3つの行動定義があります。
各アセットは BattleActionDefinition（ScriptableObject）です。Inspector で以下を設定できます。

- Action：実行する既存の行動（Attack / Defend / StrongAttack）
- Display Name：ボタンの表示名
- Description：説明文

行動のダメージ計算やクールダウンなどのルールは Domain が担当します。このアセットは既存の行動と表示の定義です。新しい種類の戦闘ルールを追加する場合は Domain / UseCase 側の実装とテストが必要です。

## 既存ボタンの行動を変える

1. Assets/Prefabs/UI/Battle/BattleHudView.prefab を開く。
2. AttackButton / DefendButton / StrongAttackButton のいずれかを選択。
3. BattleActionButton コンポーネントの Definition に、使いたい行動定義アセットをドラッグする。
4. 保存して Play。ボタンの表示名・実行する行動・クールダウン・使用可否が割り当てに従って切り替わります。

例えば AttackButton の Definition を Defend に変えると、そのボタンも防御を実行します。GameObject の名前や配置順には依存しません。同じ行動を複数ボタンに割り当てることもできます。

## 定義を追加する

Project ウィンドウで右クリック → Create > Battle Demo > Action Definition。
Action / Display Name / Description を設定し、ボタンの Definition に割り当てます。既存アセットを複製して表示名だけ変えることもできます。

## ボタンを追加・並べ替える

HUD 内の設定済みボタンを複製して配置し、Definition を差し替えてください。BattleHudView の配下であれば階層を増やしても、画面を開く際に自動検出されます。配置と大きさは通常の RectTransform で調整できます。HUD コードやボタンの固定リストを編集する必要はありません。

新規の Unity UI Button から作る場合は BattleActionButton を追加し、Definition と Label（TMP_Text）を設定します。Label が空の場合は子の TMP_Text を自動取得します。
Description 欄には説明文を表示したい TMP_Text を任意で指定できます。別ボタンと同じ説明用 Text を共有すると上書きされるため、各ボタン専用の Text を使用してください。
Button の On Click() に手動でメソッドを登録する必要はありません。追加済みの実行処理があると二重実行につながるため、On Click() は空にしてください。

定義が未設定・未知の行動・敵ターン・戦闘終了・クールダウン中の場合はボタンを無効にします。HUD を閉じるとイベントと R3 の購読を解除し、再び開く際に結び直します。

## データとコードの流れ

BattleActionDefinition → BattleActionButton → HUD の共通コールバック → PlayPlayerTurnUseCase → Domain

使用可否と残りクールダウンは Domain → BattleViewModel → 各 BattleActionButton に反映します。View は各行動のルールを判定しません。
行動定義は HUD Prefab から直接参照されるため、Addressables では Prefab の依存アセットとして含まれます。個別の Addressable 登録は不要です。既にバンドルをビルドした場合は再ビルドしてください。

## 確認範囲

従来の42ケースに、共通コマンド状態の7ケースを追加し、.NET 上で合計49ケース成功しています。未初期化・使用可否・クールダウン・敵ターン・終了・再戦・未知の行動を確認しました。Unity 上の Inspector 操作、コンポーネントの実際の購読・クリック・画面表示は未確認です。
