# Unity Turn-Based Battle Demo

## 概要

本プロジェクトは、Unity を用いた 1vs1 のターン制バトルデモです。
Zenject / UniTask / R3 / MessagePipe / MasterMemory / Wwise / Addressables を組み合わせ、
実務を意識したクライアントアーキテクチャで構成しています。

本デモでは、ゲームフロー自体はシンプルに保ちつつ、
以下のような**商用案件を意識した設計方針**を取り入れています。

* 依存関係を Zenject で管理
* 業務フローを UseCase 単位で分離
* UI を定義駆動で生成・配置
* マスターデータを MasterMemory で管理
* モジュール間通知を MessagePipe で疎結合化
* 音響基盤を Wwise で抽象化
* データ配布と更新を Addressables で分割管理

---

## アーキテクチャ図

```text
┌──────────────────────────────────────────────────────────────┐
│                        Presentation                          │
│  View / ViewModel(R3) / UI Components                        │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                        Application                           │
│  UseCase / GameFlow / Message Handlers                       │
│  - StartBattleUseCase                                        │
│  - AttackUseCase                                             │
│  - UI Open/Close orchestration                               │
└──────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                          Domain                              │
│  BattleSystem / Entities / Rules                             │
│  ※ Unity 非依存の純ロジック                                   │
└──────────────────────────────────────────────────────────────┘
                              │
               ┌──────────────┴──────────────┐
               ▼                             ▼
┌──────────────────────────────┐  ┌──────────────────────────────┐
│        Infrastructure        │  │          Installers          │
│  Asset / UI / Audio / Data   │  │  Zenject Composition Root    │
│  Message / Save / Scene      │  │  ProjectInstaller            │
│                              │  │  SceneInstaller              │
│  - Addressables              │  │  UIInstaller                 │
│  - MasterMemory              │  └──────────────────────────────┘
│  - MessagePipe               │
│  - Wwise                     │
└──────────────────────────────┘
```

---

## レイヤー構成

### 1. Core

共通ユーティリティや補助クラスを配置する層です。
ログ、拡張メソッド、共通 Result 型など、
**どの層からも利用されるが業務知識を持たないコード**を置きます。

### 2. Domain

ゲームルールの中心となる層です。
例えば BattleSystem、ダメージ計算、勝敗判定などをここに置きます。

この層は以下に依存しません。

* Unity API
* Zenject
* Addressables
* Wwise
* MessagePipe

つまり、**もっとも純度の高いロジック層**です。

### 3. Application

ユースケース単位で処理の流れを管理する層です。
Domain を直接 UI から呼ばず、UseCase を経由することで、
「何を」「どの順番で」「どの通知と組み合わせて」実行するかを整理しています。

例:

* StartBattleUseCase
* AttackUseCase
* ResultPopupOpenUseCase

### 4. Infrastructure

外部システムや基盤機能をまとめる層です。
いわゆる従来の GameFramework が持っていた役割を、
**小さなサービスに分割して再構成したレイヤー**です。

主な担当:

* AssetService（Addressables）
* UIService / PopupService
* AudioService（Wwise）
* MasterDataRepository（MasterMemory）
* Messaging（MessagePipe）
* Save / Scene

### 5. Presentation

画面表示と入力を担当する層です。
View は MonoBehaviour、ViewModel は R3 を用いて状態を保持します。

方針としては、

* View: 表示と入力のみ
* ViewModel: 表示用状態の保持
* UseCase: 処理の実行

という責務分離を行っています。

### 6. Installers

Zenject による依存関係の登録を行う Composition Root です。
プロジェクト全体やシーン単位の依存関係を一か所に集め、
インスタンス生成の責務を分離しています。

---

## 依存方向

本プロジェクトでは、依存方向を意識して層を分割しています。

```text
Presentation -> Application -> Domain
        ↓
Infrastructure
```

* Presentation は Application を利用する
* Application は Domain と Infrastructure を利用する
* Domain は外部層に依存しない

この構成により、

* テストしやすい
* 差し替えしやすい
* 責務が明確になる

という利点があります。

---

## UI アーキテクチャ

本プロジェクトでは、従来の単一 UIManager ではなく、
UI の責務を分割しています。

### 構成

* UIService: UI の開閉制御
* PopupService: Popup 専用制御
* UIViewFactory: Prefab 生成
* UILayerService: 配置先レイヤー管理
* UIRegistry: 開いている UI の管理
* UIMaster: UI 定義データ

### 設計意図

各 UI はコードに直書きせず、UIMaster によって以下を定義します。

* PrefabKey
* Layer
* Type
* Cache 方針
* Singleton 制御
* BlockInput

これにより、UIService 自体を変更せずに
**画面追加やレイヤー変更が可能**になります。

---

## マスターデータ構成

MasterMemory を利用し、マスターデータを分割して管理しています。

### 分割例

* master_core.bytes

  * CharacterMaster
  * SkillMaster
  * StageMaster

* master_ui.bytes

  * UIMaster

* master_audio.bytes

  * AudioEventMaster

### 分割理由

* 更新粒度を細かくするため
* 起動時ロードを最適化するため
* モジュール単位で責務を分けるため

Addressables により必要なタイミングでロードし、
MasterMemory の `MemoryDatabase` として利用します。

---

## メッセージング方針

モジュール間の通知には MessagePipe を使用しています。

例:

* BattleStarted
* DamageApplied
* BattleFinished
* PlaySeRequested
* OpenPopupRequested

これにより、
UI、Audio、Log などの機能を直接参照せずに連携でき、
**疎結合な構成**を維持できます。

---

## オーディオ方針

オーディオ再生は Wwise を直接各所から呼ばず、
`IAudioService` を介して抽象化しています。

これにより、

* 実装差し替えが容易
* 業務ロジックと音響実装を分離できる
* テスト時にダミー実装へ置き換え可能

という利点があります。

---

## フォルダ構成

```text
Assets/
├─ Core
├─ Domain
├─ Application
├─ Infrastructure
│  ├─ Asset
│  ├─ Audio
│  ├─ MasterData
│  ├─ Messaging
│  ├─ Scene
│  ├─ Save
│  └─ UI
├─ Presentation
│  ├─ Battle
│  ├─ Common
│  └─ Title
├─ Installers
└─ Scenes
```

---

## 代表的な処理フロー

### 攻撃処理

```text
BattleHudView
  ↓ ボタン入力
BattleViewModel
  ↓
AttackUseCase
  ↓
BattleSystem
  ↓
DamageApplied を Publish
  ↓
- BattleViewModel が HP を更新
- AudioHandler が SE 再生要求を処理
- LogHandler がログを追加
- 必要に応じて ResultPopup を開く
```

このように、入力、業務フロー、純ロジック、通知、表示更新を分離しています。

---

## この構成を採用した理由

本プロジェクトでは、単に動作するデモを作るのではなく、
以下の観点を重視しました。

* 拡張しやすいこと
* 依存関係が明確であること
* 実案件に近い説明ができること
* UI / Audio / Data / Logic が混ざらないこと

そのため、従来の「巨大な Manager を中心にした構成」ではなく、
責務ごとにサービスと層を分けた構成を採用しています。

---

## 今後の拡張案

* Popup BackStack 管理
* 画面遷移アニメーション
* MasterData のビルドパイプライン自動化
* Addressables のリモート更新対応強化
* 戦闘スキル演出の追加
* セーブデータと進行状態の統合管理


