# Unity 6000.3.12f1 向け 作業内容と起動手順

## 開き方

1. Windows のパス長制限を避けるため、ZIP を `C:\Dev\BattleDemo` など短いパスに展開してください。
2. Unity Hub から Assets / Packages / ProjectSettings があるフォルダーを追加します。
3. **Unity 6000.3.12f1** で開き、初回の Package Manager の依存関係取得とコンパイルを待ちます。Git のインストールとネット接続が必要です。
4. `Assets/Scenes/Title.unity` を開いて Play。Start Expedition → 第1戦 → 強化選択 → 第2戦 → 強化選択 → ボス → 最終結果 → Retry またはタイトル の順に確認できます。
5. プレイヤービルド前は Addressables Groups の Build > New Build > Default Build Script を実行します。Title がビルドの先頭シーンです。

Editor の Addressables Play Mode Script は Use Asset Database を選択すると、ローカルアセットで確認できます。
マスターを再生成する場合は Tools > MasterData > Build Demo MasterData を実行し、配布用 Addressables も再ビルドしてください。

## 補完した機能

- 結果画面からタイトルへ戻り、再び戦闘を開始できる導線。
- Start の連打によるシーンロード重複の抑止。
- UI のファクトリー・レジストリー・ViewModel をシーン単位で管理。シーンの DI で UI を生成。
- UIMaster の Cache / Singleton / BlockInput の適用。同時 Open は直列化し、非 Singleton は最後に開いたものから Close。
- Popup / Screen の Type 検証。Popup の BackStack や画面遷移アニメーションは拡張対象のままです。
- UI 破棄時の Addressables 参照解放。マスターロードは失敗箇所から再試行可能にし、ロードハンドルを解放。
- HUD の購読解除と再購読、敵ターン待機のキャンセル、シーンごとの戦闘ログ初期化と40行制限。
- Domain を Unity API 非参照の asmdef に分離。入力検証と EditMode テストを追加。
- Battle シーンを直接再生する場合の初期化。

## 音声について

本プロジェクトは音声を使用しません。音声サービス・通知・音声マスター・シーンの AudioListener を削除済みです。Unity の Disable Audio も有効にしています。追加の音声 SDK や SoundBank は不要です。

## 検証結果

- 音声項目を除いた core マスターを再生成し、同梱 MessagePack ライブラリで全3テーブルをデコードして値を確認済み。

- Domain 全ソースを PowerShell の Add-Type で実際に C# コンパイルし、ターン交代・ラウンド数・勝利・敗北・HP 下限・再初期化・負のダメージ拒否を確認済み。
- Unity Test Runner 用のテストを Assets/Tests/EditMode に追加。Window > General > Test Runner > EditMode > Run All で実行できます。
- Unity 全体のコンパイル・Test Runner・Play Mode・プレイヤービルドは未確認です。今回の調整ではご指定どおり Unity を起動していません。最終的な動作確認は利用者側で実施してください。

元リポジトリのライブラリとシーン・Prefab・バイナリマスターを引き継いでいます。README も音声なしの構成に更新しています。第三者ライセンスおよび LICENSE も保持しています。

## 面接向け改善：依存方向と異常系テスト

- Application の OpenResultPopupUseCase を削除し、結果通知を受けた BattleEventPresenter が表示を担当します。Application は Presentation の View / Context を参照しません。
- 敵の待機と行動は PlayPlayerTurnUseCase に集約。HUD は入力を渡し、表示を更新します。ITurnDelay の実装には UniTask を使用しています。
- UseCase が入力の重複・通知中の再入・未初期化・終了後の行動を拒否します。
- シーン終了および HUD 閉鎖のキャンセルに加え、BattleSession の Revision で古い待機処理から再戦への干渉を防ぎます。
- マスターロードを IMasterDatabaseLoader に分離。失敗したキーと原因を MasterDataLoadException に保持し、再度 InitializeAsync を呼ぶと失敗箇所から再試行します。成功済みの DB は再ロードしません。
- MasterDatabaseInitialization の共通処理は、テスト用の制御可能なローダーで再試行・同時呼び出しを検証します。Addressables 自体の通信やハンドル解放は Unity 側で別途確認してください。
- 独立した NUnit テストから製品コードを参照できるよう BattleDemo.Runtime と Editor 用アセンブリを定義しました。既存のスクリプト GUID は保持しています。

### テストの範囲と結果

Unity を起動せず、実際の製品ソースと NUnit 3.14.0 / UniTask 2.5.11 / MessagePipe 1.8.2 の .NET アセンブリを使って **26 ケース成功**を確認しました。
これはロジックの検証です。Unity 用パッケージ、ソースジェネレーター、Zenject の実際のシーン注入、Prefab の表示、Addressables の実ロードは未検証です。

- ArchitectureTests: 2 ケース。Application から Presentation への参照と Domain への外部依存を検出。
- Domain: 6 ケース（パラメーター化された勝敗2ケースを含む）。
- BattleUseCaseTests: 14 ケース。通知順序・操作制限・終了通知1回・再入防止・スキル取得失敗後の再試行・再戦・遅延処理の重複/キャンセルを確認。
- MasterDatabaseInitializationTests: 4 ケース。Core失敗・UIだけの失敗・同時初期化・null応答からの再試行を確認。

Unity では Test Runner > EditMode > Run All を実行してください。追加したテストは Assets/Tests/Application、既存の Domain テストは Assets/Tests/EditMode にあります。

## Addressables のコンパイル修正

BattleDemo.Runtime.asmdef に UniTask.Addressables の参照を追加しました。AssetService とマスターローダーは型付き ToUniTask<T>() でロード結果を取得し、AssetService のロード失敗時にもハンドルを解放します。既存プロジェクトへ適用する場合は、この asmdef と両ローダーの3ファイルをまとめて更新してください。Unity 上でのコンパイルは未確認です。

## 3つの行動と敵の予告（最新の追加）

操作とルールは PLAY_GUIDE.ja.md を参照してください。HUD Prefab に防御・強攻撃ボタン、敵の予告、ガード状態、クールダウン表示を追加しました。Canvas は 1280×720 を基準に画面サイズへ追従します。追加の Inspector 設定は不要です。

通常攻撃20、強攻撃40、防御は次の敵攻撃を半減（端数切り上げ）。強攻撃は使用したターンの3ターン後に再使用可能です。敵の通常攻撃10、3ターンごとの強攻撃30、HP140に調整し、同梱の core マスターも再生成しました。バイナリマスターとエディターの生成コードは同じ値です。

今回の追加後は合計 **42ケース成功**（従来26ケース＋行動・バランスの16ケース）です。製品の C# ソースと実ライブラリを用いた .NET 上の検証で、Unity は起動していません。Prefab の参照整合性と C# 構文も確認しました。新しいボタンの見た目・シーン注入・実際の画面操作は Unity 側でご確認ください。

既存プロジェクトへ反映する場合は Assets/Scripts、Assets/Tests、HUD/UIRoot の Prefab、master_core.bytes を含む一式を更新してください。Addressables をビルド済みの場合は再ビルドが必要です。

## 行動定義の割り当て（最新）

ACTION_CONFIGURATION.ja.md に Inspector の設定手順をまとめました。Assets/Settings/BattleActions の定義を各 BattleActionButton に割り当てます。HUD には3ボタンを設定済みで、行動ごとの専用クリック処理は削除しました。既存の Domain のルールは維持しています。

今回追加した共通状態の7ケースを含め、最新のテスト結果は49ケース成功です。Unity は起動していません。Assets/Scripts、Assets/Tests、Assets/Settings/BattleActions、BattleHudView.prefab を .meta とともに更新してください。

## 全3戦の周回フロー（最新）

PLAY_GUIDE.ja.md を現在のルールに更新しました。タイトルから3戦に挑戦し、第1・2戦後に3択の強化を選びます。HPは最大20回復して持ち越し、最後に戦績を表示します。途中敗北・最終勝利の両方から再挑戦できます。

- Domain.Run.RunState：周回フェーズ、HP、強化、勝利数・総ターン数・累計被ダメージ。
- Application.Services.RunSession：周回状態を保持。BattleSession は1戦ごとの防御・クールダウン・勝敗を保持。
- RunFlowUseCase：新規周回、強化、次の戦闘の準備とロード、決着の記録を担当。旧 StartBattleUseCase は削除しました。
- ResultPopupView：中間結果では強化3択、最終結果では Retry expedition / Return to title を表示。次の戦闘のロード失敗時は選択済みの強化を再度押して再試行できます。強化は重複適用しません。
- StageMaster：行動パターンと表示名を追加。3ステージ・4キャラクター・4スキルを同梱バイナリへ反映。

従来49ケースに29ケース追加し、最新の結果は **78ケース成功**です。3戦クリア・途中敗北と再挑戦・HP持ち越し・回復上限・強化全9通り・戦績・決着通知重複・ロード失敗後の再試行・敵予告を検証しました。Unity は起動していません。

既存プロジェクトへ適用するときは Scripts / Tests と .meta、BattleHudView / ResultPopupView / TitleScreenView の Prefab、Defend の行動定義、master_core.bytes を含む一式を更新してください。古い StartBattleUseCase.cs と .meta は削除してください。スキーマとバイナリの両方が変わるため、Addressables は必ず再ビルドしてください。
