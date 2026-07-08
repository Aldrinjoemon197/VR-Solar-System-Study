using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SunDestructionController : MonoBehaviour
{
    [Header("Two Shot Ending")]
    public bool requireWarningShot = true;
    public float duplicateHitCooldown = 0.25f;

    [Header("Sun Split")]
    public float openingDistanceMultiplier = 0.55f;
    public float flyingHalfDistanceMultiplier = 5.5f;
    public float flyingHalfDuration = 4.0f;
    public float flyingHalfSpinDegrees = 220f;
    public bool destroyFlyingHalfAfterMove = true;

    [Header("Planet Escape")]
    public float planetEscapeSpeed = 1.2f;
    public float planetEscapeAcceleration = 0.08f;
    public float planetTangentialDrift = 0.35f;

    [Header("Sun Light")]
    public bool addSunPointLight = true;
    public float sunLightRangeMultiplier = 75f;
    public float sunLightIntensity = 1.1f;
    public float helperLightIntensity = 0.55f;
    public float helperLightOffsetMultiplier = 2.0f;
    public Color sunLightColor = new Color(1f, 0.86f, 0.55f, 1f);

    [Header("Tablets")]
    [Tooltip("X = side of Sun, Y = above Sun, Z = toward/away from camera, measured in Sun radii.")]
    public Vector3 warningTabletOffset = new Vector3(0.80f, 1.08f, 0.18f);
    public Vector3 layerTabletOffset = new Vector3(0.95f, 1.15f, 0.18f);
    public float tabletScaleMultiplier = 0.32f;
    public float minimumTabletScale = 3.2f;

    private bool warningShown;
    private bool destroyed;
    private float lastHitTime = -999f;
    private GameObject warningTablet;
    private Light sunPointLight;
    private readonly List<Light> helperSunLights = new List<Light>();

    void Start()
    {
        EnsureSunLight();
    }

    private struct SunLayer
    {
        public string objectName;
        public string label;
        public string description;
        public float outerRatio;
        public float innerRatio;
        public Color color;
        public bool transparent;

        public SunLayer(string objectName, string label, string description, float outerRatio, float innerRatio, Color color, bool transparent)
        {
            this.objectName = objectName;
            this.label = label;
            this.description = description;
            this.outerRatio = outerRatio;
            this.innerRatio = innerRatio;
            this.color = color;
            this.transparent = transparent;
        }
    }

    public void OnLaserHit()
    {
        HandleSunLaserHit(transform.forward);
    }

    public void OnLaserHit(RaycastHit hit)
    {
        Vector3 direction = hit.collider != null ? (hit.point - transform.position).normalized : transform.forward;
        HandleSunLaserHit(direction);
    }

    public void HandleSunLaserHit(Vector3 laserDirectionWorld)
    {
        if (destroyed || Time.time - lastHitTime < duplicateHitCooldown)
        {
            return;
        }

        lastHitTime = Time.time;

        if (requireWarningShot && !warningShown)
        {
            warningShown = true;
            ShowWarningTablet();
            return;
        }

        destroyed = true;
        HideWarningTablet();
        ProceduralSolarSystemAudio.Ensure().PlaySplit();
        SplitSun(laserDirectionWorld);
        ReleasePlanets();
    }

    void ShowWarningTablet()
    {
        if (warningTablet != null)
        {
            warningTablet.SetActive(true);
            return;
        }

        warningTablet = CreateTablet(
            "Sun Warning Tablet",
            GetTabletPosition(warningTabletOffset),
            "WARNING",
            "Shoot Sun again\nPlanets drift away",
            "Second shot starts ending"
        );
    }

    void HideWarningTablet()
    {
        if (warningTablet != null)
        {
            warningTablet.SetActive(false);
        }
    }

    void SplitSun(Vector3 laserDirectionWorld)
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        float radius = renderer != null ? Mathf.Max(renderer.bounds.extents.x, renderer.bounds.extents.y, renderer.bounds.extents.z) : 1f;
        Material surfaceMaterial = renderer != null ? renderer.material : PlanetSplitMeshUtility.CreateLayerMaterial(new Color(1f, 0.72f, 0.10f), false);
        List<SunLayer> layers = GetSunLayers();
        Vector3 splitAxis = transform.right.sqrMagnitude > 0.001f ? transform.right.normalized : Vector3.right;
        float opening = radius * openingDistanceMultiplier;

        GameObject leftHalf = CreateLayeredSunHalf(gameObject.name + "_Left_Half", radius, false, surfaceMaterial, layers);
        GameObject rightHalf = CreateLayeredSunHalf(gameObject.name + "_Right_Half", radius, true, surfaceMaterial, layers);

        leftHalf.transform.position = transform.position;
        leftHalf.transform.rotation = transform.rotation;
        rightHalf.transform.position = transform.position + splitAxis * opening;
        rightHalf.transform.rotation = transform.rotation;

        AddStaticHalfCollider(leftHalf, radius, true);
        MakeFlyingHalf(rightHalf, splitAxis);
        CreateLayerTablet(leftHalf.transform, layers);
        HideOriginalSun();
    }

    List<SunLayer> GetSunLayers()
    {
        return new List<SunLayer>
        {
            new SunLayer("Corona", "CORONA", "Outer hot plasma", 1.08f, 1.00f, new Color(1.00f, 0.95f, 0.45f, 0.22f), true),
            new SunLayer("Photosphere", "PHOTOSPHERE", "Visible surface", 1.00f, 0.88f, new Color(1.00f, 0.72f, 0.08f), false),
            new SunLayer("ConvectiveZone", "CONVECTIVE ZONE", "Rising hot plasma", 0.88f, 0.70f, new Color(1.00f, 0.42f, 0.05f), false),
            new SunLayer("RadiativeZone", "RADIATIVE ZONE", "Light diffuses outward", 0.70f, 0.25f, new Color(1.00f, 0.18f, 0.05f), false),
            new SunLayer("Core", "CORE", "Fusion power source", 0.25f, -1f, new Color(1.00f, 0.95f, 0.70f), false)
        };
    }

    GameObject CreateLayeredSunHalf(string rootName, float radius, bool positiveXHalf, Material surfaceMaterial, List<SunLayer> layers)
    {
        GameObject root = new GameObject(rootName);

        for (int i = 0; i < layers.Count; i++)
        {
            SunLayer layer = layers[i];
            Material mat = PlanetSplitMeshUtility.CreateLayerMaterial(layer.color, layer.transparent);
            float outer = radius * layer.outerRatio;

            if (layer.innerRatio < 0f)
            {
                CreateSolidPart(layer.objectName, root.transform, outer, positiveXHalf, mat);
            }
            else
            {
                CreateShellPart(layer.objectName, root.transform, outer, radius * layer.innerRatio, positiveXHalf, mat);
            }
        }

        CreateOuterSurfacePart("SunOuterSurface", root.transform, radius, positiveXHalf, surfaceMaterial);
        return root;
    }

    void CreateShellPart(string objectName, Transform parent, float outerRadius, float innerRadius, bool positiveXHalf, Material material)
    {
        GameObject part = new GameObject(objectName);
        part.transform.SetParent(parent, false);
        part.AddComponent<MeshFilter>().mesh = PlanetSplitMeshUtility.CreateHalfShellMesh(outerRadius, innerRadius, positiveXHalf, 48, 24);
        MeshRenderer mr = part.AddComponent<MeshRenderer>();
        mr.material = material;
        MeshCollider collider = part.AddComponent<MeshCollider>();
        collider.sharedMesh = part.GetComponent<MeshFilter>().mesh;
    }

    void CreateSolidPart(string objectName, Transform parent, float radius, bool positiveXHalf, Material material)
    {
        GameObject part = new GameObject(objectName);
        part.transform.SetParent(parent, false);
        part.AddComponent<MeshFilter>().mesh = PlanetSplitMeshUtility.CreateSolidHalfSphereMesh(radius, positiveXHalf, 48, 24);
        MeshRenderer mr = part.AddComponent<MeshRenderer>();
        mr.material = material;
        MeshCollider collider = part.AddComponent<MeshCollider>();
        collider.sharedMesh = part.GetComponent<MeshFilter>().mesh;
    }

    void CreateOuterSurfacePart(string objectName, Transform parent, float radius, bool positiveXHalf, Material material)
    {
        GameObject part = new GameObject(objectName);
        part.transform.SetParent(parent, false);
        part.AddComponent<MeshFilter>().mesh = PlanetSplitMeshUtility.CreateOuterSurfaceHalfMesh(radius, positiveXHalf, 48, 24);
        MeshRenderer mr = part.AddComponent<MeshRenderer>();
        mr.material = material;
    }

    void AddStaticHalfCollider(GameObject half, float radius, bool isInert)
    {
        SphereCollider collider = half.AddComponent<SphereCollider>();
        collider.radius = radius;
        collider.center = Vector3.zero;

        if (isInert)
        {
            half.tag = "Untagged";
            half.layer = gameObject.layer;
        }
    }

    void MakeFlyingHalf(GameObject half, Vector3 direction)
    {
        SunFlyingHalfMover mover = half.AddComponent<SunFlyingHalfMover>();
        mover.Begin(direction.normalized, flyingHalfDistanceMultiplier, flyingHalfDuration, flyingHalfSpinDegrees, destroyFlyingHalfAfterMove);
    }

    void HideOriginalSun()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            if (r != null)
            {
                r.enabled = false;
            }
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider c in colliders)
        {
            if (c != null)
            {
                c.enabled = false;
            }
        }

        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && script != this)
            {
                script.enabled = false;
            }
        }
    }

    void ReleasePlanets()
    {
        Transform[] all = FindObjectsOfType<Transform>();
        HashSet<Transform> released = new HashSet<Transform>();

        foreach (Transform t in all)
        {
            if (t == null || !IsPlanetName(t.name))
            {
                continue;
            }

            Transform planetRoot = GetPlanetRoot(t);

            if (planetRoot == null || released.Contains(planetRoot) || IsChildOfReleasedRoot(planetRoot, released))
            {
                continue;
            }

            released.Add(planetRoot);
            DisableOrbitScripts(planetRoot);

            Vector3 away = planetRoot.position - transform.position;
            away.y *= 0.35f;

            if (away.sqrMagnitude < 0.001f)
            {
                away = planetRoot.position.normalized;
            }

            Vector3 tangent = Vector3.Cross(Vector3.up, away.normalized);
            Vector3 direction = (away.normalized + tangent * planetTangentialDrift).normalized;
            SunPlanetEscapeMover mover = planetRoot.gameObject.AddComponent<SunPlanetEscapeMover>();
            mover.Begin(direction, planetEscapeSpeed, planetEscapeAcceleration);
        }
    }

    Transform GetPlanetRoot(Transform start)
    {
        Transform current = start;

        while (current.parent != null && IsPlanetName(current.parent.name))
        {
            current = current.parent;
        }

        return current;
    }

    bool IsChildOfReleasedRoot(Transform candidate, HashSet<Transform> released)
    {
        foreach (Transform root in released)
        {
            if (candidate == root || candidate.IsChildOf(root))
            {
                return true;
            }
        }

        return false;
    }

    bool IsPlanetName(string objectName)
    {
        string n = objectName.ToLower();
        return n.Contains("mercury") || n.Contains("venus") || n.Contains("earth") || n.Contains("mars") ||
               n.Contains("jupiter") || n.Contains("saturn") || n.Contains("uranus") || n.Contains("neptune");
    }

    void DisableOrbitScripts(Transform planet)
    {
        MonoBehaviour[] scripts = planet.GetComponentsInChildren<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null)
            {
                continue;
            }

            string typeName = script.GetType().Name.ToLower();

            if (typeName == "rotation")
            {
                script.enabled = false;
            }
        }
    }

    void CreateLayerTablet(Transform parent, List<SunLayer> layers)
    {
        string body =
            "CORONA: hot plasma\n" +
            "PHOTOSPHERE: visible surface\n" +
            "CONVECTIVE: rising gas\n" +
            "RADIATIVE: light moves out\n" +
            "CORE: fusion energy";

        GameObject tablet = CreateTablet(
            "Sun Layer Tablet",
            GetTabletPosition(layerTabletOffset),
            "SUN LAYERS",
            body.TrimEnd('\n'),
            "Fusion makes sunlight"
        );

        tablet.transform.SetParent(parent, true);
    }

    Vector3 GetTabletPosition(Vector3 offset)
    {
        float radius = GetSunVisualRadius();
        Vector3 side = GetCameraSideDirection();
        Vector3 towardCamera = GetCameraTowardDirection();

        return transform.position +
               side * radius * offset.x +
               Vector3.up * radius * offset.y +
               towardCamera * radius * offset.z;
    }

    GameObject CreateTablet(string objectName, Vector3 position, string title, string body, string footer)
    {
        GameObject tablet = new GameObject(objectName);
        tablet.transform.position = position;
        tablet.transform.rotation = Quaternion.identity;
        float dynamicScale = Mathf.Max(minimumTabletScale, GetSunVisualRadius() * tabletScaleMultiplier);
        tablet.transform.localScale = Vector3.one * dynamicScale;

        Material panelMat = PlanetSplitMeshUtility.CreateLayerMaterial(new Color(0.02f, 0.025f, 0.035f, 0.90f), true);
        Material borderMat = PlanetSplitMeshUtility.CreateLayerMaterial(new Color(1f, 0.72f, 0.10f, 1f), true);

        PlanetSplitMeshUtility.CreateCube("Tablet Background", tablet.transform, Vector3.zero, Vector3.zero, new Vector3(2.20f, 1.30f, 0.025f), panelMat);
        PlanetSplitMeshUtility.CreateCube("Tablet Top Border", tablet.transform, new Vector3(0f, 0.665f, -0.015f), Vector3.zero, new Vector3(2.24f, 0.030f, 0.025f), borderMat);
        PlanetSplitMeshUtility.CreateCube("Tablet Bottom Border", tablet.transform, new Vector3(0f, -0.665f, -0.015f), Vector3.zero, new Vector3(2.24f, 0.030f, 0.025f), borderMat);
        PlanetSplitMeshUtility.CreateCube("Tablet Left Border", tablet.transform, new Vector3(-1.120f, 0f, -0.015f), Vector3.zero, new Vector3(0.030f, 1.32f, 0.025f), borderMat);
        PlanetSplitMeshUtility.CreateCube("Tablet Right Border", tablet.transform, new Vector3(1.120f, 0f, -0.015f), Vector3.zero, new Vector3(0.030f, 1.32f, 0.025f), borderMat);

        TextMesh titleText = PlanetSplitMeshUtility.CreateText("Tablet Title", tablet.transform, new Vector3(-0.96f, 0.505f, -0.04f), 0.048f, TextAnchor.UpperLeft);
        titleText.text = title;
        titleText.fontSize = 52;

        TextMesh bodyText = PlanetSplitMeshUtility.CreateText("Tablet Body", tablet.transform, new Vector3(-0.96f, 0.285f, -0.04f), 0.022f, TextAnchor.UpperLeft);
        bodyText.text = body;
        bodyText.fontSize = 30;

        TextMesh footerText = PlanetSplitMeshUtility.CreateText("Tablet Footer", tablet.transform, new Vector3(-0.96f, -0.545f, -0.04f), 0.023f, TextAnchor.UpperLeft);
        footerText.text = footer;
        footerText.fontSize = 30;

        VenusTabletBillboard billboard = tablet.AddComponent<VenusTabletBillboard>();
        billboard.keepUpright = true;
        return tablet;
    }

    void EnsureSunLight()
    {
        if (!addSunPointLight)
        {
            return;
        }

        float radius = GetSunVisualRadius();
        float range = Mathf.Max(25f, radius * sunLightRangeMultiplier);

        sunPointLight = GetComponentInChildren<Light>();
        if (sunPointLight == null || sunPointLight.type != LightType.Point)
        {
            GameObject lightObject = new GameObject("Sun 360 Degree Point Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.zero;
            sunPointLight = lightObject.AddComponent<Light>();
        }

        sunPointLight.type = LightType.Point;
        sunPointLight.color = sunLightColor;
        sunPointLight.intensity = sunLightIntensity;
        sunPointLight.range = range;
        sunPointLight.shadows = LightShadows.None;

        EnsureHelperLight("Sun Light +X", Vector3.right, radius, range);
        EnsureHelperLight("Sun Light -X", Vector3.left, radius, range);
        EnsureHelperLight("Sun Light +Z", Vector3.forward, radius, range);
        EnsureHelperLight("Sun Light -Z", Vector3.back, radius, range);
        EnsureHelperLight("Sun Light +Y", Vector3.up, radius, range);
        EnsureHelperLight("Sun Light -Y", Vector3.down, radius, range);
    }

    void EnsureHelperLight(string objectName, Vector3 localDirection, float radius, float range)
    {
        Transform existing = transform.Find(objectName);
        Light light;

        if (existing == null)
        {
            GameObject lightObject = new GameObject(objectName);
            lightObject.transform.SetParent(transform, false);
            existing = lightObject.transform;
            light = lightObject.AddComponent<Light>();
        }
        else
        {
            light = existing.GetComponent<Light>();

            if (light == null)
            {
                light = existing.gameObject.AddComponent<Light>();
            }
        }

        existing.localPosition = localDirection.normalized * radius * helperLightOffsetMultiplier;
        light.type = LightType.Point;
        light.color = sunLightColor;
        light.intensity = helperLightIntensity;
        light.range = range;
        light.shadows = LightShadows.None;

        if (!helperSunLights.Contains(light))
        {
            helperSunLights.Add(light);
        }
    }

    float GetSunVisualRadius()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();

        if (renderer == null)
        {
            return Mathf.Max(1f, transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        }

        return Mathf.Max(renderer.bounds.extents.x, renderer.bounds.extents.y, renderer.bounds.extents.z);
    }

    Vector3 GetCameraSideDirection()
    {
        Camera cam = Camera.main;
        Vector3 side = cam != null ? cam.transform.right : transform.right;
        side.y = 0f;

        if (side.sqrMagnitude < 0.001f)
        {
            side = Vector3.right;
        }

        return side.normalized;
    }

    Vector3 GetCameraTowardDirection()
    {
        Camera cam = Camera.main;
        Vector3 toward = cam != null ? cam.transform.position - transform.position : -transform.forward;
        toward.y = 0f;

        if (toward.sqrMagnitude < 0.001f)
        {
            toward = -transform.forward;
            toward.y = 0f;
        }

        if (toward.sqrMagnitude < 0.001f)
        {
            toward = Vector3.back;
        }

        return toward.normalized;
    }
}

public class SunFlyingHalfMover : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 direction;
    private float duration;
    private float spinDegrees;
    private float timer;
    private float moveDistance;
    private bool destroyAfterMove;

    public void Begin(Vector3 flyDirection, float distanceMultiplier, float duration, float spinDegrees, bool destroyAfterMove)
    {
        startPosition = transform.position;
        direction = flyDirection.sqrMagnitude > 0.001f ? flyDirection.normalized : Vector3.right;
        this.duration = Mathf.Max(0.1f, duration);
        this.spinDegrees = spinDegrees;
        this.destroyAfterMove = destroyAfterMove;

        Renderer renderer = GetComponentInChildren<Renderer>();
        float radius = renderer != null ? Mathf.Max(renderer.bounds.extents.x, renderer.bounds.extents.y, renderer.bounds.extents.z) : 1f;
        moveDistance = radius * Mathf.Max(1f, distanceMultiplier);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        transform.position = startPosition + direction * moveDistance * eased;
        transform.Rotate(Vector3.up, spinDegrees * Time.deltaTime / duration, Space.World);

        if (destroyAfterMove && t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}

public class SunPlanetEscapeMover : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float acceleration;

    public void Begin(Vector3 escapeDirection, float startSpeed, float acceleration)
    {
        direction = escapeDirection.sqrMagnitude > 0.001f ? escapeDirection.normalized : Vector3.forward;
        speed = startSpeed;
        this.acceleration = acceleration;
    }

    void Update()
    {
        speed += acceleration * Time.deltaTime;
        transform.position += direction * speed * Time.deltaTime;
    }
}
