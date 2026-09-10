using System;
using System.Collections.Generic;
using System.Linq;
using JimmysUnityUtilities;
using LogicWorld.Audio;
using UnityEngine;

namespace SkysModManager.Client;

public class RecommendedModRepos : MonoBehaviour
{
    [SerializeField] public ModRepoContainer Container;

    public static readonly HashSet<string> IgnoredUrls = [];

    public readonly List<(ModRepoItem item, (string title, string url) mod)> Items = [];
    public event Action OnSuccessfulInstall;

    public static RecommendedModRepos Build(Transform parent)
    {
        var container = ModRepoContainer.Build(parent);
        var rec = container.AddComponent<RecommendedModRepos>();
        rec.Container = container;

        container.Update.gameObject.SetActive(false);
        container.Header.TextField.text = "Quick Picks:";
        container.UpdateHover.enabled = false;

        foreach (var mod in Recommendations)
        {
            var item = ModRepoItem.Build(container.ItemParent);
            item.Indent = 60;

            item.TextField.text = $"{mod.title} <color=#c0c0c0>({mod.url})</color>";
            item.Toggle.gameObject.SetActive(false);
            var addButton = Instantiate(container.Update, item.Toggle.transform.parent);
            addButton.Localized.SetLocalizationKey("SkysModManagerMenu.Gui.Menu.Recommended.Install");
            addButton.transform.SetSiblingIndex(item.Toggle.gameObject.transform.GetSiblingIndex());
            addButton.gameObject.SetActive(true);


            addButton.HoverButton.OnClickEnd += () => rec.Install(mod);
            rec.Items.Add((item, mod));
        }
        RepoDataManager.OnRepoListChanged += () => IgnoredUrls.UnionWith(RepoDataManager.RepoList.Select(r => r.Origin));

        return rec;
    }

    public void Open()
    {
        var anyActive = false;
        foreach (var (item, mod) in Items)
        {
            var ignore = IgnoredUrls.Contains(mod.url);
            item.gameObject.SetActive(!ignore);
            anyActive = anyActive || !ignore;
        }
        gameObject.SetActive(anyActive);
    }

    public void Install((string title, string url) mod)
    {
        if (!RepoDataManager.TryInstallRepo(mod.title, mod.url))
            SoundPlayer.PlayFail();
        else
            OnSuccessfulInstall?.Invoke();
    }

    public static readonly List<(string title, string url)> Recommendations = [
        ("Sky's Mods", "https://github.com/skyjoe999/SkysLogicWorldMods.git"),
        ("Ecconia's Mods", "https://github.com/Ecconia/Ecconia-LogicWorld-Mods.git"),
    ];
}
