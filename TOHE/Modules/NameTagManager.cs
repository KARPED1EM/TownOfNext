using AmongUs.Data;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

#nullable enable
public static class NameTagManager
{
    private static readonly string TAGS_DIRECTORY_PATH = @"./TOHE_Data/NameTags/";
    public static Dictionary<string, NameTag> NameTags = new();
    public static void ApplyFor(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || player == null) return;

        if (!(player.AmOwner || (player.IsModClient() && NameTags.ContainsKey(player.FriendCode)))) return;

        string name = player.GetTrueName();
        if (Main.nickName != "" && player.AmOwner) name = Main.nickName;

        if (AmongUsClient.Instance.IsGameStarted && player.AmOwner)
        {
            if (Options.FormatNameMode.GetInt() == 1 && Main.nickName == "")
                name = Palette.GetColorName(Camouflage.PlayerSkins[PlayerControl.LocalPlayer.PlayerId].ColorId);
        }

        if (NameTags.ContainsKey(player.FriendCode))
        {
            name = NameTags[player.FriendCode].Apply(name, player.AmOwner, !GameStates.IsLobby);
        }
        else if (player.AmOwner && GameStates.IsLobby)
            name = Options.GetSuffixMode() switch
            {
                SuffixModes.TOHE => name += $"\r\n<color={Main.ModColor}>TOHE v{Main.PluginVersion}</color>",
                SuffixModes.Streaming => name += $"\r\n<size=1.7><color={Main.ModColor}>{GetString("SuffixMode.Streaming")}</color></size>",
                SuffixModes.Recording => name += $"\r\n<size=1.7><color={Main.ModColor}>{GetString("SuffixMode.Recording")}</color></size>",
                SuffixModes.RoomHost => name += $"\r\n<size=1.7><color={Main.ModColor}>{GetString("SuffixMode.RoomHost")}</color></size>",
                SuffixModes.OriginalName => name += $"\r\n<size=1.7><color={Main.ModColor}>{DataManager.player.Customization.Name}</color></size>",
                SuffixModes.DoNotKillMe => name += $"\r\n<size=1.7><color={Main.ModColor}>{GetString("SuffixModeText.DoNotKillMe")}</color></size>",
                SuffixModes.NoAndroidPlz => name += $"\r\n<size=1.7><color={Main.ModColor}>{GetString("SuffixModeText.NoAndroidPlz")}</color></size>",
                _ => name
            };

        if (name != player.name && player.CurrentOutfitType == PlayerOutfitType.Default)
            player.RpcSetName(name);
    }
    public static void Init()
    {
        NameTags = new();
        var files = Directory.EnumerateFiles(TAGS_DIRECTORY_PATH, "*.json", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            try { ReadTagsFromFile(file); }
            catch (Exception ex)
            {
                Logger.Error($"Load Tag From: {file} Failed\n" + ex.ToString(), "NameTagManager", false);
            }
        }
        Logger.Msg($"{NameTags.Count} Name Tags Loaded", "NameTagManager");
    }
    public static void ReadTagsFromFile(string path)
    {
        if (path.ToLower().Contains("template")) return;
        var text = File.ReadAllText(path);
        var obj = JObject.Parse(text);
        var tag = GetTagFromJObject(obj);
        string friendCode = Path.GetFileNameWithoutExtension(path);
        if (tag != null && friendCode != null)
        {
            NameTags.Add(friendCode, tag);
            Logger.Info($"Name Tag Loaded: {friendCode}", "NameTagManager");
        }
    }
    public static NameTag? GetTagFromJObject(JObject obj)
    {
        var tag = new NameTag();

        if (obj.TryGetValue("UpperText", out var upper))
            tag.UpperText = GetComponent(upper);

        if (obj.TryGetValue("Prefix", out var prefix))
            tag.Prefix = GetComponent(prefix);

        if (obj.TryGetValue("Suffix", out var suffix))
            tag.Suffix = GetComponent(suffix);

        if (obj.TryGetValue("Name", out var name))
            tag.Name = GetComponent(name, true);

        Component? GetComponent(JToken token, bool force = false)
        {
            if (token == null) return null;
            var com = new Component
            {
                Text = token["Text"]?.ToString(),
                SizePercentage = GetSizePercentage(token["SizePercentage"]?.ToString()),
                TextColor = GetTextColor(token["Color"]?.ToString()),
                Gradient = GetGradient(token["Gradient"]?.ToString()),
                Spaced = GetSpaced(token["Spaced"]?.ToString()) ?? true
            };
            return (com.Text != null || force) ? com : null;
        }

        float? GetSizePercentage(string? str)
        {
            if (str is null or "") return null;
            float size = 100;
            try { size = float.Parse(str); } catch { return null; }
            return size;
        }

        Color32? GetTextColor(string? str)
        {
            if (str is null or "") return null;
            return Utils.TryParseToColor32(str, out var color) ? color : null;
        }

        ColorGradient? GetGradient(string? str)
        {
            if (str is null or "") return null;
            var args = str.Split(',', '，');
            if (args.Length < 2) return null;
            List<Color> colors = new();
            args.Do(arg =>
            {
                if (Utils.TryParseToColor32(arg, out var color))
                    colors.Add(color.GetValueOrDefault());
            });
            var gradient = new ColorGradient(colors.ToArray());
            return gradient.IsValid ? gradient : null;
        }

        bool? GetSpaced(string? str)
        {
            if (str == null) return null;
            return str.ToLower() == "true";
        }

        return tag;
    }
    public class NameTag
    {
        public Component? UpperText { get; set; }
        public Component? Prefix { get; set; }
        public Component? Suffix { get; set; }
        public Component? Name { get; set; }
        public string Apply(string name, bool host, bool inOneLine = false)
        {
            if (host && GameStates.IsOnlineGame)
            {
                name = $"<color=#ffd6ec>TOHE</color><color=#baf7ca>★</color>" + name;
            }
            if (Name != null)
            {
                Name.Text = name;
                name = Name.Generate(false);
            }
            name = Prefix?.Generate() + name + Suffix?.Generate();
            if (Options.CurrentGameMode == CustomGameMode.SoloKombat && host && GameStates.IsOnlineGame)
            {
                name = $"<color=#f55252><size=1.7>{GetString("ModeSoloKombat")}</size></color>\r\n" + name;
            }
            else if (!inOneLine)
            {
                var upperText = UpperText?.Generate(false);
                if (upperText is not null and not "")
                    name = upperText + "\r\n" + name;
            }
            return name;
        }
    }
    public class Component
    {
        public float? SizePercentage { get; set; }
        public string? Text { get; set; }
        public Color32? TextColor { get; set; }
        public ColorGradient? Gradient { get; set; }
        public bool Spaced { get; set; } = true;
        public string Generate(bool applySpace = true)
        {
            if (Text == null) return "";
            var text = Text;
            if (Gradient != null && Gradient.IsValid) text = Gradient.Apply(text);
            else if (TextColor != null) text = Utils.ColorString(TextColor.Value, text);
            if (Spaced && applySpace) text = " " + text + " ";
            if (SizePercentage != null) text = $"<size={SizePercentage}%>{text}</size>";
            return text;
        }
    }
    public class ColorGradient
    {
        private List<Color> Colors;
        private float Spacing;
        public ColorGradient(params Color[] colors)
        {
            Colors = new();
            Colors.AddRange(colors);
            Spacing = 1f / (Colors.Count - 1);
        }
        public bool IsValid => Colors.Count >= 2;
        public string Apply(string input)
        {
            if (input.Length == 0) return input;
            if (input.Length == 1) return Utils.ColorString(Colors[0], input);
            float step = 1f / (input.Length - 1);
            StringBuilder sb = new();
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                var color = Evaluate(step * i);
                sb.Append(Utils.ColorString(color, c.ToString()));
            }
            return sb.ToString();
        }
        private Color Evaluate(float percent)
        {
            if (percent > 1) percent = 1;
            int indexLow = Mathf.FloorToInt(percent / Spacing);
            if (indexLow >= Colors.Count - 1) return Colors[^1];
            int indexHigh = indexLow + 1;
            float percentClamp = (Colors.Count - 1) * (percent - indexLow * Spacing);

            Color colorA = Colors[indexLow];
            Color colorB = Colors[indexHigh];

            float r = colorA.r + percentClamp * (colorB.r - colorA.r);
            float g = colorA.g + percentClamp * (colorB.g - colorA.g);
            float b = colorA.b + percentClamp * (colorB.b - colorA.b);

            return new Color(r, g, b);
        }
    }
}
#nullable disable