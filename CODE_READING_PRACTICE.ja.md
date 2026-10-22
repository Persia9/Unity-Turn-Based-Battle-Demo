# 主要コードを読む練習

目標は全クラスを暗記することではなく、「入口・状態・ルール・通知・後始末」を指し示しながら説明できることです。まず60〜90分で一巡し、答えを隠して口頭説明してください。

## 1. 最初に読む順番

| 順番 | ファイル | 探すもの |
|---|---|---|
| 1 | [RunState](Assets/Scripts/Domain/Run/RunState.cs) | 周回フェーズ、持ち越す値、強化、戦績 |
| 2 | [BattleState](Assets/Scripts/Domain/Battle/BattleState.cs) | 1戦の状態、入力可否、予告、クールダウン |
| 3 | [BattleSystem](Assets/Scripts/Domain/Battle/BattleSystem.cs) | 倍率、軽減、端数処理、効果の適用 |
| 4 | [RunFlowUseCase](Assets/Scripts/Application/UseCases/RunFlowUseCase.cs) | 新規周回、戦闘の準備、次の戦闘への遷移 |
| 5 | [PlayPlayerTurnUseCase](Assets/Scripts/Application/UseCases/PlayPlayerTurnUseCase.cs) | プレイヤー行動から敵行動への進行、キャンセル |
| 6 | [AttackUseCase](Assets/Scripts/Application/UseCases/AttackUseCase.cs) | データ取得、Domain呼び出し、通知順序 |
| 7 | [BattleActionButton](Assets/Scripts/Presentation/Battle/Commands/BattleActionButton.cs) | 定義の参照、共通入力、購読解除 |
| 8 | [BattleEventPresenter](Assets/Scripts/Presentation/Battle/BattleEventPresenter.cs) | 通知から表示更新、結果画面を開く場所 |
| 9 | [BattleViewModel](Assets/Scripts/Presentation/Battle/BattleViewModel.cs) | 表示用状態と行動ごとの使用可否 |
| 10 | [ProjectInstaller](Assets/Scripts/Installers/ProjectInstaller.cs)、[BattleSceneInstaller](Assets/Scripts/Installers/BattleSceneInstaller.cs) | 生成と破棄の単位 |

各ファイルで「入力は何か」「何を書き換えるか」「何を呼ぶか」「何に依存するか」を1行ずつメモします。

## 2. 紙に描く4つの処理

### A. 強攻撃ボタンを押してから敵が行動するまで

まず答えを見ず、クラス名を矢印でつないでください。

答えの目安：

```text
BattleActionButton.OnClicked
→ BattleHudView.ExecuteAction
→ PlayPlayerTurnUseCase.Execute
→ AttackUseCase.ExecutePlayerAction
→ リポジトリからスキル取得
→ BattleSystem.ApplyPlayerAction / BattleState
→ DamageApplied・BattleActionResolved・TurnChangedなどを通知
→ Presenter → ViewModel → R3購読でViewへ反映
→ ITurnDelay.Wait
→ キャンセル・Revision確認
→ AttackUseCase.ExecuteEnemyAttack
```

決着した場合はTurnChangedではなくBattleFinishedを通知します。プレイヤーが先に倒した場合、敵の待機は開始しません。

### B. 勝利してから次の戦闘が始まるまで

```text
BattleFinished
→ BattleEventPresenter.OnBattleFinished
→ RunFlowUseCase.CompleteBattle
→ RunState.CompleteBattle（戦績・回復・強化待ちへ遷移）
→ ResultPopupView
→ 強化選択
→ RunFlowUseCase.ChooseUpgrade
→ RunState.ChooseUpgrade
→ PrepareCurrentBattle（周回の値から新しい戦闘を初期化）
→ Battleシーンをロード → BattleStarted
```

問い：強化後のシーンロードだけ失敗したら、周回状態はどこまで進んでいますか？

答え：強化は既に適用済みで、次の戦数へ進んでいます。`_advancePending` により再試行では強化を再適用せず、同じ戦闘を準備・ロードし直します。永続的なトランザクションや保存からの復旧を実装したわけではありません。

### C. 敵の待機中に画面を閉じた場合

答え：HUDのOnClosedが呼び出し側のトークンをキャンセルし、シーン破棄ではUseCaseのDisposeも寿命トークンをキャンセルします。待機後のThrowIfCancellationRequestedでも確認します。トークンとは別にRevisionを比較し、セッションが再初期化されていたら古い処理を適用しません。

`ITurnDelay` をテスト用実装に差し替え、キャンセルを無視して完了するケースも検証しています。破棄の時点までの実行済み処理を巻き戻す仕組みではありません。

### D. ボタンを攻撃から防御へ変更する場合

答え：InspectorでDefinitionをDefendの定義アセットへ差し替えます。共通コンポーネントが表示名と行動IDを読み、ViewModelの共通コマンド状態から使用可否を取得します。Viewコードの分岐追加は不要です。

ただし、新しい「毒」行動の効果そのものを定義アセットだけで追加することはできません。

## 3. 面接の想定質問と答えの要点

| 質問 | 答える要点 | 見せるコード |
|---|---|---|
| BattleSessionとRunSessionを分けた理由は？ | リセットする値と戦闘間で残す値を分けるため | RunState、PrepareCurrentBattle |
| BattleSessionはシーンごとに生成される？ | いいえ。Projectスコープで保持し、Initializeで1戦の内容を作り直す | ProjectInstaller、BattleSession |
| なぜボタンを無効化するだけでは不十分？ | 見た目の制御だけに頼らず、不正なタイミングや重複呼び出しを処理側でも拒否する | AttackUseCase、PlayPlayerTurnUseCase |
| `_busy`と`_executing`の違いは？ | 前者は敵の待機を含む進行全体の重複防止、後者は同期通知中の再入防止 | 両UseCase |
| キャンセルだけでなくRevisionを使う理由は？ | 古い待機が完了しても、新しい戦闘に適用しないため | BattleSession、PlayPlayerTurnUseCase |
| R3とMessagePipeの使い分けは？ | 現在の表示値と、発生したイベントを分ける。どちらも寿命に応じて購読解除 | ViewModel、Presenter、Button |
| DomainをUnity非依存にする理由は？ | ルールを画面・シーン・外部ライブラリから独立させて検証するため | Domain asmdef、Domainテスト |
| 予告と敵の行動がずれない理由は？ | 同じEnemyIntentの規則を参照する | BattleState、BattleSystem |
| データでどこまで変えられる？ | 既存行動の割り当て・表示、敵の値・パターンなど。新しい効果はコードが必要 | Definition、StageMaster、BattleSystem |
| この構成の弱点は？ | 規模に比べ依存が多い。Domain以外は単一Runtimeアセンブリが中心。画面結合の自動テストは未実装 | README、asmdef |
| 次に何を改善する？ | PlayModeで一連の遷移を自動検証し、ビルド・データ検証の再現性を上げる | テストの現在の範囲と比較 |

文章を暗記するより、回答した直後に該当メソッドを開く練習をしてください。

## 4. テストから仕様を逆に読む

1. [BattleActionRulesTests](Assets/Tests/EditMode/BattleActionRulesTests.cs) のクールダウンテストを読み、使用ターン1／不可2・3／再使用4を説明する。
2. [BattleUseCaseTests](Assets/Tests/Application/BattleUseCaseTests.cs) の通知順序と再入テストを読み、UIの連打防止と何が違うか説明する。
3. [RunFlowUseCaseTests](Assets/Tests/Application/RunFlowUseCaseTests.cs) のシーンロード失敗テストを読み、2回呼んでも強化が1回分で済む理由を説明する。
4. [RunStateTests](Assets/Tests/EditMode/RunStateTests.cs) の最終勝利を読み、3戦目では回復も次の強化も発生しないことを説明する。
5. [MasterDatabaseInitializationTests](Assets/Tests/Application/MasterDatabaseInitializationTests.cs) を読み、Core成功・UI失敗時に次回何をロードするか説明する。

「78件あるから十分」とは答えず、何を確認し、何を確認していないかを説明します。Prefabの表示・実際のDI・Addressables通信・性能は、このロジックテストだけでは保証しません。

## 5. 小さな変更課題

提出用の版を保存し、練習用のコピーで取り組んでください。

### 課題1：表示だけを変更

Attackの表示名をNormal attackに変更し、同じ定義を別のボタンにも割り当てます。

合格条件：C#を変更せず、両ボタンの表示と同じ行動の実行を確認できる。コードのルールまで変更できたとは説明しない。

### 課題2：強攻撃倍率を2倍から3倍へ変更

変更箇所を探す前に、影響するテストと表示を3つ以上挙げてください。

答えの目安：BattleSystemの倍率、ダメージ期待値、戦闘終了タイミングや攻略例、StrongAttack定義の表示名、予想されるゲームバランス。Domainだけ変更して「x2」表示を残さないこと。

### 課題3：回復量を20から15へ変更

RunState.RecoveryHpを変更した場合、どのテスト・結果文言・攻略例が変わるか調べます。

合格条件：回復上限とHP持ち越しを区別し、戦績の累計被ダメージが回復で減らない理由を説明できる。

### 課題4：ロード失敗の確認

実運用のデータを壊す代わりに、RunFlowUseCaseTestsの偽SceneServiceが失敗するケースを読みます。失敗時の周回フェーズ・Round・MaxHp・シーン呼び出し回数を、実行前に予想してください。

## 6. 最後に60秒で説明する

コードを閉じて、次の5点を各1文で説明します。

1. このゲームでプレイヤーが判断すること。
2. Domain / Application / Presentationの役割。
3. 戦闘と周回の状態を分けた理由。
4. 中断・重複入力・ロード失敗への対処。
5. 自動テストと手動確認の範囲、次に検証する点。

詰まった項目だけコードへ戻ります。分からない質問を受けたときは推測で断定せず、「その点は未検証なので、この経路を確認します」と調べる場所を示してください。
