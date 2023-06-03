using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

using TOHE.Roles.Core;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.AddOns.Crewmate;

namespace TOHE;

[Flags]
public enum CustomGameMode
{
    Standard = 0x01,
    SoloKombat = 0x02,
    All = int.MaxValue
}

[HarmonyPatch]
public static class Options
{
    static Task taskOptionsLoad;
    [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize)), HarmonyPostfix]
    public static void OptionsLoadStart()
    {
        Logger.Info("Options.Load Start", "Options");
        taskOptionsLoad = Task.Run(Load);
    }
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
    public static void WaitOptionsLoad()
    {
        taskOptionsLoad.Wait();
        Logger.Info("Options.Load End", "Options");
    }

    // 预设
    public const int PresetId = 0;
    private static readonly string[] presets =
    {
        Main.Preset1.Value, Main.Preset2.Value, Main.Preset3.Value,
        Main.Preset4.Value, Main.Preset5.Value
    };

    // 游戏模式
    public static OptionItem GameMode;
    public static CustomGameMode CurrentGameMode
        => GameMode.GetInt() switch
        {
            1 => CustomGameMode.SoloKombat,
            _ => CustomGameMode.Standard
        };

    public static readonly string[] gameModes =
    {
        "Standard", "SoloKombat"
    };

    // 地图启用
    public static bool IsActiveSkeld => AddedTheSkeld.GetBool() || Main.NormalOptions.MapId == 0;
    public static bool IsActiveMiraHQ => AddedMiraHQ.GetBool() || Main.NormalOptions.MapId == 1;
    public static bool IsActivePolus => AddedPolus.GetBool() || Main.NormalOptions.MapId == 2;
    public static bool IsActiveAirship => AddedTheAirShip.GetBool() || Main.NormalOptions.MapId == 4;

    // 职业数量・生成模式&概率
    public static Dictionary<CustomRoles, OptionItem> CustomRoleCounts;
    public static Dictionary<CustomRoles, StringOptionItem> CustomRoleSpawnChances;
    public static readonly string[] Rates =
    {
            "Rate0",  "Rate5",  "Rate10", "Rate20", "Rate30", "Rate40",
            "Rate50", "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
    };
    public static readonly string[] RoleSpwanModes =
    {
        "RoleOff", "RoleRate", "RoleOn"
    };
    public static readonly string[] RoleSpwanToggle =
    {
        "RoleOff", "AddonEnabled"
    };
    public static readonly string[] CheatResponsesNames =
    {
        "Ban", "Kick", "NoticeMe","NoticeEveryone"
    };
    public static readonly string[] ConfirmEjectionsModes =
    {
        "ConfirmEjections.None",
        "ConfirmEjections.Team",
        "ConfirmEjections.Role"
    };

    #region 创建选项

    //* 阵营&职业的详细设定 *//

    public static float DefaultKillCooldown = Main.NormalOptions?.KillCooldown ?? 20;
    public static OptionItem DefaultShapeshiftCooldown;

    public static OptionItem DeadImpCantSabotage;
    public static OptionItem ImpKnowAlliesRole;
    public static OptionItem ImpKnowWhosMadmate;
    public static OptionItem MadmateKnowWhosImp;
    public static OptionItem MadmateKnowWhosMadmate;
    public static OptionItem ImpCanKillMadmate;
    public static OptionItem MadmateCanKillImp;

    public static OptionItem NeutralRolesMinPlayer;
    public static OptionItem NeutralRolesMaxPlayer;
    public static OptionItem NeutralRoleWinTogether;
    public static OptionItem NeutralWinTogether;

    public static OptionItem NoLimitAddonsNum;

    public static OptionItem LoverKnowRoles;
    public static OptionItem LoverSuicide;

    public static OptionItem MadmateSpawnMode;
    public static OptionItem MadmateCountMode;
    public static OptionItem SheriffCanBeMadmate;
    public static OptionItem MayorCanBeMadmate;
    public static OptionItem NGuesserCanBeMadmate;
    public static OptionItem SnitchCanBeMadmate;
    public static OptionItem JudgeCanBeMadmate;
    public static OptionItem MadSnitchTasks;

    //* 游戏设置 *//

    public static OptionItem EnableGM;

    // 驱逐相关设定
    public static OptionItem CEMode;
    public static OptionItem ConfirmEjectionsNK;
    public static OptionItem ConfirmEjectionsNonNK;
    public static OptionItem ConfirmEjectionsNeutralAsImp;
    public static OptionItem ShowImpRemainOnEject;
    public static OptionItem ShowNKRemainOnEject;
    public static OptionItem ShowTeamNextToRoleNameOnEject;

    // 禁用相关设定
    public static OptionItem DisableVanillaRoles;
    public static OptionItem DisableHiddenRoles;
    public static OptionItem DisableTaskWin;

    public static OptionItem DisableTasks;
    public static OptionItem DisableSwipeCard;
    public static OptionItem DisableSubmitScan;
    public static OptionItem DisableUnlockSafe;
    public static OptionItem DisableUploadData;
    public static OptionItem DisableStartReactor;
    public static OptionItem DisableResetBreaker;

    public static OptionItem DisableMeeting;
    public static OptionItem DisableCloseDoor;
    public static OptionItem DisableSabotage;

    public static OptionItem DisableDevices;
    public static OptionItem DisableSkeldDevices;
    public static OptionItem DisableSkeldAdmin;
    public static OptionItem DisableSkeldCamera;
    public static OptionItem DisableMiraHQDevices;
    public static OptionItem DisableMiraHQAdmin;
    public static OptionItem DisableMiraHQDoorLog;
    public static OptionItem DisablePolusDevices;
    public static OptionItem DisablePolusAdmin;
    public static OptionItem DisablePolusCamera;
    public static OptionItem DisablePolusVital;
    public static OptionItem DisableAirshipDevices;
    public static OptionItem DisableAirshipCockpitAdmin;
    public static OptionItem DisableAirshipRecordsAdmin;
    public static OptionItem DisableAirshipCamera;
    public static OptionItem DisableAirshipVital;
    public static OptionItem DisableDevicesIgnoreConditions;
    public static OptionItem DisableDevicesIgnoreImpostors;
    public static OptionItem DisableDevicesIgnoreMadmates;
    public static OptionItem DisableDevicesIgnoreNeutrals;
    public static OptionItem DisableDevicesIgnoreCrewmates;
    public static OptionItem DisableDevicesIgnoreAfterAnyoneDied;

    // 会议相关设定
    public static OptionItem SyncButtonMode;
    public static OptionItem SyncedButtonCount;

    public static OptionItem AllAliveMeeting;
    public static OptionItem AllAliveMeetingTime;

    public static OptionItem AdditionalEmergencyCooldown;
    public static OptionItem AdditionalEmergencyCooldownThreshold;
    public static OptionItem AdditionalEmergencyCooldownTime;

    public static OptionItem VoteMode;
    public static OptionItem WhenSkipVote;
    public static OptionItem WhenSkipVoteIgnoreFirstMeeting;
    public static OptionItem WhenSkipVoteIgnoreNoDeadBody;
    public static OptionItem WhenSkipVoteIgnoreEmergency;
    public static OptionItem WhenNonVote;
    public static OptionItem WhenTie;

    // 破坏相关设定
    public static OptionItem CommsCamouflage;
    public static OptionItem DisableReportWhenCC;

    public static OptionItem SabotageTimeControl;
    public static OptionItem PolusReactorTimeLimit;
    public static OptionItem AirshipReactorTimeLimit;

    public static OptionItem LightsOutSpecialSettings;
    public static OptionItem DisableAirshipViewingDeckLightsPanel;
    public static OptionItem DisableAirshipGapRoomLightsPanel;
    public static OptionItem DisableAirshipCargoLightsPanel;

    // 地图相关设定
    public static OptionItem AirShipVariableElectrical;
    public static OptionItem DisableAirshipMovingPlatform;

    // 其它设定
    public static OptionItem RandomMapsMode;
    public static OptionItem AddedTheSkeld;
    public static OptionItem AddedMiraHQ;
    public static OptionItem AddedPolus;
    public static OptionItem AddedTheAirShip;
    // public static OptionItem AddedDleks;

    public static OptionItem RandomSpawn;
    public static OptionItem AirshipAdditionalSpawn;

    public static OptionItem LadderDeath;
    public static OptionItem LadderDeathChance;

    public static OptionItem FixFirstKillCooldown;
    public static OptionItem ShieldPersonDiedFirst;
    public static OptionItem KillFlashDuration;

    // 幽灵相关设定
    public static OptionItem GhostIgnoreTasks;
    public static OptionItem GhostCanSeeOtherRoles;
    public static OptionItem GhostCanSeeOtherTasks;
    public static OptionItem GhostCanSeeOtherVotes;
    public static OptionItem GhostCanSeeDeathReason;

    /* 系统设定 */

    public static OptionItem KickLowLevelPlayer;
    public static OptionItem KickAndroidPlayer;
    public static OptionItem KickPlayerFriendCodeNotExist;
    public static OptionItem ApplyDenyNameList;
    public static OptionItem ApplyBanList;
    public static OptionItem AutoKickStart;
    public static OptionItem AutoKickStartAsBan;
    public static OptionItem AutoKickStartTimes;
    public static OptionItem AutoKickStopWords;
    public static OptionItem AutoKickStopWordsAsBan;
    public static OptionItem AutoKickStopWordsTimes;
    public static OptionItem AutoWarnStopWords;

    public static OptionItem ShareLobby;
    public static OptionItem ShareLobbyMinPlayer;

    public static OptionItem LowLoadMode;

    public static OptionItem EndWhenPlayerBug;

    public static OptionItem CheatResponses;

    public static OptionItem AutoDisplayKillLog;
    public static OptionItem AutoDisplayLastRoles;
    public static OptionItem AutoDisplayLastResult;

    public static OptionItem SuffixMode;
    public static OptionItem HideGameSettings;
    public static OptionItem DIYGameSettings;
    public static OptionItem PlayerCanSetColor;
    public static OptionItem FormatNameMode;
    public static OptionItem DisableEmojiName;
    public static OptionItem ChangeNameToRoleInfo;
    public static OptionItem SendRoleDescriptionFirstMeeting;
    public static OptionItem NoGameEnd;
    public static OptionItem AllowConsole;
    public static OptionItem RoleAssigningAlgorithm;

    public static OptionItem KPDCamouflageMode;

    public static OptionItem EnableUpMode;

#endregion

    /* 选项 */

    public static readonly string[] madmateSpawnMode =
    {
        "MadmateSpawnMode.Assign",
        "MadmateSpawnMode.FirstKill",
        "MadmateSpawnMode.SelfVote",
    };
    public static readonly string[] madmateCountMode =
    {
        "MadmateCountMode.None",
        "MadmateCountMode.Imp",
        "MadmateCountMode.Crew",
    };
    public static readonly string[] suffixModes =
    {
        "SuffixMode.None",
        "SuffixMode.Version",
        "SuffixMode.Streaming",
        "SuffixMode.Recording",
        "SuffixMode.RoomHost",
        "SuffixMode.OriginalName",
        "SuffixMode.DoNotKillMe",
        "SuffixMode.NoAndroidPlz"
    };
    public static readonly string[] roleAssigningAlgorithms =
    {
        "RoleAssigningAlgorithm.Default",
        "RoleAssigningAlgorithm.NetRandom",
        "RoleAssigningAlgorithm.HashRandom",
        "RoleAssigningAlgorithm.Xorshift",
        "RoleAssigningAlgorithm.MersenneTwister",
    };
    public static readonly string[] formatNameModes =
    {
        "FormatNameModes.None",
        "FormatNameModes.Color",
        "FormatNameModes.Snacks",
    };
    public static readonly string[] voteModes =
    {
        "Default", "Suicide", "SelfVote", "Skip"
    };
    public static readonly string[] tieModes =
    {
        "TieMode.Default", "TieMode.All", "TieMode.Random"
    };
    public static VoteMode GetWhenSkipVote() => (VoteMode)WhenSkipVote.GetValue();
    public static VoteMode GetWhenNonVote() => (VoteMode)WhenNonVote.GetValue();
    public static SuffixModes GetSuffixMode() => (SuffixModes)SuffixMode.GetValue();

    public static int UsedButtonCount = 0;
    public static int SnitchExposeTaskLeft = 1;

    public static bool IsLoaded = false;
    public static int GetRoleCount(CustomRoles role)
    {
        return GetRoleChance(role) == 0 ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : 0;
    }
    public static int GetRoleChance(CustomRoles role)
    {
        if (!CustomRoleSpawnChances.TryGetValue(role, out var option)) return 0;
        if (option.Selections.Length == 2) return option.GetInt() == 0 ? 0 : 2;
        if (option.Selections.Length == 3) return option.GetInt();
        else return option.GetInt() switch
        {
            0 => 0,
            1 => 5,
            _ => option.GetInt() * 10 - 10
        };
    }
    public static void Load()
    {
        if (IsLoaded) return;
        // 预设
        _ = PresetOptionItem.Create(0, TabGroup.SystemSettings)
            .SetColor(new Color32(255, 235, 4, byte.MaxValue))
            .SetHeader(true);

        // 游戏模式
        GameMode = StringOptionItem.Create(1, "GameMode", gameModes, 0, TabGroup.GameSettings, false)
            .SetHeader(true);

        Logger.Msg("开始加载职业设置", "Load Options");

        #region 职业详细设置
        CustomRoleCounts = new();
        CustomRoleSpawnChances = new();

        var sortedRoleInfo = CustomRoleManager.AllRolesInfo.Values.OrderBy(role => role.ConfigId);

        // 各职业的总体设定
        ImpKnowAlliesRole = BooleanOptionItem.Create(900045, "ImpKnowAlliesRole", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetHeader(true);
        ImpKnowWhosMadmate = BooleanOptionItem.Create(900046, "ImpKnowWhosMadmate", false, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        ImpCanKillMadmate = BooleanOptionItem.Create(900049, "ImpCanKillMadmate", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);

        MadmateKnowWhosMadmate = BooleanOptionItem.Create(900048, "MadmateKnowWhosMadmate", false, TabGroup.ImpostorRoles, false)
            .SetHeader(true)
            .SetGameMode(CustomGameMode.Standard);
        MadmateKnowWhosImp = BooleanOptionItem.Create(900047, "MadmateKnowWhosImp", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);
        MadmateCanKillImp = BooleanOptionItem.Create(900050, "MadmateCanKillImp", true, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);

        DefaultShapeshiftCooldown = FloatOptionItem.Create(5011, "DefaultShapeshiftCooldown", new(5f, 999f, 5f), 15f, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Seconds);
        DeadImpCantSabotage = BooleanOptionItem.Create(900051, "DeadImpCantSabotage", false, TabGroup.ImpostorRoles, false)
            .SetGameMode(CustomGameMode.Standard);

        NeutralRolesMinPlayer = IntegerOptionItem.Create(505007, "NeutralRolesMinPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Players);
        NeutralRolesMaxPlayer = IntegerOptionItem.Create(505009, "NeutralRolesMaxPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Players);
        NeutralRoleWinTogether = BooleanOptionItem.Create(505011, "NeutralRoleWinTogether", false, TabGroup.NeutralRoles, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetHeader(true);
        NeutralWinTogether = BooleanOptionItem.Create(505013, "NeutralWinTogether", false, TabGroup.NeutralRoles, false).SetParent(NeutralRoleWinTogether)
            .SetGameMode(CustomGameMode.Standard);

        NoLimitAddonsNum = BooleanOptionItem.Create(6050250, "NoLimitAddonsNum", false, TabGroup.Addons, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true);

        // GM
        EnableGM = BooleanOptionItem.Create(100, "GM", false, TabGroup.GameSettings, false)
            .SetColor(Utils.GetRoleColor(CustomRoles.GM))
            .SetHeader(true);

        bool setupExpNow = false;
        
    StartSetupRoleOptions:

        if (setupExpNow)
        {
            TextOptionItem.Create(909090, "OtherRoles.ImpostorRoles", TabGroup.OtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(247, 70, 49, byte.MaxValue));
        }

        // Impostor
        sortedRoleInfo.Where(role => role.CustomRoleType == CustomRoleTypes.Impostor && role.Experimental == setupExpNow).Do(info =>
        {
            SetupRoleOptions(info.ConfigId, info.Tab, info.RoleName);
            info.OptionCreator?.Invoke();
        });

        if (setupExpNow)
        {
            TextOptionItem.Create(909092, "OtherRoles.CrewmateRoles", TabGroup.OtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(140, 255, 255, byte.MaxValue));
        }

        // Crewmate
        sortedRoleInfo.Where(role => role.CustomRoleType == CustomRoleTypes.Crewmate && role.Experimental == setupExpNow).Do(info =>
        {
            SetupRoleOptions(info.ConfigId, info.Tab, info.RoleName);
            info.OptionCreator?.Invoke();
        });

        if (setupExpNow)
        {
            TextOptionItem.Create(909094, "OtherRoles.NeutralRoles", TabGroup.OtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 171, 27, byte.MaxValue));
        }

        // Neutral
        sortedRoleInfo.Where(role => role.CustomRoleType == CustomRoleTypes.Neutral && role.Experimental == setupExpNow).Do(info =>
        {
            switch (info.RoleName)
            {
                case CustomRoles.Jackal: //ジャッカルは1人固定
                    SetupSingleRoleOptions(info.ConfigId, info.Tab, info.RoleName, 1);
                    break;
                default:
                    SetupRoleOptions(info.ConfigId, info.Tab, info.RoleName);
                    break;
            }
            info.OptionCreator?.Invoke();
        });

        if (setupExpNow)
        {
            TextOptionItem.Create(909096, "OtherRoles.Addons", TabGroup.OtherRoles)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 154, 206, byte.MaxValue));
        }

        // Experimental Roles
        if (!setupExpNow)
        {
            setupExpNow = true;
            goto StartSetupRoleOptions;
        }

        // Add-Ons
        SetupLoversRoleOptionsToggle(50300);
        Watcher.SetupCustomOption();
        Workhorse.SetupCustomOption();
        SetupMadmateRoleOptionsToggle(6050390);
        LastImpostor.SetupCustomOption();

        #endregion

        Logger.Msg("开始加载系统设置", "Load Options");

        #region 系统设置

        KickLowLevelPlayer = IntegerOptionItem.Create(6090074, "KickLowLevelPlayer", new(0, 100, 1), 0, TabGroup.SystemSettings, false)
            .SetValueFormat(OptionFormat.Level)
            .SetHeader(true);
        KickAndroidPlayer = BooleanOptionItem.Create(6090071, "KickAndroidPlayer", false, TabGroup.SystemSettings, false);
        KickPlayerFriendCodeNotExist = BooleanOptionItem.Create(1_000_101, "KickPlayerFriendCodeNotExist", false, TabGroup.SystemSettings, true);
        ApplyDenyNameList = BooleanOptionItem.Create(1_000_100, "ApplyDenyNameList", true, TabGroup.SystemSettings, true);
        ApplyBanList = BooleanOptionItem.Create(1_000_110, "ApplyBanList", true, TabGroup.SystemSettings, true);
        AutoKickStart = BooleanOptionItem.Create(1_000_010, "AutoKickStart", false, TabGroup.SystemSettings, false);
        AutoKickStartTimes = IntegerOptionItem.Create(1_000_024, "AutoKickStartTimes", new(0, 99, 1), 1, TabGroup.SystemSettings, false).SetParent(AutoKickStart)
            .SetValueFormat(OptionFormat.Times);
        AutoKickStartAsBan = BooleanOptionItem.Create(1_000_026, "AutoKickStartAsBan", false, TabGroup.SystemSettings, false).SetParent(AutoKickStart);
        AutoKickStopWords = BooleanOptionItem.Create(1_000_011, "AutoKickStopWords", false, TabGroup.SystemSettings, false);
        AutoKickStopWordsTimes = IntegerOptionItem.Create(1_000_022, "AutoKickStopWordsTimes", new(0, 99, 1), 3, TabGroup.SystemSettings, false).SetParent(AutoKickStopWords)
            .SetValueFormat(OptionFormat.Times);
        AutoKickStopWordsAsBan = BooleanOptionItem.Create(1_000_028, "AutoKickStopWordsAsBan", false, TabGroup.SystemSettings, false).SetParent(AutoKickStopWords);
        AutoWarnStopWords = BooleanOptionItem.Create(1_000_012, "AutoWarnStopWords", false, TabGroup.SystemSettings, false);

        ShareLobby = BooleanOptionItem.Create(6090065, "ShareLobby", true, TabGroup.SystemSettings, false)
            .SetHeader(true)
            .SetColor(Color.cyan);
        ShareLobbyMinPlayer = IntegerOptionItem.Create(6090067, "ShareLobbyMinPlayer", new(3, 12, 1), 5, TabGroup.SystemSettings, false).SetParent(ShareLobby)
            .SetValueFormat(OptionFormat.Players);

        LowLoadMode = BooleanOptionItem.Create(6080069, "LowLoadMode", false, TabGroup.SystemSettings, false)
            .SetHeader(true)
            .SetColor(Color.red);

        EndWhenPlayerBug = BooleanOptionItem.Create(1_000_025, "EndWhenPlayerBug", true, TabGroup.SystemSettings, false)
            .SetHeader(true)
            .SetColor(Color.blue);

        CheatResponses = StringOptionItem.Create(6090121, "CheatResponses", CheatResponsesNames, 0, TabGroup.SystemSettings, false)
            .SetHeader(true);

        AutoDisplayKillLog = BooleanOptionItem.Create(1_000_006, "AutoDisplayKillLog", true, TabGroup.SystemSettings, false)
            .SetHeader(true);
        AutoDisplayLastRoles = BooleanOptionItem.Create(1_000_000, "AutoDisplayLastRoles", true, TabGroup.SystemSettings, false);
        AutoDisplayLastResult = BooleanOptionItem.Create(1_000_007, "AutoDisplayLastResult", true, TabGroup.SystemSettings, false);

        SuffixMode = StringOptionItem.Create(1_000_001, "SuffixMode", suffixModes, 0, TabGroup.SystemSettings, true)
            .SetHeader(true);
        HideGameSettings = BooleanOptionItem.Create(1_000_002, "HideGameSettings", false, TabGroup.SystemSettings, false);
        DIYGameSettings = BooleanOptionItem.Create(1_000_013, "DIYGameSettings", false, TabGroup.SystemSettings, false);
        PlayerCanSetColor = BooleanOptionItem.Create(1_000_014, "PlayerCanSetColor", false, TabGroup.SystemSettings, false);
        FormatNameMode = StringOptionItem.Create(1_000_003, "FormatNameMode", formatNameModes, 0, TabGroup.SystemSettings, false);
        DisableEmojiName = BooleanOptionItem.Create(1_000_016, "DisableEmojiName", true, TabGroup.SystemSettings, false);
        ChangeNameToRoleInfo = BooleanOptionItem.Create(1_000_004, "ChangeNameToRoleInfo", false, TabGroup.SystemSettings, false);
        SendRoleDescriptionFirstMeeting = BooleanOptionItem.Create(1_000_0016, "SendRoleDescriptionFirstMeeting", false, TabGroup.SystemSettings, false);
        NoGameEnd = BooleanOptionItem.Create(900_002, "NoGameEnd", false, TabGroup.SystemSettings, false);
        AllowConsole = BooleanOptionItem.Create(900_005, "AllowConsole", false, TabGroup.SystemSettings, false);
        RoleAssigningAlgorithm = StringOptionItem.Create(1_000_005, "RoleAssigningAlgorithm", roleAssigningAlgorithms, 4, TabGroup.SystemSettings, true)
           .RegisterUpdateValueEvent(
                (object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue)
            );

        KPDCamouflageMode = BooleanOptionItem.Create(1_000_015, "KPDCamouflageMode", false, TabGroup.SystemSettings, false)
            .SetHeader(true)
            .SetColor(new Color32(255, 192, 203, byte.MaxValue));

        DebugModeManager.SetupCustomOption();

        EnableUpMode = BooleanOptionItem.Create(6090665, "EnableYTPlan", false, TabGroup.SystemSettings, false)
            .SetColor(Color.cyan)
            .SetHeader(true);

        #endregion 

        Logger.Msg("开始加载游戏设置", "Load Options");

        #region 游戏设置

        // SoloKombat
        SoloKombatManager.SetupCustomOption();

        // 驱逐相关设定
        TextOptionItem.Create(66_123_126, "MenuTitle.Ejections", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));

        CEMode = StringOptionItem.Create(6091223, "ConfirmEjectionsMode", ConfirmEjectionsModes, 2, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowImpRemainOnEject = BooleanOptionItem.Create(6090115, "ShowImpRemainOnEject", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowNKRemainOnEject = BooleanOptionItem.Create(6090119, "ShowNKRemainOnEject", true, TabGroup.GameSettings, false).SetParent(ShowImpRemainOnEject)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));
        ShowTeamNextToRoleNameOnEject = BooleanOptionItem.Create(6090125, "ShowTeamNextToRoleNameOnEject", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 238, 232, byte.MaxValue));

        // 禁用相关设定
        TextOptionItem.Create(66_123_120, "MenuTitle.Disable", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));

        DisableVanillaRoles = BooleanOptionItem.Create(6090069, "DisableVanillaRoles", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableHiddenRoles = BooleanOptionItem.Create(6090070, "DisableHiddenRoles", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableTaskWin = BooleanOptionItem.Create(66_900_001, "DisableTaskWin", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));

        // 禁用任务
        DisableTasks = BooleanOptionItem.Create(100300, "DisableTasks", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableSwipeCard = BooleanOptionItem.Create(100301, "DisableSwipeCardTask", false, TabGroup.GameSettings, false).SetParent(DisableTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableSubmitScan = BooleanOptionItem.Create(100302, "DisableSubmitScanTask", false, TabGroup.GameSettings, false).SetParent(DisableTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableUnlockSafe = BooleanOptionItem.Create(100303, "DisableUnlockSafeTask", false, TabGroup.GameSettings, false).SetParent(DisableTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableUploadData = BooleanOptionItem.Create(100304, "DisableUploadDataTask", false, TabGroup.GameSettings, false).SetParent(DisableTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableStartReactor = BooleanOptionItem.Create(100305, "DisableStartReactorTask", false, TabGroup.GameSettings, false).SetParent(DisableTasks)
            .SetGameMode(CustomGameMode.Standard);
        DisableResetBreaker = BooleanOptionItem.Create(100306, "DisableResetBreakerTask", false, TabGroup.GameSettings, false).SetParent(DisableTasks)
            .SetGameMode(CustomGameMode.Standard);

        DisableMeeting = BooleanOptionItem.Create(66_900_002, "DisableMeeting", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableCloseDoor = BooleanOptionItem.Create(66_900_003, "DisableCloseDoor", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));
        DisableSabotage = BooleanOptionItem.Create(66_900_004, "DisableSabotage", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue));

        // 禁用设备
        DisableDevices = BooleanOptionItem.Create(101200, "DisableDevices", false, TabGroup.GameSettings, false)
            .SetColor(new Color32(255, 153, 153, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        DisableSkeldDevices = BooleanOptionItem.Create(101210, "DisableSkeldDevices", false, TabGroup.GameSettings, false).SetParent(DisableDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableSkeldAdmin = BooleanOptionItem.Create(101211, "DisableSkeldAdmin", false, TabGroup.GameSettings, false).SetParent(DisableSkeldDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableSkeldCamera = BooleanOptionItem.Create(101212, "DisableSkeldCamera", false, TabGroup.GameSettings, false).SetParent(DisableSkeldDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableMiraHQDevices = BooleanOptionItem.Create(101220, "DisableMiraHQDevices", false, TabGroup.GameSettings, false).SetParent(DisableDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableMiraHQAdmin = BooleanOptionItem.Create(101221, "DisableMiraHQAdmin", false, TabGroup.GameSettings, false).SetParent(DisableMiraHQDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableMiraHQDoorLog = BooleanOptionItem.Create(101222, "DisableMiraHQDoorLog", false, TabGroup.GameSettings, false).SetParent(DisableMiraHQDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisablePolusDevices = BooleanOptionItem.Create(101230, "DisablePolusDevices", false, TabGroup.GameSettings, false).SetParent(DisableDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisablePolusAdmin = BooleanOptionItem.Create(101231, "DisablePolusAdmin", false, TabGroup.GameSettings, false).SetParent(DisablePolusDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisablePolusCamera = BooleanOptionItem.Create(101232, "DisablePolusCamera", false, TabGroup.GameSettings, false).SetParent(DisablePolusDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisablePolusVital = BooleanOptionItem.Create(101233, "DisablePolusVital", false, TabGroup.GameSettings, false).SetParent(DisablePolusDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipDevices = BooleanOptionItem.Create(101240, "DisableAirshipDevices", false, TabGroup.GameSettings, false).SetParent(DisableDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipCockpitAdmin = BooleanOptionItem.Create(101241, "DisableAirshipCockpitAdmin", false, TabGroup.GameSettings, false).SetParent(DisableAirshipDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipRecordsAdmin = BooleanOptionItem.Create(101242, "DisableAirshipRecordsAdmin", false, TabGroup.GameSettings, false).SetParent(DisableAirshipDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipCamera = BooleanOptionItem.Create(101243, "DisableAirshipCamera", false, TabGroup.GameSettings, false).SetParent(DisableAirshipDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipVital = BooleanOptionItem.Create(101244, "DisableAirshipVital", false, TabGroup.GameSettings, false).SetParent(DisableAirshipDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreConditions = BooleanOptionItem.Create(101290, "IgnoreConditions", false, TabGroup.GameSettings, false).SetParent(DisableDevices)
            .SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreImpostors = BooleanOptionItem.Create(101291, "IgnoreImpostors", false, TabGroup.GameSettings, false).SetParent(DisableDevicesIgnoreConditions)
            .SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreNeutrals = BooleanOptionItem.Create(101293, "IgnoreNeutrals", false, TabGroup.GameSettings, false).SetParent(DisableDevicesIgnoreConditions)
            .SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreCrewmates = BooleanOptionItem.Create(101294, "IgnoreCrewmates", false, TabGroup.GameSettings, false).SetParent(DisableDevicesIgnoreConditions)
            .SetGameMode(CustomGameMode.Standard);
        DisableDevicesIgnoreAfterAnyoneDied = BooleanOptionItem.Create(101295, "IgnoreAfterAnyoneDied", false, TabGroup.GameSettings, false).SetParent(DisableDevicesIgnoreConditions)
            .SetGameMode(CustomGameMode.Standard);

        //会议相关设定
        TextOptionItem.Create(66_123_122, "MenuTitle.Meeting", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue));

        // 会议限制次数
        SyncButtonMode = BooleanOptionItem.Create(100200, "SyncButtonMode", false, TabGroup.GameSettings, false)
            .SetHeader(true)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        SyncedButtonCount = IntegerOptionItem.Create(100201, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.GameSettings, false).SetParent(SyncButtonMode)
            .SetValueFormat(OptionFormat.Times)
            .SetGameMode(CustomGameMode.Standard);

        // 全员存活时的会议时间
        AllAliveMeeting = BooleanOptionItem.Create(100900, "AllAliveMeeting", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue));
        AllAliveMeetingTime = FloatOptionItem.Create(100901, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.GameSettings, false).SetParent(AllAliveMeeting)
            .SetValueFormat(OptionFormat.Seconds);

        // 附加紧急会议
        AdditionalEmergencyCooldown = BooleanOptionItem.Create(101400, "AdditionalEmergencyCooldown", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue));
        AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create(101401, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.GameSettings, false).SetParent(AdditionalEmergencyCooldown)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Players);
        AdditionalEmergencyCooldownTime = FloatOptionItem.Create(101402, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.GameSettings, false).SetParent(AdditionalEmergencyCooldown)
            .SetGameMode(CustomGameMode.Standard)
            .SetValueFormat(OptionFormat.Seconds);

        // 投票相关设定
        VoteMode = BooleanOptionItem.Create(100500, "VoteMode", false, TabGroup.GameSettings, false)
            .SetColor(new Color32(147, 241, 240, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVote = StringOptionItem.Create(100510, "WhenSkipVote", voteModes[0..3], 0, TabGroup.GameSettings, false).SetParent(VoteMode)
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVoteIgnoreFirstMeeting = BooleanOptionItem.Create(100511, "WhenSkipVoteIgnoreFirstMeeting", false, TabGroup.GameSettings, false).SetParent(WhenSkipVote)
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVoteIgnoreNoDeadBody = BooleanOptionItem.Create(100512, "WhenSkipVoteIgnoreNoDeadBody", false, TabGroup.GameSettings, false).SetParent(WhenSkipVote)
            .SetGameMode(CustomGameMode.Standard);
        WhenSkipVoteIgnoreEmergency = BooleanOptionItem.Create(100513, "WhenSkipVoteIgnoreEmergency", false, TabGroup.GameSettings, false).SetParent(WhenSkipVote)
            .SetGameMode(CustomGameMode.Standard);
        WhenNonVote = StringOptionItem.Create(100520, "WhenNonVote", voteModes, 0, TabGroup.GameSettings, false).SetParent(VoteMode)
            .SetGameMode(CustomGameMode.Standard);
        WhenTie = StringOptionItem.Create(100530, "WhenTie", tieModes, 0, TabGroup.GameSettings, false).SetParent(VoteMode)
            .SetGameMode(CustomGameMode.Standard);

        // 破坏相关设定
        TextOptionItem.Create(66_123_121, "MenuTitle.Sabotage", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(241, 212, 227, byte.MaxValue));

        // 通讯破坏小黑人
        CommsCamouflage = BooleanOptionItem.Create(900_013, "CommsCamouflage", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(241, 212, 227, byte.MaxValue));
        DisableReportWhenCC = BooleanOptionItem.Create(900_015, "DisableReportWhenCC", false, TabGroup.GameSettings, false).SetParent(CommsCamouflage)
            .SetGameMode(CustomGameMode.Standard);

        // 破坏时间设定
        SabotageTimeControl = BooleanOptionItem.Create(100800, "SabotageTimeControl", false, TabGroup.GameSettings, false)
           .SetColor(new Color32(241, 212, 227, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        PolusReactorTimeLimit = FloatOptionItem.Create(100801, "PolusReactorTimeLimit", new(1f, 60f, 1f), 30f, TabGroup.GameSettings, false).SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);
        AirshipReactorTimeLimit = FloatOptionItem.Create(100802, "AirshipReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.GameSettings, false).SetParent(SabotageTimeControl)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);

        // 停电特殊设定（飞艇）
        LightsOutSpecialSettings = BooleanOptionItem.Create(101500, "LightsOutSpecialSettings", false, TabGroup.GameSettings, false)
          .SetColor(new Color32(241, 212, 227, byte.MaxValue))
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipViewingDeckLightsPanel = BooleanOptionItem.Create(101511, "DisableAirshipViewingDeckLightsPanel", false, TabGroup.GameSettings, false).SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipGapRoomLightsPanel = BooleanOptionItem.Create(101512, "DisableAirshipGapRoomLightsPanel", false, TabGroup.GameSettings, false).SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);
        DisableAirshipCargoLightsPanel = BooleanOptionItem.Create(101513, "DisableAirshipCargoLightsPanel", false, TabGroup.GameSettings, false).SetParent(LightsOutSpecialSettings)
            .SetGameMode(CustomGameMode.Standard);

        // 地图相关设定
        TextOptionItem.Create(66_123_1234, "MenuTitle.Map", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(85, 170, 255, byte.MaxValue));
        AirShipVariableElectrical = BooleanOptionItem.Create(101600, "AirShipVariableElectrical", false, TabGroup.GameSettings, false)
            .SetHeader(true)
            .SetColor(new Color32(85, 170, 255, byte.MaxValue));
        DisableAirshipMovingPlatform = BooleanOptionItem.Create(101700, "DisableAirshipMovingPlatform", false, TabGroup.GameSettings, false)
            .SetColor(new Color32(85, 170, 255, byte.MaxValue));

        // 其它设定
        TextOptionItem.Create(66_123_123, "MenuTitle.Other", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        // 随机地图模式
        RandomMapsMode = BooleanOptionItem.Create(100400, "RandomMapsMode", false, TabGroup.GameSettings, false)
            .SetHeader(true)
            .SetColor(new Color32(193, 255, 209, byte.MaxValue));
        AddedTheSkeld = BooleanOptionItem.Create(100401, "AddedTheSkeld", false, TabGroup.GameSettings, false).SetParent(RandomMapsMode);
        AddedMiraHQ = BooleanOptionItem.Create(100402, "AddedMIRAHQ", false, TabGroup.GameSettings, false).SetParent(RandomMapsMode);
        AddedPolus = BooleanOptionItem.Create(100403, "AddedPolus", false, TabGroup.GameSettings, false).SetParent(RandomMapsMode);
        AddedTheAirShip = BooleanOptionItem.Create(100404, "AddedTheAirShip", false, TabGroup.GameSettings, false).SetParent(RandomMapsMode);
        // MapDleks = CustomOption.Create(100405, Color.white, "AddedDleks", false, RandomMapMode);

        // 随机出生点
        RandomSpawn = BooleanOptionItem.Create(101300, "RandomSpawn", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetColor(new Color32(193, 255, 209, byte.MaxValue));
        AirshipAdditionalSpawn = BooleanOptionItem.Create(101301, "AirshipAdditionalSpawn", false, TabGroup.GameSettings, false).SetParent(RandomSpawn)
            .SetGameMode(CustomGameMode.Standard);

        // 梯子摔死
        LadderDeath = BooleanOptionItem.Create(101100, "LadderDeath", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetColor(new Color32(193, 255, 209, byte.MaxValue));
        LadderDeathChance = StringOptionItem.Create(101110, "LadderDeathChance", Rates[1..], 0, TabGroup.GameSettings, false).SetParent(LadderDeath)
            .SetGameMode(CustomGameMode.Standard);

        // 修正首刀时间
        FixFirstKillCooldown = BooleanOptionItem.Create(50_900_667, "FixFirstKillCooldown", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        // 首刀保护
        ShieldPersonDiedFirst = BooleanOptionItem.Create(50_900_676, "ShieldPersonDiedFirst", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetColor(new Color32(193, 255, 209, byte.MaxValue));

        // 杀戮闪烁持续
        KillFlashDuration = FloatOptionItem.Create(90000, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.GameSettings, false)
           .SetColor(new Color32(193, 255, 209, byte.MaxValue))
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.Standard);

        // 幽灵相关设定
        TextOptionItem.Create(66_123_124, "MenuTitle.Ghost", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));

        // 幽灵设置
        GhostIgnoreTasks = BooleanOptionItem.Create(900_012, "GhostIgnoreTasks", false, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetHeader(true)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeOtherRoles = BooleanOptionItem.Create(900_010, "GhostCanSeeOtherRoles", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeOtherTasks = BooleanOptionItem.Create(900_019, "GhostCanSeeOtherTasks", true, TabGroup.GameSettings, false)
                .SetGameMode(CustomGameMode.Standard)
            .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeOtherVotes = BooleanOptionItem.Create(900_011, "GhostCanSeeOtherVotes", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
             .SetColor(new Color32(217, 218, 255, byte.MaxValue));
        GhostCanSeeDeathReason = BooleanOptionItem.Create(900_014, "GhostCanSeeDeathReason", true, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.Standard)
           .SetColor(new Color32(217, 218, 255, byte.MaxValue));

        #endregion 

        Logger.Msg("模组选项加载完毕", "Load Options");
        IsLoaded = true;
    }

    public static void SetupAddonOptions(int id, TabGroup tab, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), Rates, 0, tab, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                .SetGameMode(customGameMode) as StringOptionItem;
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 15, 1), 1, tab, false).SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }
    public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), RoleSpwanModes, 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 15, 1), 1, tab, false).SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }
    public static void SetupSingleRoleOptions(int id, TabGroup tab, CustomRoles role, int count, CustomGameMode customGameMode = CustomGameMode.Standard)
    {
        var spawnOption = StringOptionItem.Create(id, role.ToString(), RoleSpwanModes, 0, tab, false).SetColor(Utils.GetRoleColor(role))
            .SetHeader(true)
            .SetGameMode(customGameMode) as StringOptionItem;
        // 初期値,最大値,最小値が同じで、stepが0のどうやっても変えることができない個数オプション
        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(count, count, count), count, tab, false).SetParent(spawnOption)
            .SetHidden(true)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }
    private static void SetupLoversRoleOptionsToggle(int id, CustomGameMode customGameMode = CustomGameMode.Standard)
    {
        var role = CustomRoles.Lovers;
        var spawnOption = StringOptionItem.Create(id, role.ToString(), Rates, 0, TabGroup.Addons, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                .SetGameMode(customGameMode) as StringOptionItem;

        LoverKnowRoles = BooleanOptionItem.Create(id + 4, "LoverKnowRoles", true, TabGroup.Addons, false).SetParent(spawnOption)
            .SetGameMode(customGameMode);

        LoverSuicide = BooleanOptionItem.Create(id + 3, "LoverSuicide", true, TabGroup.Addons, false).SetParent(spawnOption)
            .SetGameMode(customGameMode);

        var countOption = IntegerOptionItem.Create(id + 1, "NumberOfLovers", new(2, 2, 1), 2, TabGroup.Addons, false).SetParent(spawnOption)
            .SetHidden(true)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }
    private static void SetupMadmateRoleOptionsToggle(int id, CustomGameMode customGameMode = CustomGameMode.Standard)
    {
        var role = CustomRoles.Madmate;
        var spawnOption = StringOptionItem.Create(id, role.ToString(), RoleSpwanToggle, 0, TabGroup.Addons, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                .SetGameMode(customGameMode) as StringOptionItem;

        MadmateSpawnMode = StringOptionItem.Create(id + 10, "MadmateSpawnMode", madmateSpawnMode, 0, TabGroup.Addons, false).SetParent(spawnOption);
        MadmateCountMode = StringOptionItem.Create(id + 11, "MadmateCountMode", madmateCountMode, 0, TabGroup.Addons, false).SetParent(spawnOption);
        SheriffCanBeMadmate = BooleanOptionItem.Create(id + 12, "SheriffCanBeMadmate", false, TabGroup.Addons, false).SetParent(spawnOption);
        MayorCanBeMadmate = BooleanOptionItem.Create(id + 13, "MayorCanBeMadmate", false, TabGroup.Addons, false).SetParent(spawnOption);
        NGuesserCanBeMadmate = BooleanOptionItem.Create(id + 14, "NGuesserCanBeMadmate", false, TabGroup.Addons, false).SetParent(spawnOption);
        SnitchCanBeMadmate = BooleanOptionItem.Create(id + 15, "SnitchCanBeMadmate", false, TabGroup.Addons, false).SetParent(spawnOption);
        MadSnitchTasks = IntegerOptionItem.Create(id + 16, "MadSnitchTasks", new(1, 99, 1), 3, TabGroup.Addons, false).SetParent(SnitchCanBeMadmate)
            .SetValueFormat(OptionFormat.Pieces);
        JudgeCanBeMadmate = BooleanOptionItem.Create(id + 17, "JudgeCanBeMadmate", false, TabGroup.Addons, false).SetParent(spawnOption);

        var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 15, 1), 1, TabGroup.Addons, false).SetParent(spawnOption)
            .SetHidden(true)
            .SetGameMode(customGameMode);

        CustomRoleSpawnChances.Add(role, spawnOption);
        CustomRoleCounts.Add(role, countOption);
    }
    public class OverrideTasksData
    {
        public static Dictionary<CustomRoles, OverrideTasksData> AllData = new();
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        public OptionItem doOverride;
        public OptionItem assignCommonTasks;
        public OptionItem numLongTasks;
        public OptionItem numShortTasks;

        public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role)
        {
            IdStart = idStart;
            Role = role;
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), Utils.GetRoleName(role)) } };
            doOverride = BooleanOptionItem.Create(idStart++, "doOverride", false, tab, false).SetParent(CustomRoleSpawnChances[role])
                .SetValueFormat(OptionFormat.None);
            doOverride.ReplacementDictionary = replacementDic;
            assignCommonTasks = BooleanOptionItem.Create(idStart++, "assignCommonTasks", true, tab, false).SetParent(doOverride)
                .SetValueFormat(OptionFormat.None);
            assignCommonTasks.ReplacementDictionary = replacementDic;
            numLongTasks = IntegerOptionItem.Create(idStart++, "roleLongTasksNum", new(0, 99, 1), 3, tab, false).SetParent(doOverride)
                .SetValueFormat(OptionFormat.Pieces);
            numLongTasks.ReplacementDictionary = replacementDic;
            numShortTasks = IntegerOptionItem.Create(idStart++, "roleShortTasksNum", new(0, 99, 1), 3, tab, false).SetParent(doOverride)
                .SetValueFormat(OptionFormat.Pieces);
            numShortTasks.ReplacementDictionary = replacementDic;

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするOverrideTasksDataが作成されました", "OverrideTasksData");
        }
        public static OverrideTasksData Create(int idStart, TabGroup tab, CustomRoles role)
        {
            return new OverrideTasksData(idStart, tab, role);
        }
        public static OverrideTasksData Create(SimpleRoleInfo roleInfo, int idOffset)
        {
            return new OverrideTasksData(roleInfo.ConfigId + idOffset, roleInfo.Tab, roleInfo.RoleName);
        }
    }
}