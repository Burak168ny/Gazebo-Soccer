using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Oyun içi arayüz: dükkan + yetenek kontrolleri. Destedeki yetenekler maç başında SIRAYLA bağlanır:
/// - Dükkan: 6 satır. i. satır destenin i. yeteneği olur (adı yazar, 3 seviye butonu onu satın alır).
/// - Kullan butonları: yetenekler ihtiyacı kadar butonu sırayla alır (Işınlanma/Duvar 2, çoğu 1, pasifler 0).
/// - Yetenek joystick'leri: joystick isteyen yetenekler (Sabitleyen Disk) sırayla birini alır.
/// Buton/joystick, yetenek kullanılabilir seviyeye gelince görünür.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Serializable]
    public class ShopRow
    {
        [Tooltip("Satırın kök objesi (destede o sırada yetenek yoksa gizlenir). Boş bırakılabilir.")]
        public GameObject root;
        [Tooltip("Yeteneğin adının yazacağı yazı")]
        public TMP_Text nameText;
        [Tooltip("1., 2. ve 3. seviye butonları (sırayla)")]
        public Button[] levelButtons = new Button[SkillBase.MaxLevel];

        [NonSerialized] public SkillBase skill;
        [NonSerialized] public TMP_Text[] priceTexts;
        [NonSerialized] public Color[] baseColors;
        [NonSerialized] public ColorBlock[] baseColorBlocks;
        [NonSerialized] public int[] shownState;
    }

    /// Bir kullan butonunun bağlantısı.
    class ActionBinding
    {
        public SkillBase skill;
        public int action;
        public TMP_Text text;
        public string label;
        public int shown = -1;
    }

    /// Bir yetenek joystick'inin bağlantısı.
    class JoystickBinding
    {
        public SkillBase skill;
        public Vector2 lastDir;
        public bool dragging;
        public TMP_Text cooldownText;
        public CanvasGroup group;
        public int shownCooldown = -1;
    }

    [Header("Dükkan")]
    [SerializeField] Button shopToggleButton;
    [SerializeField] GameObject shopPanel;
    [Tooltip("Oyuncunun altınını gösteren yazı (boş bırakılabilir)")]
    [SerializeField] TMP_Text goldText;
    [Tooltip("Sadece maç sırasında görünecek arayüz kökü. ShopUI bu objenin İÇİNDE olmamalı. Boş bırakılabilir.")]
    [SerializeField] GameObject inGameRoot;
    [Tooltip("Dükkan satırları (6 tane, destenin sırasıyla)")]
    [SerializeField] ShopRow[] rows = new ShopRow[PlayerDeck.Size];
    [SerializeField] Color ownedColor = new Color(0.08f, 0.42f, 0.16f); // koyu yeşil

    [Header("Yetenek kontrolleri")]
    [Tooltip("Yeteneklere sırayla dağıtılacak kullan butonları (yazıları otomatik değişir)")]
    [SerializeField] Button[] actionButtons;
    [Tooltip("Joystick isteyen yeteneklere sırayla dağıtılacak joystick'ler")]
    [SerializeField] Joystick[] skillJoysticks;
    [Tooltip("Her joystick için bekleme süresi yazısı (aynı sırayla). Boşsa joystick'in içindeki TMP yazı kullanılır.")]
    [SerializeField] TMP_Text[] skillJoystickCooldownTexts;
    [Tooltip("Pasif yeteneklerin durum yazıları (ör. İkinci Şans bekleme süresi). Sırayla dağıtılır.")]
    [SerializeField] TMP_Text[] passiveStatusTexts;
    [Tooltip("Yetenek joystick'i en az bu kadar çekilmiş olmalı (0-1)")]
    [SerializeField, Range(0f, 1f)] float minJoystickPull = 0.3f;

    const int StateLocked = 0, StateBuyable = 1, StateTooExpensive = 2, StateOwned = 3;

    ActionBinding[] _actions;
    JoystickBinding[] _joysticks;
    SkillBase[] _statusSkills;
    string[] _shownStatus;
    int _shownGold = int.MinValue;
    KeeperController _boundKeeper;
    int _boundDeckCode;

    void Awake()
    {
        if (shopToggleButton != null) shopToggleButton.onClick.AddListener(ToggleShop);
        if (shopPanel != null) shopPanel.SetActive(false);

        foreach (ShopRow row in rows)
        {
            int n = row.levelButtons.Length;
            row.priceTexts = new TMP_Text[n];
            row.baseColors = new Color[n];
            row.baseColorBlocks = new ColorBlock[n];
            row.shownState = new int[n];

            for (int i = 0; i < n; i++)
            {
                Button b = row.levelButtons[i];
                row.shownState[i] = -1;
                if (b == null) continue;

                ShopRow r = row;
                int level = i + 1;
                b.onClick.AddListener(() => Buy(r, level));
                row.priceTexts[i] = b.GetComponentInChildren<TMP_Text>(true);
                row.baseColors[i] = b.targetGraphic != null ? b.targetGraphic.color : Color.white;
                row.baseColorBlocks[i] = b.colors;
            }
        }

        _actions = new ActionBinding[actionButtons != null ? actionButtons.Length : 0];
        for (int i = 0; i < _actions.Length; i++)
        {
            _actions[i] = new ActionBinding();
            if (actionButtons[i] == null) continue;
            int index = i;
            actionButtons[i].onClick.AddListener(() => Use(index));
            _actions[i].text = actionButtons[i].GetComponentInChildren<TMP_Text>(true);
            actionButtons[i].gameObject.SetActive(false);
        }

        _joysticks = new JoystickBinding[skillJoysticks != null ? skillJoysticks.Length : 0];
        for (int i = 0; i < _joysticks.Length; i++)
        {
            _joysticks[i] = new JoystickBinding();
            if (skillJoysticks[i] == null) continue;

            TMP_Text t = skillJoystickCooldownTexts != null && i < skillJoystickCooldownTexts.Length ? skillJoystickCooldownTexts[i] : null;
            if (t == null) t = skillJoysticks[i].GetComponentInChildren<TMP_Text>(true);
            _joysticks[i].cooldownText = t;
            if (t != null) t.text = "";

            CanvasGroup g = skillJoysticks[i].GetComponent<CanvasGroup>();
            if (g == null) g = skillJoysticks[i].gameObject.AddComponent<CanvasGroup>();
            _joysticks[i].group = g;

            skillJoysticks[i].gameObject.SetActive(false);
        }

        int statusCount = passiveStatusTexts != null ? passiveStatusTexts.Length : 0;
        _statusSkills = new SkillBase[statusCount];
        _shownStatus = new string[statusCount];
        for (int i = 0; i < statusCount; i++)
            if (passiveStatusTexts[i] != null) { passiveStatusTexts[i].text = ""; passiveStatusTexts[i].enabled = false; }
    }

    public void ToggleShop()
    {
        if (shopPanel != null) shopPanel.SetActive(!shopPanel.activeSelf);
    }

    void Buy(ShopRow row, int level)
    {
        KeeperController k = KeeperController.Local;
        if (k == null || row.skill == null || row.skill.Level != level - 1) return; // sadece sıradaki seviye
        k.RPC_BuySkill((int)row.skill.Id);
    }

    void Use(int index)
    {
        ActionBinding a = _actions[index];
        if (a.skill == null) return;
        if (a.skill.HandleLocalAction(a.action)) return; // ör. Telekinezi: joystick'i açar
        if (NetworkBootstrap.Instance != null)
            NetworkBootstrap.Instance.QueueAbility(a.skill.Id, a.action);
    }

    // ---------------- Bağlama (maç başında / deste gelince) ----------------

    void Bind(KeeperController k)
    {
        _boundKeeper = k;
        _boundDeckCode = k.DeckCode;
        _shownGold = int.MinValue;

        int buttonIndex = 0, joystickIndex = 0, neededButtons = 0, statusIndex = 0;
        for (int i = 0; i < _statusSkills.Length; i++) { _statusSkills[i] = null; _shownStatus[i] = null; }
        foreach (ActionBinding a in _actions) a.skill = null;
        foreach (JoystickBinding j in _joysticks) { j.skill = null; j.dragging = false; j.shownCooldown = -1; }

        for (int i = 0; i < rows.Length; i++)
        {
            ShopRow row = rows[i];
            int id = k.DeckSkillAt(i);
            row.skill = id >= 0 ? k.GetSkill((SkillId)id) : null;
            for (int b = 0; b < row.shownState.Length; b++) row.shownState[b] = -1;

            if (row.root != null) row.root.SetActive(row.skill != null);
            if (row.nameText != null) row.nameText.text = row.skill != null ? SkillCatalog.NameOf(row.skill.Id) : "";
            for (int b = 0; b < row.levelButtons.Length; b++)
            {
                if (row.priceTexts[b] != null) row.priceTexts[b].text = row.skill != null ? row.skill.PriceOf(b + 1).ToString() : "";
                if (row.skill == null && row.levelButtons[b] != null) row.levelButtons[b].interactable = false;
            }

            if (row.skill == null) continue;
            neededButtons += row.skill.ActionButtonCount;

            // Kullan butonlarını sırayla dağıt
            for (int action = 0; action < row.skill.ActionButtonCount && buttonIndex < _actions.Length; action++, buttonIndex++)
            {
                ActionBinding a = _actions[buttonIndex];
                a.skill = row.skill;
                a.action = action;
                string extra = row.skill.ActionLabel(action);
                a.label = SkillCatalog.NameOf(row.skill.Id) + (string.IsNullOrEmpty(extra) ? "" : " " + extra);
                a.shown = -1;
            }

            // Durum yazısı isteyen pasif yeteneğe yazı ver (seviye alınınca görünür)
            if (row.skill.HasPassiveStatus)
            {
                if (statusIndex < _statusSkills.Length) _statusSkills[statusIndex++] = row.skill;
                else Debug.LogWarning("ShopUI: Yeterli durum yazısı yok. Passive Status Texts listesine TMP yazı ekle.");
            }

            // Joystick isteyen yeteneğe joystick ver
            if (row.skill.UsesJoystick)
            {
                if (joystickIndex < _joysticks.Length) _joysticks[joystickIndex++].skill = row.skill;
                else Debug.LogWarning("ShopUI: Yeterli yetenek joystick'i yok. Skill Joysticks listesine joystick ekle.");
            }
        }

        if (buttonIndex < neededButtons)
            Debug.LogWarning("ShopUI: Destedeki yetenekler için yeterli kullan butonu yok. Action Buttons listesine buton ekle.");
    }

    // ---------------- Her kare ----------------

    void Update()
    {
        MatchManager mm = MatchManager.Instance;
        KeeperController k = KeeperController.Local;
        bool playing = mm != null && k != null && k.Object != null && k.Object.IsValid && mm.Phase == MatchPhase.Playing;

        if (inGameRoot != null && inGameRoot.activeSelf != playing) inGameRoot.SetActive(playing);
        if (!playing)
        {
            if (shopPanel != null && shopPanel.activeSelf) shopPanel.SetActive(false);
            HideControls();
            _boundKeeper = null;
            return;
        }

        if (k != _boundKeeper || k.DeckCode != _boundDeckCode) Bind(k);

        int gold = mm.GoldOf(k.Slot);
        if (gold != _shownGold)
        {
            _shownGold = gold;
            if (goldText != null) goldText.text = gold.ToString();
        }

        foreach (ShopRow row in rows) UpdateRow(row, gold);
        for (int i = 0; i < _actions.Length; i++) UpdateActionButton(i);
        for (int i = 0; i < _joysticks.Length; i++) UpdateJoystick(i);
        for (int i = 0; i < _statusSkills.Length; i++) UpdateStatus(i);
    }

    void UpdateStatus(int i)
    {
        TMP_Text t = passiveStatusTexts[i];
        if (t == null) return;

        SkillBase s = _statusSkills[i];
        bool show = s != null && s.ShowsPassiveStatus;
        if (t.enabled != show) t.enabled = show; // objeyi kapatmıyoruz, sadece yazıyı (objeye başka şey bağlıysa etkilenmesin)
        if (!show) return;

        string text = s.PassiveStatus;
        if (text == _shownStatus[i]) return;
        _shownStatus[i] = text;
        t.text = text;
    }

    void UpdateRow(ShopRow row, int gold)
    {
        SkillBase skill = row.skill;
        if (skill == null) return;

        for (int i = 0; i < row.levelButtons.Length; i++)
        {
            if (row.levelButtons[i] == null) continue;
            int level = i + 1;
            int state = skill.Level >= level ? StateOwned
                      : skill.Level == level - 1 ? (gold >= skill.PriceOf(level) ? StateBuyable : StateTooExpensive)
                      : StateLocked;

            if (state == row.shownState[i]) continue;
            row.shownState[i] = state;
            ApplyButtonState(row, i, state);
        }
    }

    void UpdateActionButton(int i)
    {
        Button button = actionButtons[i];
        ActionBinding a = _actions[i];
        if (button == null) return;

        bool show = a.skill != null && a.skill.HasAction(a.action);
        if (button.gameObject.activeSelf != show) button.gameObject.SetActive(show);
        if (!show) return;

        // Susturulmuşsa bekleme gibi görünür (hangisi uzunsa)
        float silence = _boundKeeper != null && !a.skill.UsableWhileSilenced ? _boundKeeper.SilenceRemaining : 0f;
        int cd = Mathf.CeilToInt(Mathf.Max(a.skill.CooldownRemainingFor(a.action), silence));
        bool canNow = a.skill.CanUseAction(a.action);
        int key = cd * 2 + (canNow ? 1 : 0);
        if (key == a.shown) return;

        a.shown = key;
        button.interactable = cd <= 0 && canNow;
        if (a.text != null) a.text.text = cd > 0 ? a.label + "\n" + cd : a.label;
    }

    void UpdateJoystick(int i)
    {
        Joystick joystick = skillJoysticks[i];
        JoystickBinding j = _joysticks[i];
        if (joystick == null) return;

        bool show = j.skill != null && j.skill.JoystickActive;
        if (joystick.gameObject.activeSelf != show) joystick.gameObject.SetActive(show);
        if (!show) { j.dragging = false; return; }

        // Bekleme süresi: sayaç yazısı + joystick soluk
        float silence = _boundKeeper != null ? _boundKeeper.SilenceRemaining : 0f;
        int cd = Mathf.CeilToInt(Mathf.Max(j.skill.CooldownRemaining, silence));
        if (cd != j.shownCooldown)
        {
            j.shownCooldown = cd;
            if (j.cooldownText != null) j.cooldownText.text = cd > 0 ? cd.ToString() : "";
            if (j.group != null) j.group.alpha = cd > 0 ? 0.45f : 1f;
        }

        Vector2 d = joystick.Direction;
        if (d.sqrMagnitude > 0.0001f)
        {
            j.lastDir = d;     // çekiliyor
            j.dragging = true;
        }
        else if (j.dragging)  // bırakıldı
        {
            j.dragging = false;
            bool forward = j.skill.JoystickAnyDirection || j.lastDir.y > 0.05f; // çoğunda arka yarım sayılmaz
            if (forward && j.lastDir.magnitude >= minJoystickPull && j.skill.CanUseJoystick && cd <= 0)
                j.skill.OnJoystickReleased(j.lastDir.normalized);
            j.lastDir = Vector2.zero;
        }
    }

    void HideControls()
    {
        if (actionButtons != null)
            foreach (Button b in actionButtons)
                if (b != null && b.gameObject.activeSelf) b.gameObject.SetActive(false);
        if (skillJoysticks != null)
            foreach (Joystick j in skillJoysticks)
                if (j != null && j.gameObject.activeSelf) j.gameObject.SetActive(false);
        if (passiveStatusTexts != null)
            foreach (TMP_Text t in passiveStatusTexts)
                if (t != null && t.enabled) t.enabled = false;
    }

    void ApplyButtonState(ShopRow row, int i, int state)
    {
        Button b = row.levelButtons[i];
        ColorBlock colors = row.baseColorBlocks[i];

        if (state == StateOwned)
        {
            colors.disabledColor = Color.white; // yeşil soluk görünmesin
            b.colors = colors;
            if (b.targetGraphic != null) b.targetGraphic.color = ownedColor;
            b.interactable = false;
        }
        else
        {
            b.colors = colors;
            if (b.targetGraphic != null) b.targetGraphic.color = row.baseColors[i];
            b.interactable = state == StateBuyable;
        }
    }
}
