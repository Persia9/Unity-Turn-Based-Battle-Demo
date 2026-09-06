#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Infrastructure.MasterData.Masters;
using MasterMemory;
using UnityEditor;
using UnityEngine;

namespace Infrastructure.MasterData.Editor
{
    public static class MasterDataBuildMenu
    {
        [MenuItem("Tools/MasterData/Build Demo MasterData")]
        public static void BuildAll()
        {
            var coreBuilder = new DatabaseBuilder();
            coreBuilder.Append(new[]
            {
                new CharacterMaster { Id = 1, Name = "Hero", MaxHp = 100, Attack = 20, DefaultSkillId = 1 },
                new CharacterMaster { Id = 2, Name = "Slime", MaxHp = 80, Attack = 15, DefaultSkillId = 2 },
            });
            coreBuilder.Append(new[]
            {
                new SkillMaster { Id = 1, Name = "Slash", Power = 20, SeEventId = "se_attack" },
                new SkillMaster { Id = 2, Name = "Bite", Power = 15, SeEventId = "se_damage" },
            });
            coreBuilder.Append(new[]
            {
                new StageMaster { Id = 1, PlayerCharacterId = 1, EnemyCharacterId = 2 },
            });

            var uiBuilder = new DatabaseBuilder();
            uiBuilder.Append(new[]
            {
                new UIMaster
                {
                    Id = "TitleScreen", PrefabKey = "UI/Title/TitleScreen", Layer = 1, Type = 0, Cache = true,
                    Singleton = true, BlockInput = false
                },
                new UIMaster
                {
                    Id = "BattleHud", PrefabKey = "UI/Battle/BattleHud", Layer = 0, Type = 2, Cache = true,
                    Singleton = true, BlockInput = false
                },
                new UIMaster
                {
                    Id = "ResultPopup", PrefabKey = "UI/Battle/ResultPopup", Layer = 2, Type = 1, Cache = false,
                    Singleton = false, BlockInput = true
                },
            });

            var audioBuilder = new DatabaseBuilder();
            audioBuilder.Append(new[]
            {
                new AudioEventMaster { Id = "bgm_title", EventName = "Play_BGM_Title" },
                new AudioEventMaster { Id = "bgm_battle", EventName = "Play_BGM_Battle" },
                new AudioEventMaster { Id = "se_attack", EventName = "Play_SE_Attack" },
                new AudioEventMaster { Id = "se_damage", EventName = "Play_SE_Damage" },
            });

            Write("Assets/MasterData/Binary/master_core.bytes", coreBuilder.Build());
            Write("Assets/MasterData/Binary/master_ui.bytes", uiBuilder.Build());
            Write("Assets/MasterData/Binary/master_audio.bytes", audioBuilder.Build());

            AssetDatabase.Refresh();
            Debug.Log("Demo master data built.");
        }

        private static void Write(string path, ReadOnlySpan<byte> bytes)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, bytes.ToArray());
        }
    }
}
#endif