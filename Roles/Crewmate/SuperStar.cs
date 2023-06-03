using AmongUs.GameOptions;

using TOHE.Roles.Core;

namespace TOHE.Roles.Crewmate;
public sealed class SuperStar : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(SuperStar),
            player => new SuperStar(player),
            CustomRoles.SuperStar,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            8020165,
            SetupOptionItem,
            "ss",
            "#f6f657"
        );
    public SuperStar(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionEveryoneKnowSuperStar;
    enum OptionName
    {
        EveryOneKnowSuperStar,
    }

    private static void SetupOptionItem()
    {
        OptionEveryoneKnowSuperStar = BooleanOptionItem.Create(RoleInfo, 10, OptionName.EveryOneKnowSuperStar, true, false);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        return (Is(seen) && OptionEveryoneKnowSuperStar.GetBool()) ? Utils.ColorString(RoleInfo.RoleColor, "★") : "";
    }
}