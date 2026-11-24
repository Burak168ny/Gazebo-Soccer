using Fusion.Sockets;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;


public class Spawner : MonoBehaviour, INetworkRunnerCallbacks
{
    // Kameralar için engeller
    public GameObject[] cameraObstacles;  // Kameraların önündeki engeller için dizi
    public Collider[] goalColliders; // Kalelerin Collider referansları


    public JoystickController joystickController; // Joystick referansını alalım
    public ShieldJoystickController shieldJoystickController; //
    public FreedomJoystickController freedomJoystickController; //
    public Joystick freedomJoystick;
    public Joystick movementJoystick; // Hareket joystick referansı
    public Joystick shieldJoystick; // Kalkan joystick referansı
    public Joystick shootJoystick; // Kalkan joystick referansı
    public Button jumpButton; // UI buton referansı
    public Button visibilityButton; // Görünmezlik için UI butonu
    public Button benderXButton;
    public Button benderYButton;
    public Button benderZButton;
    public Button abilityToggleButton;
    private bool toggleAbility = false;




    public int i = 0;
    public GameObject cameraPrefab; // Kamera prefab referansı
private Dictionary<PlayerRef, GameObject> playerCameras = new Dictionary<PlayerRef, GameObject>(); // Oyuncu kameralarını saklamak için bir dictionary
public Dictionary<PlayerRef, Vector3> playerStartPositions = new Dictionary<PlayerRef, Vector3>();

private Dictionary<PlayerRef, int> playerScores = new Dictionary<PlayerRef, int>();




private Vector3[] cameraPositions = new Vector3[]
{
    new Vector3(10.4f, 27, -26),
    new Vector3(-20.2f, 27, -8.5f),
    new Vector3(-20.5f, 27, 28),
    new Vector3(10f, 27, 45.5f),
    new Vector3(40.5f, 27, 28),
    new Vector3(40.5f, 27, -8.4f)
};

private Vector3[] cameraRotations = new Vector3[]
{
    new Vector3(38, 0, 0),
    new Vector3(38, 60, 0),
    new Vector3(38, 120, 0),
    new Vector3(38, 180, 0),
    new Vector3(38, 240, 0),
    new Vector3(38, 300, 0)
};

    private Vector3 previousFreedomJoystickInput = Vector3.zero;

    private NetworkRunner networkRunner;
    private bool isJumping = false;
    private bool toggleVisibility = false;
    private bool isShooting = false;
    private float shootAngle;
    private bool canShoot = false; // Şut atmak için bayrak
    private bool isScaleXUpHeld;
    private bool isScaleYUpHeld;
    private bool isScaleZUpHeld;

    [SerializeField] private NetworkPrefabRef[] networkPrefabRefs; // Prefab referanslarını saklamak için bir dizi

    private Dictionary<PlayerRef, NetworkObject> spawnCharacter = new Dictionary<PlayerRef, NetworkObject>();

    private Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(10, 8, -10),
        new Vector3(9.5f, 8, 30),
        new Vector3(27, 8, 20),
        new Vector3(27, 8, 0),
        new Vector3(-7, 8, 20),
        new Vector3(-7.5f, 8, -0.5f)
    };

    private Vector3[] spawnRotations = new Vector3[]
    {
        new Vector3(0, 0, 0),
        new Vector3(0, 180, 0),
        new Vector3(0, -120, 0),
        new Vector3(0, -60, 0),
        new Vector3(0, 120, 0),
        new Vector3(0, 60, 0)
    };

    public void OnJumpButtonPressed() // Yeni tanımladığımız zıplama butonu fonksiyonu
    {
        isJumping = true;
    }

    public void OnVisibilityButtonPressed() // Görünmezlik butonu işlevi
    {
        toggleVisibility = true;
    }

    public void OnAbilityTogglePressed()
    {
        toggleAbility = !toggleAbility;
    }

    private void Start()
    {
        // Butonlar için basılı tutulma olaylarını ayarla
        AddEvent(benderXButton, () => isScaleXUpHeld = true, () => isScaleXUpHeld = false);
        AddEvent(benderYButton, () => isScaleYUpHeld = true, () => isScaleYUpHeld = false);
        AddEvent(benderZButton, () => isScaleZUpHeld = true, () => isScaleZUpHeld = false);



        if (jumpButton != null)
        {
            jumpButton.onClick.AddListener(OnJumpButtonPressed);
        }

        // if (visibilityButton != null)
        // {
        //     visibilityButton.onClick.AddListener(OnVisibilityButtonPressed);
        // }

        // SilentBall ball = FindObjectOfType<SilentBall>();
        // if (ball != null)
        // {
        //     ball.SetInvisibility(false); // Oyunun başında topu görünür yap
        // }
    }
        private void AddEvent(Button button, System.Action onPointerDown, System.Action onPointerUp)
    {
        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

        var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener((_) => onPointerDown());
        trigger.triggers.Add(pointerDown);

        var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener((_) => onPointerUp());
        trigger.triggers.Add(pointerUp);
    }

    void Update()
    {
        Vector3 shootJoystickInput = joystickController.GetShootJoystickInput();  // Şut joystick'in pozisyonunu kontrol et
        Vector3 shieldJoystickInput = shieldJoystickController.GetShieldJoystickInput();  // Kalkan joystick'in pozisyonunu kontrol et
        // Vector3 freedomJoystickInput = freedomJoystickController.GetFreedomJoystickInput(); //





        if (shootJoystickInput != Vector3.zero)
        {
            CalculateShootDirection();  // Şut yönünü hesapla
            canShoot = true;
            isShooting = false;
        }
        else if (canShoot && !isShooting)
        {
            isShooting = true;
            canShoot = false;  // Şut işlemi tamamlandı
        }

            if (shieldJoystickInput != Vector3.zero)
    {
        ShieldControllerNet shieldController = FindObjectOfType<ShieldControllerNet>();
        if (shieldController != null)
        {
            shieldController.ThrowShield(shieldJoystickInput); // Kalkanı fırlat
        }
    }
        
    }


    private void CalculateShootDirection()
    {
        float joystickX = joystickController.GetShootJoystickHorizontalInput(); // Joystick X ekseni değerini al (-1 ile 1 arasında bir değer döner)

        shootAngle = Mathf.Lerp(190f, -10f, (joystickX + 1) / 2f); // Joystick değerini 0 ile 180 derece arasında bir açıya çevir ve ortalama 90 dereceye göre ayarla
    }

    async void GameStart(GameMode mode)
    {
        networkRunner = gameObject.AddComponent<NetworkRunner>();
        networkRunner.ProvideInput = true;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }
        await networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoomNestedMango",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void ShutdownHost()
    {
        if (networkRunner != null)
        {
            // NetworkRunner'ı düzgünce kapat
            networkRunner.Shutdown();
        }
    }

private void OnGUI()
{
    if (networkRunner == null)
    {
        // GUIStyle oluşturuldu
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 70; // Yazı boyutunu ayarlayın

        if (GUI.Button(new Rect(0, 0, 500, 100), "Host", buttonStyle))
        {
            GameStart(GameMode.Host);
        }
        if (GUI.Button(new Rect(0, 110, 500, 100), "Join", buttonStyle))
        {
            GameStart(GameMode.Client);
        }
    }
}




    // [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    // public void RpcSetButtonAndJoystickVisibility(int characterIndex)
    // {
    //     SetButtonAndJoystickVisibility(characterIndex);
    // }





public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
        // Host tarafında karakter spawn etme
        if (runner.IsServer)
        {
            int characterIndex = GetCharacterIndexForPlayer(player);
            // Her oyuncu için görünürlük ayarını güncellemek için RPC çağrısı yapıyoruz
            // RpcSetButtonAndJoystickVisibility(characterIndex);


            if (characterIndex >= 0)
            {
                Vector3 spawnPosition = spawnPositions[i % spawnPositions.Length];
                playerStartPositions[player] = spawnPosition;
                Quaternion spawnRotation = Quaternion.Euler(spawnRotations[i % spawnRotations.Length]);

                // İlgili prefab'ı seç
                NetworkPrefabRef selectedPrefab = networkPrefabRefs[characterIndex];
                NetworkObject networkObject = runner.Spawn(selectedPrefab, spawnPosition, spawnRotation, player);
                
                spawnCharacter.Add(player, networkObject);

                i++;
            }
        }

    // Kamera konumu ve rotasyonu sadece host için 0. pozisyonda olsun
    Vector3 cameraSpawnPosition;
    Quaternion cameraSpawnRotation;

    if (runner.IsServer && player == runner.LocalPlayer)
    {
        // Host kamerası için 0. pozisyon ve rotasyon
        cameraSpawnPosition = cameraPositions[0];
        cameraSpawnRotation = Quaternion.Euler(cameraRotations[0]);
    }
    else
    {
        // Diğer oyuncular için farklı pozisyon ve rotasyon
        int playerIndex = player.RawEncoded % cameraPositions.Length;
        cameraSpawnPosition = cameraPositions[playerIndex];
        cameraSpawnRotation = Quaternion.Euler(cameraRotations[playerIndex]);
    }

    // Kamera spawn etme
    GameObject playerCamera = Instantiate(cameraPrefab, cameraSpawnPosition, cameraSpawnRotation);
    playerCameras.Add(player, playerCamera);

    // Sadece local player için kamerayı aktif et
    if (player == runner.LocalPlayer)
    {
        playerCamera.SetActive(true);  // Local player'ın kamerasını aktif et

        // Burada oyuncunun sıraya göre objeyi görünmez yapıyoruz
        int localPlayerIndex = (player.RawEncoded -2) % cameraObstacles.Length;
        // Ana obje için MeshRenderer alıyoruz
        MeshRenderer[] meshRenderers = cameraObstacles[localPlayerIndex].GetComponentsInChildren<MeshRenderer>();

        // Tüm MeshRenderer bileşenlerini devre dışı bırak
        foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.enabled = false; // Obje ve alt objelerdeki MeshRenderer'ları görünmez yap
            }
        }
    else
    {
        playerCamera.SetActive(false);  // Diğer oyuncuların kameralarını pasif yap
    }
}





    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (spawnCharacter.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            spawnCharacter.Remove(player);
        }
    }

// [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
// public void RpcApplyPushToAll(Vector3 pushDirection)
// {
//     if (pushDirection == Vector3.zero)
//     {
//         Debug.LogWarning("Joystick sıfır durumda, kuvvet uygulanmayacak.");
//         return;
//     }

//     foreach (var character in spawnCharacter.Values)
//     {
//         var pushController = character.GetComponent<FreedomPushController>();
//         if (pushController != null && character.HasStateAuthority ) // Sunucuda kontrol et
//         {
//             Debug.Log("Kuvvet sunucuda uygulanıyor.");
//             pushController.ApplyPush(pushDirection); // Kuvvet uygulama
//         }
//     }
// } 



    public void MenuButton()
    {
        if (networkRunner != null)
        {
            networkRunner.Shutdown(); // NetworkRunner'ı sonlandır
        }
        SceneManager.LoadScene("MainMenu");
    }


    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        
        int characterIndex = GetCharacterIndexForPlayer(runner.LocalPlayer);
        // SetButtonAndJoystickVisibility(characterIndex);

        // Joystick inputlarını al
        Vector3 joystickInput = joystickController.GetMovementInput();
        Vector3 shieldInput = shieldJoystickController.GetShieldJoystickInput();
        Vector3 freedomInput = freedomJoystickController.GetFreedomJoystickInput();

        // Pozisyon bilgisini güncelle
        data.resetPosition = transform.position; // Karakterin mevcut pozisyonunu gönder
        
        //Vector3 shieldInput = shieldJoystickController.GetMovementInput();

        // Inputları doldur
        //data.ReadInput(joystickInput);

        // Inputları doldur
        //data.ReadInput(shieldInput);

            // Skor güncellemesini tetikleme

        if (isScaleXUpHeld)
{
    data.buttons.Set(NetworkInputData.SCALE_X_UP, true);
}
if (isScaleYUpHeld)
{
    data.buttons.Set(NetworkInputData.SCALE_Y_UP, true);
}
if (isScaleZUpHeld)
{
    data.buttons.Set(NetworkInputData.SCALE_Z_UP, true);
}


        // Girdileri doldur
        data.ReadInput(joystickInput, shieldInput, freedomInput);  // Hem hareket hem de kalkan girdilerini gönder

        if (isJumping)  
        {
            data.buttons.Set(NetworkInputData.JUMP, true);
            isJumping = false;  // Zıplama inputunu sıfırla
        }


        // Şut çekme kontrolü (butondan gelen)
        if (isShooting)
        {
            data.shootDirection = shootAngle; // Şut yönünü input'a ekle
            data.buttons.Set(NetworkInputData.SHOOT, true);
            Debug.Log("OnInput: SHOOT girdisi gönderiliyor, açı: " + shootAngle);
            isShooting = false; // Şut inputunu sıfırla
        }
            
        // Görünmezlik kontrolü (toggleVisibility yalnızca bir defa true yapılıyor ve sıfırlanıyor)
        if (toggleVisibility)
        {
            data.buttons.Set(NetworkInputData.TOGGLE_VISIBILITY, true);
            toggleVisibility = false; // Görünmezlik inputunu sıfırla
        }
        
        if (freedomInput != Vector3.zero) // FreedomInput varsa tetikle
        {
            data.freedomDirection = freedomInput;
            data.buttons.Set(NetworkInputData.FREEDOM_PUSH, true);
        }
        
        if (toggleAbility)
        {
            data.buttons.Set(NetworkInputData.TOGGLE_ABILITY, true);
            toggleAbility = false;
        }

        
        // Önceki joystick girdisini güncelle
        previousFreedomJoystickInput = freedomInput;

        // Kalkan fırlatma inputu
        if (shieldInput != Vector3.zero)
        {
            data.buttons.Set(NetworkInputData.SHIELD_THROW, true);  // Kalkan fırlatma bayrağı
            data.shieldDirection = shieldInput;  // Kalkan yönünü de inputa ekle
        }

        // Butonlar basılı tutuluyorsa ilgili işlemi gerçekleştir
        if (isScaleXUpHeld)
        {
            data.buttons.Set(NetworkInputData.SCALE_X_UP, true);
        }

        if (isScaleYUpHeld)
        {
            data.buttons.Set(NetworkInputData.SCALE_Y_UP, true);
        }

        if (isScaleZUpHeld)
        {
            data.buttons.Set(NetworkInputData.SCALE_Z_UP, true);
        }

        // Boyut büyütme kontrolleri
        if (Input.GetKey(KeyCode.Keypad6)) data.buttons.Set(NetworkInputData.SCALE_X_UP, true);
        if (Input.GetKey(KeyCode.Keypad4)) data.buttons.Set(NetworkInputData.SCALE_Y_UP, true);
        if (Input.GetKey(KeyCode.Keypad5)) data.buttons.Set(NetworkInputData.SCALE_Z_UP, true);
        // Şut çekme kontrolü
        if (Input.GetKey(KeyCode.R))
        {
            data.buttons.Set(NetworkInputData.SHOOT, true);
        }

        // Handle visibility toggle input
        // if (Input.GetKeyDown(KeyCode.I))
        // {
        //     data.buttons.Set(NetworkInputData.TOGGLE_VISIBILITY, true);
        // }


        input.Set(data);
    }



    // public void OnInput(NetworkRunner runner, NetworkInput input)
    // {
    //     var data = new NetworkInputData();

    //     // Yön girdilerini oku
    //     Vector3 direction = Vector3.zero;
    //     if (Input.GetKey(KeyCode.W)) direction += Vector3.forward;
    //     if (Input.GetKey(KeyCode.S)) direction += Vector3.back;
    //     if (Input.GetKey(KeyCode.A)) direction += Vector3.left;
    //     if (Input.GetKey(KeyCode.D)) direction += Vector3.right;

    //     data.direction = direction;

    //     // Zıplama isteğini kontrol et
    //     if (Input.GetKey(KeyCode.Space))
    //     {
    //         data.buttons.Set(NetworkInputData.JUMP, true);
    //     }

    //     // Sunucuya girdileri gönder
    //     input.Set(data);
    // }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // Girdi eksik olduğunda yapılacak işlemler
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        // Oyun kapatıldığında yapılacak işlemler
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        // Sunucuya bağlandığında yapılacak işlemler
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        // Sunucudan bağlantı kesildiğinde yapılacak işlemler
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // Bağlantı isteği alındığında yapılacak işlemler
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        // Bağlantı başarısız olduğunda yapılacak işlemler
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        // Simülasyon mesajı alındığında yapılacak işlemler
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // Oturum listesi güncellendiğinde yapılacak işlemler
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        // Özel kimlik doğrulama yanıtı alındığında yapılacak işlemler
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        // Sunucu göçü gerçekleştiğinde yapılacak işlemler
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        // Güvenilir veri alındığında yapılacak işlemler
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // Sahne yüklendiğinde yapılacak işlemler
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        // Sahne yüklenmeye başladığında yapılacak işlemler
    }

    // Eksik yöntemlerin eklenmesi
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player)
    {
        // Obje AOI'den çıktığında yapılacak işlemler
        // Bu yöntem `INetworkRunnerCallbacks` arayüzünün bir parçasıdır ve uygulanmalıdır. !
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player)
    {
        // Obje AOI'ye girdiğinde yapılacak işlemler
        // Bu yöntem `INetworkRunnerCallbacks` arayüzünün bir parçasıdır ve uygulanmalıdır. !
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        // Sunucudan bağlantı kesildiğinde yapılacak işlemler
        // Bu yöntem `INetworkRunnerCallbacks` arayüzünün bir parçasıdır ve uygulanmalıdır. !
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        // Güvenilir veri alındığında yapılacak işlemler
        // Bu yöntem `INetworkRunnerCallbacks` arayüzünün bir parçasıdır ve uygulanmalıdır. !
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        // Güvenilir veri ilerlemesi alındığında yapılacak işlemler
        // Bu yöntem `INetworkRunnerCallbacks` arayüzünün bir parçasıdır ve uygulanmalıdır. !
    }

    // Yeni eklenen metod
    private int GetCharacterIndexForPlayer(PlayerRef player)
    {
        // Varsayılan olarak 0 indeksini kullan
        int characterIndex = 4;

        // Eğer bu local oyuncuysa, PlayerPrefs'ten seçilen karakteri al
        if (player == networkRunner.LocalPlayer)
        {
            characterIndex = UnityEngine.Random.Range(0, 5);
        }
        else
        {
            // Diğer oyuncular için karakter seçimini ağ üzerinden almamız gerekiyor
            // Bu kısmı implement etmek için ek bir ağ senkronizasyonu gerekebilir
            // Şimdilik rastgele bir karakter seç
            characterIndex = UnityEngine.Random.Range(0, 5);
        }
        return characterIndex;
    }

    // private void SetButtonAndJoystickVisibility(int characterIndex)
    // {
    //     // Zıplama ve hareket joysticki her karakter için aktif
    //     movementJoystick.gameObject.SetActive(false);
    //     shootJoystick.gameObject.SetActive(false);
    //     shieldJoystick.gameObject.SetActive(false);
    //     freedomJoystick.gameObject.SetActive(false);
    //     movementJoystick.gameObject.SetActive(false);
    //     shootJoystick.gameObject.SetActive(false);
        
    //     jumpButton.gameObject.SetActive(false);
    //     visibilityButton.gameObject.SetActive(false);
    //     jumpButton.gameObject.SetActive(false);
    //     benderXButton.gameObject.SetActive(false);
    //     benderYButton.gameObject.SetActive(false);
    //     benderZButton.gameObject.SetActive(false);
    //     //abilityToggleButton.gameObject.SetActive(false);
        

    //     // Diğer buton ve joysticklerin durumu karakter indeksine göre ayarlanır
    //     if (characterIndex == 0)
    //     {
    //         shieldJoystick.gameObject.SetActive(true);
    //         movementJoystick.gameObject.SetActive(true);
    //         shootJoystick.gameObject.SetActive(true);
    //         jumpButton.gameObject.SetActive(true);
    //     }
    //     else if (characterIndex == 1)
    //     {
    //         movementJoystick.gameObject.SetActive(true);
    //         benderXButton.gameObject.SetActive(true);
    //         benderYButton.gameObject.SetActive(true);
    //         benderZButton.gameObject.SetActive(true);
    //     }
    //     else if (characterIndex == 2)
    //     {
    //         visibilityButton.gameObject.SetActive(true);
    //         movementJoystick.gameObject.SetActive(true);
    //         shootJoystick.gameObject.SetActive(true);
    //         jumpButton.gameObject.SetActive(true);
    //     }
    //     else if (characterIndex == 3)
    //     {
    //         abilityToggleButton.gameObject.SetActive(true);
    //         movementJoystick.gameObject.SetActive(true);
    //         shootJoystick.gameObject.SetActive(true);
    //         jumpButton.gameObject.SetActive(true);
    //     }
    //     else if (characterIndex == 4)
    //     {
    //         freedomJoystick.gameObject.SetActive(true);
    //         movementJoystick.gameObject.SetActive(true);
    //         shootJoystick.gameObject.SetActive(true);
    //         jumpButton.gameObject.SetActive(true);
    //     }
    //     else if (characterIndex == 5)
    //     {
    //         movementJoystick.gameObject.SetActive(true);
    //         shootJoystick.gameObject.SetActive(true);
    //         jumpButton.gameObject.SetActive(true);
    //     }
    // }

}
