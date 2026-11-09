using System;
using System.Collections.Generic;
using Domain.Battle;
using Infrastructure.MasterData.Masters;
using Infrastructure.UI;

namespace Infrastructure.MasterData.Editor
{
    [Serializable]
    public sealed class MasterDataSourceData
    {
        public List<CharacterSourceRow> Characters = new();
        public List<SkillSourceRow> Skills = new();
        public List<StageSourceRow> Stages = new();
        public List<UISourceRow> UI = new();
    }

    [Serializable]
    public sealed class CharacterSourceRow
    {
        public int Id;
        public string Name;
        public int MaxHp;
        public int DefaultSkillId;

        public CharacterMaster ToMaster() => new CharacterMaster
            { Id = Id, Name = Name, MaxHp = MaxHp, DefaultSkillId = DefaultSkillId };
    }

    [Serializable]
    public sealed class SkillSourceRow
    {
        public int Id;
        public string Name;
        public int Power;
        public SkillMaster ToMaster() => new SkillMaster { Id = Id, Name = Name, Power = Power };
    }

    [Serializable]
    public sealed class StageSourceRow
    {
        public int Id;
        public int PlayerCharacterId;
        public int EnemyCharacterId;
        public EnemyAttackPattern EnemyPattern;
        public string DisplayName;

        public StageMaster ToMaster() => new StageMaster
        {
            Id = Id, PlayerCharacterId = PlayerCharacterId, EnemyCharacterId = EnemyCharacterId,
            EnemyPattern = (int)EnemyPattern, DisplayName = DisplayName
        };
    }

    [Serializable]
    public sealed class UISourceRow
    {
        public string Id;
        public string PrefabKey;
        public UILayer Layer;
        public UIType Type;
        public bool Cache;
        public bool Singleton;
        public bool BlockInput;

        public UIMaster ToMaster() => new UIMaster
        {
            Id = Id, PrefabKey = PrefabKey, Layer = (int)Layer, Type = (int)Type,
            Cache = Cache, Singleton = Singleton, BlockInput = BlockInput
        };
    }
}