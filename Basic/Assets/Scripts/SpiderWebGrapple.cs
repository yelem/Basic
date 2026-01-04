using UnityEngine;

public class SpiderWebGrapple : MonoBehaviour
{
    [Header("Настройки паутины")]
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float pullSpeed = 25f;
    [SerializeField] private float maxPullSpeed = 40f;
    [SerializeField] private LayerMask grappleLayer;

    [Header("Настройки импульса")]
    [SerializeField] private float launchMultiplier = 1.5f; // Множитель скорости при отпускании
    [SerializeField] private float minLaunchSpeed = 10f;    // Минимальная скорость запуска
    [SerializeField] private bool maintainYVelocity = true;  // Сохранять вертикальную скорость

    [Header("Визуализация")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform firePoint;

    [Header("Прицел")]
    [SerializeField] private RectTransform crosshair;
    [SerializeField] private Color validColor = Color.green;
    [SerializeField] private Color invalidColor = Color.red;
    [SerializeField] private float crosshairSize = 20f;

    [Header("Камера")]
    [SerializeField] private Transform cameraTransform;

    private Vector3 grapplePoint;
    private bool isGrappling = false;
    private bool canGrapple = false;
    private Rigidbody rb;
    private UnityEngine.UI.Image crosshairImage;
    private float currentPullSpeed;
    private Vector3 lastVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Если камера не назначена, пробуем найти
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        // Настройка LineRenderer если не назначен
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.gray;
        }

        lineRenderer.enabled = false;

        // Создание прицела если не назначен
        if (crosshair == null)
        {
            CreateCrosshair();
        }
        else
        {
            crosshairImage = crosshair.GetComponent<UnityEngine.UI.Image>();
        }

        currentPullSpeed = pullSpeed;
    }

    void Update()
    {
        // Проверка возможности зацепа
        CheckGrappleTarget();

        // ЛКМ - выстрелить паутиной
        if (Input.GetMouseButtonDown(0))
        {
            StartGrapple();
        }

        // Отпускание ЛКМ - отцепиться с импульсом
        if (Input.GetMouseButtonUp(0) && isGrappling)
        {
            LaunchPlayer();
            StopGrapple();
        }

        // Обновление визуализации паутины
        if (isGrappling)
        {
            DrawRope();
        }
    }

    void FixedUpdate()
    {
        // Притягивание к точке зацепа с ускорением
        if (isGrappling)
        {
            Vector3 direction = (grapplePoint - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, grapplePoint);

            // Постепенно увеличиваем скорость притягивания
            currentPullSpeed = Mathf.Min(currentPullSpeed + pullSpeed * 0.5f * Time.fixedDeltaTime, maxPullSpeed);

            // Применяем силу притяжения
            rb.AddForce(direction * currentPullSpeed, ForceMode.Acceleration);

            // Сохраняем текущую скорость для запуска
            lastVelocity = rb.linearVelocity;

            // Автоматическое отцепление при достижении точки
            if (distance < 2f)
            {
                LaunchPlayer();
                StopGrapple();
            }
        }
    }

    void StartGrapple()
    {
        // Проверяем, можем ли мы зацепиться
        if (!canGrapple) return;

        // Raycast от камеры в центр экрана (первое лицо)
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, grappleLayer))
        {
            grapplePoint = hit.point;
            isGrappling = true;
            lineRenderer.enabled = true;
            currentPullSpeed = pullSpeed; // Сброс скорости

            Debug.Log($"Зацепились за: {hit.collider.name}");
        }
    }

    void LaunchPlayer()
    {
        if (!isGrappling) return;

        // Получаем текущую скорость
        Vector3 launchVelocity = lastVelocity;

        // Увеличиваем скорость с учётом множителя
        float currentSpeed = launchVelocity.magnitude;
        float targetSpeed = Mathf.Max(currentSpeed * launchMultiplier, minLaunchSpeed);

        if (currentSpeed > 0.1f)
        {
            // Сохраняем направление, но увеличиваем скорость
            launchVelocity = launchVelocity.normalized * targetSpeed;
        }
        else
        {
            // Если скорость мала, запускаем в направлении движения к точке
            Vector3 direction = (grapplePoint - transform.position).normalized;
            launchVelocity = direction * minLaunchSpeed;
        }

        // Опционально: сохраняем вертикальную составляющую для лучшего контроля
        if (maintainYVelocity && lastVelocity.y > 0)
        {
            launchVelocity.y = Mathf.Max(launchVelocity.y, lastVelocity.y);
        }

        // Применяем импульс
        rb.linearVelocity = launchVelocity;

        Debug.Log($"Запуск! Скорость: {launchVelocity.magnitude:F1} м/с");
    }

    void StopGrapple()
    {
        isGrappling = false;
        lineRenderer.enabled = false;
        currentPullSpeed = pullSpeed;
    }

    void DrawRope()
    {
        // Паутина идёт от точки выстрела (или от камеры) до точки зацепа
        Vector3 startPos = firePoint != null ? firePoint.position : cameraTransform.position;
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, grapplePoint);
    }

    void CheckGrappleTarget()
    {
        // Проверка от центра экрана (первое лицо)
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, grappleLayer))
        {
            canGrapple = true;
            UpdateCrosshair(validColor);
        }
        else
        {
            canGrapple = false;
            UpdateCrosshair(invalidColor);
        }
    }

    void CreateCrosshair()
    {
        // Создание Canvas если его нет
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Создание прицела
        GameObject crosshairObj = new GameObject("Crosshair");
        crosshairObj.transform.SetParent(canvas.transform, false);

        crosshair = crosshairObj.AddComponent<RectTransform>();
        crosshair.anchorMin = new Vector2(0.5f, 0.5f);
        crosshair.anchorMax = new Vector2(0.5f, 0.5f);
        crosshair.sizeDelta = new Vector2(crosshairSize, crosshairSize);
        crosshair.anchoredPosition = Vector2.zero;

        crosshairImage = crosshairObj.AddComponent<UnityEngine.UI.Image>();

        // Создание текстуры прицела (крестик)
        Texture2D tex = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        // Горизонтальная линия
        for (int x = 20; x < 44; x++)
            for (int y = 30; y < 34; y++)
                pixels[y * 64 + x] = Color.white;

        // Вертикальная линия
        for (int x = 30; x < 34; x++)
            for (int y = 20; y < 44; y++)
                pixels[y * 64 + x] = Color.white;

        // Центральная точка
        for (int x = 28; x < 36; x++)
            for (int y = 28; y < 36; y++)
                pixels[y * 64 + x] = Color.clear;

        tex.SetPixels(pixels);
        tex.Apply();

        crosshairImage.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }

    void UpdateCrosshair(Color color)
    {
        if (crosshairImage != null && !isGrappling)
        {
            crosshairImage.color = color;
        }
    }

    // Визуализация в редакторе
    void OnDrawGizmos()
    {
        if (isGrappling)
        {
            Gizmos.color = Color.yellow;
            Vector3 startPos = firePoint != null ? firePoint.position : (cameraTransform != null ? cameraTransform.position : transform.position);
            Gizmos.DrawLine(startPos, grapplePoint);
            Gizmos.DrawSphere(grapplePoint, 0.3f);

            // Показываем направление скорости
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, lastVelocity.normalized * 2f);
        }
    }
}