using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TOHE.Modules.NameTagPanel;

public class NameTagPanel
{
    public static ToggleButtonBehaviour TagOptionsButton { get; private set; }
    public static SpriteRenderer CustomBackground { get; private set; }
    public static GameObject Slider { get; private set; }
    public static Dictionary<string, GameObject> Items { get; private set; }
    
    private static int numItems = 0;
    public static void Init(OptionsMenuBehaviour optionsMenuBehaviour)
    {
        var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;

        UiElement[] selectableButtons = optionsMenuBehaviour.ControllerSelectable.ToArray();
        PassiveButton leaveButton = null;
        PassiveButton returnButton = null;
        for (int i = 0; i < selectableButtons.Length; i++)
        {
            var button = selectableButtons[i];
            if (button == null) continue;
            if (button.name == "LeaveGameButton") leaveButton = button.GetComponent<PassiveButton>();
            else if (button.name == "ReturnToGameButton") returnButton = button.GetComponent<PassiveButton>();
        }
        var generalTab = mouseMoveToggle.transform.parent.parent.parent;

        if (CustomBackground == null || TagOptionsButton == null)
        {
            numItems = 0;
            CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
            CustomBackground.name = "Name Tag Panel Background";
            CustomBackground.transform.localScale = new(0.9f, 0.9f, 1f);
            CustomBackground.transform.localPosition += Vector3.back * 8;
            CustomBackground.gameObject.SetActive(false);

            var closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            closeButton.transform.localPosition = new(1.3f, -2.3f, -6f);
            closeButton.name = "Close";
            closeButton.Text.text = Translator.GetString("Close");
            closeButton.Background.color = Palette.DisabledGrey;
            var closePassiveButton = closeButton.GetComponent<PassiveButton>();
            closePassiveButton.OnClick = new();
            closePassiveButton.OnClick.AddListener(new Action(() =>
            {
                CustomBackground.gameObject.SetActive(false);
            }));

            TagOptionsButton = Object.Instantiate(mouseMoveToggle, generalTab);
            var pos = leaveButton?.transform?.localPosition;
            TagOptionsButton.transform.localPosition = pos != null ? pos.Value - new Vector3(1.3f, 0f, 0f) : new(-1.3f, -2.4f, 1f);
            TagOptionsButton.name = "Name Tag Options";
            TagOptionsButton.Text.text = Translator.GetString("NameTagOptions");
            if (ColorUtility.TryParseHtmlString(Main.ModColor, out var modColor))
            {
                TagOptionsButton.Background.color = modColor;
            }
            var tagOptionsPassiveButton = TagOptionsButton.GetComponent<PassiveButton>();
            tagOptionsPassiveButton.OnClick = new();
            tagOptionsPassiveButton.OnClick.AddListener(new Action(() =>
            {
                CustomBackground.gameObject.SetActive(true);
            }));

            var sliderTemplate = AccountManager.Instance.transform.FindChild("MainSignInWindow/SignIn/AccountsMenu/Accounts/Slider").gameObject;
            if (sliderTemplate != null && Slider == null)
            {
                Slider = Object.Instantiate(sliderTemplate, CustomBackground.transform);
                Slider.name = "Name Tags Slider";
                Slider.transform.localPosition = new Vector3(0f, 0f, -1f);
                Slider.transform.localScale = new Vector3(1.2f, 1.5f, 1f);
                var scroller = Slider.GetComponent<Scroller>();
                scroller.ScrollWheelSpeed = 0.3f;
                scroller.SetYBoundsMax(50f);
            }
        }

        RefreshTagList();

    }
    public static void RefreshTagList()
    {
        var scroller = Slider.GetComponent<Scroller>();
        scroller.Inner.gameObject.ForEachChild((Action<GameObject>)(DestroyObj));
        static void DestroyObj(GameObject obj)
        {
            if (obj.name.StartsWith("AccountButton")) Object.Destroy(obj);
        }

        var numberSetter = AccountManager.Instance.transform.FindChild("DOBEnterScreen/EnterAgePage/MonthMenu/Months").GetComponent<NumberSetter>();
        var buttonPrefab = numberSetter.ButtonPrefab.gameObject;

        Items = new();
        foreach (var nameTag in NameTagManager.AllNameTags)
        {
            numItems++;
            var button = Object.Instantiate(buttonPrefab, scroller.Inner);
            button.transform.localPosition = new(-0.7f, 0.9f -0.4f * numItems, -0.5f);
            button.transform.localScale = new(1f, 0.8f, 1f);
            button.name = "Name Tag Item For " + nameTag.Key;
            Object.Destroy(button.GetComponent<UIScrollbarHelper>());
            Object.Destroy(button.GetComponent<NumberButton>());
            button.transform.GetChild(0).GetComponent<TextMeshPro>().text = nameTag.Key;
            var renderer = button.GetComponent<SpriteRenderer>();
            renderer.size.Set(3f, 0.45f);
            renderer.color = Palette.DisabledGrey;
            var rollover = button.GetComponent<ButtonRolloverHandler>();
            rollover.OutColor = Palette.DisabledGrey;
            var passiveButton = button.GetComponent<PassiveButton>();
            passiveButton.OnClick = new();
            passiveButton.OnClick.AddListener(new Action(() =>
            {
                NameTagEditMenu.Toggle(nameTag.Key, null);
            }));
        }
    }
}
