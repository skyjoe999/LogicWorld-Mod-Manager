Note: This mod is still in development, but it should be able to update itself as I continue to update it.

Additionally: changes are only saved if you press the edit button (better UI for this is planned, feel free to make a PR)

For Linux users: steams runtime sandbox appears to break this mod (I do not know how to fix this, again, PRs welcome)
## Installation:

First you're going to need to make sure you have git installed or install it (https://git-scm.com/).



Then simply open a command prompt in the game's mods folder (`LogicWorld/GameData/`) and run the following command:

```
git clone --sparse --depth=1 "https://github.com/skyjoe999/LogicWorld-Mod-Manager.git" "SkysModManagerData/ModManager" && git -C "SkysModManagerData/ModManager" sparse-checkout set SkysModManager && echo this_folder_was_setup_with_skys_command >> "SkysModManagerData/setup_info.txt"
```

Once done, open the game, press the `Mods` button on the main menu and then press `Manage +` to access the mod manager.
