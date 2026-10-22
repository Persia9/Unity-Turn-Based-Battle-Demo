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
                new CharacterMaster { Id = 2, Name = "Slime", MaxHp = 80, Attack = 8, DefaultSkillId = 2 },
                new CharacterMaster { Id = 3, Name = "Sentinel", MaxHp = 130, Attack = 10, DefaultSkillId = 3 },
                new CharacterMaster { Id = 4, Name = "Iron Warden", MaxHp = 150, Attack = 10, DefaultSkillId = 4 },
            });
            coreBuilder.Append(new[]
            {
                new SkillMaster { Id = 1, Name = "Slash", Power = 20 },
                new SkillMaster { Id = 2, Name = "Bite", Power = 8 },
                new SkillMaster { Id = 3, Name = "Slam", Power = 10 },
                new SkillMaster { Id = 4, Name = "Crush", Power = 10 },
            });
            coreBuilder.Append(new[]
            {
                new StageMaster { Id = 1, PlayerCharacterId = 1, EnemyCharacterId = 2, EnemyPattern = 0, DisplayName = "First Encounter" },
                new StageMaster { Id = 2, PlayerCharacterId = 1, EnemyCharacterId = 3, EnemyPattern = 1, DisplayName = "Read the Forecast" },
                new StageMaster { Id = 3, PlayerCharacterId = 1, EnemyCharacterId = 4, EnemyPattern = 2, DisplayName = "Iron Warden" },
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

            Write("Assets/MasterData/Binary/master_core.bytes", coreBuilder.Build());
            Write("Assets/MasterData/Binary/master_ui.bytes", uiBuilder.Build());

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