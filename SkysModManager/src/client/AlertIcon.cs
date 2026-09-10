using JimmysUnityUtilities;
using LogicUI;
using LogicUI.HoverTags;
using LogicUI.MenuParts;
using LogicUI.Palettes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkysModManager.Client;

public class AlertIcon : MonoBehaviour
{
    [SerializeField] public FontIcon ForegroundIcon;
    [SerializeField] public TMP_Text ForegroundText;
    [SerializeField] public FontIcon BackgroundIcon;
    [SerializeField] public TMP_Text BackgroundText;
    [SerializeField] public LayoutElement Layout;
    [SerializeField] public HoverTagArea_String Hover;

    private static AlertIcon Template;
    public static AlertIcon Build(Transform parent)
    {
        if (Template != null)
        {
            var template = Instantiate(Template, parent);
            template.name = "Alert";
            return template;
        }

        var background = Instantiate(SimpleButton.Prefab.FontIcon, ModManagerUIHelper.UIPrefabs);
        var alert = background.AddComponent<AlertIcon>();
        alert.BackgroundIcon = background;
        alert.name = "Alert (Template)";


        alert.BackgroundIcon.SetIcon("f06a");
        alert.Layout = alert.GetOrAddComponent<LayoutElement>();
        alert.Layout.minHeight = alert.Layout.minWidth = 0;
        alert.Layout.preferredHeight = alert.Layout.preferredWidth = 50;
        alert.Layout.ignoreLayout = true;
        alert.BackgroundText = alert.BackgroundIcon.GetComponent<TMP_Text>();
        alert.BackgroundText.font = Fonts.FontAwesomeSolid;

        var alertTrans = alert.BackgroundIcon.GetRectTransform();
        alertTrans.pivot = new(0.5f, 0.5f);
        alertTrans.sizeDelta = new(50, 50);


        alert.ForegroundText = Instantiate(alert.BackgroundText, alert.transform);
        alert.ForegroundText.name = "Outline";
        alert.ForegroundIcon = alert.ForegroundText.GetComponent<FontIcon>();
        DestroyImmediate(alert.ForegroundText.GetComponent<AlertIcon>());

        alert.ForegroundText.color = Color24.DarkCharcoal.WithAlphaChannel();
        alert.ForegroundText.font = Fonts.FontAwesomeRegular;

        alertTrans = alert.ForegroundText.GetRectTransform();
        alertTrans.sizeDelta += new Vector2(5, 5);
        alertTrans.anchorMin = new(0, 0);
        alertTrans.anchorMax = new(1, 1);
        alertTrans.offsetMin = -(alertTrans.offsetMax = new(5, 5));

        alert.Hover = alert.AddComponent<HoverTagArea_String>();

        alert.SetBackgroundColor("#FF5349").SetForegroundColor(PaletteColor.Tertiary).ClearHoverText();

        Template = alert;
        return Build(parent);
    }



    public AlertIcon ClearHoverText() => SetHoverText("");
    public AlertIcon SetHoverText(string text)
    {
        Hover.enabled = text != "";
        Hover.TextOnHover = text;
        return this;
    }


    public AlertIcon SetForegroundColor(Color24 color) => SetForegroundColor(color.WithAlphaChannel());
    public AlertIcon SetForegroundColor(string color) => SetForegroundColor(Color24.Parse(color).WithAlphaChannel());
    public AlertIcon SetForegroundColor(Color color)
    {
        ForegroundIcon.enabled = false;
        ForegroundText.color = color;
        return this;
    }
    public AlertIcon SetForegroundColor(PaletteColor color)
    {
        ForegroundIcon.enabled = true;
        ForegroundIcon.SetPaletteColor(color);
        return this;
    }


    public AlertIcon SetBackgroundColor(Color24 color) => SetBackgroundColor(color.WithAlphaChannel());
    public AlertIcon SetBackgroundColor(string color) => SetBackgroundColor(Color24.Parse(color).WithAlphaChannel());
    public AlertIcon SetBackgroundColor(Color color)
    {
        BackgroundIcon.enabled = false;
        BackgroundText.color = color;
        return this;
    }
    public AlertIcon SetBackgroundColor(PaletteColor color)
    {
        BackgroundIcon.enabled = true;
        BackgroundIcon.SetPaletteColor(color);
        return this;
    }
}
