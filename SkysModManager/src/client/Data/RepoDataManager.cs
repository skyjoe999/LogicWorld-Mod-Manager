using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JECS;
using JimmysUnityUtilities;
using LogicAPI;
using LogicAPI.Modding;
using LogicLog;
using LogicSettings;
using LogicWorld.SharedCode.Modding;

namespace SkysModManager.Client;

public static class RepoDataManager
{
    public static ILogicLogger Logger;
    public static string ExistingRepoInfosPath => Path.Join(RepoPath, "RepoInfos.jecs");

    public static readonly List<RepoData> RepoList = [];
    public static event Action OnRepoListChanged;

    private static bool CheckedForUpdatesAlready = false;
    public static void LoadExistingRepos()
    {
        RepoList.Clear();
        RepoList.AddRange(new DataFile(ExistingRepoInfosPath).Get<List<RepoData>>("RepoList", []) ?? []);
        foreach (var repo in RepoList)
            repo.PopulateMods(); // For some reason serializing this data did not work...
        OnRepoListChanged?.Invoke();
    }

    public static void SaveExistingRepos() => new DataFile(ExistingRepoInfosPath).Set("RepoList", RepoList);

    public static bool TryInstallRepo(string name, string url)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url) || RepoList.Any(r => r.Origin == url))
            return false;
        InstallRepo(name, url, out var success);
        return success;
    }

    public static void InstallRepo(string title, string url, out bool success)
    {
        var folder = new string([.. title.Replace(' ', '_').Where(c => c.IsLetterOrDigit() || c is '_' or '-' or '.')]);

        if (folder == "")
            folder = "UnnamedMod";

        // Make sure the folder name is unique.
        var originalFolder = folder;
        var count = 1;
        while (Directory.Exists(Path.Join(RepoPath, folder)))
            folder = originalFolder + "_" + count++;

        var path = Path.Join(RepoPath, folder);

        RunGitCommand(RepoPath,
            out success,
            "clone",
            "--sparse", // Don't add the folders to the file system
            "--depth=1", // Don't download the branch history (will also stop non-main branches from downloading)
            "--no-local", // Treat local repos like remote repos (not sure this does anything useful but it feels safer)
            "--quiet", // Stops progress from being printed to stderr (why does it go to stderr in the first place???)
            url,
            path
        );

        if (!success)
            return;
        var newRepo = new RepoData()
        {
            Path = path,
            Origin = url,
            Title = title,
        };

        FixRootIgnore(path);
        newRepo.PopulateMods();
        RepoList.Add(newRepo);
        SaveExistingRepos();

        OnRepoListChanged?.Invoke();
    }


    public static void CheckForUpdates(bool runFetch = true, bool onlyOnce = false)
    {
        if (onlyOnce && CheckedForUpdatesAlready)
            return;
        CheckedForUpdatesAlready = true;

        foreach (var repo in RepoList)
            repo.CheckForUpdates(runFetch);
        OnRepoListChanged?.Invoke();
    }


    public static bool TrySetupPath(IModFiles files)
    {
        if (!string.IsNullOrWhiteSpace(RepoPath))
            return true;

        // This will handle if you set up the mod with the installation command.
        // If you somehow trigger this accidentally, that is not on me... (the conditions are unreasonably specific.)
        if (files is not FolderModFiles)
            return false;

        var skysFolder = Path.GetDirectoryName(files.Path.TrimEnd('/', '\\'));
        var potentialPath = Path.GetDirectoryName(skysFolder);
        var safetyFilePath = Path.Join(potentialPath, "setup_info.txt");
        if (!File.Exists(safetyFilePath))
            return false;

        using var stream = File.OpenRead(safetyFilePath);
        using var streamReader = new StreamReader(stream);
        if (streamReader.ReadToEnd().Trim() != "this_folder_was_setup_with_skys_command")
            return false;

        RepoPath = potentialPath;
        if (!File.Exists(ExistingRepoInfosPath)) // If the data already exists, dont overwrite it.
        {
            RepoList.Clear();
            var newRepo = new RepoData()
            {
                Path = skysFolder,
                Origin = "https://github.com/skyjoe999/LogicWorld-Mod-Manager.git",
                Title = "Sky's Mod Manager",
            };
            newRepo.PopulateMods();
            newRepo.CheckForUpdates(runFetch: false);
            RepoList.Add(newRepo);
            SaveExistingRepos();
        }
        else
            LoadExistingRepos();
        return true;
    }

    // This is bad and awful but the alternative is Ecconia's repo being fully incompatible...
    internal static void FixRootIgnore(string path)
    {
        if (File.Exists(Path.Join(path, ModPaths.IgnoreFile)))
            File.Delete(Path.Join(path, ModPaths.IgnoreFile));
    }

    public static List<string> RunGitCommand(string cwd, out bool success, params string[] args)
    {
        var arguments = string.Join(" ", args.Select(a => a.StartsWith('"') && a.EndsWith('"') ? a : $"\"{a}\""));
        var process = new Process
        {
            StartInfo =
            {
                FileName = "git",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = cwd,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        var output = new List<string>();
        var error = new StringBuilder();
        process.OutputDataReceived += (_, e) => output.Add(e.Data ?? "");
        process.ErrorDataReceived += (_, e) => error.AppendLine(e.Data ?? "");

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        var err = error.ToString();
        if (process.ExitCode != 0)
        {
            Logger.Error($"Process ({ProcessString()}) returned with exit code {process.ExitCode}\n{err}".TrimEnd('\n', '\r'));
            success = false;
            return output;
        }
        else if (!err.IsNullOrWhiteSpace())
            Logger.Warn($"Process ({ProcessString()}) printed non-fatal error:\n{err}".TrimEnd('\n', '\r'));

        success = true;
        return output;

        // Taken from System.Diagnostics.Process.StartWithCreateProcess's error message.
        string ProcessString() => $"ApplicationName='{process.StartInfo.FileName}', CommandLine='{process.StartInfo.Arguments}', CurrentDirectory='{process.StartInfo.WorkingDirectory}'";
    }

    public static (List<string> stdOut, bool success) RunGitCommand(string cwd, params string[] args) => (RunGitCommand(cwd, out var success, args), success);

    public static void ResetEvent() => OnRepoListChanged = null;

    [Setting_Secret("SkysModManager.RepoPath")]
    private static string _RepoPath { set; get; }
    public static string RepoPath { set => SettingsManager.Instance.SetSetting("SkysModManager.RepoPath", _RepoPath = value, false, false); get => _RepoPath; }
}
