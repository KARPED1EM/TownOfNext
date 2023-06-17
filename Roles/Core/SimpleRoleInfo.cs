using AmongUs.GameOptions;
using System;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Core;

public class SimpleRoleInfo
{
    public Type ClassType;
    public Func<PlayerControl, RoleBase> CreateInstance;
    public CustomRoles RoleName;
    public Func<RoleTypes> BaseRoleType;
    public CustomRoleTypes CustomRoleType;
    public Color RoleColor;
    public string RoleColorCode;
    public int ConfigId;
    public TabGroup Tab;
    public OptionItem RoleOption => CustomRoleSpawnChances[RoleName];
    public bool IsEnable = false;
    public OptionCreatorDelegate OptionCreator;
    public string ChatCommand;
    public bool RequireResetCam;
    private Func<AudioClip> introSound;
    public bool Experimental;
    public AudioClip IntroSound => introSound?.Invoke();

    public SimpleRoleInfo(
        Type classType,
        Func<PlayerControl, RoleBase> createInstance,
        CustomRoles roleName,
        Func<RoleTypes> baseRoleType,
        CustomRoleTypes customRoleType,
        int configId,
        OptionCreatorDelegate optionCreator,
        string chatCommand,
        string colorCode = "",
        bool requireResetCam = false,
        TabGroup tab = TabGroup.GameSettings,
        Func<AudioClip> introSound = null,
        bool experimental = false
    )
    {
        ClassType = classType;
        CreateInstance = createInstance;
        RoleName = roleName;
        BaseRoleType = baseRoleType;
        CustomRoleType = customRoleType;
        ConfigId = configId;
        OptionCreator = optionCreator;
        RequireResetCam = requireResetCam;
        this.introSound = introSound;
        ChatCommand = chatCommand;
        Experimental = experimental;

        if (colorCode == "")
            colorCode = customRoleType switch
            {
                CustomRoleTypes.Impostor => "#ff1919",
                _ => "#ffffff"
            };
        RoleColorCode = colorCode;

        ColorUtility.TryParseHtmlString(colorCode, out RoleColor);

        if (Experimental) tab = TabGroup.OtherRoles;
        else if (tab == TabGroup.GameSettings)
            tab = CustomRoleType switch
            {
                CustomRoleTypes.Impostor => TabGroup.ImpostorRoles,
                CustomRoleTypes.Crewmate => TabGroup.CrewmateRoles,
                CustomRoleTypes.Neutral => TabGroup.NeutralRoles,
                CustomRoleTypes.Addon => TabGroup.Addons,
                _ => tab
            };
        Tab = tab;

        CustomRoleManager.AllRolesInfo.Add(roleName, this);
    }
    public delegate void OptionCreatorDelegate();
}