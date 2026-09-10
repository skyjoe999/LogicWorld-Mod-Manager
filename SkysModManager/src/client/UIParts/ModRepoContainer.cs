using System.Collections.Generic;
using System.Linq;
using JimmysUnityUtilities;
using LogicUI.HoverTags;
using LogicWorld.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace SkysModManager.Client;

public class ModRepoContainer : MonoBehaviour
{
    [SerializeField] public ModRepoItem Header;
    [SerializeField] public RectTransform ItemParent;
    [SerializeField] public SimpleButton Trash;
    [SerializeField] public SimpleButton Update;
    [SerializeField] public HoverTagArea_Localized UpdateHover;

    private RepoData RepoData;

    public readonly List<(ModRepoItem item, RepoModData mod)> Items = [];

    private static readonly ObjectPoolUtility<ModRepoItem> ItemPool = new(ModRepoItem.Build);
    private static ModRepoContainer Template;

    public static ModRepoContainer Build(Transform parent)
    {
        if (Template != null)
        {
            var template = Instantiate(Template, parent);
            template.name = "Repo";
            return template;
        }

        var background = Instantiate(SimpleButton.Prefab, ModManagerUIHelper.UIPrefabs);
        background.HoverButton.EnableButton = false;
        background.HoverButton.SetPaletteAlpha(0);
        DestroyImmediate(background.Layout);
        DestroyImmediate(background.Text.gameObject);
        DestroyImmediate(background.FontIcon.gameObject);

        var repo = background.AddComponent<ModRepoContainer>();
        DestroyImmediate(background);
        repo.name = "Repo (Template)";


        var listLayout = repo.AddComponent<VerticalLayoutGroup>();
        listLayout.padding = new(20, 20, 15, 15);
        listLayout.spacing = 10;

        repo.Header = ModRepoItem.Build(repo.transform);
        repo.Header.name = "Header";
        repo.Header.TextField.text = "Repo name <color=#1d7cea>(origin)</color>";
        repo.Header.TextField.pointSize *= 1.25f;
        repo.Header.Toggle.gameObject.SetActive(false);

        repo.Trash = Instantiate(SimpleButton.Prefab, repo.Header.transform);
        repo.Trash.IconLayout.minWidth = repo.Trash.IconLayout.minHeight = 50;
        repo.Trash.FontIcon.SetIcon("f1f8");
        repo.Trash.transform.SetSiblingIndex(1);
        repo.Trash.Text.gameObject.SetActive(false);
        repo.Trash.gameObject.SetActive(false);

        repo.Update = Instantiate(SimpleButton.Prefab, repo.Header.transform);
        repo.Update.FontIcon.gameObject.SetActive(false);
        repo.Update.transform.SetSiblingIndex(1);
        repo.Update.Localized.SetLocalizationKey("SkysModManagerMenu.Gui.Menu.Repo.UpdateRepo");
        repo.Update.Text.fontSize = 25;
        repo.UpdateHover = repo.Update.AddComponent<HoverTagArea_Localized>();


        repo.ItemParent = repo.GetRectTransform();
        Template = repo;

        return Build(parent);
    }

    public void SetupForRepo(RepoData repo, bool onlyInstalled = true)
    {
        RepoData = repo;
        foreach (var (item, _) in Items)
            ItemPool.Recycle(item);
        Items.Clear();

        Update.gameObject.SetActive(onlyInstalled && (repo.BranchStatus?.behind ?? 0) > 0);

        UpdateHover.enabled = false;
        Update.HoverButton.SetPaletteColor(LogicUI.Palettes.PaletteColor.Accent);
        Header.gameObject.SetActive(true);

        Header.TextField.text = onlyInstalled
            ? $"{repo.Title}"
            : $"{repo.Title} <color=#c0c0c0>({repo.Origin})</color>";

        if (repo.Mods is null)
            return;

        foreach (var mod in repo.GenerateTree(onlyInstalled))
        {
            var item = ItemPool.Get(ItemParent);
            Items.Add((item, mod.mod));
            item.Indent = mod.indent * 40 + 60;
            item.ModData = mod.mod;

            if (onlyInstalled)
            {
                item.TextField.text = mod.mod is { } modData
                    ? $"{modData.Manifest.Name} <color=#c0c0c0>v{modData.Manifest.Version}</color>"
                    : mod.text;
                item.UpdateDependencyAlert();
                item.Toggle.gameObject.SetActive(false);
            }
            else
            {
                if (mod.mod is { } modData)
                {
                    item.TextField.text = mod.text + $"{modData.Manifest.ID} <color=#c0c0c0>({modData.Manifest.Name} v{modData.Manifest.Version})</color>";
                    item.Toggle.gameObject.SetActive(true);
                    item.Toggle.SetValueWithoutNotify(modData.Installed, animate: false);
                    item.UpdateDependencyAlert();
                }
                else
                {
                    item.TextField.text = mod.text;
                    item.Toggle.gameObject.SetActive(false);
                    item.DependencyAlert.gameObject.SetActive(false);
                }
            }

            // This is cheating but saves a lot of trouble </3
            if (mod.mod?.Manifest.ID == SkysModManager_ClientMod.ModID && mod.mod?.Installed == true)
                item.Toggle.gameObject.SetActive(false);
        }
    }


    private bool _Editing = false;
    public bool Editing
    {
        get => _Editing;
        set
        {
            if (_Editing == value)
                return;
            _Editing = value;
            if (RepoData is null)
                return;
            if (!value)
                StopEditing(saveChanges: true);
            else
                SetupForRepo(RepoData, false);
        }
    }

    public void StopEditing(bool saveChanges)
    {
        Editing = false;
        if (saveChanges)
        {
            var list = new HashSet<string>(Items.Where(pair => pair.mod is not null && pair.item.Toggle.Value).Select(pair => pair.mod.LocalPath));
            if (!RepoData.SparseList.SetEquals(list))
                RepoData.UpdateSparseList(list);
        }

        SetupForRepo(RepoData, true);
    }

    protected virtual void Start() => Update.HoverButton.OnClickEnd += TryUpdating;


    public void TryUpdating()
    {
        if (RepoData is null)
            throw new("Tried update before repo was set?");

        RepoData.Update(Input.GetKey(KeyCode.LeftShift), out var success, out var fetchFoundNew);
        if (fetchFoundNew)
        {
            UpdateHover.enabled = true;
            UpdateHover.LocalizationKey = "SkysModManagerMenu.Gui.Menu.Repo.NewFetchOnUpdate";
            Update.HoverButton.SetPaletteColor(LogicUI.Palettes.PaletteColor.AccentModified);
            SoundPlayer.PlaySoundGlobal(Sounds.ButtonDown); // Idk, I just didn't want the default error because it didn't *fail* technically
        }
        else if (!success)
        {
            UpdateHover.enabled = true;
            UpdateHover.LocalizationKey = "SkysModManagerMenu.Gui.Menu.Repo.PullFailed";
            Update.HoverButton.SetPaletteColor(LogicUI.Palettes.PaletteColor.Tertiary);
            SoundPlayer.PlayFail();
        }
        else
        {
            RepoData.PopulateMods(); // Not really sure what to do if this fails...
            RepoData.CheckForUpdates(runFetch: false); // Update the branch info
            SetupForRepo(RepoData, !Editing);
        }
    }
}
