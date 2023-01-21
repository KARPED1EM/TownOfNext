using System.Collections.Generic;
namespace TownOfHost
{
    public static class MadcapKiller
    {
        public static List<byte> playerIdList = new();
        public static readonly int Id = 9000000;
        private static OptionItem InitialCd;
        private static OptionItem ReduceTime;
        private static OptionItem MinimumCd;
        private static float ReducedTime;
        private static bool haskill = false;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Madcapkiller);
            InitialCd = FloatOptionItem.Create(Id + 10, "InitialCd", new(60f, 900f, 1f), 450f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Madcapkiller])
                .SetValueFormat(OptionFormat.Seconds);
            ReduceTime = FloatOptionItem.Create(Id + 11, "ReduceTime", new(5f, 180f, 1f), 60f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Madcapkiller])
                .SetValueFormat(OptionFormat.Seconds);
            MinimumCd = FloatOptionItem.Create(Id + 12, "MinimumCd", new(5f, 180f, 1f), 60f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Madcapkiller])
            .SetValueFormat(OptionFormat.Seconds);
        }

        public static void Init()
        {
            playerIdList = new();
        }

        public static void Add(byte Madcap)
        {
            playerIdList.Add(Madcap);
        }

        public static void OnCheckMurder(PlayerControl killer, bool CanMurder = true)
        {
            if (!killer.Is(CustomRoles.Madcapkiller)) return;
            if (CanMurder)
                killer.MarkDirtySettings();
        }

        public static void ApplyKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] =  !haskill ? InitialCd.GetFloat() : InitialCd.GetFloat() - ReducedTime;
        public static bool HasKilled(PlayerControl pc)
           => pc != null && pc.Is(CustomRoles.Madcapkiller) && pc.IsAlive() && Main.PlayerStates[pc.PlayerId].GetKillCount(true) > 0;

        public static void FixedUpdate(PlayerControl player)
        {
            if (HasKilled(player))
            {
                haskill = HasKilled(player);
                if (InitialCd.GetFloat() - ReducedTime > MinimumCd.GetFloat() && InitialCd.GetFloat() - ReducedTime - ReduceTime.GetFloat() > MinimumCd.GetFloat())
                {
                    ReducedTime = ReduceTime.GetFloat() * (float)Main.PlayerStates[player.PlayerId].GetKillCount();
                }
                return;
            }
        }
    }
}