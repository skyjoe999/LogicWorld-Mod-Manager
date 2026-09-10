using System.Reflection;
using JimmysUnityUtilities;
using LogicUI.HoverTags;
using LogicUI.Layouts.Controllers;
using LogicUI.MenuParts;
using LogicUI.MenuParts.Toggles;
using LogicUI.MenuTypes;
using LogicUI.MenuTypes.ConfigurableMenus;
using LogicWorld.GameStates;
using UnityEngine;
using UnityEngine.UI;

using Toggle = LogicUI.MenuParts.Toggles.Toggle;

namespace SkysModManager.Client;

public partial class SkysModManagerMenu : ToggleableSingletonMenu<SkysModManagerMenu>
{
    public Toggle Edit;
    public RectTransform RepoParent;
    public RecommendedModRepos Recommended;
    public GameObject Alert;
    private AddRepoField AddField;

    public static SkysModManagerMenu Build(ConfigurableMenu root, Transform content, ScrollRect scrollRect)
    {
        var menu = root.AddComponent<SkysModManagerMenu>();
        menu.name = "Skys Mod Manager Menu";

        var configurableUtil = root.GetComponent<ConfigurableMenuUtility>();
        configurableUtil.TitleLocalizor.SetLocalizationKey("SkysModManagerMenu.Gui.Menu.Title");
        configurableUtil.OnCloseButtonPressed += () => GameStateManager.TransitionTo("MHG.ModsMenu");


        var layout = content.AddComponent<GrowElementListLayout>();
        layout.CountElementsFromFront = true;
        layout.ControlChildThickness = true;
        layout.Spacing = 7;


        scrollRect = Instantiate(scrollRect, content);
        scrollRect.content.DestroyAllChildren();
        scrollRect.content.GetComponent<VerticalLayoutGroup>().childControlHeight = true;
        ((RectTransform)scrollRect.transform.GetChild(0)).SetMarginLeft(0); // Don't ask

        menu.RepoParent = scrollRect.content;
        menu.Recommended = RecommendedModRepos.Build(menu.RepoParent);
        menu.Recommended.OnSuccessfulInstall += () => { menu.Edit.Value = true; };
        menu.AddField = AddRepoField.Build(content);
        menu.AddField.Button.HoverButton.OnClickEnd += menu.OnAddButtonPressed;
        RepoDataManager.OnRepoListChanged += () => { menu.AddField.ShowInput = false; menu.Refresh(); };

        {
            menu.Edit = (Toggle)typeof(ConfigurableMenu).GetField("ShowMenuSettingsToggle", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root);
            menu.Edit = Instantiate(menu.Edit, menu.Edit.transform.parent);
            menu.Edit.name = "Edit";
            var offset = menu.Edit.GetRectTransform().anchoredPosition.x;
            menu.Edit.GetRectTransform().SetAnchoredPositionX(offset * 2);
            configurableUtil.GetRectTransform().offsetMax += new Vector2(offset, 0);
            ((HoverButton)typeof(ConfigurableMenu).GetField("ResizeMove", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(root))
                .GetRectTransform().offsetMax += new Vector2(offset, 0);
            var icon = menu.Edit.GetComponentInChildren<ToggleIcon>(true);
            icon.SetIconUnicodeDirect("f303");
            menu.Edit.GetComponentInChildren<HoverTagArea_Localized>(true).LocalizationKey = "SkysModManagerMenu.Gui.Menu.EditButton.Hover";
            menu.Edit.OnValueChanged += menu.EditModeToggle;
        }

        root.GetComponent<Canvas>().sortingOrder = 20; // Above the mod menu but far behind the console.
        return menu;
    }
}
