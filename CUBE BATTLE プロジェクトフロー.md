# CUBE BATTLE プロジェクトの処理フロー

## 1. クライアントのビルドと更新コンテンツの配信

> **中文对照** 第 2 步为目标平台和 App Version 建立基线。第 3 步出底包，第 4 步校验基线后生成热更 DLL 和 Bundle，并输出到 ServerData。

```mermaid
flowchart LR
    A["1 Configure<br/>ビルド対象と更新設定を構成"] --> B["2 Prepare Client Content<br/>対象プラットフォームと App Version のベースラインを作成"]
    B --> C["3 Build Demo Player<br/>初期コンテンツを同梱したクライアントをビルド"]
    B --> D["4 Build Code And Resource Update<br/>ベースラインを検証し、更新 DLL と AssetBundle をビルド"]
    D --> E["ServerData/HybridCLR<br/>プラットフォーム別にマニフェストとコンテンツを出力"]
    C --> F["クライアントを起動"]
    E --> F
```

ベースラインはプラットフォームと App Version の組み合わせに対応します。手順 4 では、このベースラインを使って互換性を検証し、クライアントのビルド時に用意した AOT 補完メタデータを使用します。ベースライン作成時からビルド入力が変わった場合は、該当する入力を元に戻すか、App Version を上げて新しいクライアントを準備します。

## 2. クライアントの起動とコンテンツの選択

> **中文对照** Player 先建立随包或缓存候选，再查远端。远端校验失败时沿用已验证的候选。加载 AOT 元数据和热更程序集、打开标题 UI 并收到就绪信号后，才尝试提交 active.txt。

```mermaid
flowchart TD
    A["StartupLauncher<br/>Zenject の ProjectContext からコンテナを取得"] --> B{"実行環境"}
    B -->|Editor| C["エディター用のローダーとコンパイル済みゲームアセンブリを使用"]
    B -->|Player| D["同梱マニフェストを読み込み<br/>active.txt が指すキャッシュを検証"]
    D --> E{"Remote Root が設定されている？"}
    E -->|はい| F["配信先のマニフェストを取得<br/>クライアント ID、対象プラットフォーム、AOT メタデータを検証"]
    F -->|成功| G["ファイルを再利用またはダウンロードし、内容を検証"]
    F -->|失敗| H["検証済みキャッシュまたは同梱コンテンツを使用"]
    G -->|取得や検証に失敗| H
    E -->|いいえ| H
    G -->|成功| I["AOT メタデータを補完して更新 DLL をロード<br/>AssetBundle 用のローダーを使用"]
    H --> I
    C --> J["GameEntry.Start<br/>ゲーム用サービスを登録し、タイトルシーンをロード"]
    I --> J
    J --> K["タイトル UI の準備完了<br/>StartupReady.Complete"]
    K --> L["Player でキャッシュを選択した場合<br/>active.txt を更新"]
```

コンテンツの取得と検証が済んだ後、ゲームが起動して UI の準備完了を通知すると、起動処理は使用するキャッシュを `active.txt` に記録します。更新コンテンツでの起動に失敗した場合は、そのバージョンを拒否済みとして記録し、次回以降の起動時に同じバージョンをスキップします。

## 3. タイトル画面からバトルへ

> **中文对照** 标题按钮把模式存进共享 RunSession。两个模式进入同一战斗场景；三连战按 1、2、3 推进，无限模式在普通轮复用 1、2，Boss 轮用 3。续战在同一场景中准备。

```mermaid
flowchart TD
    A["タイトル画面"] -->|三連戦モード| B["StartNewRun：Expedition"]
    A -->|無限モード| C["StartNewRun：Endless"]
    B --> D["ResetRun<br/>モードと初期状態を RunSession に保存"]
    C --> D
    D --> E["バトルシーンをロード"]
    E --> F["BattleSceneEntryPoint が初期化を開始<br/>PrepareCurrentBattle が RunSession を参照"]
    F -->|三連戦| G["進行に合わせてステージ 1 → 2 → 3 を使用"]
    F -->|無限| H["通常ラウンドはステージ 1・2、ボスラウンドは 3<br/>ラウンド数に応じて敵の HP と攻撃力を調整"]
    G --> I["BattleSession を初期化し、バトル HUD を表示"]
    H --> I
    I --> J{"バトル結果"}
    J -->|勝利して続行| K["強化を選択し、同じシーン内で次のバトルを準備"]
    K --> M["次のステージを決定し、BattleSession と HUD を更新"]
    M --> J
    J -->|敗北または三連戦クリア| L["リザルトを表示"]
```

タイトル画面で選んだモードは、共有の `RunSession` に保存されます。バトルシーンは同じ状態を参照します。エディターでバトルシーンを直接実行し、状態が `NotStarted` の場合は、`PrepareCurrentBattle` が既定の三連戦モードで開始します。

## 4. バトル用マスターデータの流れ

> **中文对照** Stages 关联玩家、敌人和攻击模式，Characters 提供属性和默认技能 ID，Skills 提供技能数据。Excel 转为 master_core.bytes，RunFlowUseCase 从仓库读取并组装本场战斗。

```mermaid
flowchart LR
    A["Stages.xlsx<br/>ステージごとの登場キャラクターと敵の行動パターン"] --> D["Build From Excel<br/>Excel → CSV → master_core.bytes"]
    B["Characters.xlsx<br/>キャラクターのステータスとデフォルトスキル ID"] --> D
    C["Skills.xlsx<br/>スキルデータ"] --> D
    D --> E["MasterMemory のリポジトリ"]
    E --> F["RunFlowUseCase<br/>モードとラウンドに応じてバトルを初期化"]
```

説明するときは、タイトル画面のモード選択から始め、`RunSession` を経てバトルが始まるまでを追うと、ゲームの進行を示せます。その後、起動フローに戻ると、クライアントが更新されたコードやリソースを取得する仕組みを説明できます。
