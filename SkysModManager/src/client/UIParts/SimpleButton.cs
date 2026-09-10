using JimmysUnityUtilities;
using LogicLocalization;
using LogicUI.MenuParts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkysModManager.Client;

public class SimpleButton : MonoBehaviour
{
    public static SimpleButton Prefab;

    [SerializeField] public HoverButton HoverButton;
    [SerializeField] public FontIcon FontIcon;
    [SerializeField] public LayoutElement IconLayout;
    [SerializeField] public HorizontalLayoutGroup Layout;
    [SerializeField] public LocalizedTextMesh Localized;
    [SerializeField] public TMP_Text Text;

    public static void BuildPrefab(Transform parent, HoverButton template)
    {
        var button = Instantiate(template, parent);
        Prefab = button.AddComponent<SimpleButton>();
        Prefab.name = "Button";
        Prefab.HoverButton = button;

        Prefab.FontIcon = Instantiate(Prefab.GetComponentInChildren<FontIcon>(true), Prefab.transform);
        Prefab.FontIcon.name = "Icon";
        var iconTrans = Prefab.FontIcon.GetRectTransform();
        iconTrans.anchorMin = iconTrans.anchorMax = new(1, 0.5f);
        Prefab.IconLayout = Prefab.FontIcon.AddComponent<LayoutElement>();
        Prefab.IconLayout.preferredWidth = Prefab.IconLayout.preferredHeight = 0; // Will be controlled by min.
        DestroyImmediate(Prefab.FontIcon.GetComponent<AspectRatioFitter>());


        Prefab.Localized = Instantiate(Prefab.GetComponentInChildren<LocalizedTextMesh>(true), Prefab.transform);
        Prefab.Localized.name = "Text";
        Prefab.Text = Prefab.Localized.GetComponent<TMP_Text>();
        Prefab.Text.enableAutoSizing = false;

        Prefab.FontIcon.transform.SetAsFirstSibling();
        Prefab.Localized.transform.SetAsFirstSibling();
        while (Prefab.transform.childCount > 2) // Cleanup anything else
        {
            var child = Prefab.transform.GetChild(2);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        Prefab.Layout = Prefab.AddComponent<HorizontalLayoutGroup>();
        Prefab.Layout.childControlWidth = true;
        Prefab.Layout.childForceExpandWidth = false;
        Prefab.Layout.spacing = 20;
        Prefab.Layout.padding = new(10, 10, 10, 10);
        Prefab.IconLayout.minWidth = Prefab.IconLayout.minHeight = 56;
    }
}
