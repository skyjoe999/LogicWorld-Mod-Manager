using System;
using System.Collections.Generic;
using System.Linq;
using JECS;
using JECS.MemoryFiles;
using LogicAPI;
using LogicAPI.Modding;

namespace SkysModManager.Client;

public class RepoData
{
    public string Path;
    public string Origin;
    public string Title;
    [DontSaveThis]
    public List<RepoModData> Mods;
    [DontSaveThis]
    public HashSet<string> SparseList = [];
    [DontSaveThis]
    public (bool gone, int ahead, int behind)? BranchStatus;

    [DontSaveThis]
    public RepoModData RootFolderMod; // These are not currently supported as they break the sparse checkout system, but hopefully I fix that soon...

    public List<RepoModData> PopulateMods()
    {
        var output = RunCommand(out var success, "sparse-checkout", "list");
        if (!success)
            return Mods = null;
        // Get the list of folders already checked out.
        SparseList = [.. output.Select(s => s.Trim().Replace("\\", "/")).Where(s => s is not null && s.Length != 0)];

        (output, success) = RunCommand(
            "ls-files", "--format=%(path)", "-c", "-i", // List all files that match these filters
            "-x", ModPaths.Manifest, // Match all manifests
            "-x", ModPaths.IgnoreFile, // Match all ignore files
            "-x", "!*/" // Don't match any folders (so we wont match anything like "folder/ignore/file.txt")
        );
        if (!success)
            return Mods = null;

        var paths = output.Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim().Replace("\\", "/")).ToArray();

        // Filter out any ignored mods. (Because of how we "install" them, we can't get these to run anyways.)
        var ignores = paths
            .Where(s => s.EndsWith(ModPaths.IgnoreFile))
            .Select(s => s.Substring(0, s.Length - ModPaths.IgnoreFile.Length))
            .ToHashSet();
        ignores.Remove(""); // I hate this!!!

        Mods = [];
        foreach (var path in paths.Where(s => s.EndsWith(ModPaths.Manifest))) // We shouldn't need this filter but better safe than sorry.
        {
            if (ignores.Any(i => path.StartsWith(i)))
                continue;

            // Read the contents of the file
            (output, success) = RunCommand("cat-file", "--textconv", $"HEAD:{path}");
            if (!success)
                return Mods = null;

            var manifest = new MemoryReadOnlyDataFile(string.Join("\n", output)).GetAsObject<ModManifest>();
            var local = path.Substring(0, path.Length - ModPaths.Manifest.Length - 1);
            Mods.Add(new(local, manifest, SparseList.Contains(local)));
        }

        SparseList.IntersectWith(Mods.Select(m => m.LocalPath));

        var rootIndex = Mods.FindIndex(mod => mod.LocalPath.Trim() == "");
        RootFolderMod = rootIndex >= 0 ? Mods[rootIndex] : null;

        return Mods;
    }

    public (bool gone, int ahead, int behind)? CheckForUpdates(bool runFetch = true)
    {
        if (runFetch)
        {
            RunCommand(out var successFetch, "fetch", "--deepen=0");
            if (!successFetch)
                return BranchStatus = null;
        }

        // var output = RunCommand(out  success, "branch", "--format=%(HEAD)...%(refname:short)...%(upstream:short)...%(upstream:track,nobracket)");
        var output = RunCommand(out var success, "branch", "--format=%(HEAD)%(upstream:track,nobracket)");
        if (!success)
            return BranchStatus = null;

        var head = output.FirstOrDefault(s => s.Trim().StartsWith('*'))?.Trim();
        if (head is null)
            return BranchStatus = null;
        head = head.Substring(1).TrimStart('[').TrimEnd(']').ToLower();

        if (head.Length == 0)
            return BranchStatus = (false, 0, 0);

        if (head == "gone")
            return BranchStatus = (true, 0, 0);

        var comma = head.IndexOf(',');
        if (comma != -1)
            return BranchStatus = int.TryParse(head.AsSpan("ahead ".Length, comma - "ahead ".Length), out var ahead) && int.TryParse(head.AsSpan(comma + " behind ".Length + 1), out var behind)
                ? (false, ahead, behind) : null;

        if (head.StartsWith("ahead "))
            return BranchStatus = int.TryParse(head.AsSpan("ahead ".Length), out var ahead)
                ? (false, ahead, 0) : null;

        if (head.StartsWith("behind "))
            return BranchStatus = int.TryParse(head.AsSpan("behind ".Length), out var behind)
                ? (false, 0, behind) : null;

        throw new($"Unexpected branch format '{head}'");
    }

    public void UpdateSparseList(HashSet<string> list)
    {
        if (!RunCommand(["sparse-checkout", "set", .. list]).success)
            return;
        SparseList = list;
        foreach (var mod in Mods)
            mod.Installed = SparseList.Contains(mod.LocalPath);
    }

    public IEnumerable<(string text, int indent, RepoModData mod)> GenerateTree(bool onlyInstalled)
    {
        var modPaths = Mods.Select(mod =>
        {
            var path = "";
            return (parts: mod.LocalPath.Split("/").Select(s => (root: path, path: path += "/" + s, folder: s)).ToList(), mod);
        }).ToList();

        var countByRoot = modPaths.SelectMany(pair => pair.parts).GroupBy(pair => pair.root).ToDictionary(group => group.Key, group => group.Count());

        var tree = new List<(string text, int indent, RepoModData mod)>();

        var printedRoots = new HashSet<string>();
        foreach (var (parts, mod) in modPaths)
        {
            if (onlyInstalled && !mod.Installed)
                continue;
            var rootDept = "";
            var indent = 0;
            foreach (var (root, path, folder) in parts)
            {
                if (!countByRoot.TryGetValue(path, out var count))
                    yield return (rootDept, indent, mod);
                else if (printedRoots.Add(path))
                    if (count > 1)
                    {
                        yield return (rootDept + folder, indent++, null);
                        rootDept = "";
                    }
                    else
                        rootDept += folder + "/";
                else
                    indent++;
            }
        }
    }


    public void Update(bool force, out bool success, out bool fetchFoundNew)
    {
        success = false;
        var originalStatus = BranchStatus;
        if (CheckForUpdates() is not { } newStatus)
            throw new($"Could not fetch status on repo {Path}");

        if (fetchFoundNew = originalStatus != newStatus)
            return;

        if (!(success = RunCommand("pull", "--deepen=0", "--rebase").success))
            // This code is a mess of best guesses, but the worst that can happen *should* only be loss of modified files...
            if (force)
            {
                if (!(success = RunCommand("pull", "--deepen=0", "--rebase", "--force", "--autostash").success))
                    return; // The pull still failed
                else if(!RunCommand("stash", "pop").success)
                {
                    RepoDataManager.Logger.Error("Failed to merge unsaved changes back in, preforming hard reset");
                    if (!(success = RunCommand("reset", "--hard").success))
                        return; // The pull had issues that we forced away, restoring the stash brought the issues back, and now we can't restore it...
                }
            }
            else
                return;

        RepoDataManager.FixRootIgnore(Path);
    }

    public (List<string> stdOut, bool success) RunCommand(params string[] args) => (RunCommand(out var success, args), success);
    public List<string> RunCommand(out bool success, params string[] args) => RepoDataManager.RunGitCommand(Path, out success, args);
}


public class RepoModData(string localPath, ModManifest manifest, bool installed)
{
    public string LocalPath = localPath;
    public ModManifest Manifest = manifest;
    public bool Installed = installed;
}
