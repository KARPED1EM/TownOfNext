using Hazel;
using System.Collections.Generic;

namespace TOHE.Roles.Impostor;

public static class Concealer
{
    private static readonly int Id = 903534;
    public static List<byte> playerIdList = new();

    private static OptionItem ShapeshiftCooldown;
    private static OptionItem ShapeshiftDuration;

    public static int HiddenCount;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Concealer);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 2, "ShapeshiftCooldown", new(1f, 999f, 1f), 25f, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Concealer])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDuration = FloatOptionItem.Create(Id + 4, "ShapeshiftDuration", new(1f, 999f, 1f), 10f, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Concealer])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        HiddenCount = 0;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
    }
    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetConcealerTimer, SendOption.Reliable, -1);
        writer.Write(HiddenCount);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        HiddenCount = reader.ReadInt32();
    }
    public static bool IsHidding => HiddenCount >= 1 && !GameStates.IsMeeting;
    public static void OnShapeshift(bool shapeshifting)
    {
        HiddenCount += shapeshifting ? 1 : -1;
        SendRPC();
        Camouflage.CheckCamouflage();
    }
    public static void OnReportDeadBody()
    {
        HiddenCount = 0;
        SendRPC();
    }
}