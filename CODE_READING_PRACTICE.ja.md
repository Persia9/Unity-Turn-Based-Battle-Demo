# Unity Turn-Based Battle Demo 完全コードリーディングガイド

この資料は現在のリポジトリを実際に読み直して作成したガイドです。クラス名の暗記ではなく、次をコードで説明できる状態を目指します。

1. 状態を誰が所有しているか。
2. 入力がDomainへ届き、表示へ戻る順番。
3. Project、Scene、Prefabの寿命。
4. 非同期、キャンセル、再入、失敗の扱い。
5. テストが保証する範囲と、まだ保証しない範囲。

この文書でいう「現在」はUnity `6000.3.12f1`、`BattleUnitSnapshot.BasePower`方式、PlayMode smoke test 4本が存在する構成を指します。

---

## 1. ゲーム仕様を先に理解する

このデモは、三連戦と無限モードを選べる1対1ターン制バトルです。

```text
Title
  ├─ Expedition
  │    ↓
Battle 1: Slime / 通常攻撃のみ
  ↓ 勝利・最大20HP回復・強化選択
Battle 2: Sentinel / 3ターンごとに強攻撃
  ↓ 勝利・最大20HP回復・強化選択
Battle 3: Iron Warden / 強攻撃を連続する周期
  ↓
Cleared または Defeated
  ↓
Retry または Title

  └─ Endless
       ↓
     Battle → 強化選択 → 次のBattleを敗北まで反復
     3ラウンドごとに敵能力上昇、10ラウンドごとにBoss
```

プレイヤーの行動：

| 行動 | 効果 | 制限 |
|---|---|---|
| Attack | `BasePower + DamageBonus` | 毎プレイヤーターン使用可能 |
| Defend | その場のダメージ0。次の敵攻撃を軽減 | 次の1hitで消費 |
| StrongAttack | `(BasePower + DamageBonus) × 2` | 次の2プレイヤーターンは使用不可 |

敵のStrongAttackは基本威力の3倍です。防御時は最終威力に`GuardDamagePercent`を掛けて切り上げます。

```text
通常ガード: 15 × 50% = 7.5 → 8
Guard強化後: 30 × 35% = 10.5 → 11
```

強化：

| 強化 | RunStateへの効果 |
|---|---|
| Attack | `DamageBonus += 5` |
| Vitality | `MaxHp += 25`、現在HPも最大25回復 |
| Guard | `GuardDamagePercent -= 15`、下限20% |

---

## 2. アーキテクチャ

```text
Unity Scene / Prefab
        ↓
Presentation: View・ViewModel・Presenter
        ↓
Application: UseCase・Session・Message
        ↓
Domain: 状態・ルール

Infrastructure: Addressables・MasterMemory・UI生成・Scene操作
Installers: 上記をZenjectで接続
```

これは厳密なClean Architectureの同心円ではありません。Applicationの一部はRepositoryやSceneのInfrastructure interfaceを参照し、製品コードの大部分は1つの`BattleDemo.Runtime` Assemblyです。Domainだけが独立しています。

| Assembly | 内容 | 特徴 |
|---|---|---|
| `BattleDemo.Domain` | BattleとRunのルール | `noEngineReferences: true` |
| `BattleDemo.Runtime` | Application、Presentation、Infrastructure、Installer | Unityと外部ライブラリを利用 |
| `BattleDemo.Editor` | MasterData生成 | Editor限定 |
| `BattleDemo.Domain.Tests` | Domain tests | EditMode |
| `BattleDemo.Application.Tests` | UseCase、Repository、構造tests | EditMode |
| `BattleDemo.PlayMode.Tests` | AddressablesとTitle Scene | PlayMode |

`ArchitectureTests`はソースを走査し、Domainが外側やUnityを参照しないこと、ApplicationがPresentationを参照しないことを補助的に検査します。コンパイラーによる完全な層分離ではありません。

---

## 3. 最重要：BattleStateとRunState

### BattleState：1戦だけの状態

- Player／EnemyのSnapshot
- 現在の手番、ターン数
- 勝敗
- StrongAttackの再使用ターン
- EnemyAttackPattern
- 今回持ち込んだ攻撃ボーナス、防御率
- 次の敵攻撃に対する防御状態

初期化すると手番、勝敗、Cooldown、防御状態がリセットされます。

### RunState：三連戦／無限モードをまたぐ状態

- `RunPhase`
- `RunMode`（Expedition／Endless）
- Round、Wins
- 現在HP、最大HP
- 攻撃・防御強化
- 総ターン、累計被ダメージ
- `_battleStartingHp`

累計被ダメージは各hitを加算せず、`戦闘開始HP - 戦闘終了HP`で記録します。現在は戦闘中回復がないため実質同じですが、将来回復を追加すると「受けた総ダメージ」と意味がずれます。

### SessionとRevision

`BattleSession`と`RunSession`はProjectスコープのSingletonです。Sceneをロードしても周回状態が残ります。

`BattleSession.Revision`はInitializeごとに増える世代番号です。

```text
Battle A: Revision 1 → 敵待機中
Battle BをInitialize: Revision 2
古い待機が完了
Revision 1 != 2 → 古い敵攻撃を中止
```

---

## 4. MasterDataからSnapshotまで

現在の攻撃力の正は`SkillMaster.Power`です。`CharacterMaster.Attack`はありません。

```text
StageMaster
├─ PlayerCharacterId
├─ EnemyCharacterId
└─ EnemyPattern
       ↓
CharacterMaster
├─ Name
├─ MaxHp
└─ DefaultSkillId
       ↓
SkillMaster.Power
       ↓
BattleUnitSnapshot.BasePower
```

`RunFlowUseCase.PrepareCurrentBattle()`がStage、Character、Skillを解決し、その戦闘の値をSnapshotへ固定します。

- 攻撃ごとのMasterData再検索を避ける。
- 戦闘開始後は確定値だけで動く。
- Master定義と実行中の戦闘状態を分ける。

`SkillMaster.Power`は定義値、`BattleUnitSnapshot.BasePower`は戦闘開始時に解決した実行値です。HP変更時はSnapshotを作り直しますが、ID、Name、MaxHp、BasePowerを維持してHPだけ変えます。

---

## 5. 起動からTitle表示まで

```text
Resources/ProjectContext.prefab
  └─ ProjectInstaller
       ├─ Addressables service
       ├─ MasterData provider / repositories
       ├─ BattleSession / RunSession
       ├─ RunFlowUseCase / AttackUseCase
       └─ MessagePipe brokers

Assets/Scenes/Title.unity
  └─ SceneContext
       └─ TitleSceneInstaller
            ├─ Scene共通UIサービス
            └─ TitleSceneEntryPoint
```

実行順：

```text
TitleSceneEntryPoint.Initialize
→ InitializeMasterDataUseCase.Execute
→ Master/CoreとMaster/UIをAddressablesからロード
→ IUIService.Open<TitleScreenView>("TitleScreen")
→ UIMasterからPrefabKeyとLayerを取得
→ UI/Title/TitleScreenをロード
→ DiContainer.InstantiatePrefab
→ TitleScreenView.OnOpened
```

TitleのPlayMode testは、この経路でTitleScreenがScene上にactiveで生成されたことまで確認します。

EntryPointの`Run().Forget()`自体には明示的な`try/catch`がありません。初期化失敗時の画面内リトライUIはありません。

---

## 6. StartからBattle Sceneまで

```text
TitleScreenView.OnExpeditionClicked / OnEndlessClicked
→ 両モードボタンを無効化
→ RunFlowUseCase.StartNewRun(mode)
   ├─ _loadingで二重開始を拒否
   ├─ Stage 1とPlayer Characterを取得
   ├─ RunState.Start(MaxHp)
   ├─ PrepareCurrentBattle
   │    ├─ Stage / Character / Skillを解決
   │    └─ BattleSession.Initialize → Revision増加
   └─ Battle Sceneをロード
```

`_loading`はUnityメインスレッド利用を前提としたboolガードです。2回目の要求はキューへ入らずreturnします。

---

## 7. Battle Sceneの構築

`BattleSceneInstaller`がSceneスコープを構築します。

| 登録 | 役割 |
|---|---|
| `BattleViewModel` | 表示用の現在値 |
| `UniTaskTurnDelay` | 敵行動まで700ms待機 |
| `PlayPlayerTurnUseCase` | PlayerからEnemyまでの進行 |
| `BattleSceneEntryPoint` | BattleHudを開く |
| `BattleEventPresenter` | Messageを購読し表示更新 |
| `UIService`一式 | View生成、Layer、Cache、Popup |

`BattleSceneEntryPoint`はMasterDataを初期化し、BattleSessionが未初期化なら`PrepareCurrentBattle()`を呼びます。Battle Sceneから直接Playしても最初のRunを準備できます。Title経由では既に初期化済みなので二重初期化しません。

`BattleHudView.OnOpened()`：

1. View破棄と連動するCancellationTokenSourceを作る。
2. 子の`BattleActionButton`をBindする。
3. ViewModelのReactivePropertyをTextへ購読する。
4. BattleSession／RunSessionから初期値を入れる。
5. Command、予告、ログを初期化する。

---

## 8. 1ターンの完全な流れ

```text
BattleActionButton.OnClicked
→ BattleHudView.ExecuteAction
→ PlayPlayerTurnUseCase.Execute
   ├─ _disposed / _busy / cancel確認
   └─ Revision保存
→ AttackUseCase.ExecutePlayerAction
   ├─ 手番・終了・再入・行動可否確認
   └─ attacker.BasePowerを使う
→ BattleSystem.ApplyPlayerAction
   ├─ DamageBonus加算
   ├─ Strongなら2倍
   ├─ Defend／Cooldown状態設定
   └─ BattleState.ApplyDamage
→ MessagePipe
   ├─ DamageApplied（Defend以外）
   ├─ BattleActionResolved
   └─ BattleFinished または TurnChanged
→ BattleEventPresenter
→ BattleViewModel
→ R3購読でHUDとButtonへ反映

BattleActionResolved
→ CubeBattleCharacterPresenter
→ Cube人物のAttack／Defend／Hit／Death演出
```

決着していなければ：

```text
700ms待機
→ CancellationToken再確認
→ Revision再確認
→ AttackUseCase.ExecuteEnemyAttack
→ EnemyIntentを採用
→ Strongなら3倍
→ 防御中ならGuardDamagePercentを掛けて切り上げ
→ 防御を1回で消費
→ Playerへダメージ
```

### `_busy`と`_executing`

| ガード | 場所 | 防ぐもの |
|---|---|---|
| `_busy` | PlayPlayerTurnUseCase | 待機を含む一連の非同期進行の重複 |
| `_executing` | AttackUseCase | 同期Message publish中の再入 |

Buttonの`interactable`だけに頼らず、Button、UseCase、Domainで可否を再確認します。

---

## 9. Domainルールの読み方

### TurnCount

個別行動数ではなくプレイヤーターンのRound番号です。

```text
Player / Turn 1
Enemy  / Turn 1
Player / Turn 2
```

### StrongAttack cooldown

Turn 1で使うと`_strongReadyOnTurn = 4`です。

```text
Turn 1: 使用
Turn 2: CD 2
Turn 3: CD 1
Turn 4: 再使用可能
```

### EnemyIntent

予告と実攻撃が同じpropertyを参照します。

| Pattern | StrongAttack |
|---|---|
| NormalOnly | なし |
| EveryThirdTurn | 3、6、9… |
| BossCycle | 2・3、6・7… |

### 防御と例外安全性

防御は次の敵行動で`ConsumePlayerDefense()`により1回だけ消費されます。不正Powerやoverflowは状態変更前に例外になるよう順序付けされ、テストされています。

---

## 10. MessagePipeとR3

MessagePipeは「起きた事実」、R3は「現在表示する値」です。

| Message | 意味 |
|---|---|
| `DamageAppliedMessage` | ダメージと残HPが確定 |
| `BattleActionResolvedMessage` | 行動が解決済み |
| `TurnChangedMessage` | 次の手番へ移行 |
| `BattleFinishedMessage` | 勝者確定 |

Publish順：

```text
非致死Attack:
DamageApplied → BattleActionResolved → NextTurn → TurnChanged

致死Attack:
DamageApplied → BattleActionResolved → BattleFinished

Defend:
BattleActionResolved → NextTurn → TurnChanged
```

`BattleViewModel`はHP、ターン、ログ、予告、Command可否をReactivePropertyで保持します。`CommandsVersion`はDictionary内のCommandState更新を全Buttonへ通知するversion counterです。

---

## 11. 勝敗からResultPopupまで

```text
BattleFinishedMessage
→ BattleEventPresenter
   ├─ ViewModelを終了状態へ更新
   ├─ RunFlowUseCase.CompleteBattle
   │    └─ RunState.CompleteBattle
   └─ ResultPopupを開く
```

`CompleteBattle()`はRevisionを記録し、同じ戦闘結果の二重反映を防ぎます。

```text
敗北: InBattle → Defeated
1・2戦目勝利: InBattle → 最大20HP回復 → ChoosingUpgrade
3戦目勝利: InBattle → Cleared
```

Popup openは`Forget()`ですが、`OpenResultPopup()`内部で例外を捕捉し、Unity LogとBattle logへ記録します。

---

## 12. 強化、ロード失敗、再試行

```text
ResultPopupView.Continue
→ RunFlowUseCase.ChooseUpgrade
   ├─ RunState.ChooseUpgrade
   │    ├─ 強化適用
   │    ├─ Round++
   │    └─ Phase = InBattle
   ├─ _advancePending = true
   ├─ 次のBattleを準備
   └─ Battle Sceneロード
```

Sceneロード失敗時には強化とRound進行が完了済みです。同じUpgradeを再度押すと、`_advancePending`により強化を二重適用せず、Battle準備とSceneロードだけを再試行します。

これは同一プロセスのメモリー上の再試行であり、アプリ終了後の復旧や保存ではありません。

---

## 13. UI基盤

### UIMaster

| ID | Address | Layer | Type | Cache | Singleton | Block |
|---|---|---|---|---:|---:|---:|
| TitleScreen | UI/Title/TitleScreen | Screen | Screen | true | true | false |
| BattleHud | UI/Battle/BattleHud | Hud | Hud | true | true | false |
| ResultPopup | UI/Battle/ResultPopup | Popup | Popup | false | false | true |

### UIViewFactory

1. AddressablesからPrefabをLoad。
2. Scene側の`DiContainer.InstantiatePrefab()`で生成。
3. rootに期待するView componentがあるか確認。
4. `UIViewAssetLease`を追加。

Scene側Containerで生成するため、BattleHudの`[Inject]`はBattle SceneのViewModelやUseCaseを解決できます。Leaseはinstanceの`OnDestroy`でPrefab assetをReleaseします。

### UIService

`SemaphoreSlim`でOpen／Closeを直列化し、opened、cache、input blockerを管理します。

Open：定義取得 → Singleton確認 → Cacheまたは生成 → Layer配置 → blocker → Context → active → `OnOpened` → Registry。

Close：最後の同ID View取得 → `OnClosed` → blocker削除 → Cacheへ戻すかDestroy。

`Dispose()`はScene内のViewとblockerを破棄します。SemaphoreSlimをDisposeしないのは、await中の処理がfinallyで`Release()`する可能性があるためです。

`PopupService`と`ScreenService`はUIMasterの`UIType`を検査してUIServiceへ委譲します。

---

## 14. MasterData基盤

`Master/Core`：Character、Skill、Stage。`Master/UI`：UI定義。

Editorメニュー`Tools > MasterData > Build Demo MasterData`が`.bytes`を生成し、Addressablesの`Master/Core`と`Master/UI`で配信します。

`MasterDatabaseInitialization<T>`はSemaphoreSlimで同時初期化を直列化します。Core成功後にUIだけ失敗するとCoreを保持し、次回はUIだけ再試行します。失敗はkeyとinner exceptionを持つ`MasterDataLoadException`へ変換します。

各Repositoryは`TryFindById`を使い、見つからなければ`MasterDataNotFoundException(table, key)`へ変換します。

現在、`SkillMaster.Name`と`StageMaster.DisplayName`はRepository testでは読みますが、ゲーム画面や戦闘進行では未使用です。

---

## 15. Action Definitionの境界

BattleHudのButtonには次が割り当てられます。

- `Assets/Settings/BattleActions/Attack.asset`
- `Assets/Settings/BattleActions/Defend.asset`
- `Assets/Settings/BattleActions/StrongAttack.asset`

DefinitionはAction enum、表示名、説明文を持ちます。Prefab上の差し替えで既存Actionの割当と表示を変えられます。

ただし効果は実装しません。毒、回復など新ルールにはDomain、Application、表示、testの変更が必要です。汎用Ability systemではありません。

---

## 16. 全コードの役割

### Domain

| ファイル | 役割 |
|---|---|
| `BattleAction` / `BattleSide` / `TurnOwner` | 行動・対象・手番のID |
| `EnemyAttackPattern` | 敵行動周期 |
| `BattleUnitSnapshot` | 戦闘開始時に確定したUnit情報 |
| `BattleActionResult` | 解決済み行動の値 |
| `BattleState` | 1戦の状態遷移 |
| `BattleSystem` | ダメージ・防御・倍率 |
| `RunState` | 三連戦／無限のMode、Phase、HP、強化、戦績 |

### Application

| ファイル | 役割 |
|---|---|
| `BattleSession` / `IBattleSession` | BattleStateとRevisionを保持 |
| `RunSession` | RunStateを保持 |
| `AttackUseCase` | 1actorの行動を検証・解決・通知 |
| `PlayPlayerTurnUseCase` | PlayerからEnemyまで非同期進行 |
| `RunFlowUseCase` | 新規Run、戦闘準備、完了、強化、Scene遷移 |
| `InitializeMasterDataUseCase` | MasterData初期化入口 |
| `ReturnToTitleUseCase` | Titleへ戻る |
| `ITurnDelay` / `UniTaskTurnDelay` | 待機の差し替え点 |
| 4種のMessage | Domain結果をPresentationへ通知 |

### Presentation

| ファイル | 役割 |
|---|---|
| `TitleScreenView` | Start入力、多重押下抑止、Safe Area対応のモバイルレイアウト |
| `BattleHudView` | 初期表示、R3購読、Action送信 |
| `BattleViewModel` | 表示値とCommandState |
| `BattleEventPresenter` | Message購読、表示更新、ResultPopup |
| `CubeBattleCharacterPresenter` | 解決済み行動をCube人物の3D演出へ変換 |
| `CubeBattleCharacterView` | Cube部品の生成、突進、防御、被弾、倒れる動き |
| `BattleCharacterAnchors` | Battle SceneのPlayerRoot／EnemyRoot参照 |
| `PerformanceView` | Battle SceneのFPS、フレーム時間、メモリを低頻度更新で表示 |
| `BattleActionButton` | Definition駆動の共通Button |
| `BattleActionDefinition` | Action ID・表示名・説明 |
| `BattleCommandState` | 実行可否とCooldown |
| `ResultPopupContext` | 勝敗Context |
| `ResultPopupView` | 戦績、強化、Retry、Title |

### Infrastructure / Installer / SceneFlow

| 領域 | 役割 |
|---|---|
| Asset | Addressables Load／Release |
| MasterData | バイナリ、初期化、Repository、例外 |
| Scene | SceneManagerの抽象化 |
| UI | 定義、生成、Layer、Cache、所有権 |
| `ProjectInstaller` | Sceneをまたぐ依存とMessage broker |
| `SceneInstaller` | Scene単位UI基盤 |
| `TitleSceneInstaller` | Title EntryPoint |
| `BattleSceneInstaller` | Battle固有依存 |
| 2つのEntryPoint | 各Sceneの開始処理 |
| `GameConstants` | Scene名とMaster key |
| `ILogger` / `UnityLogger` | 初期化ログ |

---

## 17. Testから仕様を読む

### EditMode

| Suite | 保証 |
|---|---|
| `BattleStateTests` | 手番、致死、HP clamp、再初期化 |
| `BattleActionRulesTests` | 倍率、Cooldown、防御、予告、overflow |
| `EncounterRulesTests` | 敵Pattern、強化順、切り上げ |
| `RunStateTests` | 回復、強化、最終勝利、敗北、再開 |
| `BattleUseCaseTests` | Message順、再入、連打、cancel、Revision、攻略 |
| `RunFlowUseCaseTests` | 3連戦、無限難易度とボス、全強化組合せ、Scene再試行 |
| `BattleCommandStateTests` | Button可否とCooldown |
| MasterData系tests | 同時初期化、部分再試行、Repository例外 |
| `ArchitectureTests` | 依存方向 |

### PlayMode 4本

1. TitleScreen Addressとroot component。
2. BattleHud Addressとroot component。
3. ResultPopup Addressとroot component。
4. Title Sceneを実ロードし、実DI・MasterData・Addressables経由でTitleScreen表示。

### 未保証

- Battle Sceneの全`[Inject]`とInspector参照。
- Button操作によるTitle → Battle → Upgrade → ResultのE2E。
- ResultPopupの全Button／UpgradeSlot参照。
- `Use Existing Build`のAssetBundle／Catalog。
- Player build、性能、長時間反復。
- 保存とアプリ終了後の復旧。保存機能自体がない。

Prefab smoke testはAddressとroot componentを確認しますが、private SerializeFieldすべては検証しません。Title Scene testはTitle側の統合保証であり、Battle側ではありません。

---

## 18. 現在の注意点・改善余地

1. `RunState.BattleCount = 3`と`RecoveryHp = 20`に対し、Viewにも`/3`と`20 HP`が直書きされています。
2. PresenterのDefendログは`damage is halved`固定ですが、Guard強化後は35%や20%を受けます。計算とDefenseTextは正しく、ログだけ一般化されていません。
3. MasterDataの`EnemyPattern`、UIの`Layer`／`Type`はintからenumへcastします。UI側は変換時の範囲検証がありません。
4. UI IDは文字列で複数箇所を接続します。MasterData、Addressables、呼び出し側の一致が必要です。
5. 小規模デモに対してZenject、UniTask、R3、MessagePipe、MasterMemory、Addressablesと基盤が多めです。教材としての意図と簡略化案の両方を説明すべきです。
6. Applicationは具象Unity APIを避けていますが、Infrastructure namespaceのinterfaceを参照します。厳密にするならportをApplicationへ移す余地があります。
7. SessionはProjectスコープなのでTitleへ戻るだけではRunStateは消えません。StartNewRunで明示的に初期化します。

---

## 19. 推奨の読む順番

### 30分

1. `RunState`
2. `BattleState`
3. `BattleSystem`
4. `RunFlowUseCase`
5. `PlayPlayerTurnUseCase`
6. `AttackUseCase`
7. `BattleEventPresenter`
8. `BattleHudView`

各ファイルで「入力」「読む状態」「書く状態」「通知」「失敗」を1行ずつ書きます。

### 2時間

上記に加えます。

1. ProjectInstallerとSceneInstaller。
2. 2つのSceneEntryPoint。
3. UIService、UIViewFactory、UIViewAssetLease。
4. MasterData初期化とRepository。
5. BattleUseCaseTestsとRunFlowUseCaseTests。
6. PlayMode 4本。

### 完全コース

1. Scene YAML／InspectorでSceneContext、Installer、UIRoot参照。
2. PrefabのSerializeField。
3. 3つのBattleActionDefinition。
4. Addressables Groupの5 Address。
5. MasterData生成と`.bytes`。
6. Test RunnerでEditMode／PlayMode。
7. Titleから3戦を手動で通す。

---

## 20. 自分で描く5シーケンス

答えを見ずに描きます。

1. Unity起動 → TitleScreen。
2. Start → BattleHud。
3. StrongAttack → 敵行動完了。
4. 勝利 → Upgrade → 次Battle。
5. Sceneロード失敗 → 同じUpgradeで再試行。

次の語が入っているか確認します。

```text
ProjectContext / SceneContext / EntryPoint / Addressables / MasterData
RunState / BattleState / Revision / MessagePipe / BattleViewModel / CancellationToken
```

---

## 21. 面接の質問と答え

### なぜBattleStateとRunStateを分ける？

戦闘ごとに消す手番・防御・Cooldownと、持ち越すHP・強化・戦績を分けるためです。境界は`PrepareCurrentBattle()`です。

### なぜSkill.PowerをSnapshotへコピーする？

戦闘開始時に定義を解決し、戦闘中は確定値だけで動かすためです。攻撃ごとのRepository参照も避けます。

### なぜCancellationTokenとRevisionの両方？

待機がcancelを無視して完了しても、古い処理を新Battleへ適用しないためです。

### R3とMessagePipeをなぜ両方？

MessagePipeはDamageやTurnChangedという事実、R3はHPやButton可否という現在値です。

### SemaphoreSlimの目的は？

Master初期化やUI Open／Closeが共有状態を同時変更しないよう、非同期処理を直列化します。

### PlayMode testの保証範囲は？

3 UI Addressとroot View、Title Sceneの実接続です。Battle Sceneの完全操作は未保証です。

### 弱点は？

規模に対して基盤が多い、Runtime Assemblyが大きい、文字列と表示定数が重複、Battle統合testが浅い点です。

---

## 22. 変更課題

### StrongAttackを3倍にする

`BattleSystem`、Domain／UseCase tests、攻略、`StrongAttack.asset`の表示名と説明を確認します。

### 4戦にする

`BattleCount`、StageMaster、Viewの`/3`、Result文言、Stage ID、攻略testを確認します。

### 戦闘中回復を追加する

BattleAction、BattleSystem、Snapshot HP更新、Definition、Message、`TotalDamageTaken`の意味を確認します。

### UIを追加する

UIMaster、Addressables、Prefab root、UIType／Layer、呼び出し元、Prefab smoke testを確認します。

---

## 23. 最後に60秒で説明する

コードを閉じ、各1〜2文で説明します。

1. プレイヤーが何を判断するゲームか。
2. BattleStateとRunStateを分けた理由。
3. ButtonからDomain、表示へ戻る経路。
4. ProjectとSceneスコープの違い。
5. Cancellation、Revision、再入ガード。
6. Skill.PowerとSnapshot.BasePower。
7. EditModeとPlayModeの保証範囲。
8. 現在の改善余地。

詰まったら「所有者」を探します。状態はState／Session、進行はUseCase、表示はViewModel／View、外部I/OはInfrastructure、生成と寿命はInstallerです。
