using System.Collections.Generic;
using AmongUs.GameOptions;

namespace TownOfHost
{
    static class CustomRolesHelper
    {
        private static readonly List<CustomRoles> Impostors = new()
        {
            CustomRoles.Impostor,
            CustomRoles.Shapeshifter,
            CustomRoles.BountyHunter,
            CustomRoles.Vampire,
            CustomRoles.Witch,
            CustomRoles.Zombie,
            CustomRoles.Warlock,
            CustomRoles.Assassin,
            CustomRoles.Hacker,
            CustomRoles.Miner,
            CustomRoles.Escapee,
            CustomRoles.SerialKiller,
            CustomRoles.Mare,
            CustomRoles.Puppeteer,
            CustomRoles.EvilWatcher,
            CustomRoles.TimeThief,
            CustomRoles.Mafia,
            CustomRoles.Minimalism,
            CustomRoles.FireWorks,
            CustomRoles.Sniper,
            CustomRoles.EvilTracker,
            CustomRoles.EvilGuesser,
            CustomRoles.AntiAdminer,
            CustomRoles.Sans,
            CustomRoles.Bomber
        };

    public static void Init()
        {

        }
/*
        public static bool IsNK(this CustomRoles role) // 是否带刀中立
        {
            return
                role is CustomRoles.Egoist or
                CustomRoles.Jackal or
                CustomRoles.OpportunistKiller;
        }

        public static bool IsNNK(this CustomRoles role) // 是否无刀中立
        {
            return
                role is CustomRoles.Arsonist or
                CustomRoles.Opportunist or
                CustomRoles.Mario or
                CustomRoles.God or
                CustomRoles.Jester or
                CustomRoles.Terrorist or
                CustomRoles.EgoSchrodingerCat or
                CustomRoles.Executioner;
        }
*/

        public static bool IsNeutralKilling(this CustomRoles role) //是否邪恶中立（抢夺或单独胜利的中立）
        {
            return
                role is CustomRoles.Arsonist or
                CustomRoles.Egoist or
                CustomRoles.Jackal or
                CustomRoles.God or
                CustomRoles.Mario;
        }
        public static bool IsCK(this CustomRoles role) //是否带刀船员
        {
            return
                role is CustomRoles.ChivalrousExpert or
                CustomRoles.Sheriff;
        }

        public static bool IsImpostor(this CustomRoles role)
        {
            return Impostors.Contains(role);
        }
        public static bool IsMadmate(this CustomRoles role)
        {
            return
                role is CustomRoles.Madmate or
                CustomRoles.SKMadmate or
                CustomRoles.MadGuardian or
                CustomRoles.MadSnitch or
                CustomRoles.MSchrodingerCat;
        }
        public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role.IsMadmate();
        public static bool IsNeutral(this CustomRoles role)
        {
            return
                role is CustomRoles.Jester or
                CustomRoles.Opportunist or
                CustomRoles.Mario or
                CustomRoles.SchrodingerCat or
                CustomRoles.Terrorist or
                CustomRoles.Executioner or
                CustomRoles.Arsonist or
                CustomRoles.Egoist or
                CustomRoles.EgoSchrodingerCat or
                CustomRoles.Jackal or
                CustomRoles.JSchrodingerCat or
                CustomRoles.HASTroll or
                CustomRoles.HASFox or
                CustomRoles.OpportunistKiller or
                CustomRoles.God;
        }
        public static bool IsCrewmate(this CustomRoles role) => !role.IsImpostorTeam() && !role.IsNeutral();
        public static bool IsVanilla(this CustomRoles role)
        {
            return
                role is CustomRoles.Crewmate or
                CustomRoles.Engineer or
                CustomRoles.Scientist or
                CustomRoles.GuardianAngel or
                CustomRoles.Impostor or
                CustomRoles.Shapeshifter;
        }
        public static bool IsKilledSchrodingerCat(this CustomRoles role)
        {
            return role is
                CustomRoles.SchrodingerCat or
                CustomRoles.MSchrodingerCat or
                CustomRoles.CSchrodingerCat or
                CustomRoles.EgoSchrodingerCat or
                CustomRoles.JSchrodingerCat;
        }

        public static RoleType GetRoleType(this CustomRoles role)
        {
            RoleType type = RoleType.Crewmate;
            if (role.IsImpostor()) type = RoleType.Impostor;
            if (role.IsNeutral()) type = RoleType.Neutral;
            if (role.IsMadmate()) type = RoleType.Madmate;
            return type;
        }
        public static int GetCount(this CustomRoles role)
        {
            if (role.IsVanilla())
            {
                var roleOpt = Main.NormalOptions.RoleOptions;
                return role switch
                {
                    CustomRoles.Engineer => roleOpt.GetNumPerGame(RoleTypes.Engineer),
                    CustomRoles.Scientist => roleOpt.GetNumPerGame(RoleTypes.Scientist),
                    CustomRoles.Shapeshifter => roleOpt.GetNumPerGame(RoleTypes.Shapeshifter),
                    CustomRoles.GuardianAngel => roleOpt.GetNumPerGame(RoleTypes.GuardianAngel),
                    CustomRoles.Crewmate => roleOpt.GetNumPerGame(RoleTypes.Crewmate),
                    _ => 0
                };
            }
            else
            {
                return Options.GetRoleCount(role);
            }
        }
        public static float GetChance(this CustomRoles role)
        {
            if (role.IsVanilla())
            {
                var roleOpt = Main.NormalOptions.RoleOptions;
                return role switch
                {
                    CustomRoles.Engineer => roleOpt.GetChancePerGame(RoleTypes.Engineer),
                    CustomRoles.Scientist => roleOpt.GetChancePerGame(RoleTypes.Scientist),
                    CustomRoles.Shapeshifter => roleOpt.GetChancePerGame(RoleTypes.Shapeshifter),
                    CustomRoles.GuardianAngel => roleOpt.GetChancePerGame(RoleTypes.GuardianAngel),
                    CustomRoles.Crewmate => roleOpt.GetChancePerGame(RoleTypes.Crewmate),
                    _ => 0
                } / 100f;
            }
            else
            {
                return Options.GetRoleChance(role);
            }
        }
        public static bool IsEnable(this CustomRoles role) => role.GetCount() > 0;
        public static bool CanMakeMadmate(this CustomRoles role)
            => role switch
            {
                CustomRoles.Shapeshifter => true,
                CustomRoles.EvilTracker => EvilTracker.CanCreateMadmate.GetBool(),
                CustomRoles.Egoist => Egoist.CanCreateMadmate.GetBool(),
                _ => false,
            };
    }
    public enum RoleType
    {
        Crewmate,
        Impostor,
        Neutral,
        Madmate
    }
}