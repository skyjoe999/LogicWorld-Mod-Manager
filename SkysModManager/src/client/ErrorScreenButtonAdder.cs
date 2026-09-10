using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using LogicLocalization;
using LogicUI.MenuParts;
using LogicWorld.UI;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SkysModManager.Client;

public static class ErrorScreenButtonAdder
{
    public static HoverButton Button;

    public static void Setup()
    {
        if (ErrorScreen.Instance == null)
            throw new("Error screen instance is null");

        var mainButton = (typeof(ErrorScreen).GetField("MainMenuButton", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new("Could not find field for error screen button"))
            .GetValue(ErrorScreen.Instance) as HoverButton;

        if (mainButton == null)
            throw new("Main menu button instance is null");

        var newButton = Object.Instantiate(mainButton, mainButton.transform.parent);
        newButton.gameObject.SetActive(false);

        var localized = newButton.GetComponentInChildren<LocalizedTextMesh>(true);
        var text = newButton.GetComponentInChildren<TMP_Text>(true);
        if (localized != null)
            localized.enabled = false;
        if (text != null)
            text.text = "Restart Without Mods";
        else
            throw new("Button had no text component");

        newButton.transform.SetSiblingIndex(0);
        newButton.OnClickEnd += RestartGameWithoutMods;
        Button = newButton;
        newButton.gameObject.SetActive(true);
    }

    public static void RestartGameWithoutMods()
    {
        var args = Environment.GetCommandLineArgs();
        Process.Start(args[0], string.Join(" ", args.Skip(1).Append("--").Append("--disable-mods").Select(s => $"\"{s}\"")));
        Application.Quit();
    }
}