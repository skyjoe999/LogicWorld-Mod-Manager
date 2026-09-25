using System;
using System.Linq;
using System.Reflection;
using JimmysUnityUtilities;
using LogicAPI.Client;
using LogicInitializable.TaskProcessing;
using LogicSettings;
using LogicUI.MenuParts;
using LogicUI.MenuTypes.ConfigurableMenus;
using LogicWorld;
using LogicWorld.GameStates;
using LogicWorld.SharedCode;
using LogicWorld.UI.MainMenu.Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Console = FancyPantsConsole.Console;
using Object = UnityEngine.Object;

namespace SkysModManager.Client;

public class SkysModManager_ClientMod : ClientMod
{
    public static string ModID;

    protected override void Initialize()
    {
        if (Environment.GetCommandLineArgs().Any(s => s == "--disable-mods"))
        {
            var modLoadType = typeof(LogicWorld.Modding.Loading.ModInstanceCallerTask).Assembly.GetType("LogicWorld.Modding.Loading.ModLoader", throwOnError: true).GetNestedType("ModLoadingTask");
            var modTask = (TaskGroup)modLoadType.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            typeof(TaskGroup).GetField("CurrentTaskIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(modTask, 1000);
        }
        try { RepoDataManager.RunGitCommand(GameData.GameDataLocation, "version"); }
        catch (System.ComponentModel.Win32Exception exc){throw new("Could not run git command", exc);}

        try { ErrorScreenButtonAdder.Setup(); }
        catch (Exception exc) { Logger.Exception(exc); }

        ModID = Manifest.ID;
        RepoDataManager.Logger = Logger;

        SceneManager.sceneLoaded += (scene, b) =>
        {
            if (scene.name == "UI_MainMenu")
            {
                if (ErrorScreenButtonAdder.Button != null) // At this point disabling mods won't help anymore
                    Object.Destroy(ErrorScreenButtonAdder.Button.gameObject);

                if (!RepoDataManager.TrySetupPath(Files))
                {
                    // // There should be a menu for this but I am tired and who cares?
                    // RepoDataManager.RepoPath = Path.Join(GameData.GameDataLocation, "SkysModManagerData");
                    // Directory.CreateDirectory(RepoDataManager.RepoPath);

                    // Nope, I decided that I want people to install this correctly so that this mod itself will be updated.
                    var exc = new Exception("Could not find the install directory, are you sure you followed the installation instructions on GitHub?");
                    SceneAndNetworkManager.TriggerErrorScreen(exc);
                    throw exc;
                }

                RepoDataManager.ResetEvent();
                MainUISetup(scene, out var menu);

                try
                {
                    RepoDataManager.LoadExistingRepos();
                    RepoDataManager.CheckForUpdates(onlyOnce: true);
                    RepoDependencyChecker.Reload();
                }
                catch (Exception exc)
                {
                    Logger.Exception(exc);
                }
            }
        };
    }

    // This is a mess, we dont look at this!
    private void MainUISetup(Scene scene, out SkysModManagerMenu modManagerMenu)
    {
        ModManagerUIHelper.ResetPrefabs();

        var settingsMenu = Resources.FindObjectsOfTypeAll<SettingsMenu>()[0];

        var console = Object.Instantiate(Console.Instance, new InstantiateParameters() { scene = scene });
        var menuRoot = console.gameObject;
        Object.DestroyImmediate(console);
        var configurableMenu = menuRoot.GetComponent<ConfigurableMenu>();

        configurableMenu.GetComponentInChildren<ConfigurableMenuSettings>(true).transform.DestroyChildrenAfterIndex(2);
        var content = menuRoot.transform.GetChild(1).GetChild(0);
        content.DestroyAllChildren();

        var modMenu = Resources.FindObjectsOfTypeAll<ModsMenu>()[0];
        var openFolder = (HoverButton)typeof(ModsMenu).GetField("OpenModsFolderButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(modMenu);
        SimpleButton.BuildPrefab(ModManagerUIHelper.UIPrefabs, openFolder);

        // "Manage" button.
        SimpleButton modButton;
        {
            modButton = Object.Instantiate(SimpleButton.Prefab, openFolder.transform.parent);
            modButton.name = "Manage";

            modButton.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var modButtonTrans = modButton.GetRectTransform();
            modButtonTrans.SetAnchorMaxX(0);
            modButtonTrans.SetAnchorMinX(0);
            modButtonTrans.pivot = new(0, modButtonTrans.pivot.y);
            modButtonTrans.SetAnchoredPositionX(0);


            modButton.Localized.SetLocalizationKey("SkysModManagerMenu.Gui.ManageButton");
            modButton.FontIcon.SetIcon("f067");
        }

        try
        {
            var manageAlert = AlertIcon.Build(modButton.transform);

            var alertTrans = manageAlert.GetRectTransform();
            alertTrans.anchorMin = alertTrans.anchorMax = new(1, 1);
            alertTrans.anchoredPosition = new(0, 0);

            var transitionType = typeof(GameState).Assembly.GetType("LogicWorld.GameStates.GameStateTransitionButton");
            var transitionField = transitionType.GetField("GameStateTextID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var transitionButton = (Component)Resources.FindObjectsOfTypeAll(transitionType).First(obj => "MHG.ModsMenu".Equals(transitionField.GetValue(obj)));
            var mainAlert = Object.Instantiate(manageAlert, transitionButton.transform);

            RepoDataManager.OnRepoListChanged += () =>
            {
                var any = RepoDataManager.RepoList.Any(r => (r.BranchStatus?.behind ?? 0) > 0);
                if (manageAlert != null)
                    manageAlert.gameObject.SetActive(any);
                if (mainAlert != null)
                    mainAlert.gameObject.SetActive(any);
            };
        }
        catch (Exception exc)
        {
            Logger.Exception(exc, "Failed to build main menu alerts");
        }

        modButton.HoverButton.OnClickEnd += () => GameStateManager.TransitionTo(SkysModManagerMenu.GameStateTextID);

        var scrollRect = ((RectTransform)typeof(ModsMenu).GetField("ModItemsParent", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(modMenu))
            .GetComponentInParent<ScrollRect>(true);

        modManagerMenu = SkysModManagerMenu.Build(configurableMenu, content, scrollRect);
    }
}
