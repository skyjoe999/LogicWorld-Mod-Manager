using JimmysUnityUtilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkysModManager.Client;

public static class ModManagerUIHelper
{
    private static RectTransform _UIPrefabs;
    public static RectTransform UIPrefabs
    {
        get
        {
            if (_UIPrefabs == null)
                ResetPrefabs();
            return _UIPrefabs;
        }
    }

    public static void ResetPrefabs()
    {
        if (_UIPrefabs != null)
            Object.Destroy(_UIPrefabs);
        _UIPrefabs = (RectTransform)new GameObject($"Skys Mod Manager {nameof(UIPrefabs)}", typeof(RectTransform)).transform;
        _UIPrefabs.gameObject.SetActive(false);
    }



    // Fully zero logic world related code!
    public static TMP_InputField GenerateInputField()
    {
        var textField = new GameObject("Text Field", typeof(RectTransform)).AddComponent<TMP_InputField>();
        textField.transition = Selectable.Transition.None;

        textField.textViewport = new GameObject("Viewport", typeof(RectTransform)).GetRectTransform();
        ExpandInto(textField.textViewport, textField.transform);
        textField.textViewport.AddComponent<RectMask2D>();

        textField.textComponent = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        ExpandInto(textField.textComponent.GetRectTransform(), textField.textViewport);

        return textField;

        static void ExpandInto(RectTransform rect, Transform parent)
        {
            rect.SetParent(parent, false);
            rect.anchorMax = Vector2.one;
            rect.offsetMax = rect.offsetMin = rect.anchorMin = Vector2.zero;
        }
    }
}
