# 設計説明：Unity Turn-Based Battle Demo

この資料は、面接で「何を作ったか」だけでなく「なぜこの構造にしたか」を説明するための例です。丸暗記ではなく、実際のコードを開いて自分の言葉で説明できる状態を目指します。

## 1. 30秒で説明する

> Unity 6で制作した、三連戦と無限モードを持つ1対1のターン制バトルです。View、進行処理、ゲームルールを分離し、戦闘ルールはUnity APIへ依存しないDomainとして実装しました。戦闘をまたぐ状態と1戦だけの状態を分け、非同期の敵ターン、シーン遷移、UIの多重操作、Addressablesの解放まで扱っています。英語・日本語、モバイルSafe Area、EditMode／PlayModeテストにも対応しています。

## 2. このデモで示したい能力

- ゲームルールとUnity表示処理の分離
- 状態の所有者と寿命を決める設計
- 非同期処理のキャンセル、再入防止、古い処理の無効化
- DI、イベント、Reactiveな値の適切な使い分け
- モバイルUI、ローカライズ、パフォーマンスへの配慮
- 正常系だけでなく、ロード失敗や二重操作を含むテスト

Cubeは仮素材ではなく、外部ライセンスへ依存せず、ルールとクライアント実装を再現可能にするための表示です。キャラクター生成と演出はPresentationへ閉じているため、Prefab／Animatorへ交換してもDomainの変更は不要です。

## 3. 全体構造

```text
Scene / Prefab
      ↓ 入力・表示
Presentation
      ↓ ユースケース呼び出し
Application
      ↓ 状態遷移・計算
Domain

Infrastructure ─ Addressables、MasterMemory、Scene、UI生成
Installers     ─ Zenjectによる生成と接続
```

| 層 | 責務 | 例 |
|---|---|---|
| Domain | 戦闘・周回ルール、状態遷移 | `BattleSystem`、`BattleState`、`RunState` |
| Application | 1操作や画面進行の調整 | `AttackUseCase`、`PlayPlayerTurnUseCase`、`RunFlowUseCase` |
| Presentation | 入力、表示値、演出 | `BattleHudView`、`BattleViewModel`、各Presenter |
| Infrastructure | 外部技術の具体実装 | Repository、Addressables、`UIService`、`SceneService` |
| Installers | オブジェクトの寿命と依存関係 | `ProjectInstaller`、`BattleSceneInstaller` |

厳密なClean Architectureを名乗る構成ではありません。Domainは独立Assemblyですが、それ以外の製品コードは主に`BattleDemo.Runtime`にあります。小規模デモでAssemblyを細分化しすぎず、重要な依存方向は構造テストでも確認する判断です。

## 4. 状態を2種類に分けた理由

### BattleState：1戦だけの状態

- Player／EnemyのHPと基礎攻撃力
- 現在の手番とターン数
- 防御状態
- 強攻撃のクールダウン
- 敵の行動パターンと次の行動
- 勝敗

### RunState：戦闘をまたいで残る状態

- 三連戦／無限のモード
- 現在ラウンド、勝利数
- 持ち越すHP、最大HP
- 攻撃・防御強化
- 総ターン、累計被ダメージ
- `InBattle`、`ChoosingUpgrade`、`Cleared`、`Defeated`という進行状態

分離しなかった場合、次の戦闘へHPと強化を残す処理で、前戦の防御状態やクールダウンまで残る危険があります。`RunFlowUseCase.PrepareCurrentBattle()`がRunの値から新しいBattleを毎回構築することで、持ち越す値を明示しています。

## 5. 入力から表示更新まで

```text
BattleActionButton
  → BattleHudView
  → PlayPlayerTurnUseCase
  → AttackUseCase
  → BattleSystem / BattleState
  → MessagePipe
  → BattleEventPresenter
  → BattleViewModel
  → HUD更新
```

役割を分ける理由は次の通りです。

- Buttonは「何が押されたか」だけを渡す。
- UseCaseはプレイヤー行動、待機、敵行動という順序を担当する。
- Domainはダメージ、防御、クールダウン、勝敗を判断する。
- Presenterは発生したイベントを表示用の値とログへ変換する。
- ViewはTextやButtonを更新する。

強攻撃ボタンを無効にしても、Domain側でも使用可否を検証します。UI制御だけをルールの保証にしないためです。

## 6. R3とMessagePipeを分けた理由

| 手段 | このデモで表すもの | 例 |
|---|---|---|
| R3 | 現在表示すべき値 | 現在HP、ターン表示、行動可能状態 |
| MessagePipe | その瞬間に発生した出来事 | ダメージ、行動解決、ターン変更、決着 |
| 直接呼び出し | 返り値が必要な命令 | 攻撃実行、Run開始、強化選択 |

全てをイベント化すると処理順とエラー経路が追いにくくなります。命令はUseCaseへ直接送り、複数の表示が反応する出来事だけをMessageとして配信します。

## 7. 非同期処理と多重実行

敵ターンまでの待機にはUniTaskを使います。重要なのは待機そのものではなく、待機中に画面が閉じた場合です。

- HUDの寿命に連動したCancellationTokenを渡す。
- Viewが閉じたら待機をキャンセルする。
- 待機前に保存した`BattleSession.Revision`と現在値を比較する。
- 新しい戦闘が始まっていれば、古い敵ターンを実行しない。
- UseCaseの`_busy`や`_loading`で同時実行を拒否する。

`Revision`は「処理がキャンセル通知に間に合わなかった場合」の追加防御です。

## 8. 三連戦と無限モード

`RunMode`を`RunState`が所有し、三連戦だけが3勝で`Cleared`へ遷移します。無限モードは勝利後に必ず強化選択へ進みます。

```text
Expedition: Round 3勝利 → Cleared
Endless:    勝利 → ChoosingUpgrade → 次ラウンド
```

無限モードの難易度計算は`EndlessDifficulty`へ分離しています。

- 3ラウンドごとにTier上昇
- HPはTierごとに25%上昇
- 攻撃力はTierごとに15%上昇
- 10ラウンドごとにボスStageを使用

既存のBattleSystemへ条件分岐を追加せず、戦闘開始時にSnapshotへ確定値を渡します。そのため、通常モードと無限モードは同じ戦闘ルールを再利用できます。

## 9. MasterDataとSnapshot

```text
StageMaster
  → CharacterMaster
  → SkillMaster.Power
  → BattleUnitSnapshot.BasePower
```

Repositoryは定義を取得し、`RunFlowUseCase`が戦闘開始時のSnapshotへ変換します。戦闘中にMasterDataを何度も参照しないため、実行中の値が安定し、Domainテストも単純になります。

## 10. UIサービスとSemaphoreSlim

`UIService`は画面やPopupの生成・表示・破棄を担当します。同じUIへ複数のOpen／Close要求が同時に来ると、二重生成や所有権の競合が起きます。そのため、非同期ゲートで操作を直列化します。

説明のポイント：

- `SemaphoreSlim(1, 1)`は非同期処理用の鍵として使う。
- `WaitAsync`後は必ず`finally`で`Release`する。
- UI生成の成功・失敗に関係なく鍵を返す。
- GateとAddressables Handleを誰が破棄するかを明確にする。

## 11. Addressablesの所有権

ロードしたものは「誰が、いつ解放するか」をセットで決めます。

- 成功したUI Asset：所有するFactory／Serviceの破棄時に解放
- 失敗したロード：失敗処理内で成功済みHandleを解放
- Sceneをまたいで残すSession：ProjectContextが所有
- Scene固有のView／Presenter：SceneContextが所有

ロードに成功したことと、参照を解放できることの両方が必要です。

## 12. ローカライズ

Domainは日本語／英語の文言を知りません。Presentationが`ILocalizationService`から表示文言を取得します。

- 初回は端末言語を使用
- TitleでJP／EN切り替え
- 選択を`PlayerPrefs`へ保存
- 日本語はNoto Sans JPを使用
- 未登録キーはキー自体を表示して不足を発見しやすくする

## 13. テスト戦略

| テスト | 主な保証 |
|---|---|
| Domain EditMode | ダメージ、防御、Cooldown、Run状態遷移、無限難易度 |
| Application EditMode | UseCaseの順序、失敗後の再試行、二重完了防止、Repository |
| Architecture test | DomainのUnity非依存、ApplicationからPresentationへの禁止 |
| PlayMode smoke test | UI Address、Prefab参照、Title Sceneの実生成 |

保証していないことも説明します。

- 全戦闘を実操作するE2E PlayMode test
- Android／iOS全端末の表示
- Addressables Player BuildのCI検証
- 長時間の無限モードによるバランス

## 14. 設計上のトレードオフ

### ライブラリが多い

小規模ゲームとしてはZenject、UniTask、R3、MessagePipe、MasterMemory、Addressablesは多めです。このデモでは各基盤の責務を示す目的があります。製品ではチーム標準、保守期間、機能規模に応じて減らします。

### Runtime Assemblyが大きい

Domain以外を細かくAssembly分割していないため、依存禁止の一部は構造テストです。規模が拡大したらApplication、Presentation、Infrastructureをasmdefで分けます。

### セーブがない

現在のプレイ時間では途中保存の優先度が低く、面接で示したいクライアント設計から外れるため実装していません。無限モードの継続時間が長くなるなら再検討します。

## 15. 交換可能性を説明する例

> Cubeキャラクターは`CubeBattleCharacterPresenter`が生成し、DomainはGameObjectやAnimatorを参照しません。将来人型モデルへ変更する場合は、PresenterをPrefab／Animator版へ差し替え、BattleActionResolvedMessageをAttack、Defend、Hit、Deathへ対応させます。BattleStateやBattleSystemは変更しません。

