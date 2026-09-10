using System.Collections.Generic;
using LogicUI.MenuTypes;
using LogicUI.MenuTypes.ConfigurableMenus;
using UnityEngine;

namespace SkysModManager.Client;

public class RepoPathPopup : ToggleableSingletonMenu<RepoPathPopup>
{
    public static void Build(ConfigurableMenu root, Transform content, Dictionary<string, GameObject> settingPrefabs)
    {
        // root.name = "Repo Path Popup";
        // content.AddComponent<VerticalLayoutGroup>();

        // var configurableUtil = root.GetComponent<ConfigurableMenuUtility>();
        // configurableUtil.TitleLocalizor.SetLocalizationKey("Sky's Mod Manager Repo Path");
        // configurableUtil.OnCloseButtonPressed += () => GameStateManager.TransitionTo(MainMenuBase.GameStateTextID);


        // var header = new GameObject("Header", typeof(RectTransform));
        // header.transform.SetParent(content);
        // header.transform.localScale = Vector3.one;
        // var headerText = header.AddComponent<TextMeshProUGUI>();
        // headerText.text = "Please select a path for Sky's Mod Manager to use";


        // var input = new GameObject("Input Field", typeof(RectTransform));
        // input.transform.SetParent(content);
        // input.transform.localScale = Vector3.one;
        // var inputField = input.AddComponent<TMP_InputField>();
        // inputField.





        // root.GetComponent<Canvas>().sortingOrder = 10; // Above the title screen but far behind the console.
        // var instance = root.AddComponent<RepoPathPopup>();
        // CoroutineUtility.RunAfterOneFrame(() => { if (Instance == null) { instance.Initialize(); } });
    }
}
