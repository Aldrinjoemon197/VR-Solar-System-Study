using UnityEngine;
using UnityEngine.XR;
using System.Collections;

public class QuestHandGunLaser : MonoBehaviour
{
    [Header("Gun / Muzzle")]
    public Transform muzzle;
    public bool createSimpleGunVisual = true;

    public Vector3 muzzleLocalPosition = new Vector3(0f, -0.03f, 0.28f);
    public Vector3 muzzleLocalRotation = new Vector3(-10f, 0f, 0f);

    [Header("Stable Green Aim Cylinder")]
    public float aimDistance = 1000f;
    public float aimRadius = 0.025f;
    public Color aimColor = Color.green;
    public float aimHoldTime = 0.25f;

    [Header("Red Cylinder Bullets")]
    public float bulletMaxDistance = 1000f;
    public float bulletLength = 2.0f;
    public float bulletRadius = 0.08f;
    public float bulletSpeed = 250f;
    public float bulletMaxLifetime = 3f;
    public Color bulletColor = Color.red;

    [Header("Controls")]
    public bool useMouseBackup = true;
    public bool useTriggerButton = true;
    public bool useGripButton = false;

    [Tooltip("ON = right trigger cannot shoot while the player is piloting the spaceship.")]
    public bool blockShootingWhileInShip = true;

    [Header("Hit")]
    public LayerMask hitLayers = ~0;

    private InputDevice leftController;
    private InputDevice rightController;

    private GameObject aimBeam;
    private Renderer aimRenderer;
    private Material aimMaterial;
    private Material bulletMaterial;

    private float aimTimer;
    private bool wasShootPressed;

    void Start()
    {
        if (muzzle == null)
        {
            GameObject muzzleObject = new GameObject("Gun Muzzle");
            muzzleObject.transform.SetParent(transform);
            muzzleObject.transform.localPosition = muzzleLocalPosition;
            muzzleObject.transform.localRotation = Quaternion.Euler(muzzleLocalRotation);
            muzzle = muzzleObject.transform;
        }

        aimMaterial = CreateColoredMaterial(aimColor);
        bulletMaterial = CreateColoredMaterial(bulletColor);
        ProceduralSolarSystemAudio.Ensure();

        CreateAimBeam();
        EnsureSunCanBeShot();

        if (createSimpleGunVisual)
            CreateSimpleGun();
    }

    void Update()
    {
        if (!leftController.isValid)
            leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (!rightController.isValid)
            rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        HandleAim();
        HandleShoot();
    }

    void HandleAim()
    {
        if (blockShootingWhileInShip && PlayerIsPilotingShip())
        {
            aimTimer = 0f;

            if (aimBeam != null)
            {
                aimBeam.SetActive(false);
            }

            return;
        }

        bool aimPressed = IsLeftAimPressed();

        if (aimPressed)
            aimTimer = aimHoldTime;

        if (aimTimer > 0f)
        {
            aimTimer -= Time.deltaTime;
            UpdateAimBeam();
        }
        else
        {
            aimBeam.SetActive(false);
        }
    }

    void HandleShoot()
    {
        if (blockShootingWhileInShip && PlayerIsPilotingShip())
        {
            wasShootPressed = IsRightShootPressed();
            return;
        }

        bool shootPressed = IsRightShootPressed();

        if (shootPressed && !wasShootPressed)
            FireBullet();

        wasShootPressed = shootPressed;
    }

    bool IsLeftAimPressed()
    {
        if (useMouseBackup && Input.GetMouseButton(0))
            return true;

        return IsTriggerPressed(leftController) || IsGripPressed(leftController);
    }

    bool IsRightShootPressed()
    {
        if (useMouseBackup && Input.GetMouseButtonDown(1))
            return true;

        return IsTriggerPressed(rightController) || IsGripPressed(rightController);
    }

    bool IsTriggerPressed(InputDevice controller)
    {
        if (!useTriggerButton)
            return false;

        bool triggerButton = false;
        float triggerValue = 0f;

        controller.TryGetFeatureValue(CommonUsages.triggerButton, out triggerButton);
        controller.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);

        return triggerButton || triggerValue > 0.15f;
    }

    bool IsGripPressed(InputDevice controller)
    {
        if (!useGripButton)
            return false;

        bool gripButton = false;
        float gripValue = 0f;

        controller.TryGetFeatureValue(CommonUsages.gripButton, out gripButton);
        controller.TryGetFeatureValue(CommonUsages.grip, out gripValue);

        return gripButton || gripValue > 0.15f;
    }

    bool PlayerIsPilotingShip()
    {
        SpaceshipThirdPersonController ship = FindObjectOfType<SpaceshipThirdPersonController>();
        return ship != null && ship.enabled && ship.IsInShipMode;
    }

    void CreateAimBeam()
    {
        aimBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        aimBeam.name = "Stable Green Gun Aim Beam";
        aimBeam.transform.SetParent(transform);

        Collider col = aimBeam.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        aimRenderer = aimBeam.GetComponent<Renderer>();
        aimRenderer.material = aimMaterial;

        aimBeam.SetActive(false);
    }

    void UpdateAimBeam()
    {
        Vector3 start = muzzle.position;
        Vector3 direction = muzzle.forward.normalized;
        Vector3 end = start + direction * aimDistance;

        if (GetValidHit(start, direction, aimDistance, out RaycastHit hit))
            end = hit.point;

        SetCylinderBetween(aimBeam, start, end, aimRadius);
        aimBeam.SetActive(true);
    }

    void FireBullet()
    {
        ProceduralSolarSystemAudio.Ensure().PlayLaser();

        Vector3 start = muzzle.position;
        Vector3 direction = muzzle.forward.normalized;
        Vector3 target = start + direction * bulletMaxDistance;

        RaycastHit hitInfo = new RaycastHit();
        bool hasHit = GetValidHit(start, direction, bulletMaxDistance, out hitInfo);

        if (hasHit)
            target = hitInfo.point;

        StartCoroutine(MoveBullet(start, target, direction, hasHit, hitInfo));
    }

    IEnumerator MoveBullet(Vector3 start, Vector3 target, Vector3 direction, bool hasHit, RaycastHit hitInfo)
    {
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bullet.name = "Small Red Cylinder Laser Bullet";

        Collider col = bullet.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Renderer renderer = bullet.GetComponent<Renderer>();
        renderer.material = bulletMaterial;

        float distanceToTarget = Vector3.Distance(start, target);
        float travelled = 0f;
        float timer = 0f;

        while (travelled < distanceToTarget && timer < bulletMaxLifetime)
        {
            timer += Time.deltaTime;
            travelled += bulletSpeed * Time.deltaTime;

            Vector3 currentStart = start + direction * travelled;
            Vector3 currentEnd = currentStart + direction * bulletLength;

            SetCylinderBetween(bullet, currentStart, currentEnd, bulletRadius);

            yield return null;
        }

        if (hasHit)
        {
            ProceduralSolarSystemAudio.Ensure().PlayHit();
            TriggerHitMethods(hitInfo, direction);
        }

        Destroy(bullet);
    }

    bool GetValidHit(Vector3 start, Vector3 direction, float distance, out RaycastHit finalHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            start,
            direction,
            distance,
            hitLayers,
            QueryTriggerInteraction.Ignore
        );

        finalHit = new RaycastHit();
        float closest = Mathf.Infinity;
        bool found = false;

        Transform playerRoot = transform.root;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            // Ignore the XR player, gun, controller, and anything inside XR Origin
            if (hit.collider.transform.root == playerRoot)
                continue;

            if (hit.distance < closest)
            {
                closest = hit.distance;
                finalHit = hit;
                found = true;
            }
        }

        return found;
    }

    void EnsureSunCanBeShot()
    {
        Transform[] allTransforms = FindObjectsOfType<Transform>();

        foreach (Transform t in allTransforms)
        {
            if (t == null || !t.name.ToLower().Contains("sun"))
            {
                continue;
            }

            if (t.GetComponentInChildren<Renderer>() == null)
            {
                continue;
            }

            if (t.GetComponent<SunDestructionController>() == null)
            {
                t.gameObject.AddComponent<SunDestructionController>();
            }

            if (t.GetComponent<Collider>() == null)
            {
                Renderer renderer = t.GetComponentInChildren<Renderer>();
                SphereCollider collider = t.gameObject.AddComponent<SphereCollider>();
                collider.radius = renderer != null ? Mathf.Max(renderer.bounds.extents.x, renderer.bounds.extents.y, renderer.bounds.extents.z) / Mathf.Max(0.001f, t.lossyScale.x) : 1f;
            }

            return;
        }
    }

    void SetCylinderBetween(GameObject cylinder, Vector3 start, Vector3 end, float radius)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;

        if (length < 0.001f)
            return;

        cylinder.transform.position = start + direction * 0.5f;
        cylinder.transform.up = direction.normalized;

        // Unity cylinder height is 2 units along local Y
        cylinder.transform.localScale = new Vector3(
            radius,
            length * 0.5f,
            radius
        );
    }

    void TriggerHitMethods(RaycastHit hit, Vector3 direction)
    {
        GameObject obj = hit.collider.gameObject;

        if (LooksLikeOriginalSun(obj.transform))
        {
            Transform sunTransform = FindSunTransform(obj.transform);
            SunDestructionController sunController = sunTransform != null ? sunTransform.GetComponent<SunDestructionController>() : obj.GetComponentInParent<SunDestructionController>();

            if (sunController == null)
            {
                sunController = (sunTransform != null ? sunTransform.gameObject : obj).AddComponent<SunDestructionController>();
            }

            sunController.HandleSunLaserHit(direction);
            return;
        }

        obj.SendMessage("OnLaserHit", hit, SendMessageOptions.DontRequireReceiver);
        obj.SendMessageUpwards("OnLaserHit", hit, SendMessageOptions.DontRequireReceiver);

        obj.SendMessage("Split", SendMessageOptions.DontRequireReceiver);
        obj.SendMessageUpwards("Split", SendMessageOptions.DontRequireReceiver);

        obj.SendMessage("SplitEarth", SendMessageOptions.DontRequireReceiver);
        obj.SendMessageUpwards("SplitEarth", SendMessageOptions.DontRequireReceiver);

        obj.SendMessage("SplitPlanet", SendMessageOptions.DontRequireReceiver);
        obj.SendMessageUpwards("SplitPlanet", SendMessageOptions.DontRequireReceiver);

        obj.SendMessage("SplitInHalf", SendMessageOptions.DontRequireReceiver);
        obj.SendMessageUpwards("SplitInHalf", SendMessageOptions.DontRequireReceiver);

        obj.SendMessage("SplitInHalf", direction, SendMessageOptions.DontRequireReceiver);
        obj.SendMessageUpwards("SplitInHalf", direction, SendMessageOptions.DontRequireReceiver);

        obj.SendMessage("BreakPlanet", SendMessageOptions.DontRequireReceiver);
        obj.SendMessageUpwards("BreakPlanet", SendMessageOptions.DontRequireReceiver);

        obj.SendMessage("Explode", SendMessageOptions.DontRequireReceiver);
        obj.SendMessageUpwards("Explode", SendMessageOptions.DontRequireReceiver);
    }

    bool LooksLikeOriginalSun(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            string n = current.name.ToLower();

            if (n.Contains("_left_half") || n.Contains("_right_half") || n.Contains("sunouter") || n.Contains("corona") || n.Contains("photosphere") || n.Contains("convective") || n.Contains("radiative") || n == "core")
            {
                return false;
            }

            if (n.Contains("sun"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    Transform FindSunTransform(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            if (current.name.ToLower().Contains("sun"))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    void CreateSimpleGun()
    {
        Material gunMat = CreateColoredMaterial(new Color(0.05f, 0.05f, 0.05f));

        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Simple Gun Barrel";
        barrel.transform.SetParent(transform);
        barrel.transform.localPosition = new Vector3(0f, -0.03f, 0.16f);
        barrel.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
        barrel.transform.localScale = new Vector3(0.04f, 0.18f, 0.04f);
        barrel.GetComponent<Renderer>().material = gunMat;

        Collider barrelCol = barrel.GetComponent<Collider>();
        if (barrelCol != null)
            Destroy(barrelCol);

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Simple Gun Handle";
        handle.transform.SetParent(transform);
        handle.transform.localPosition = new Vector3(0f, -0.12f, 0.03f);
        handle.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
        handle.transform.localScale = new Vector3(0.08f, 0.18f, 0.06f);
        handle.GetComponent<Renderer>().material = gunMat;

        Collider handleCol = handle.GetComponent<Collider>();
        if (handleCol != null)
            Destroy(handleCol);
    }

    Material CreateColoredMaterial(Color color)
    {
        Shader shader = Shader.Find("HDRP/Unlit");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_UnlitColor"))
            mat.SetColor("_UnlitColor", color);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.5f);
        }

        return mat;
    }
}
