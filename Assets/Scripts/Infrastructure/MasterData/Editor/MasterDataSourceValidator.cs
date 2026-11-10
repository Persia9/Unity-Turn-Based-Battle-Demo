using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Battle;
using Domain.Run;
using Infrastructure.UI;

namespace Infrastructure.MasterData.Editor
{
    public static class MasterDataSourceValidator
    {
        public static IReadOnlyList<string> Validate(MasterDataSourceData data)
        {
            var errors = new List<string>();
            if (data == null)
            {
                errors.Add("Data is missing.");
                return errors;
            }

            errors.AddRange(LocalizationSourceValidator.Validate(data.Localization));
            var characters = Rows(data.Characters, "Characters", errors);
            var skills = Rows(data.Skills, "Skills", errors);
            var stages = Rows(data.Stages, "Stages", errors);
            var ui = Rows(data.UI, "UI", errors);
            var characterIds = CheckIds(characters, x => x.Id, "Characters", errors);
            var skillIds = CheckIds(skills, x => x.Id, "Skills", errors);
            var stageIds = CheckIds(stages, x => x.Id, "Stages", errors);
            foreach (var row in characters)
            {
                if (string.IsNullOrWhiteSpace(row.Name)) errors.Add($"Character {row.Id}: Name is required.");
                if (row.MaxHp <= 0) errors.Add($"Character {row.Id}: MaxHp must be positive.");
                if (!skillIds.Contains(row.DefaultSkillId))
                    errors.Add($"Character {row.Id}: skill {row.DefaultSkillId} does not exist.");
            }

            foreach (var row in skills)
            {
                if (string.IsNullOrWhiteSpace(row.Name)) errors.Add($"Skill {row.Id}: Name is required.");
                if (row.Power <= 0) errors.Add($"Skill {row.Id}: Power must be positive.");
            }

            foreach (var row in stages)
            {
                if (!characterIds.Contains(row.PlayerCharacterId))
                    errors.Add($"Stage {row.Id}: player {row.PlayerCharacterId} does not exist.");
                if (!characterIds.Contains(row.EnemyCharacterId))
                    errors.Add($"Stage {row.Id}: enemy {row.EnemyCharacterId} does not exist.");
                if (!Enum.IsDefined(typeof(EnemyAttackPattern), row.EnemyPattern))
                    errors.Add($"Stage {row.Id}: EnemyPattern is invalid.");
                if (string.IsNullOrWhiteSpace(row.DisplayName)) errors.Add($"Stage {row.Id}: DisplayName is required.");
            }

            // Both expedition and endless currently select from the first ExpeditionBattleCount stages.
            for (var id = 1; id <= RunRules.ExpeditionBattleCount; id++)
                if (!stageIds.Contains(id))
                    errors.Add($"Required stage {id} is missing.");
            var uiIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in ui)
            {
                if (string.IsNullOrWhiteSpace(row.Id)) errors.Add("UI: Id is required.");
                else if (!uiIds.Add(row.Id)) errors.Add($"UI: duplicate Id '{row.Id}'.");
                if (string.IsNullOrWhiteSpace(row.PrefabKey)) errors.Add($"UI '{row.Id}': PrefabKey is required.");
                if (!Enum.IsDefined(typeof(UIType), row.Type)) errors.Add($"UI '{row.Id}': Type is invalid.");
            }

            RequireUI(ui, "TitleScreen", UIType.Screen, errors);
            RequireUI(ui, "BattleHud", UIType.Hud, errors);
            RequireUI(ui, "ResultPopup", UIType.Popup, errors);
            return errors;
        }

        private static List<T> Rows<T>(List<T> rows, string table, List<string> errors) where T : class
        {
            if (rows == null)
            {
                errors.Add($"{table}: list is missing.");
                return new List<T>();
            }

            if (rows.Any(x => x == null)) errors.Add($"{table}: contains an empty row.");
            return rows.Where(x => x != null).ToList();
        }

        private static HashSet<int> CheckIds<T>(IEnumerable<T> rows, Func<T, int> id, string table, List<string> errors)
        {
            var ids = new HashSet<int>();
            foreach (var row in rows)
            {
                var value = id(row);
                if (value <= 0) errors.Add($"{table}: Id {value} must be positive.");
                if (!ids.Add(value)) errors.Add($"{table}: duplicate Id {value}.");
            }

            return ids;
        }

        private static void RequireUI(List<UISourceRow> rows, string id, UIType type, List<string> errors)
        {
            var row = rows.FirstOrDefault(x => x.Id == id);
            if (row == null) errors.Add($"Required UI '{id}' is missing.");
            else if (row.Type != type) errors.Add($"UI '{id}': expected Type {type}.");
        }
    }
}