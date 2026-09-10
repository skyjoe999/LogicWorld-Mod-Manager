using JimmysUnityUtilities;
using LogicUI.MenuTypes;
using LogicWorld.Audio;
using LogicWorld.GameStates;
using LogicWorld.UI.MainMenu.Modding;

namespace SkysModManager.Client;

public partial class SkysModManagerMenu : ToggleableSingletonMenu<SkysModManagerMenu>
{
    private readonly TrackedObjectPoolUtility<ModRepoContainer> RepoPool = new(ModRepoContainer.Build);

    public override void Initialize()
    {
        base.Initialize();
        // // I'll add this later, for now it'll just always check on launch.
        // GetComponent<ConfigurableMenu>().Settings.AddSetting_Toggle(new()
        // {
        //     SettingKey = "SkysModManager.Settings.CheckForUpdates",
        //     OnValueUpdated = _ => { },
        // });
        OnMenuHidden += () =>
        {
            if (this != Instance)
                return;
            // Make sure no changes are saved.
            Edit.SetValueWithoutNotify(false);
            foreach (var item in RepoPool.ActiveObjects)
                item.StopEditing(saveChanges: false);

            // This is super hacky but it works.
            if (AddField.ShowInput)
            {
                AddField.ShowInput = false;
                Refresh();
                ShowMenu();
                CoroutineUtility.RunAfterOneFrame(() => GameStateManager.TransitionTo(GameStateTextID));
            }

            // Without this the press escape to close function will break the mod menu's back button </3
            else if (GameStateManager.CurrentStateID == GameStateTextID)
                CoroutineUtility.RunAfterOneFrame(() => GameStateManager.TransitionTo("MHG.ModsMenu"));
        };
    }

    public void AddRepoToUI(RepoData repo)
    {
        var item = RepoPool.Get(RepoParent);
        item.SetupForRepo(repo, !Edit.Value);
        if (AddField.ShowInput)
            item.gameObject.SetActive(false);
    }

    public void EditModeToggle(bool value)
    {
        if (!Instance.gameObject.activeInHierarchy)
            return;
        foreach (var item in RepoPool.ActiveObjects)
            item.Editing = value;
    }

    public void Refresh()
    {
        if (AddField.ShowInput)
            Recommended.Open();
        else
            Recommended.gameObject.SetActive(false);

        RepoPool.RecycleAllActiveItems();
        foreach (var repo in RepoDataManager.RepoList)
            AddRepoToUI(repo);

        if(Edit.Value)
            EditModeToggle(true);
    }

    public void OnAddButtonPressed()
    {
        if (!AddField.ShowInput)
        {
            AddField.UrlBox.text = AddField.NameBox.text = "";
            AddField.ShowInput = true;
        }
        else if (RepoDataManager.TryInstallRepo(AddField.NameBox.text, AddField.UrlBox.text))
        {
            AddField.ShowInput = false;
            Edit.SetValueWithoutNotify(true);
        }
        else
        {
            SoundPlayer.PlayFail();
            return;
        }

        Refresh();
    }

    public static string GameStateTextID => "SkysModManager.SkysModManagerMenu";
    public class SkysModManagerMenuState : GameState
    {
        public override bool PlayerCanMoveAndLookAround => false;
        public override string TextID => GameStateTextID;
        public override void OnEnter()
        {
            ModsMenu.ShowMenu();
            ShowMenu();
        }

        public override void OnExit() => HideMenu();
    }
}
