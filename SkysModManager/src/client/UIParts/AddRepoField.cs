using System.Reflection;
using JimmysUnityUtilities;
using LogicLocalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Console = FancyPantsConsole.Console;

namespace SkysModManager.Client;

public class AddRepoField : MonoBehaviour
{
    [SerializeField] public SimpleButton Button;
    [SerializeField] public TMP_InputField NameBox;
    [SerializeField] public TMP_InputField UrlBox;

    public const string AddIcon = "f067";
    public const string RepoIcon = "f126";

    private bool _ShowInput;
    public bool ShowInput
    {
        get => _ShowInput;
        set
        {
            if (_ShowInput == value)
                return;
            _ShowInput = value;

            UrlBox.gameObject.SetActive(_ShowInput);
            NameBox.gameObject.SetActive(_ShowInput);
            Button.Localized.gameObject.SetActive(!_ShowInput);
            Button.FontIcon.SetIcon(_ShowInput ? AddIcon : RepoIcon);
        }
    }


    public static AddRepoField Build(Transform parent)
    {
        var root = new GameObject("Add Repo", typeof(RectTransform)).AddComponent<AddRepoField>();

        var rect = root.GetRectTransform();
        rect.SetParent(parent, false);
        rect.anchorMax = Vector2.one;
        rect.offsetMax = rect.offsetMin = rect.anchorMin = Vector2.zero;
        root.AddComponent<HorizontalLayoutGroup>();


        var consoleInput = (TMP_InputField)typeof(Console).GetField("CommandInputField", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Console.Instance);
        root.NameBox = Instantiate(consoleInput, root.transform);
        root.NameBox.AddComponent<LayoutElement>().preferredWidth = 100000;
        root.NameBox.placeholder.GetComponent<LocalizedTextMesh>().SetLocalizationKey("SkysModManagerMenu.Gui.Menu.Add.Name");
        root.NameBox.gameObject.SetActive(false);

        root.UrlBox = Instantiate(root.NameBox, root.transform);
        root.UrlBox.placeholder.GetComponent<LocalizedTextMesh>().SetLocalizationKey("SkysModManagerMenu.Gui.Menu.Add.Origin");


        root.Button = Instantiate(SimpleButton.Prefab, root.transform);
        root.Button.Text.alignment = TextAlignmentOptions.Center; // This does absolutely nothing, the reason it does nothing is entirely unknown to me and I'm about to cry...
        root.Button.Localized.SetLocalizationKey("SkysModManagerMenu.Gui.Menu.Add.Start");
        root.Button.Localized.AddComponent<LayoutElement>().preferredWidth = 100000;
        root.Button.FontIcon.SetIcon(RepoIcon);

        return root;
    }
}