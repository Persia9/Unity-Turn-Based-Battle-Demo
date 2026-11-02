using System;
using System.Collections.Generic;
using System.Globalization;
using Application.Services;
using UnityEngine;

namespace Infrastructure.Localization
{
    public sealed class UnityLocalizationService : ILocalizationService
    {
        private const string PreferenceKey = "game.language";
        private readonly Dictionary<string, string> _english = new()
        {
            ["language.toggle"] = "JP",
            ["title.genre"] = "TACTICAL TURN-BASED RPG",
            ["title.subtitle"] = "THREE-BATTLE EXPEDITION",
            ["title.tagline"] = "READ THE ENEMY   •   CHOOSE YOUR ACTION   •   SURVIVE",
            ["title.start"] = "BEGIN EXPEDITION",
            ["title.start.expedition"] = "EXPEDITION\nTHREE BATTLES",
            ["title.start.endless"] = "ENDLESS\nFIGHT UNTIL DEFEAT",
            ["side.player"] = "PLAYER", ["side.enemy"] = "ENEMY",
            ["character.hero"] = "HERO", ["character.slime"] = "SLIME",
            ["character.sentinel"] = "SENTINEL", ["character.warden"] = "WARDEN",
            ["battle.run"] = "BATTLE {0:00} / 03   {1}\nATK BOOST +{2}    GUARD {3}%",
            ["battle.run.endless"] = "ENDLESS  ROUND {0:00}   {1}\nATK BOOST +{2}    GUARD {3}%",
            ["battle.turn"] = "TURN {0}  •  {1}", ["battle.start"] = "Battle Start: choose an action.",
            ["battle.finished"] = "Battle finished", ["battle.intent.normal"] = "Enemy next: Normal attack",
            ["battle.intent.strong"] = "Enemy next: STRONG ATTACK x3 - Consider defending!",
            ["battle.guard.active"] = "Guard active: next hit takes {0}% damage",
            ["battle.guard.inactive"] = "Defend: reduce the next enemy hit by {0}%",
            ["battle.hero_hp"] = "<size=18><color=#82B7FF>HERO</color>   ATK {2}</size>\nHP  {0} / {1}",
            ["battle.enemy_hp"] = "<size=18>ATK {2}   <color=#FF9B8E>ENEMY</color></size>\nHP  {0} / {1}",
            ["action.attack"] = "Attack", ["action.defend"] = "Defend", ["action.strong"] = "Strong attack x2",
            ["action.cooldown"] = "{0} (CD {1})", ["action.unassigned"] = "Unassigned",
            ["log.player_damage"] = "Player takes {0} damage.", ["log.enemy_damage"] = "Enemy takes {0} damage.",
            ["log.player_defends"] = "Player defends: next enemy damage is reduced.",
            ["log.action"] = "{0} uses {1}.", ["log.turn_changed"] = "Turn changed: {0}",
            ["log.battle_finished"] = "Battle finished. Winner: {0}",
            ["log.result_failed"] = "Failed to open the result screen.",
            ["result.title"] = "EXPEDITION  //  RESULT", ["result.round_clear"] = "BATTLE {0}/3 CLEARED!",
            ["result.title.endless"] = "ENDLESS  //  RESULT",
            ["result.round_clear.endless"] = "ROUND {0} CLEARED!",
            ["result.cleared"] = "EXPEDITION CLEARED!", ["result.failed"] = "EXPEDITION FAILED",
            ["result.failed.endless"] = "ENDLESS RUN ENDED",
            ["result.progress"] = "PROGRESS", ["result.wins"] = "{0} / 3 WINS", ["result.reached"] = "REACHED {0} / 3",
            ["result.wins.endless"] = "{0} WINS", ["result.reached.endless"] = "REACHED ROUND {0}",
            ["result.turns"] = "TURNS", ["result.damage"] = "DAMAGE TAKEN",
            ["result.guidance.upgrade"] = "Recovered up to 20 HP  •  Choose one upgrade for the next battle",
            ["result.guidance.retry"] = "Retry starts a fresh expedition from battle 1",
            ["result.guidance.retry.endless"] = "Retry starts a fresh endless run from round 1",
            ["result.attack"] = "Attack +5\nBoost all attacks", ["result.vitality"] = "Vitality +25\nMax HP +25 & heal 25",
            ["result.guard"] = "Guard +15 pts\nReduce damage while guarding",
            ["result.retry"] = "Retry expedition", ["result.retry.endless"] = "Retry endless run", ["result.return"] = "Return to title",
            ["result.load_upgrade_failed"] = "Load failed. Press your chosen upgrade again to retry.",
            ["result.load_retry_failed"] = "Load failed. Retry or return to title."
        };

        private readonly Dictionary<string, string> _japanese = new()
        {
            ["language.toggle"] = "EN",
            ["title.genre"] = "戦略ターン制RPG", ["title.subtitle"] = "三連戦エクスペディション",
            ["title.tagline"] = "敵を読む   •   行動を選ぶ   •   三連戦を生き抜く", ["title.start"] = "遠征を開始",
            ["title.start.expedition"] = "三連戦モード\n3戦を突破",
            ["title.start.endless"] = "無限モード\n敗北まで挑戦",
            ["side.player"] = "プレイヤー", ["side.enemy"] = "敵",
            ["character.hero"] = "勇者", ["character.slime"] = "スライム",
            ["character.sentinel"] = "センチネル", ["character.warden"] = "ウォーデン",
            ["battle.run"] = "バトル {0:00} / 03   {1}\n攻撃強化 +{2}    防御 {3}%",
            ["battle.run.endless"] = "無限モード  ラウンド {0:00}   {1}\n攻撃強化 +{2}    防御 {3}%",
            ["battle.turn"] = "ターン {0}  •  {1}", ["battle.start"] = "戦闘開始：行動を選んでください。",
            ["battle.finished"] = "戦闘終了", ["battle.intent.normal"] = "敵の次の行動：通常攻撃",
            ["battle.intent.strong"] = "敵の次の行動：強攻撃 ×3 — 防御推奨！",
            ["battle.guard.active"] = "防御中：次の被ダメージは{0}%",
            ["battle.guard.inactive"] = "防御：次の敵攻撃を{0}%軽減",
            ["battle.hero_hp"] = "<size=18><color=#82B7FF>勇者</color>   攻撃 {2}</size>\nHP  {0} / {1}",
            ["battle.enemy_hp"] = "<size=18>攻撃 {2}   <color=#FF9B8E>敵</color></size>\nHP  {0} / {1}",
            ["action.attack"] = "攻撃", ["action.defend"] = "防御", ["action.strong"] = "強攻撃 ×2",
            ["action.cooldown"] = "{0}（あと{1}ターン）", ["action.unassigned"] = "未設定",
            ["log.player_damage"] = "プレイヤーは{0}ダメージを受けた。", ["log.enemy_damage"] = "敵に{0}ダメージ。",
            ["log.player_defends"] = "プレイヤーは防御した。次の被ダメージを軽減。",
            ["log.action"] = "{0}の行動：{1}。", ["log.turn_changed"] = "{0}のターン。",
            ["log.battle_finished"] = "戦闘終了。勝者：{0}", ["log.result_failed"] = "結果画面を開けませんでした。",
            ["result.title"] = "遠征  //  戦闘結果", ["result.round_clear"] = "バトル {0}/3 クリア！",
            ["result.title.endless"] = "無限モード  //  戦闘結果",
            ["result.round_clear.endless"] = "ラウンド {0} クリア！",
            ["result.cleared"] = "遠征クリア！", ["result.failed"] = "遠征失敗",
            ["result.failed.endless"] = "無限モード終了",
            ["result.progress"] = "進行状況", ["result.wins"] = "{0} / 3 勝", ["result.reached"] = "到達 {0} / 3",
            ["result.wins.endless"] = "{0} 勝", ["result.reached.endless"] = "到達ラウンド {0}",
            ["result.turns"] = "総ターン", ["result.damage"] = "被ダメージ",
            ["result.guidance.upgrade"] = "HPを最大20回復しました  •  次のバトル用に強化を1つ選択",
            ["result.guidance.retry"] = "リトライするとバトル1から再開します",
            ["result.guidance.retry.endless"] = "リトライするとラウンド1から再開します",
            ["result.attack"] = "攻撃 +5\nすべての攻撃を強化", ["result.vitality"] = "生命力 +25\n最大HP +25・HPを25回復",
            ["result.guard"] = "防御 +15\n防御中のダメージを軽減",
            ["result.retry"] = "遠征をリトライ", ["result.retry.endless"] = "無限モードをリトライ", ["result.return"] = "タイトルへ戻る",
            ["result.load_upgrade_failed"] = "読込に失敗しました。同じ強化をもう一度押してください。",
            ["result.load_retry_failed"] = "読込に失敗しました。再試行するかタイトルへ戻ってください。"
        };

        public GameLanguage CurrentLanguage { get; private set; }
        public event Action LanguageChanged;

        public UnityLocalizationService()
        {
            var saved = PlayerPrefs.GetString(PreferenceKey, string.Empty);
            CurrentLanguage = saved == "ja" || (string.IsNullOrEmpty(saved) && UnityEngine.Application.systemLanguage == SystemLanguage.Japanese)
                ? GameLanguage.Japanese
                : GameLanguage.English;
        }

        public string Get(string key, params object[] arguments)
        {
            var table = CurrentLanguage == GameLanguage.Japanese ? _japanese : _english;
            if (!table.TryGetValue(key, out var value) && !_english.TryGetValue(key, out value)) value = key;
            return arguments == null || arguments.Length == 0
                ? value
                : string.Format(CultureInfo.InvariantCulture, value, arguments);
        }

        public void ToggleLanguage()
        {
            CurrentLanguage = CurrentLanguage == GameLanguage.English ? GameLanguage.Japanese : GameLanguage.English;
            PlayerPrefs.SetString(PreferenceKey, CurrentLanguage == GameLanguage.Japanese ? "ja" : "en");
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }
    }
}
