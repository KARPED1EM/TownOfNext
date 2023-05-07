using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TOHE.Roles.Neutral;
using UnityEngine;

[assembly: AssemblyFileVersion(TOHE.Main.PluginVersion)]
[assembly: AssemblyInformationalVersion(TOHE.Main.PluginVersion)]
[assembly: AssemblyVersion(TOHE.Main.PluginVersion)]
namespace TOHE;

[BepInPlugin(PluginGuid, "TOHE", PluginVersion)]
[BepInIncompatibility("jp.ykundesu.supernewroles")]
[BepInProcess("Among Us.exe")]
public class Main : BasePlugin
{
    // == プログラム設定 / Program Config ==
    public static readonly string ModName = "TOHE";
    public static readonly string ModColor = "#ffc0cb";
    public static readonly bool AllowPublicRoom = true;
    public static readonly string ForkId = "TOHE";
    public const string OriginalForkId = "OriginalTOH";
    public static HashAuth DebugKeyAuth { get; private set; }
    public const string DebugKeyHash = "c0fd562955ba56af3ae20d7ec9e64c664f0facecef4b3e366e109306adeae29d";
    public const string DebugKeySalt = "59687b";
    public static ConfigEntry<string> DebugKeyInput { get; private set; }
    public static readonly string MainMenuText = "哟这不TOHE吗，几天不见Bug又多了？";
    public const string PluginGuid = "com.karped1em.townofhostedited";
    public const string PluginVersion = "2.3.2";
    public const int PluginCreate = 3;
    public const bool Canary = false;

    public static readonly bool ShowQQButton = true;
    public static readonly string QQInviteUrl = "https://jq.qq.com/?_wv=1027&k=2RpigaN6";
    public static readonly bool ShowDiscordButton = true;
    public static readonly string DiscordInviteUrl = "https://discord.gg/hkk2p9ggv4";

    public Harmony Harmony { get; } = new Harmony(PluginGuid);
    public static Version version = Version.Parse(PluginVersion);
    public static BepInEx.Logging.ManualLogSource Logger;
    public static bool hasArgumentException = false;
    public static string ExceptionMessage;
    public static bool ExceptionMessageIsShown = false;
    public static bool AlreadyShowMsgBox = false;
    public static string credentialsText;
    public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
    //Client Options
    public static ConfigEntry<string> HideName { get; private set; }
    public static ConfigEntry<string> HideColor { get; private set; }
    public static ConfigEntry<bool> UnlockFPS { get; private set; }
    public static ConfigEntry<bool> AutoStart { get; private set; }
    public static ConfigEntry<bool> ForceOwnLanguage { get; private set; }
    public static ConfigEntry<bool> ForceOwnLanguageRoleName { get; private set; }
    public static ConfigEntry<bool> SwitchVanilla { get; private set; }
    public static ConfigEntry<bool> VersionCheat { get; private set; }
    public static ConfigEntry<bool> GodMode { get; private set; }
    public static ConfigEntry<int> MessageWait { get; private set; }

    public static Dictionary<byte, PlayerVersion> playerVersion = new();
    //Preset Name Options
    public static ConfigEntry<string> Preset1 { get; private set; }
    public static ConfigEntry<string> Preset2 { get; private set; }
    public static ConfigEntry<string> Preset3 { get; private set; }
    public static ConfigEntry<string> Preset4 { get; private set; }
    public static ConfigEntry<string> Preset5 { get; private set; }
    //Other Configs
    public static ConfigEntry<string> WebhookURL { get; private set; }
    public static ConfigEntry<string> BetaBuildURL { get; private set; }
    public static ConfigEntry<float> LastKillCooldown { get; private set; }
    public static ConfigEntry<float> LastShapeshifterCooldown { get; private set; }
    public static OptionBackupData RealOptionsData;
    public static Dictionary<byte, PlayerState> PlayerStates = new();
    public static Dictionary<byte, string> AllPlayerNames = new();
    public static Dictionary<(byte, byte), string> LastNotifyNames;
    public static Dictionary<byte, Color32> PlayerColors = new();
    public static Dictionary<byte, PlayerState.DeathReason> AfterMeetingDeathPlayers = new();
    public static Dictionary<CustomRoles, string> roleColors;
    public static bool IsFixedCooldown => CustomRoles.Vampire.IsEnable();
    public static float RefixCooldownDelay = 0f;
    public static GameData.PlayerInfo LastVotedPlayerInfo;
    public static string LastVotedPlayer;
    public static List<byte> ResetCamPlayerList = new();
    public static List<byte> winnerList = new();
    public static List<string> winnerNameList = new();
    public static List<int> clientIdList = new();
    public static List<(string, byte, string)> MessagesToSend = new();
    public static bool isChatCommand = false;
    public static List<PlayerControl> LoversPlayers = new();
    public static bool isLoversDead = true;
    public static Dictionary<byte, float> AllPlayerKillCooldown = new();
    public static Dictionary<byte, Vent> LastEnteredVent = new();
    public static Dictionary<byte, Vector2> LastEnteredVentLocation = new();
    public static List<byte> CyberStarDead = new();
    public static List<byte> BoobyTrapBody = new();
    public static Dictionary<byte, byte> KillerOfBoobyTrapBody = new();
    public static Dictionary<byte, string> DetectiveNotify = new();
    public static List<byte> OverDeadPlayerList = new();
    public static bool DoBlockNameChange = false;
    public static int updateTime;
    public static bool newLobby = false;
    public static Dictionary<int, int> SayStartTimes = new();
    public static Dictionary<int, int> SayBanwordsTimes = new();
    public static Dictionary<byte, float> AllPlayerSpeed = new();
    public const float MinSpeed = 0.0001f;
    public static List<byte> CleanerBodies = new();
    public static List<byte> BrakarVoteFor = new();
    public static Dictionary<byte, (byte, float)> BitPlayers = new();
    public static Dictionary<byte, float> WarlockTimer = new();
    public static Dictionary<byte, float> AssassinTimer = new();
    public static Dictionary<byte, PlayerControl> CursedPlayers = new();
    public static Dictionary<byte, bool> isCurseAndKill = new();
    public static Dictionary<byte, int> MafiaRevenged = new();
    public static Dictionary<byte, int> GuesserGuessed = new();
    public static Dictionary<byte, int> CapitalismAddTask = new();
    public static Dictionary<byte, int> CapitalismAssignTask = new();
    public static Dictionary<(byte, byte), bool> isDoused = new();
    public static Dictionary<(byte, byte), bool> isDraw = new();
    public static Dictionary<byte, (PlayerControl, float)> ArsonistTimer = new();
    public static Dictionary<byte, (PlayerControl, float)> RevolutionistTimer = new();
    public static Dictionary<byte, long> RevolutionistStart = new();
    public static Dictionary<byte, long> RevolutionistLastTime = new();
    public static Dictionary<byte, int> RevolutionistCountdown = new();
    public static Dictionary<byte, byte> PuppeteerList = new();
    public static Dictionary<byte, byte> SpeedBoostTarget = new();
    public static Dictionary<byte, int> MayorUsedButtonCount = new();
    public static Dictionary<byte, int> ParaUsedButtonCount = new();
    public static Dictionary<byte, int> MarioVentCount = new();
    public static Dictionary<byte, long> VeteranInProtect = new();
    public static Dictionary<byte, int> VeteranNumOfUsed = new();
    public static Dictionary<byte, long> GrenadierBlinding = new();
    public static Dictionary<byte, long> MadGrenadierBlinding = new();
    public static Dictionary<byte, int> CursedWolfSpellCount = new();
    public static int AliveImpostorCount;
    public static bool isCursed;
    public static Dictionary<byte, bool> CheckShapeshift = new();
    public static Dictionary<byte, byte> ShapeshiftTarget = new();
    public static Dictionary<(byte, byte), string> targetArrows = new();
    public static Dictionary<byte, Vector2> EscapeeLocation = new();
    public static bool VisibleTasksCount = false;
    public static string nickName = "";
    public static bool introDestroyed = false;
    public static int DiscussionTime;
    public static int VotingTime;
    public static byte currentDousingTarget = byte.MaxValue;
    public static byte currentDrawTarget = byte.MaxValue;
    public static float DefaultCrewmateVision;
    public static float DefaultImpostorVision;
    public static bool IsInitialRelease = DateTime.Now.Month == 1 && DateTime.Now.Day is 17;
    public static bool IsAprilFools = DateTime.Now.Month == 4 && DateTime.Now.Day is 1;
    public static bool ResetOptions = true;
    public static byte FirstDied = byte.MaxValue;
    public static byte ShieldPlayer = byte.MaxValue;
    public static int MadmateNum = 0;
    public static int BardCreations = 0;
    public static Dictionary<byte, byte> Provoked = new();

    public static Dictionary<byte, CustomRoles> DevRole = new();

    public static IEnumerable<PlayerControl> AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null);
    public static IEnumerable<PlayerControl> AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive() && !p.Data.Disconnected && !Pelican.IsEaten(p.PlayerId));

    public static Main Instance;

    //一些很新的东东

    public static string OverrideWelcomeMsg = "";
    public static int HostClientId;

    public static List<string> TName_Snacks_CN = new() { "冰激凌", "奶茶", "巧克力", "蛋糕", "甜甜圈", "可乐", "柠檬水", "冰糖葫芦", "果冻", "糖果", "牛奶", "抹茶", "烧仙草", "菠萝包", "布丁", "椰子冻", "曲奇", "红豆土司", "三彩团子", "艾草团子", "泡芙", "可丽饼", "桃酥", "麻薯", "鸡蛋仔", "马卡龙", "雪梅娘", "炒酸奶", "蛋挞", "松饼", "西米露", "奶冻", "奶酥", "可颂", "奶糖" };
    public static List<string> TName_Snacks_EN = new() { "Ice cream", "Milk tea", "Chocolate", "Cake", "Donut", "Coke", "Lemonade", "Candied haws", "Jelly", "Candy", "Milk", "Matcha", "Burning Grass Jelly", "Pineapple Bun", "Pudding", "Coconut Jelly", "Cookies", "Red Bean Toast", "Three Color Dumplings", "Wormwood Dumplings", "Puffs", "Can be Crepe", "Peach Crisp", "Mochi", "Egg Waffle", "Macaron", "Snow Plum Niang", "Fried Yogurt", "Egg Tart", "Muffin", "Sago Dew", "panna cotta", "soufflé", "croissant", "toffee" };
    public static string Get_TName_Snacks => TranslationController.Instance.currentLanguage.languageID is SupportedLangs.SChinese or SupportedLangs.TChinese ?
        TName_Snacks_CN[IRandom.Instance.Next(0, TName_Snacks_CN.Count)] :
        TName_Snacks_EN[IRandom.Instance.Next(0, TName_Snacks_EN.Count)];

    public override void Load()
    {
        Instance = this;

        //Client Options
        HideName = Config.Bind("Client Options", "Hide Game Code Name", "TOHE");
        HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
        AutoStart = Config.Bind("Client Options", "AutoStart", false);
        UnlockFPS = Config.Bind("Client Options", "UnlockFPS", false);
        SwitchVanilla = Config.Bind("Client Options", "SwitchVanilla", false);
        VersionCheat = Config.Bind("Client Options", "VersionCheat", false);
        GodMode = Config.Bind("Client Options", "GodMode", false);
        ForceOwnLanguage = Config.Bind("Client Options", "ForceOwnLanguage", false);
        ForceOwnLanguageRoleName = Config.Bind("Client Options", "ForceOwnLanguageRoleName", false);
        DebugKeyInput = Config.Bind("Authentication", "Debug Key", "");

        Logger = BepInEx.Logging.Logger.CreateLogSource("TOHE");
        TOHE.Logger.Enable();
        TOHE.Logger.Disable("NotifyRoles");
        TOHE.Logger.Disable("SwitchSystem");
        TOHE.Logger.Disable("ModNews");
        if (!DebugModeManager.AmDebugger)
        {
            TOHE.Logger.Disable("2018k");
            TOHE.Logger.Disable("Github");
            TOHE.Logger.Disable("CustomRpcSender");
            //TOHE.Logger.Disable("ReceiveRPC");
            TOHE.Logger.Disable("SendRPC");
            TOHE.Logger.Disable("SetRole");
            TOHE.Logger.Disable("Info.Role");
            TOHE.Logger.Disable("TaskState.Init");
            //TOHE.Logger.Disable("Vote");
            TOHE.Logger.Disable("RpcSetNamePrivate");
            //TOHE.Logger.Disable("SendChat");
            TOHE.Logger.Disable("SetName");
            //TOHE.Logger.Disable("AssignRoles");
            //TOHE.Logger.Disable("RepairSystem");
            //TOHE.Logger.Disable("MurderPlayer");
            //TOHE.Logger.Disable("CheckMurder");
            TOHE.Logger.Disable("PlayerControl.RpcSetRole");
            TOHE.Logger.Disable("SyncCustomSettings");
        }
        //TOHE.Logger.isDetail = true;

        // 認証関連-初期化
        DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);

        // 認証関連-認証
        DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

        Preset1 = Config.Bind("Preset Name Options", "Preset1", "Preset_1");
        Preset2 = Config.Bind("Preset Name Options", "Preset2", "Preset_2");
        Preset3 = Config.Bind("Preset Name Options", "Preset3", "Preset_3");
        Preset4 = Config.Bind("Preset Name Options", "Preset4", "Preset_4");
        Preset5 = Config.Bind("Preset Name Options", "Preset5", "Preset_5");
        WebhookURL = Config.Bind("Other", "WebhookURL", "none");
        BetaBuildURL = Config.Bind("Other", "BetaBuildURL", "");
        MessageWait = Config.Bind("Other", "MessageWait", 1);
        LastKillCooldown = Config.Bind("Other", "LastKillCooldown", (float)30);
        LastShapeshifterCooldown = Config.Bind("Other", "LastShapeshifterCooldown", (float)30);

        CustomWinnerHolder.Reset();
        ServerAddManager.Init();
        Translator.Init();
        BanManager.Init();
        TemplateManager.Init();
        SpamManager.Init();
        DevManager.Init();
        Cloud.Init();

        IRandom.SetInstance(new NetRandomWrapper());

        hasArgumentException = false;
        ExceptionMessage = "";
        try
        {
            roleColors = new Dictionary<CustomRoles, string>()
            {
                //バニラ役職
                {CustomRoles.Crewmate, "#ffffff"},
                {CustomRoles.Engineer, "#8cffff"},
                {CustomRoles.Scientist, "#8cffff"},
                {CustomRoles.GuardianAngel, "#ffffff"},
                //特殊クルー役職
                {CustomRoles.Bait, "#00f7ff"},
                {CustomRoles.Luckey, "#b8d7a3" },
                {CustomRoles.Needy, "#a4dffe"},
                {CustomRoles.SabotageMaster, "#3333ff"},
                {CustomRoles.Snitch, "#b8fb4f"},
                {CustomRoles.Mayor, "#204d42"},
                {CustomRoles.Paranoia, "#c993f5"},
                {CustomRoles.Psychic, "#6F698C"},
                {CustomRoles.Sheriff, "#f8cd46"},
                {CustomRoles.SuperStar, "#f6f657"},
                {CustomRoles.CyberStar, "#ee4a55" },
                {CustomRoles.SpeedBooster, "#00ffff"},
                {CustomRoles.Doctor, "#80ffdd"},
                {CustomRoles.Trapper, "#5a8fd0"},
                {CustomRoles.Dictator, "#df9b00"},
                {CustomRoles.Detective, "#7160e8" },
                {CustomRoles.SwordsMan, "#f0e68c"},
                {CustomRoles.NiceGuesser, "#eede26"},
                {CustomRoles.Transporter, "#42D1FF"},
                {CustomRoles.TimeManager, "#6495ed"},
                {CustomRoles.Veteran, "#a77738"},
                {CustomRoles.Bodyguard, "#185abd"},
                {CustomRoles.Counterfeiter, "#e0e0e0"},
                {CustomRoles.Grenadier, "#3c4a16"},
                {CustomRoles.Medicaler, "#00a4ff"},
                {CustomRoles.Divinator, "#882c83"},
                {CustomRoles.Glitch, "#dcdcdc"},
                {CustomRoles.Judge, "#f8d85a"},
                {CustomRoles.Mortician, "#333c49"},
                {CustomRoles.Mediumshiper, "#a200ff"},
                {CustomRoles.Observer, "#a8e0fa"},
                //第三陣営役職
                {CustomRoles.Arsonist, "#ff6633"},
                {CustomRoles.Jester, "#ec62a5"},
                {CustomRoles.Terrorist, "#00e600"},
                {CustomRoles.Executioner, "#611c3a"},
                {CustomRoles.God, "#f96464"},
                {CustomRoles.Opportunist, "#4dff4d"},
                {CustomRoles.Mario, "#ff6201"},
                {CustomRoles.Jackal, "#00b4eb"},
                {CustomRoles.Innocent, "#8f815e"},
                {CustomRoles.Pelican, "#34c84b"},
                {CustomRoles.Revolutionist, "#ba4d06"},
                {CustomRoles.FFF, "#414b66"},
                {CustomRoles.Konan, "#4d4dff"},
                {CustomRoles.Gamer, "#68bc71"},
                {CustomRoles.DarkHide, "#483d8b"},
                {CustomRoles.Workaholic, "#008b8b"},
                {CustomRoles.Collector, "#9d8892"},
                {CustomRoles.Provocateur, "#74ba43"},
                {CustomRoles.Sunnyboy, "#ff9902"},
                {CustomRoles.BloodKnight, "#630000"},
                {CustomRoles.Totocalcio, "#ff9409"},
                {CustomRoles.Succubus, "#cf6acd"},
                // GM
                {CustomRoles.GM, "#ff5b70"},
                //サブ役職
                {CustomRoles.NotAssigned, "#ffffff"},
                {CustomRoles.LastImpostor, "#ff1919"},
                {CustomRoles.Lovers, "#ff9ace"},
                {CustomRoles.Ntr, "#00a4ff"},
                {CustomRoles.Madmate, "#ff1919"},
                {CustomRoles.Watcher, "#800080"},
                {CustomRoles.Flashman, "#ff8400"},
                {CustomRoles.Lighter, "#eee5be"},
                {CustomRoles.Seer, "#61b26c"},
                {CustomRoles.Brakar, "#1447af"},
                {CustomRoles.Oblivious, "#424242"},
                {CustomRoles.Bewilder, "#c894f5"},
                {CustomRoles.Workhorse, "#00ffff"},
                {CustomRoles.Fool, "#e6e7ff"},
                {CustomRoles.Avanger, "#ffab1b"},
                {CustomRoles.Youtuber, "#fb749b"},
                {CustomRoles.Egoist, "#5600ff"},
                {CustomRoles.TicketsStealer, "#ff1919"},
                {CustomRoles.DualPersonality, "#3a648f"},
                {CustomRoles.Mimic, "#ff1919"},
                {CustomRoles.Reach, "#74ba43"},
                {CustomRoles.Charmed, "#cf6acd"},
                //SoloKombat
                {CustomRoles.KB_Normal, "#f55252"}
            };
            foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
            {
                switch (role.GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Impostor:
                        roleColors.TryAdd(role, "#ff1919");
                        break;
                    default:
                        break;
                }
            }
        }
        catch (ArgumentException ex)
        {
            TOHE.Logger.Error("错误：字典出现重复项", "LoadDictionary");
            TOHE.Logger.Exception(ex, "LoadDictionary");
            hasArgumentException = true;
            ExceptionMessage = ex.Message;
            ExceptionMessageIsShown = false;
        }
        TOHE.Logger.Info($"{Application.version}", "AmongUs Version");

        var handler = TOHE.Logger.Handler("GitVersion");
        handler.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}");
        handler.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}");
        handler.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}");
        handler.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}");
        handler.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}");
        handler.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}");

        ClassInjector.RegisterTypeInIl2Cpp<ErrorText>();

        Harmony.PatchAll();

        if (!DebugModeManager.AmDebugger) ConsoleManager.DetachConsole();
        else ConsoleManager.CreateConsole();

        TOHE.Logger.Msg("========= TOHE loaded! =========", "Plugin Load");
    }
}
public enum CustomRoles
{
    //Default
    Crewmate = 0,
    //Impostor(Vanilla)
    Impostor,
    Shapeshifter,
    //Impostor
    BountyHunter,
    FireWorks,
    Mafia,
    SerialKiller,
    ShapeMaster,
    EvilGuesser,
    Minimalism,
    Zombie,
    Sniper,
    Vampire,
    Witch,
    Warlock,
    Assassin,
    Hacker,
    Miner,
    Escapee,
    Mare,
    Puppeteer,
    TimeThief,
    EvilTracker,
    AntiAdminer,
    Sans,
    Bomber,
    BoobyTrap,
    Scavenger,
    Capitalism,
    Gangster,
    Cleaner,
    BallLightning,
    Greedier,
    CursedWolf,
    ImperiusCurse,
    QuickShooter,
    Concealer,
    Eraser,
    OverKiller,
    Hangman,
    Bard,
    Swooper,
    Crewpostor,
    //Crewmate(Vanilla)
    Engineer,
    GuardianAngel,
    Scientist,
    //Crewmate
    Bait,
    Luckey,
    Needy,
    SuperStar,
    CyberStar,
    Mayor,
    Paranoia,
    Psychic,
    SabotageMaster,
    Sheriff,
    Snitch,
    SpeedBooster,
    Trapper,
    Dictator,
    Doctor,
    Detective,
    SwordsMan,
    NiceGuesser,
    Transporter,
    TimeManager,
    Veteran,
    Bodyguard,
    Counterfeiter,
    Grenadier,
    Medicaler,
    Divinator,
    Glitch,
    Judge,
    Mortician,
    Mediumshiper,
    Observer,
    //Neutral
    Arsonist,
    Jester,
    God,
    Opportunist,
    Mario,
    Terrorist,
    Executioner,
    Jackal,
    Innocent,
    Pelican,
    Revolutionist,
    FFF,
    Konan,
    Gamer,
    DarkHide,
    Workaholic,
    Collector,
    Provocateur,
    Sunnyboy,
    BloodKnight,
    Totocalcio,
    Succubus,

    //SoloKombat
    KB_Normal,

    //GM
    GM,

    // Sub-role after 500
    NotAssigned = 500,
    LastImpostor,
    Lovers,
    Ntr,
    Madmate,
    Watcher,
    Flashman,
    Lighter,
    Seer,
    Brakar,
    Oblivious,
    Bewilder,
    Workhorse,
    Fool,
    Avanger,
    Youtuber,
    Egoist,
    TicketsStealer,
    DualPersonality,
    Mimic,
    Reach,
    Charmed,
}
//WinData
public enum CustomWinner
{
    Draw = -1,
    Default = -2,
    None = -3,
    Error = -4,
    Impostor = CustomRoles.Impostor,
    Crewmate = CustomRoles.Crewmate,
    Jester = CustomRoles.Jester,
    Terrorist = CustomRoles.Terrorist,
    Lovers = CustomRoles.Lovers,
    Executioner = CustomRoles.Executioner,
    Arsonist = CustomRoles.Arsonist,
    Revolutionist = CustomRoles.Revolutionist,
    Jackal = CustomRoles.Jackal,
    God = CustomRoles.God,
    Mario = CustomRoles.Mario,
    Innocent = CustomRoles.Innocent,
    Pelican = CustomRoles.Pelican,
    Youtuber = CustomRoles.Youtuber,
    Egoist = CustomRoles.Egoist,
    Gamer = CustomRoles.Gamer,
    DarkHide = CustomRoles.DarkHide,
    Workaholic = CustomRoles.Workaholic,
    Collector = CustomRoles.Collector,
    BloodKnight = CustomRoles.BloodKnight,
    Succubus = CustomRoles.Succubus,
}
public enum AdditionalWinners
{
    None = -1,
    Lovers = CustomRoles.Lovers,
    Opportunist = CustomRoles.Opportunist,
    Executioner = CustomRoles.Executioner,
    FFF = CustomRoles.FFF,
    Provocateur = CustomRoles.Provocateur,
    Sunnyboy = CustomRoles.Sunnyboy,
    Totocalcio = CustomRoles.Totocalcio,
}
public enum SuffixModes
{
    None = 0,
    TOHE,
    Streaming,
    Recording,
    RoomHost,
    OriginalName,
    DoNotKillMe,
    NoAndroidPlz
}
public enum VoteMode
{
    Default,
    Suicide,
    SelfVote,
    Skip
}
public enum TieMode
{
    Default,
    All,
    Random
}