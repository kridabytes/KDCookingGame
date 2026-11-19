using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the Languageprefab root and drag in its children.
/// LanguageManager calls Setup() for every language and SetSelected() when the selection changes.
/// </summary>
public class LanguageItemUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image flag;
    [SerializeField] private Text languageName;

    [Header("Selection states")]
    [Tooltip("Shown when this language is NOT selected.")]
    [SerializeField] private GameObject unselectImage;
    [Tooltip("Shown when this language IS selected.")]
    [SerializeField] private GameObject selectImage;
    [Tooltip("Tick/icon shown when this language IS selected.")]
    [SerializeField] private GameObject selectIcon;

    public LanguageCollector.LanguageData Data { get; private set; }

    public void Setup(LanguageCollector.LanguageData data, Action<LanguageItemUI> onClicked)
    {
        Data = data;

        if (flag != null)
        {
            flag.sprite = data.flag;
            flag.enabled = data.flag != null;
        }

        if (languageName != null)
            languageName.text = data.languageName;

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke(this));
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (unselectImage != null) unselectImage.SetActive(!selected);
        if (selectImage != null) selectImage.SetActive(selected);
        if (selectIcon != null) selectIcon.SetActive(selected);
    }
}
