using System;
using System.Collections.Generic;
using System.Linq;
using LogicAPI.Modding;
using LogicWorld.SharedCode.Modding;

namespace SkysModManager.Client;

public static class RepoDependencyChecker
{
    public static readonly HashSet<string> ActiveModIDs = [];
    public static event Action ActiveListChanged;

    static RepoDependencyChecker() => RepoDataManager.OnRepoListChanged += Reload;

    public static void Reload()
    {
        ActiveModIDs.Clear();
        ActiveModIDs.UnionWith(
            RepoDataManager.RepoList.SelectMany(repo => repo.Mods).Select(mod => mod.Manifest.ID)
            .Concat(ModRegistry.InstalledMods.Select(mod => mod.Manifest.ID))
        );
        // This is a bit hacky but we don't want to complain about dependencies that are missing just because they we installed externally.
        // But we *also* don't want to count mods that have just been uninstalled.
        ActiveModIDs.ExceptWith(
            RepoDataManager.RepoList
                .SelectMany(repo => repo.Mods)
                .Where(mod => !mod.Installed)
                .Select(mod => mod.Manifest.ID)
            );
    }


    public static void ProposeInstall(string modID)
    {
        if (ActiveModIDs.Add(modID))
            ActiveListChanged?.Invoke();
    }

    public static void ProposeUninstall(string modID)
    {
        if (ActiveModIDs.Remove(modID))
            ActiveListChanged?.Invoke();
    }

    public static IEnumerable<string> FindMissingDependencies(ModManifest manifest)
    {
        return manifest.Dependencies.Union(manifest.ClientDependencies).Union(manifest.ServerDependencies)
            .Where(id => !ActiveModIDs.Contains(id));
    }
}
