using AmongUs.GameOptions;
using System.Linq;

using TOHE.Roles.Core;

namespace TOHE;

static class CustomRolesHelper
{
    public static readonly CustomRoles[] AllRoles = EnumHelper.GetAllValues<CustomRoles>();
    public static readonly CustomRoleTypes[] AllRoleTypes = EnumHelper.GetAllValues<CustomRoleTypes>();

    public static bool IsImpostor(this CustomRoles role)
    {
        var roleInfo = role.GetRoleInfo();
        if (roleInfo != null)
            return roleInfo.CustomRoleType == CustomRoleTypes.Impostor;
        return
            role is CustomRoles.Impostor or
            CustomRoles.Shapeshifter;
    }
    public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role is CustomRoles.Madmate;
    public static bool IsNeutral(this CustomRoles role)
    {
        var roleInfo = role.GetRoleInfo();
        if (roleInfo != null)
            return roleInfo.CustomRoleType == CustomRoleTypes.Neutral;
        return false;
    }
    public static bool IsCrewmate(this CustomRoles role) => role.GetRoleInfo()?.CustomRoleType == CustomRoleTypes.Crewmate || (!role.IsImpostorTeam() && !role.IsNeutral());
    public static bool IsAddon(this CustomRoles role) => (int)role > 500;
    public static bool IsValid(this CustomRoles role) => role is not CustomRoles.KB_Normal and not CustomRoles.GM and not CustomRoles.NotAssigned;
    public static bool Exist(this CustomRoles role, bool CountDeath = false) => Main.AllPlayerControls.Any(x => x.Is(role) && x.IsAlive() || CountDeath);
    public static bool IsDesyncRole(this CustomRoles role) => role.GetRoleInfo()?.CustomRoleType != CustomRoleTypes.Impostor && role.GetRoleInfo()?.BaseRoleType.Invoke() is RoleTypes.Impostor or RoleTypes.Shapeshifter or RoleTypes.ImpostorGhost;
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

    public static CustomRoleTypes GetCustomRoleTypes(this CustomRoles role)
    {
        CustomRoleTypes type = CustomRoleTypes.Crewmate;

        var roleInfo = role.GetRoleInfo();
        if (roleInfo != null)
            return roleInfo.CustomRoleType;

        if (role.IsImpostor()) type = CustomRoleTypes.Impostor;
        if (role.IsNeutral()) type = CustomRoleTypes.Neutral;
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
    public static int GetChance(this CustomRoles role)
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
            };
        }
        else
        {
            return Options.GetRoleChance(role);
        }
    }
    public static bool IsEnable(this CustomRoles role) => role.GetCount() > 0;
    public static CustomRoles GetCustomRoleTypes(this RoleTypes role)
    {
        return role switch
        {
            RoleTypes.Crewmate => CustomRoles.Crewmate,
            RoleTypes.Scientist => CustomRoles.Scientist,
            RoleTypes.Engineer => CustomRoles.Engineer,
            RoleTypes.GuardianAngel => CustomRoles.GuardianAngel,
            RoleTypes.Shapeshifter => CustomRoles.Shapeshifter,
            RoleTypes.Impostor => CustomRoles.Impostor,
            _ => throw new System.NotImplementedException()
        };
    }
    public static RoleTypes GetRoleTypes(this CustomRoles role)
    {
        var roleInfo = role.GetRoleInfo();
        if (roleInfo != null)
            return roleInfo.BaseRoleType.Invoke();
        return role switch
        {
            CustomRoles.Scientist => RoleTypes.Scientist,

            CustomRoles.Engineer => RoleTypes.Engineer,

            CustomRoles.GuardianAngel or
            CustomRoles.GM => RoleTypes.GuardianAngel,

            CustomRoles.Shapeshifter => RoleTypes.Shapeshifter,

            _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
        };
    }
}
public enum CountTypes
{
    OutOfGame,
    None,
    Crew,
    Impostor,
    Jackal,
    Pelican,
    Gamer,
    BloodKnight,
    Succubus,
}