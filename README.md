# Unity Turn-Based Battle Demo

Unity 6000.3.12f1 による、1vs1ターン制バトルです。全3戦の遠征と、敗北まで継続する無限モードを選べます。既存プロジェクトをベースに、戦闘の選択肢、周回フロー、責務分離、異常系テストを整備しました。小規模なゲームで、依存管理・非同期処理・状態通知・データロードの扱いを示すことを目的にしています。

## 遊べる内容

タイトルで次の2モードから選択します。

- 三連戦モード：第1戦 → 強化 → 第2戦 → 強化 → ボス → 最終結果。
- 無限モード：敗北するまで戦闘と強化を繰り返します。3ラウンドごとに敵のHPと攻撃力が上昇し、10ラウンドごとにボスが登場します。

- 通常攻撃・防御・強攻撃を選択。強攻撃には次の2プレイヤーターンのクールダウンがあります。
- Battle SceneではPrefabで構成した簡易3D人物が、攻撃・防御・被弾・戦闘不能を表示します。モデルと通常色はAssets/Prefabs/BattleCharactersで編集できます。
- EditorまたはDevelopment Buildでは、Battle Scene右上の`PERF`ボタンからFPS、フレーム時間、使用メモリを確認できます。本番ビルドでは自動的に無効になります。
- Title、Battle HUD、Result Popupの固定レイアウト・配色・文字サイズはPrefabで編集します。すべてのUIはMVVMを使い、ViewはUI部品の更新と入力転送、ViewModelは表示状態・多言語テキスト・UseCase呼び出しを担当します。 性能パネルもBattle HUD Prefabの子として配置し、文字の描辺はAssets/Settings/UIのMaterial Presetで設定します。
- UIは日本語／英語に対応します。初回は端末言語を参照し、Title右上の言語ボタンで切り替えた選択を`PlayerPrefs`へ保存します。ゲームロジックはローカライズキーを持たず、表示層が`ILocalizationService`を通して文言を取得します。
- Editor／モバイル版ともに60fpsを上限に動作します。モバイル版は左右両方向の横画面を正式対応とし、AndroidではSwappy frame pacingを有効化しています。
- 敵の行動予告を見て防御を判断。3種類の敵で攻撃パターンが変わります。
- 戦闘間はHPを最大20回復して持ち越し、攻撃・最大HP・防御性能から強化を選びます。
- 最終結果に勝利数、到達戦数／ラウンド、総ターン数、累計被ダメージを表示します。

## Mobile build policy

- Unity 6のBuild ProfilesからiOSまたはAndroidを選択してください。
- Product Nameは`Cube Battle`、Android Application ID／iOS Bundle Identifierは`com.wzq.turnbattle`、初期Versionは`1.0.0`に設定済みです。
- UIはPrefabのRectTransformで配置します。実機の表示領域に合わせた調整もPrefabで行います。
- Android/iOSのRelease buildではPerformance Viewを表示しません。計測する場合はDevelopment Buildを使用してください。
- 最低限、16:9、19.5:9、20:9の横画面と、左右両方の端末回転でTitle → Battle → Result → 次戦/Retryを確認してください。
- 音声は使用しません。

操作は [PLAY_GUIDE.ja.md](PLAY_GUIDE.ja.md)、行動ボタンの設定は [ACTION_CONFIGURATION.ja.md](ACTION_CONFIGURATION.ja.md) を参照してください。

## 起動

1. Windows のパス長制限を避け、`C:\Dev\BattleDemo` など短いパスへ展開します。
2. Unity Hub で Assets / Packages / ProjectSettings のあるフォルダーを追加し、6000.3.12f1 で開きます。
3. 依存パッケージの取得とコンパイル後、`Assets/Scenes/Title.unity` を開いて Play → Start Expedition。
4. Editor は LocalAssetCatalog の対応表からローカルアセットを直接読み込みます。Player ビルド前に AssetBundle を自動生成します。手動実行は Tools > AssetBundles > Build Local Bundles。

マスターは `Assets/MasterData/Excel` の5つのExcelで編集し、Tools > MasterData > Build Demo MasterData で CSV → MasterMemory のバイナリへ変換します。[編集・変換手順](MASTER_DATA_SOURCE.zh-CN.md) と [SETUP.ja.md](SETUP.ja.md) を参照してください。

## 責務と依存方向

```text
Presentation → Application → Domain
      │             │
      └──────┬──────┘
             ↓
       Infrastructure

Installers：各実装の生成と依存関係を登録
Core：共通知識のみ
```

| 層 | 主な責務 | 代表コード |
|---|---|---|
| Domain | 戦闘・周回のルールと状態遷移。Unity API非依存 | BattleSystem、BattleState、RunState |
| Application | 入力要求の処理、敵ターン待機、周回進行、通知 | AttackUseCase、PlayPlayerTurnUseCase、RunFlowUseCase |
| Presentation | ボタン入力、表示用状態、結果画面 | BattleActionButton、BattleViewModel、BattleEventPresenter |
| Infrastructure | UI生成、アセット・マスターデータのロード、シーン操作 | UIService、UIViewFactory、AssetBundleMasterDatabaseLoader |
| Installers | Project／Scene単位の依存登録 | ProjectInstaller、BattleSceneInstaller、SceneInstaller |

Domain は専用の `BattleDemo.Domain` アセンブリで Unity 参照を無効にしています。それ以外の製品コードは主に `BattleDemo.Runtime` にまとまっています。全層を個別アセンブリで隔離する構成ではなく、Application → Presentation の参照禁止などはソース検査テストで補っています。

## 設計判断

### 1. 1戦と周回の状態を分ける

BattleSession はその戦闘のHP、ターン、クールダウン、防御状態、勝敗を保持します。RunSession / RunState は戦闘をまたぐHP、強化、現在の戦数、戦績を保持します。

同じ場所に保持すると「次の戦闘へ残す値」と「初期化する値」が混ざります。戦闘開始時に RunFlowUseCase が周回状態から新しい戦闘状態を作ることで、クールダウンだけ残る等の不具合を避けています。これらはメモリー上の状態で、周回の途中保存は実装していません。

### 2. 入力、進行、ルールを分ける

ボタンは行動IDを渡し、PlayPlayerTurnUseCase がプレイヤー行動→待機→敵行動を進めます。戦闘開始時に RunFlowUseCase が SkillMaster.Power を BattleUnitSnapshot.BasePower へ確定し、AttackUseCase はそのSnapshotで Domain を実行して結果を通知します。ダメージ倍率、防御、クールダウン、敵の予告は Domain が判定します。

敵の予告と実行は同じ EnemyIntent を参照するため、別々の判定式を持ちません。結果画面は通知を受けた Presenter が開き、Application が View 型を参照しない構成です。

### 3. 定義で変えられる範囲を明確にする

BattleActionDefinition は既存の行動ID、表示名、説明文を持つ ScriptableObject です。共通の BattleActionButton に割り当てると、同じ View コードで行動を変更できます。

新しい表示名、配置、既存行動の割り当てはデータ／Prefabで変更できます。一方、回復や毒など新しいルールを追加する場合は Domain / UseCase とテストの変更が必要です。効果を自由に組み立てる汎用スキルシステムではありません。

敵・スキル・ステージ・UI定義は MasterMemory、アセット配布単位はローカル AssetBundle を利用しています。攻撃倍率や強化の効果量など、コードで定義したルールもあります。

### 4. ライブラリの役割を分ける

| ライブラリ | このデモで担当させた役割 |
|---|---|
| Zenject | Project／Sceneスコープで生成・依存を管理。シーン内のUIはシーン側のコンテナーで生成 |
| UniTask | シーン／アセットロードと敵の待機。キャンセルと例外を扱う |
| R3 | ViewModelの現在値とUIの同期、購読の寿命管理 |
| MessagePipe | ダメージ、行動、ターン、決着など発生したイベントの通知 |
| MasterMemory | 型付きのマスターデータ参照とバイナリ生成 |
| AssetBundle | StreamingAssets からの Prefab・マスターの非同期ロードと参照管理 |

R3 は「いま表示すべき値」、MessagePipe は「何が起きたか」の通知に使い分けています。同期呼び出しや状態参照も残しており、全てをイベントで接続しているわけではありません。

小規模ゲームとしては依存が多めです。今回は各基盤の責務と連携を示すため採用しています。実案件ではチーム標準と要件に応じて簡略化・差し替えを判断します。

### 5. 中断・失敗を通常の処理経路として扱う

- HUD閉鎖とシーン終了で敵待機をキャンセル。待機後にもキャンセルを確認します。
- BattleSession の Revision を確認し、古い戦闘の遅延処理を再戦へ適用しません。
- UIの無効化に加え、UseCaseでも連打・通知中の再入・終了後の攻撃を拒否します。これらの実行ガードはUnityのメインスレッド利用を前提とします。
- マスターロードは成功分を保持し、再呼び出しで失敗箇所から再試行。失敗したキーと原因を例外に残します。
- 次のシーンのロードに失敗しても、再試行で強化を二重適用しません。
- アセットの成功ロードは所有側の破棄時、失敗ロードはロード処理内で解放します。

### 6. 表示言語をゲームルールから分離する

`ILocalizationService`を境界にして英語／日本語テーブルを切り替えます。端末言語による初期選択と、ユーザーがTitleで選んだ言語の永続化を分離し、未登録キーは空文字ではなくキーそのものを表示して不足を発見しやすくしています。

日本語グリフにはNoto Sans JPの動的TextMeshPro Font Assetを使用します。フォント本体はSIL Open Font License 1.1で、ライセンス文は`Assets/Resources/Localization/OFL.txt`に同梱しています。

## 検証範囲

| 確認項目 | 状況 |
|---|---|
| 戦闘・周回・コマンド状態等のEditModeテスト | Domain、UseCase、Repository、初期化、依存方向を検証 |
| 使用した外部アセンブリ | NUnit 3.14.0、UniTask 2.5.11、MessagePipe 1.8.2、プロジェクト同梱のR3等 |
| 全9通りの強化による攻略 | 予告に応じる戦略で3戦クリアを自動検証 |
| Unity上の手動動作確認 | 利用者による確認済み |
| Unity Test RunnerでのEditModeテスト実行 | 利用環境で要確認 |
| PlayMode smoke test | UI アセット3件とTitle Scene表示の4本を追加。実行は利用環境で要確認 |
| プレイヤービルド、性能計測、リモート配布 | この作業では未確認 |

テストは `Assets/Tests/EditMode`、`Assets/Tests/Application`、`Assets/Tests/PlayMode` にあります。ロジックテストをUnity用パッケージ・ソースジェネレーター・実シーンの全自動検証と同一視していません。Unity上では Test Runner からEditModeとPlayModeをそれぞれ実行します。

## 次に改善するなら

次はPlayModeでBattle Sceneの実DIとInspector参照を検証し、タイトル→戦闘→強化→決着→再挑戦を自動化します。その後、必要に応じてデータ検証とビルド手順をCIへ組み込みます。機能追加より、再現可能な確認手順と観測性の改善を優先します。

## 面接・レビュー用資料

- [3分のデモ台本](INTERVIEW_DEMO.ja.md)
- [設計説明](DESIGN_EXPLANATION.ja.md)
- [クライアントエンジニア面接用ガイド・想定Q&A](INTERVIEW_GUIDE.ja.md)
- [主要コードの読み方・練習問題](CODE_READING_PRACTICE.ja.md)
- [操作と攻略例](PLAY_GUIDE.ja.md)
- [行動ボタンの設定](ACTION_CONFIGURATION.ja.md)

第三者ライブラリのライセンスと元プロジェクトの LICENSE は保持しています。

## 多语言数据

英日文本由 `Assets/MasterData/Excel/Localization.xlsx` 维护，导出为 `master_localization.bytes`，经 AssetBundle 和 LocalizationRepository 提供给 ILocalizationService。服务内部不再有硬编码翻译字典。修改方法和验证范围见 [多语言MasterData说明](LOCALIZATION_MASTER_DATA.zh-CN.md)。

## Excelデータ編集（現在の既定フロー）

`Assets/MasterData/Excel` の Characters / Skills / Stages / UI / Localization を編集し、Tools > MasterData > Build From Excel で5つのCSVと3つのMasterMemoryバイナリを生成します。[Excel編集手順](EXCEL_MASTER_DATA.zh-CN.md) を参照してください。旧MasterData ScriptableObjectとLegacyメニューは削除済みです。

UIの責務分離とライフサイクルは [MVVM.zh-CN.md](MVVM.zh-CN.md) に記載しています。

本地资源包的构建、加载和验证方式见 [AssetBundle 使用说明](ASSETBUNDLE.zh-CN.md)。
