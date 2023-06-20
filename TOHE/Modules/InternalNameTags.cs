using System.Collections.Generic;
using UnityEngine;
using static TOHE.NameTagManager;
using static TOHE.Translator;

namespace TOHE;

public static class InternalNameTags
{
    public static Dictionary<string, NameTag> Get() => new()
    {
        {
            "actorour#0029", //咔哥
            new()
            {
                UpperText = new()
                {
                    Text = $"∞ {GetString("Creater")} ∞",
                    Gradient = new(new Color32(198, 255, 221, 255), new Color32(251, 215, 134, 255), new Color32(247, 121, 125, 255)),
                    SizePercentage = 80
                },
                Prefix = new()
                {
                    Text = "✿",
                    TextColor = new Color32(246, 79, 89, 255)
                },
                Suffix = new()
                {
                    Text = "✿",
                    TextColor = new Color32(18, 194, 233, 255)
                },
                Name = new()
                {
                    Gradient = new(new Color32(18, 194, 233, 255), new Color32(196, 113, 237, 255), new Color32(246, 79, 89, 255)),
                    SizePercentage = 90
                }
            }
        }
    };
}