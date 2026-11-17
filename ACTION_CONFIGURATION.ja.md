# バトル行動ボタンの設定

`BattleHudView.prefab` の各ボタンに `BattleActionButton` が付いています。Inspector の **Action** で `Attack`、`Defend`、`StrongAttack` を選択します。

- **Label** には表示用の `TMP_Text` を割り当てます。空欄の場合は子のテキストを取得します。
- ボタンの文言は `BattleViewModel` がローカライズデータから取得します。使用可否と残りクールダウンも ViewModel から反映されます。
- 既存の行動を割り当てたボタンを増やすには、HUD 配下のボタンを複製し、Action と配置を調整します。
- 新しい種類の行動を追加する場合は、`BattleAction`、戦闘ルールと実行処理、表示用の文言を更新します。

入力は `BattleActionButton` → HUD の共通コールバック → `PlayPlayerTurnUseCase` → `AttackUseCase` → Domain の順に渡ります。

> **中文对照** 在 HUD 的每个 BattleActionButton 上直接选择 Action。Label 可留空，由组件找子节点文本。ViewModel 提供本地化文字、可用状态和冷却。复制按钮可以复用已有行动；新增行动类型需要修改枚举、执行规则和文案。输入经 HUD、PlayPlayerTurnUseCase 和 AttackUseCase 交给 Domain。
