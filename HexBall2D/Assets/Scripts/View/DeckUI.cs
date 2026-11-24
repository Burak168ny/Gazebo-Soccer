using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Deste menüsü (maç dışında).
/// - Yetenek listesi: her yeteneğin altındaki buton onu desteye ekler.
/// - Deste: 6 slot; slotta yeteneğin adı yazar, slotun butonu onu desteden çıkarır.
/// Deste cihaza kaydedilir ve maç başında host'a gönderilir.
/// Yetenek adları senin listedeki yazılardan alınır; dükkanda ve butonlarda da o adlar görünür.
/// </summary>
public class DeckUI : MonoBehaviour
{
    public static DeckUI Instance { get; private set; }

    [Serializable]
    public class SkillOption
    {
        public SkillId skill;
        [Tooltip("Desteye ekleme butonu")]
        public Button addButton;
        [Tooltip("Yeteneğin adının yazdığı yazı (boşsa varsayılan ad kullanılır)")]
        public TMP_Text nameText;
    }

    [Serializable]
    public class DeckSlot
    {
        [Tooltip("Slotta seçilen yeteneğin adı yazacak")]
        public TMP_Text nameText;
        [Tooltip("Desteden çıkarma butonu")]
        public Button removeButton;
    }

    [Tooltip("Deste menüsünün kök objesi (açılıp kapanan panel)")]
    [SerializeField] GameObject panel;
    [Tooltip("Paneli kapatan buton (boş bırakılabilir)")]
    [SerializeField] Button closeButton;
    [Tooltip("Sadece YAPILMIŞ yetenekleri ekle; 'Coming Soon' olanları ekleme")]
    [SerializeField] SkillOption[] options;
    [SerializeField] DeckSlot[] slots = new DeckSlot[PlayerDeck.Size];
    [SerializeField] string emptySlotText = "Boş";

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        Instance = this;

        foreach (SkillOption o in options)
        {
            if (o.nameText != null) SkillCatalog.SetName(o.skill, o.nameText.text); // senin yazdığın ad
            SkillId id = o.skill;
            if (o.addButton != null) o.addButton.onClick.AddListener(() => { PlayerDeck.Add(id); Refresh(); });
        }

        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            if (slots[i]?.removeButton != null)
                slots[i].removeButton.onClick.AddListener(() => { PlayerDeck.RemoveAt(index); Refresh(); });
        }

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (panel != null) panel.SetActive(false);
        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Toggle() { if (IsOpen) Close(); else Open(); }

    public void Open()
    {
        if (panel == null) return;
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        // Maça girilince menüyü kapat
        if (IsOpen && NetworkBootstrap.Instance != null && NetworkBootstrap.Instance.Runner != null) Close();
    }

    void Refresh()
    {
        var deck = PlayerDeck.Skills;

        for (int i = 0; i < slots.Length; i++)
        {
            DeckSlot slot = slots[i];
            if (slot == null) continue;
            bool filled = i < deck.Count;
            if (slot.nameText != null) slot.nameText.text = filled ? SkillCatalog.NameOf(deck[i]) : emptySlotText;
            if (slot.removeButton != null) slot.removeButton.interactable = filled;
        }

        foreach (SkillOption o in options)
            if (o.addButton != null) o.addButton.interactable = !PlayerDeck.IsFull && !PlayerDeck.Contains(o.skill);
    }
}
