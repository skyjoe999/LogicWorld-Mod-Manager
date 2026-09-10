using System.Linq;
using System.Reflection;
using JimmysUnityUtilities;
using LogicUI;
using LogicUI.Layouts.Controllers;
using LogicUI.MenuParts.Toggles;
using LogicUI.MenuTypes.ConfigurableMenus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Console = FancyPantsConsole.Console;

namespace SkysModManager.Client;

public class ModRepoItem : MonoBehaviour
{
    [SerializeField] public TMP_InputField TextField;
    [SerializeField] public ToggleSwitch Toggle;
    [SerializeField] public LayoutGroup Layout;
    [SerializeField] public AlertIcon DependencyAlert;

    public RepoModData ModData;

    private static ModRepoItem Template;

    public int Indent { get => Layout.padding.left; set => Layout.padding.left = value; }

    public static ModRepoItem Build(Transform parent)
    {
        if (Template != null)
        {
            var template = Instantiate(Template, parent);
            template.name = "Item";
            return template;
        }

        var item = new GameObject("Item (Template)", typeof(RectTransform)).AddComponent<ModRepoItem>();
        item.transform.SetParent(ModManagerUIHelper.UIPrefabs, false);

        var layout = item.AddComponent<GrowElementListLayout>();
        item.Layout = layout;
        layout.Spacing = 10;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.CountElementsFromFront = true;
        layout.LayoutAlignment = RectTransform.Axis.Horizontal;


        item.TextField = ModManagerUIHelper.GenerateInputField();
        item.TextField.AddComponent<LayoutElement>().preferredWidth = 100000;
        item.TextField.transform.SetParent(item.transform, false);
        item.TextField.fontAsset = Fonts.NotoSans;
        item.TextField.pointSize = 40;
        item.TextField.readOnly = true;
        item.TextField.text = "Template mod";

        item.DependencyAlert = AlertIcon.Build(item.transform).SetBackgroundColor("#ffaa00");
        item.DependencyAlert.Layout.ignoreLayout = false;
        item.DependencyAlert.Layout.preferredHeight = item.DependencyAlert.Layout.preferredWidth = 30;
        item.DependencyAlert.gameObject.SetActive(false);

        var togglePrefab = ((GameObject)typeof(ConfigurableMenuSettings).GetField("SettingPrefab_Toggle", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Console.Instance.GetComponent<ConfigurableMenu>().Settings))
            .GetComponentInChildren<ToggleSwitch>(true);

        item.Toggle = Instantiate(togglePrefab, item.transform);
        item.Toggle.GetComponent<AspectRatioFitter>().enabled = false;
        item.Toggle.GetComponentInChildren<TMP_Text>(true).gameObject.SetActive(false);
        item.Toggle.name = "Toggle";

        var toggleLayout = item.Toggle.AddComponent<LayoutElement>();
        toggleLayout.minHeight = 50;
        toggleLayout.minWidth = 50 * 1.7f;

        Template = item;
        return Build(parent);
    }

    protected void Start()
    {
        Toggle.OnValueChanged += value =>
        {
            if (enabled && ModData is not null)
            {
                if (value)
                    RepoDependencyChecker.ProposeInstall(ModData.Manifest.ID);
                else
                    RepoDependencyChecker.ProposeUninstall(ModData.Manifest.ID);
            }
        };
        RepoDependencyChecker.ActiveListChanged += UpdateDependencyAlert;
    }

    public void UpdateDependencyAlert()
    {
        if (ModData is null || !enabled || !gameObject.activeInHierarchy)
            return;

        var alert = string.Join(
            "\n",
            RepoDependencyChecker.FindMissingDependencies(ModData.Manifest)
                .Select(dep => "- " + dep)
        );

        DependencyAlert.SetHoverText($"Missing dependencies:\n" + alert);
        DependencyAlert.gameObject.SetActive(Toggle.Value && alert.Length != 0);
    }

    // public void UpdateDependencyAlert(string modID)
    // {
    //     if (ModData is null || !gameObject.activeInHierarchy)
    //         return;
    //     if (ModData.Manifest.Dependencies.Concat(ModData.Manifest.ClientDependencies).Concat(ModData.Manifest.ServerDependencies).Any(id => id == modID))
    //         UpdateDependencyAlert();
    // }
}
