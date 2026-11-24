using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Bağlantı ve eşleşme. "Oyna"ya basınca açık bir oda arar; yoksa kendisi host olur (AutoHostOrClient).
/// Girdiyi toplar ve Fusion'a verir. Oyun bitince/koparsa sahneyi yeniden yükleyip menüye döner.
/// </summary>
public class NetworkBootstrap : MonoBehaviour, INetworkRunnerCallbacks
{
    public const int MaxPlayers = 6;

    public static NetworkBootstrap Instance { get; private set; }
    public static string LastMessage = ""; // sahne yeniden yüklense de kalır

    /// Bu bilgisayarın kalıcı kimliği. Host değişince herkes kendi keeper'ını bununla geri alır.
    public static readonly int LocalId = CreateLocalId();
    /// Host değişimi sürerken true (MatchManager durumu sıfırlamasın diye).
    public static bool IsResuming { get; private set; }
    /// Bu bilgisayardaki oyuncunun slotu (-1 = yok).
    public static int LocalSlot = -1;
    /// Bu bilgisayardaki son hareket girdisi (klavye + joystick). Nişan çizgisi için.
    public static Vector2 LocalMove;

    [SerializeField] NetworkPrefabRef matchManagerPrefab;

    public NetworkRunner Runner { get; private set; }
    public bool IsConnecting { get; private set; }

    bool _reloadRequested;
    bool _throwQueued; // Space basışı bir sonraki tick'e kadar saklanır, asla kaçmaz
    Vector2 _throwAim; // şut joystick'iyle atıldıysa yön

    /// Şut joystick'i bırakılınca çağırır.
    public void QueueThrow(Vector2 localAim)
    {
        _throwQueued = true;
        _throwAim = localAim;
    }
    bool _leaving;
    int _abilityQueue; // basılan yetenek butonları (bit maskesi), bir sonraki tick'te gönderilir

    /// Dükkandaki "kullan" butonu çağırır.
    public void QueueAbility(SkillId id, int action = 0)
    {
        KeeperController k = KeeperController.Local;
        int deckIndex = k != null ? k.DeckIndexOf(id) : -1;
        if (deckIndex < 0) return;
        _abilityQueue |= 1 << (deckIndex * SkillBase.MaxActions + action);
    }

    void Awake()
    {
        Instance = this;
        LocalSlot = -1;
        KeeperController.All.Clear(); // sahne yeniden yüklenince eski kayıtlar kalmasın
        Ball.All.Clear();
        Disc.All.Clear();
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) _throwQueued = true;
#else
        if (Input.GetKeyDown(KeyCode.Space)) _throwQueued = true;
#endif

        if (_reloadRequested)
        {
            _reloadRequested = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public async void Play()
    {
        if (Runner != null) return;

        LastMessage = "";
        IsConnecting = true;

        Runner = CreateRunner(out NetworkSceneManagerDefault sceneManager);

        var result = await Runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient, // oda varsa katıl, yoksa host ol
            PlayerCount = MaxPlayers,
            SceneManager = sceneManager,
            ConnectionToken = BitConverter.GetBytes(LocalId),
        });

        IsConnecting = false;

        if (!result.Ok)
        {
            LastMessage = "Bağlantı başarısız: " + result.ShutdownReason;
            _reloadRequested = true;
        }
    }

    public void Leave()
    {
        if (_leaving) return;
        _leaving = true;
        if (Runner != null) Runner.Shutdown();
        else _reloadRequested = true;
    }

    NetworkRunner CreateRunner(out NetworkSceneManagerDefault sceneManager)
    {
        var go = new GameObject("NetworkRunner");
        NetworkRunner runner = go.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);
        sceneManager = go.AddComponent<NetworkSceneManagerDefault>();
        return runner;
    }

    static int CreateLocalId()
    {
        int id = Guid.NewGuid().GetHashCode();
        return id == 0 ? 1 : id;
    }

    /// Host tarafında: bir oyuncunun kalıcı kimliği.
    public static int GetPlayerId(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer) return LocalId;
        byte[] token = runner.GetPlayerConnectionToken(player);
        if (token != null && token.Length >= 4) return BitConverter.ToInt32(token, 0);
        return -(player.RawEncoded + 1); // token yoksa geçici kimlik
    }

    // ---------------- Host Migration ----------------

    /// Host oyundan çıktı: Photon yeni host seçti. Herkes yeni bir runner ile devam eder.
    public async void OnHostMigration(NetworkRunner oldRunner, HostMigrationToken hostMigrationToken)
    {
        IsResuming = true;
        Runner = null;

        await oldRunner.Shutdown(shutdownReason: ShutdownReason.HostMigration);
        DestroyStaleNetworkObjects(); // eski runner'ın objeleri (top, keeper) sahnede kalmasın
        KeeperController.All.Clear();
        Ball.All.Clear();
        Disc.All.Clear();

        Runner = CreateRunner(out NetworkSceneManagerDefault sceneManager);
        var result = await Runner.StartGame(new StartGameArgs
        {
            HostMigrationToken = hostMigrationToken,
            HostMigrationResume = ResumeFromSnapshot,
            ConnectionToken = BitConverter.GetBytes(LocalId),
            SceneManager = sceneManager,
        });

        IsResuming = false;

        if (!result.Ok)
        {
            LastMessage = "Host değişimi başarısız: " + result.ShutdownReason;
            _reloadRequested = true;
        }
    }

    /// Host değişiminde eski runner kapanınca objeleri sahnede kalabiliyor; yenileri gelmeden hepsini sil.
    static void DestroyStaleNetworkObjects()
    {
        foreach (NetworkObject no in FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Destroy(no.gameObject);
    }

    /// Sadece yeni host'ta çalışır: eski host'un son kaydındaki objeleri aynı durumla yeniden oluşturur.
    void ResumeFromSnapshot(NetworkRunner runner)
    {
        foreach (NetworkObject resumeObj in runner.GetResumeSnapshotNetworkObjects())
        {
            runner.Spawn(resumeObj, onBeforeSpawned: (r, newObj) => newObj.CopyStateFrom(resumeObj));
        }
    }

    // ---------------- Fusion callbacks ----------------

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null)
        {
            NetworkObject obj = runner.Spawn(matchManagerPrefab);
            mm = obj.GetComponent<MatchManager>();
        }
        mm.ServerAddPlayer(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && MatchManager.Instance != null)
            MatchManager.Instance.ServerRemovePlayer(player);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        Vector2 move = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
        }
#else
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1;
#endif

        if (MobileControls.Instance != null) move += MobileControls.Instance.Move; // mobil joystick
        data.Move = Vector2.ClampMagnitude(move, 1f);
        LocalMove = data.Move;
        data.Buttons.Set(InputButton.Throw, _throwQueued);
        data.ThrowAim = _throwAim;
        _throwQueued = false;
        _throwAim = Vector2.zero;
        for (int i = 0; i < PlayerDeck.Size * SkillBase.MaxActions; i++) // destedeki 6 yetenek × 2 buton
            if ((_abilityQueue & (1 << i)) != 0) data.Buttons.Set((int)InputButton.Ability0 + i, true);
        _abilityQueue = 0;
        input.Set(data);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (shutdownReason == ShutdownReason.HostMigration) return; // oyun devam ediyor, yeni runner kuruluyor

        if (string.IsNullOrEmpty(LastMessage) && shutdownReason != ShutdownReason.Ok)
            LastMessage = "Oyun sona erdi: " + shutdownReason;
        _reloadRequested = true;
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        // Host çıktıysa Fusion önce OnHostMigration'ı dener; değilse runner kendisi kapanır (OnShutdown).
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        LastMessage = "Bağlanılamadı: " + reason;
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { } // Fusion 2.1 imzası
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
