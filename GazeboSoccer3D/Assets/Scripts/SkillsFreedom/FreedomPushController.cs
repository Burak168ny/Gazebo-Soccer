// FreedomPushController.cs
using Fusion;
using UnityEngine;
using System.Collections; // Coroutine'ler için gerekli


public class FreedomPushController : NetworkBehaviour
{
    public float speed = 0.3f;
    public float jumpForce = 10f;

    public float pushForce = 5000f;
    public float upwardForce = 1000f;

    private Rigidbody _rb;
    private bool _isGrounded;
    private bool _canDoubleJump;
    private Vector3 previousFreedomDirection = Vector3.zero;
    private bool isPushing = false; // Coroutine'in çalışıp çalışmadığını kontrol etmek için bayrak

    // Mevcut alanlar
    public float growthRate = 0.05f; // Zamana bağlı büyüme oranı
    private bool isGrowing = false; // Büyüme aktif mi kontrolü
    private bool stopGrowthOnCollision = false;
    private bool isThrown = false; // Fırlatma durumu
    private bool hasCollided = false; // Çarpma durumu

    
    [Networked] private TickTimer delay { get; set; }
    [Networked] private Vector3 CurrentScale { get; set; } = Vector3.one;
    [Networked] private Vector3 defaultScale { get; set; } = Vector3.one;
    [Networked] private Vector3 networkedPosition { get; set; }
    [Networked] private Vector3 startPosition { get; set; }


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();    
    }

private void Start()
{
    if (Object.HasStateAuthority) // Sadece sunucuda ayarla
    {
        defaultScale = transform.localScale;

        // Spawner'dan başlangıç pozisyonunu alın
        Spawner spawner = FindObjectOfType<Spawner>();
        if (spawner != null && spawner.playerStartPositions.TryGetValue(Object.InputAuthority, out Vector3 startPos))
        {
            startPosition = startPos;
        }
    }
}



    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            HandleMovement(data);
            HandleJump(data);
            HandleFreedomPush(data);
            HandleScaling(data); // Yeni eklenen metot
        }
    }

    private void HandleMovement(NetworkInputData data)
    {
        Vector3 moveInput = new Vector3(data.direction.x, 0, data.direction.z);
        Vector3 localForward = transform.forward;
        Vector3 localRight = transform.right;
        Vector3 movement = (localForward * moveInput.z) + (localRight * moveInput.x);

        if (movement.magnitude > 0)
        {
            movement.Normalize();
        }

        _rb.AddForce(movement * speed, ForceMode.VelocityChange);
    }

    private void HandleJump(NetworkInputData data)
    {
        if (data.buttons.IsSet(NetworkInputData.JUMP))
        {
            if (_isGrounded)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpForce, _rb.linearVelocity.z);
                _canDoubleJump = true;
            }
            else if (_canDoubleJump)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpForce, _rb.linearVelocity.z);
                _canDoubleJump = false;
            }
        }
    }

    private void HandleFreedomPush(NetworkInputData data)
    {
        Vector3 freedomDirection = data.freedomDirection;

        // Eğer joystick bırakıldıysa ve önceki giriş sıfır değilse
        if (freedomDirection == Vector3.zero && previousFreedomDirection != Vector3.zero && !isPushing)
        {
            // if (Object.HasStateAuthority) // Sadece sunucuda pozisyonu güncelle
            // {
            //     networkedPosition = data.position; // Pozisyonu NetworkInputData'dan al
            // }
            StartCoroutine(DelayedPush(previousFreedomDirection)); // Gecikmeli kuvvet uygula
        }

        // Önceki joystick yönünü güncelle
        previousFreedomDirection = freedomDirection;
    }

private void HandleScaling(NetworkInputData data)
{
    if (Object.HasInputAuthority) // Sadece istemci tarafında input işle
    {
        if (data.buttons.IsSet(NetworkInputData.SCALE_X_UP) ||
            data.buttons.IsSet(NetworkInputData.SCALE_Y_UP) ||
            data.buttons.IsSet(NetworkInputData.SCALE_Z_UP))
        {
            StartCoroutine(GrowOverTime());
        }
    }
}

private void UpdateCollider(Vector3 newScale)
{
    if (Object.HasStateAuthority)
    {
        Collider collider = GetComponent<Collider>();
        if (collider is BoxCollider boxCollider)
        {
            boxCollider.size = newScale;
        }
        else if (collider is SphereCollider sphereCollider)
        {
            sphereCollider.radius = Mathf.Max(newScale.x, newScale.y, newScale.z) / 2f;
        }
        else if (collider is CapsuleCollider capsuleCollider)
        {
            capsuleCollider.height = newScale.y;
            capsuleCollider.radius = Mathf.Max(newScale.x, newScale.z) / 2f;
        }
    }
}

private void ResetScale()
{
    if (Object.HasStateAuthority) // Sadece sunucuda çalıştır
    {
        // Varsayılan boyutu geri yükle
        CurrentScale = defaultScale;
        transform.localScale = CurrentScale;

        // Collider'ı güncelle
        UpdateCollider(CurrentScale);

        // Rigidbody hareketini durdur ve pozisyonu sıfırla
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero; // Hızı sıfırla
            _rb.angularVelocity = Vector3.zero; // Açısal hızı sıfırla
            _rb.position = startPosition; // Başlangıç pozisyonuna ayarla
        }
        else
        {
            transform.position = startPosition; // Rigidbody yoksa direkt pozisyonu değiştir
        }
                growthRate = 40f; // Varsayılan değere sıfırla

    }
    SetLayerRecursively(gameObject, LayerMask.NameToLayer("Player"));
}




private IEnumerator GrowOverTime()
{
    isGrowing = true;

    while (!stopGrowthOnCollision)
    {
        Vector3 newScale = CurrentScale + Vector3.one * growthRate * Time.deltaTime;

        if (Object.HasStateAuthority) // Sadece host tarafında büyümeyi kontrol et
        {
            CurrentScale = newScale; // Büyümeyi güncelle
        }

        transform.localScale = CurrentScale; // İstemci, sunucudan gelen güncellemeyi uygular

        // Eğer CurrentScale herhangi bir eksende 3'ü geçerse büyüme oranını azalt
        if ((CurrentScale.x > 3.5f && CurrentScale.x < 3.9f) ||
            (CurrentScale.y > 3.5f && CurrentScale.y < 3.9f) ||
            (CurrentScale.z > 3.5f && CurrentScale.z < 3.9f))
        {
            growthRate *= 1.2f; // Büyüme oranını azalt
        }
        else
        {
            growthRate *= 1.08f; // Büyüme oranını artır
        }


        yield return null;
    }

    isGrowing = false;
}


private void Update()
{
    // Sunucu tarafından belirlenen büyüklüğü istemcide uygula
    if (!Object.HasStateAuthority)
    {
        transform.localScale = CurrentScale;
    }
}


    // Kuvvet uygulandıktan sonra büyümeyi başlat
    private IEnumerator DelayedPush(Vector3 direction)
    {
        isPushing = true;

        SetLayerRecursively(gameObject, LayerMask.NameToLayer("Freedom"));

        yield return new WaitForSeconds(1f);

        Vector3 localDirection = transform.TransformDirection(direction);

        Debug.Log("Joystick bırakıldı, gecikmeli kuvvet uygulanıyor: " + localDirection);

        _rb.linearVelocity = Vector3.zero;

        _rb.AddForce(localDirection.normalized * -pushForce, ForceMode.Impulse);
        _rb.AddForce(Vector3.up * upwardForce, ForceMode.Impulse);



        // Kuvvet uygulandıktan sonra büyümeyi 0.1 saniye gecikmeyle başlat
        if (!isGrowing)
        {
            yield return new WaitForSeconds(0.2f); // 0.1 saniye bekle
            StartCoroutine(GrowOverTime());
        }
            // Kuvvet uygulandıktan sonra reset işlemini başlat
    
        yield return new WaitForSeconds(2f); // 1 saniye gecikme
        ResetScale(); // Reset işlemini uygula
        
    

        isPushing = false;
    }


    // Ana nesne ve alt nesneler için katmanı değiştiren fonksiyon
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer; // Ana nesnenin katmanını değiştir

        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, newLayer); // Alt nesneler için de katmanı değiştir
            }
        }
    }


    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = false;
        }
        stopGrowthOnCollision = false;
    }

        private void OnCollisionEnter(Collision collision)
    {
        stopGrowthOnCollision = true; // Büyümeyi durdur
        if (Object.HasStateAuthority ) // Çarpışma kontrolü (örn. "Obstacle" etiketi)
        {
            if (collision.gameObject.CompareTag("Ball") || collision.gameObject.CompareTag("Goal") || collision.gameObject.CompareTag("Corner") || collision.gameObject.CompareTag("Top")) // Çarpışma kontrolü (örn. "Obstacle" etiketi)
            {
                ResetScale(); // Boyutu sıfırla
            }
        }
    }
}
