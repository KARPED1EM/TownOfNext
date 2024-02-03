using System.Text;

namespace TONX.Roles.Core.Descriptions;

public abstract class RoleDescription
{
    public RoleDescription(SimpleRoleInfo roleInfo)
    {
        RoleInfo = roleInfo;
    }

    public SimpleRoleInfo RoleInfo { get; }
    /// <summary>イントロなどで表示される短い文</summary>
    public abstract string Blurb { get; }
    /// <summary>
    /// ヘルプコマンドで使用される長い説明文<br/>
    /// AmongUs2023.7.12時点で，Impostor, Crewmateに関してはバニラ側でロング説明文が未実装のため「タスクを行う」と表示される
    /// </summary>
    public abstract string Description { get; }
    public string FullFormatHelp
    {
        get
        {
            Logger.Info("12", "test");
            var builder = new StringBuilder(256);
            builder.AppendFormat("<size={0}>\n", BlankLineSize);
            Logger.Info("13", "test");
            // 职业名
            builder.AppendFormat("<size={0}>{1}", FirstHeaderSize, Translator.GetRoleString(RoleInfo.RoleName.ToString()).Color(RoleInfo.RoleColor.ToReadableColor()));
            // 职业阵营 / 原版职业
            var roleTeam = RoleInfo.CustomRoleType;
            Logger.Info("14", "test");
            builder.AppendFormat("<size={0}> ({1}, {2})\n", BodySize, Translator.GetString($"Team{roleTeam}"), Translator.GetString("BaseOn") + Translator.GetString(RoleInfo.BaseRoleType.Invoke().ToString()));
            Logger.Info("15", "test");
            builder.AppendFormat("<size={0}>{1}\n", BodySize, Description);
            Logger.Info("16", "test");
            // 职业设定
            if (Options.CustomRoleSpawnChances.TryGetValue(RoleInfo.RoleName, out var opt))
                Utils.ShowChildrenSettings(opt, ref builder, forChat: true);
            Logger.Info("17", "test");
            return builder.ToString();
        }
    }
    public string GetFullFormatHelpWithAddonsByPlayer(PlayerControl player)
    {
        var builder = new StringBuilder(1024);

        builder.Append(FullFormatHelp);
        builder.AppendFormat("<size={0}>\n", BlankLineSize);
        builder.Append(AddonDescription.FullFormatHelpByPlayer(player));

        return builder.ToString();
    }
    public string GetFullFormatHelpWithAddonsByRole(CustomRoles rl)
    {
        var builder = new StringBuilder(1024);

        builder.AppendFormat("<size={0}>\n", BlankLineSize);
        builder.Append(AddonDescription.FullFormatHelpBySubRole(rl));

        return builder.ToString();
    }
    public const string FirstHeaderSize = "130%";
    public const string SecondHeaderSize = "100%";
    public const string BodySize = "70%";
    public const string BlankLineSize = "30%";
}
