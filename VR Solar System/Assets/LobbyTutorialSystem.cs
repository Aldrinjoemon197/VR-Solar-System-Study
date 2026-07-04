using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class LobbyTutorialSystem : MonoBehaviour
{
    public Vector3 lobbyOffsetFromShipSpawn = new Vector3(0f, 2.2f, 5.5f);
    public float orbDistance = 2.4f;

    private bool setupComplete;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimeLobby()
    {
        if (FindObjectOfType<LobbyTutorialSystem>() != null)
        {
            return;
        }

        GameObject root = new GameObject("Runtime Lobby Tutorial System");
        root.AddComponent<LobbyTutorialSystem>();
    }

    void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += SetupLobbyInEditor;
        }
#endif
    }

#if UNITY_EDITOR
    void SetupLobbyInEditor()
    {
        if (this == null || Application.isPlaying)
        {
            return;
        }

        SetupLobby();
    }
#endif

    IEnumerator Start()
    {
        if (!Application.isPlaying)
        {
            yield break;
        }

        yield return null;
        SetupLobby();
    }

    void SetupLobby()
    {
        Vector3 center = GetLobbyCenter();
        Transform sputnik = FindSputnik();

        if (sputnik != null)
        {
            center = sputnik.position + new Vector3(0f, 0.25f, 1.1f);
        }

        if (sputnik == null)
        {
            sputnik = CreateFallbackSputnik(center + new Vector3(0f, 0.55f, 0f));
        }

        RefreshExistingLobbyObjects();

        if (setupComplete && GameObject.Find("Lobby Modes Asteroid") != null && GameObject.Find("Lobby Buttons Asteroid") != null)
        {
            return;
        }

        sputnik = GroupSputnikPrefabRoot(sputnik);
        AddColliderIfNeeded(sputnik.gameObject, 0.9f);

        LobbyShootInfoTarget sputnikInfo = sputnik.gameObject.GetComponent<LobbyShootInfoTarget>();
        if (sputnikInfo == null)
        {
            sputnikInfo = sputnik.gameObject.AddComponent<LobbyShootInfoTarget>();
        }

        sputnikInfo.tabletOffset = new Vector3(0f, 1.45f, 0f);
        sputnikInfo.tabletScale = 1.35f;
        sputnikInfo.Configure(
            "SPUTNIK 1",
            "First artificial satellite launched in 1957. It proved humans could place a machine into Earth orbit.",
            "The solar system is nearby. Learn the controls here, then trigger the wormhole when ready."
        );

        LobbySlowStraightMotion motion = sputnik.gameObject.GetComponent<LobbySlowStraightMotion>();
        if (motion == null)
        {
            motion = sputnik.gameObject.AddComponent<LobbySlowStraightMotion>();
        }

        motion.travelAxis = Vector3.forward;
        motion.travelSpeed = 0.28f;
        motion.rotationSpeed = 10f;

        CreateTutorialOrb(
            "Lobby Controls Orb",
            center + new Vector3(-6.0f, 1.0f, 1.2f),
            new Color(0.15f, 0.85f, 1f, 1f),
            "CONTROLS",
            "Left stick moves. Right stick turns or looks. A moves down. B moves up.",
            "Left grip toggles ship mode. Menu/glove button starts the wormhole."
        );

        CreateTutorialOrb(
            "Lobby Tools Orb",
            center + new Vector3(4.5f, 1.8f, 5.2f),
            new Color(0.20f, 1f, 0.35f, 1f),
            "TOOLS",
            "The right-hand laser selects planets, opens tablets, and can split planet layers.",
            "Use X to scan exposed layers with the wrist hologram."
        );

        CreateTutorialOrb(
            "Lobby Modes Orb",
            center + new Vector3(-2.2f, 0.8f, 8.0f),
            new Color(1f, 0.75f, 0.18f, 1f),
            "MODES",
            "Analysis Mode includes distance, compare, atmospheric gas charts, and moon visuals.",
            "Select two planets to compare them and see both highlighted."
        );

        CreateTutorialAsteroid(
            "Lobby Modes Asteroid",
            center + new Vector3(7.0f, 0.5f, 3.0f),
            0.55f,
            "AVAILABLE MODES",
            "Ship mode lets you pilot the spacecraft. Astronaut mode lets you move freely. Analysis Mode opens distance, compare, gas chart, and moon study tools.",
            "Use the laser to select planets and reveal learning panels."
        );

        CreateTutorialAsteroid(
            "Lobby Buttons Asteroid",
            center + new Vector3(-6.5f, 1.2f, 6.2f),
            0.52f,
            "BUTTON GUIDE",
            "Left stick moves. A/X moves down. B/V moves up. Left grip enters or exits spacecraft mode. X shows the layer hologram. Y opens Solar System Analysis.",
            "Menu button creates the wormhole. Right trigger shoots. Left trigger shows the laser ray."
        );

        setupComplete = true;
    }

    Vector3 GetLobbyCenter()
    {
        Transform sputnik = FindTransformByNameContains("sputnik");

        if (sputnik != null)
        {
            return sputnik.position + new Vector3(0f, 0.25f, 1.1f);
        }

        Transform ship = FindTransformByNameContains("explorerspaceship");

        if (ship != null)
        {
            return ship.position + new Vector3(0f, 1.4f, 2.6f);
        }

        GameObject spawn = GameObject.Find("IntroStartPoint");

        if (spawn != null)
        {
            return spawn.transform.position + new Vector3(0f, 1.6f, 4f);
        }

        GameObject xrOrigin = GameObject.Find("XR Origin (VR)");

        if (xrOrigin != null)
        {
            return xrOrigin.transform.position + new Vector3(0f, 1.6f, 4f);
        }

        return new Vector3(0f, 2f, 0f);
    }

    Transform FindTransformByNameContains(string namePart)
    {
        Transform[] all = FindObjectsOfType<Transform>();
        string target = namePart.ToLower();

        foreach (Transform t in all)
        {
            if (t != null && t.name.ToLower().Replace(" ", "").Contains(target))
            {
                return t;
            }
        }

        return null;
    }

    Transform GroupSputnikPrefabRoot(Transform sputnik)
    {
        if (sputnik == null || sputnik.parent == null)
        {
            return sputnik;
        }

        Transform parent = sputnik.parent;
        string parentName = parent.name.ToLower();

        if (parentName.Contains("sputnik"))
        {
            return parent;
        }

        if (parent.childCount < 2 || parent.childCount > 12)
        {
            return sputnik;
        }

        GameObject group = new GameObject("Sputnik Lobby Group");
        group.transform.position = parent.position;
        group.transform.rotation = parent.rotation;
        group.transform.localScale = Vector3.one;

        Transform[] children = new Transform[parent.childCount];
        for (int i = 0; i < parent.childCount; i++)
        {
            children[i] = parent.GetChild(i);
        }

        foreach (Transform child in children)
        {
            child.SetParent(group.transform, true);
        }

        return group.transform;
    }

    Transform FindSputnik()
    {
        Transform[] all = FindObjectsOfType<Transform>();

        foreach (Transform t in all)
        {
            if (t != null && t.name.ToLower().Contains("sputnik"))
            {
                return t;
            }
        }

        return null;
    }

    Transform CreateFallbackSputnik(Vector3 position)
    {
        GameObject root = new GameObject("Sputnik Lobby Satellite");
        root.transform.position = position;
        root.transform.localScale = Vector3.one;

        Material bodyMat = CreateMaterial(new Color(0.75f, 0.78f, 0.82f, 1f), false);
        Material antennaMat = CreateMaterial(new Color(0.15f, 0.85f, 1f, 1f), true);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Sputnik Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = Vector3.one * 0.42f;
        body.GetComponent<Renderer>().material = bodyMat;
        Destroy(body.GetComponent<Collider>());

        Vector3[] directions = new Vector3[]
        {
            new Vector3(1f, 0.35f, 0.65f).normalized,
            new Vector3(-1f, 0.35f, 0.65f).normalized,
            new Vector3(1f, -0.35f, -0.65f).normalized,
            new Vector3(-1f, -0.35f, -0.65f).normalized
        };

        for (int i = 0; i < directions.Length; i++)
        {
            GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antenna.name = "Sputnik Antenna " + (i + 1);
            antenna.transform.SetParent(root.transform);
            SetCylinderBetween(antenna.transform, directions[i] * 0.22f, directions[i] * 1.25f, 0.018f);
            antenna.GetComponent<Renderer>().material = antennaMat;
            Destroy(antenna.GetComponent<Collider>());
        }

        return root.transform;
    }

    void CreateTutorialAsteroid(string objectName, Vector3 position, float radius, string title, string body, string footer)
    {
        if (GameObject.Find(objectName) != null)
        {
            return;
        }

        GameObject asteroid = new GameObject(objectName);
        asteroid.transform.position = position;
        asteroid.transform.localScale = Vector3.one;

        MeshFilter meshFilter = asteroid.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateAsteroidMesh(radius);

        MeshRenderer renderer = asteroid.AddComponent<MeshRenderer>();
        renderer.material = CreateStoneMaterial();

        MeshCollider collider = asteroid.AddComponent<MeshCollider>();
        collider.sharedMesh = meshFilter.mesh;
        collider.convex = true;

        LobbyShootInfoTarget target = asteroid.AddComponent<LobbyShootInfoTarget>();
        target.tabletOffset = new Vector3(0f, 1.15f, 0f);
        target.tabletScale = 1.15f;
        target.Configure(title, body, footer);

        LobbyFloatingOrbMotion motion = asteroid.AddComponent<LobbyFloatingOrbMotion>();
        motion.basePosition = position;
        motion.floatAmplitude = 0.45f;
        motion.floatSpeed = 0.62f;
        motion.orbitRadius = 0.42f;
        motion.sideAmplitude = 0.32f;
        motion.forwardAmplitude = 0.38f;
        motion.driftSpeed = 0.55f;
    }

    Mesh CreateAsteroidMesh(float radius)
    {
        Vector3[] baseVertices = new Vector3[]
        {
            new Vector3(-1f, -1f, -1f), new Vector3(1f, -1f, -1f),
            new Vector3(1f, -1f, 1f), new Vector3(-1f, -1f, 1f),
            new Vector3(-1f, 1f, -1f), new Vector3(1f, 1f, -1f),
            new Vector3(1f, 1f, 1f), new Vector3(-1f, 1f, 1f),
            new Vector3(0f, -1.45f, 0f), new Vector3(0f, 1.35f, 0f),
            new Vector3(-1.35f, 0f, 0f), new Vector3(1.25f, 0f, 0f),
            new Vector3(0f, 0f, -1.30f), new Vector3(0f, 0f, 1.40f)
        };

        for (int i = 0; i < baseVertices.Length; i++)
        {
            float lump = 0.82f + Mathf.PerlinNoise(i * 0.73f, i * 1.19f) * 0.36f;
            baseVertices[i] = baseVertices[i].normalized * radius * lump;
        }

        int[] triangles = new int[]
        {
            8, 1, 0, 8, 2, 1, 8, 3, 2, 8, 0, 3,
            9, 4, 5, 9, 5, 6, 9, 6, 7, 9, 7, 4,
            10, 0, 4, 10, 4, 7, 10, 7, 3, 10, 3, 0,
            11, 5, 1, 11, 6, 5, 11, 2, 6, 11, 1, 2,
            12, 0, 1, 12, 1, 5, 12, 5, 4, 12, 4, 0,
            13, 2, 3, 13, 6, 2, 13, 7, 6, 13, 3, 7
        };

        Mesh mesh = new Mesh();
        mesh.name = "Procedural Stone Asteroid Mesh";
        mesh.vertices = baseVertices;
        mesh.triangles = triangles;
        mesh.uv = CreateAsteroidUVs(baseVertices);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    Vector2[] CreateAsteroidUVs(Vector3[] vertices)
    {
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = vertices[i].normalized;
            float u = 0.5f + Mathf.Atan2(p.z, p.x) / (Mathf.PI * 2f);
            float v = 0.5f - Mathf.Asin(Mathf.Clamp(p.y, -1f, 1f)) / Mathf.PI;
            uvs[i] = new Vector2(u, v);
        }

        return uvs;
    }

    void RefreshExistingLobbyObjects()
    {
        RefreshExistingAsteroid("Lobby Modes Asteroid");
        EnsureInfoTarget(
            GameObject.Find("Lobby Modes Asteroid"),
            "AVAILABLE MODES",
            "Ship mode lets you pilot the spacecraft. Astronaut mode lets you move freely. Analysis Mode opens distance, compare, gas chart, and moon study tools.",
            "Use the laser to select planets and reveal learning panels.",
            new Vector3(0f, 1.15f, 0f),
            1.35f
        );

        RefreshExistingAsteroid("Lobby Buttons Asteroid");
        EnsureInfoTarget(
            GameObject.Find("Lobby Buttons Asteroid"),
            "BUTTON GUIDE",
            "Left stick moves. A/X moves down. B/V moves up. Left grip enters or exits spacecraft mode. X shows the layer hologram. Y opens Solar System Analysis.",
            "Menu button creates the wormhole. Right trigger shoots. Left trigger shows the laser ray.",
            new Vector3(0f, 1.15f, 0f),
            1.35f
        );

        RefreshExistingFloatingObject("Lobby Controls Orb", false);
        EnsureInfoTarget(
            GameObject.Find("Lobby Controls Orb"),
            "CONTROLS",
            "Left stick moves. Right stick turns or looks. A moves down. B moves up.",
            "Left grip toggles ship mode. Menu/glove button starts the wormhole.",
            new Vector3(0f, 1.0f, 0f),
            1.25f
        );

        RefreshExistingFloatingObject("Lobby Tools Orb", false);
        EnsureInfoTarget(
            GameObject.Find("Lobby Tools Orb"),
            "TOOLS",
            "The right-hand laser selects planets, opens tablets, and can split planet layers.",
            "Use X to scan exposed layers with the wrist hologram.",
            new Vector3(0f, 1.0f, 0f),
            1.25f
        );

        RefreshExistingFloatingObject("Lobby Modes Orb", false);
        EnsureInfoTarget(
            GameObject.Find("Lobby Modes Orb"),
            "MODES",
            "Analysis Mode includes distance, compare, atmospheric gas charts, and moon visuals.",
            "Select two planets to compare them and see both highlighted.",
            new Vector3(0f, 1.0f, 0f),
            1.25f
        );
    }

    void EnsureInfoTarget(GameObject obj, string title, string body, string footer, Vector3 tabletOffset, float tabletScale)
    {
        if (obj == null)
        {
            return;
        }

        LobbyShootInfoTarget target = obj.GetComponent<LobbyShootInfoTarget>();

        if (target == null)
        {
            target = obj.AddComponent<LobbyShootInfoTarget>();
        }

        target.tabletOffset = tabletOffset;
        target.tabletScale = tabletScale;
        target.Configure(title, body, footer);
    }

    void RefreshExistingAsteroid(string objectName)
    {
        GameObject asteroid = GameObject.Find(objectName);

        if (asteroid == null)
        {
            return;
        }

        MeshFilter meshFilter = asteroid.GetComponent<MeshFilter>();

        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Mesh mesh = meshFilter.sharedMesh;

            if (mesh.uv == null || mesh.uv.Length != mesh.vertexCount)
            {
                mesh.uv = CreateAsteroidUVs(mesh.vertices);
            }

            mesh.RecalculateBounds();
        }

        MeshCollider collider = asteroid.GetComponent<MeshCollider>();

        if (collider != null && meshFilter != null)
        {
            collider.sharedMesh = meshFilter.sharedMesh;
            collider.convex = true;
        }

        RefreshExistingFloatingObject(objectName, true);
    }

    void RefreshExistingFloatingObject(string objectName, bool asteroidMotion)
    {
        GameObject obj = GameObject.Find(objectName);

        if (obj == null)
        {
            return;
        }

        LobbyFloatingOrbMotion motion = obj.GetComponent<LobbyFloatingOrbMotion>();

        if (motion == null)
        {
            motion = obj.AddComponent<LobbyFloatingOrbMotion>();
        }

        if (!Application.isPlaying)
        {
            motion.basePosition = obj.transform.position;
        }

        motion.floatAmplitude = asteroidMotion ? 0.45f : 0.55f;
        motion.floatSpeed = asteroidMotion ? 0.62f : 0.72f;
        motion.orbitRadius = asteroidMotion ? 0.42f : 0.55f;
        motion.sideAmplitude = asteroidMotion ? 0.32f : 0.42f;
        motion.forwardAmplitude = asteroidMotion ? 0.38f : 0.50f;
        motion.driftSpeed = asteroidMotion ? 0.55f : 0.70f;
        motion.ResetMotionDirection();
    }

    Material CreateStoneMaterial()
    {
        Material material = CreateMaterial(new Color(0.34f, 0.31f, 0.27f, 1f), false);
        Texture2D texture = new Texture2D(32, 32);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.18f, y * 0.18f);
                Color c = Color.Lerp(new Color(0.20f, 0.19f, 0.17f), new Color(0.55f, 0.52f, 0.45f), noise);
                texture.SetPixel(x, y, c);
            }
        }

        texture.Apply();

        if (material.HasProperty("_BaseColorMap")) material.SetTexture("_BaseColorMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        return material;
    }

    void CreateTutorialOrb(string objectName, Vector3 position, Color color, string title, string body, string footer)
    {
        if (GameObject.Find(objectName) != null)
        {
            return;
        }

        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = objectName;
        orb.transform.position = position;
        orb.transform.localScale = Vector3.one * 0.42f;
        orb.GetComponent<Renderer>().material = CreateMaterial(color, true);

        LobbyShootInfoTarget target = orb.AddComponent<LobbyShootInfoTarget>();
        target.Configure(title, body, footer);

        LobbyFloatingOrbMotion motion = orb.AddComponent<LobbyFloatingOrbMotion>();
        motion.basePosition = position;
        motion.floatAmplitude = 0.55f;
        motion.floatSpeed = 0.72f;
        motion.orbitRadius = 0.55f;
        motion.sideAmplitude = 0.42f;
        motion.forwardAmplitude = 0.50f;
        motion.driftSpeed = 0.70f;
    }

    void AddColliderIfNeeded(GameObject obj, float radius)
    {
        if (obj == null || obj.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        SphereCollider collider = obj.AddComponent<SphereCollider>();
        collider.radius = radius;
        collider.isTrigger = false;
    }

    Material CreateMaterial(Color color, bool emission)
    {
        Shader shader = Shader.Find("HDRP/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("HDRP/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        SetMaterialColor(material, color, emission);
        return material;
    }

    public static void SetMaterialColor(Material material, Color color, bool emission)
    {
        if (material == null) return;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_UnlitColor")) material.SetColor("_UnlitColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }
    }

    static void SetCylinderBetween(Transform cylinder, Vector3 start, Vector3 end, float radius)
    {
        Vector3 direction = end - start;
        cylinder.localPosition = start + direction * 0.5f;
        cylinder.localScale = new Vector3(radius, direction.magnitude * 0.5f, radius);
        cylinder.up = direction.normalized;
    }
}

public class LobbyShootInfoTarget : MonoBehaviour
{
    public string title = "INFO";
    public string body = "";
    public string footer = "";
    public Vector3 tabletOffset = new Vector3(0f, 1.0f, 0f);
    public float tabletScale = 1f;
    public bool showTabletOnStart = false;
    public float tabletWorldPadding = 0.75f;

    private GameObject tabletRoot;
    private float nextAllowedHitTime;

    public void Configure(string newTitle, string newBody, string newFooter)
    {
        title = newTitle;
        body = newBody;
        footer = newFooter;
    }

    void Awake()
    {
        CacheExistingTablet();
        HideTablet();
    }

    void Start()
    {
        CacheExistingTablet();

        if (showTabletOnStart)
        {
            ShowTablet();
        }
        else
        {
            HideTablet();
        }
    }

    public void OnLaserHit()
    {
        ToggleTablet();
    }

    public void OnLaserHit(RaycastHit hit)
    {
        ToggleTablet();
    }

    void CacheExistingTablet()
    {
        if (tabletRoot != null)
        {
            return;
        }

        tabletRoot = GameObject.Find(name + " Info Tablet");
    }

    void HideTablet()
    {
        if (tabletRoot != null)
        {
            tabletRoot.SetActive(false);
        }
    }

    void ToggleTablet()
    {
        if (Time.time < nextAllowedHitTime)
        {
            return;
        }

        nextAllowedHitTime = Time.time + 0.35f;

        if (tabletRoot == null)
        {
            BuildTablet();
        }

        tabletRoot.SetActive(!tabletRoot.activeSelf);
    }

    void ShowTablet()
    {
        if (tabletRoot == null)
        {
            BuildTablet();
        }

        tabletRoot.SetActive(true);
    }

    void LateUpdate()
    {
        if (tabletRoot == null || !tabletRoot.activeSelf)
        {
            return;
        }

        tabletRoot.transform.position = GetTabletWorldPosition();

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 toCamera = tabletRoot.transform.position - cam.transform.position;
            if (toCamera.sqrMagnitude > 0.001f)
            {
                tabletRoot.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
            }
        }
    }

    void BuildTablet()
    {
        tabletRoot = new GameObject(name + " Info Tablet");
        tabletRoot.transform.position = GetTabletWorldPosition();
        tabletRoot.transform.localScale = Vector3.one * tabletScale;

        Material panelMat = CreateMaterial(new Color(0.015f, 0.022f, 0.034f, 0.92f), true);
        Material borderMat = CreateMaterial(new Color(0.10f, 0.85f, 1f, 1f), true);

        CreateCube("Tablet Panel", tabletRoot.transform, Vector3.zero, new Vector3(3.20f, 1.55f, 0.025f), panelMat);
        CreateCube("Tablet Top Border", tabletRoot.transform, new Vector3(0f, 0.805f, -0.018f), new Vector3(3.30f, 0.050f, 0.025f), borderMat);
        CreateCube("Tablet Bottom Border", tabletRoot.transform, new Vector3(0f, -0.805f, -0.018f), new Vector3(3.30f, 0.050f, 0.025f), borderMat);
        CreateCube("Tablet Left Border", tabletRoot.transform, new Vector3(-1.645f, 0f, -0.018f), new Vector3(0.050f, 1.60f, 0.025f), borderMat);
        CreateCube("Tablet Right Border", tabletRoot.transform, new Vector3(1.645f, 0f, -0.018f), new Vector3(0.050f, 1.60f, 0.025f), borderMat);

        CreateText("Tablet Title", title, new Vector3(0f, 0.555f, -0.045f), 0.060f, 64, TextAnchor.MiddleCenter);
        CreateText("Tablet Body", WrapText(body, 58, 5), new Vector3(0f, 0.110f, -0.045f), 0.036f, 44, TextAnchor.MiddleCenter);
        CreateText("Tablet Footer", WrapText(footer, 60, 2), new Vector3(0f, -0.550f, -0.045f), 0.032f, 38, TextAnchor.MiddleCenter);

        tabletRoot.SetActive(showTabletOnStart);
    }

    Vector3 GetTabletWorldPosition()
    {
        Bounds bounds;

        if (TryGetVisualBounds(out bounds))
        {
            return new Vector3(bounds.center.x, bounds.max.y + tabletWorldPadding, bounds.center.z) + tabletOffset;
        }

        return transform.position + tabletOffset + Vector3.up * tabletWorldPadding;
    }

    bool TryGetVisualBounds(out Bounds bounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        bool found = false;
        bounds = new Bounds(transform.position, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (tabletRoot != null && (renderer.transform == tabletRoot.transform || renderer.transform.IsChildOf(tabletRoot.transform)))
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    GameObject CreateCube(string objectName, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent);
        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = localScale;
        cube.GetComponent<Renderer>().material = material;
        Destroy(cube.GetComponent<Collider>());
        return cube;
    }

    void CreateText(string objectName, string value, Vector3 localPosition, float size, int fontSize, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(tabletRoot.transform);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.identity;
        textObject.transform.localScale = Vector3.one;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = value;
        text.fontSize = fontSize;
        text.characterSize = size;
        text.anchor = anchor;
        text.alignment = TextAlignment.Center;
        text.color = Color.white;
    }

    string WrapText(string text, int maxCharactersPerLine, int maxLines)
    {
        if (string.IsNullOrEmpty(text)) return "";

        string[] words = text.Split(' ');
        string result = "";
        string line = "";
        int lineCount = 0;

        foreach (string word in words)
        {
            string candidate = string.IsNullOrEmpty(line) ? word : line + " " + word;

            if (candidate.Length > maxCharactersPerLine && !string.IsNullOrEmpty(line))
            {
                if (lineCount > 0) result += "\n";
                result += line;
                lineCount++;
                line = word;

                if (lineCount >= maxLines) return result;
            }
            else
            {
                line = candidate;
            }
        }

        if (!string.IsNullOrEmpty(line) && lineCount < maxLines)
        {
            if (lineCount > 0) result += "\n";
            result += line;
        }

        return result;
    }

    Material CreateMaterial(Color color, bool emission)
    {
        Shader shader = Shader.Find("HDRP/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("HDRP/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader);
        LobbyTutorialSystem.SetMaterialColor(material, color, emission);
        return material;
    }
}

public class LobbyFloatingOrbMotion : MonoBehaviour
{
    public Vector3 basePosition;
    public float floatAmplitude = 0.25f;
    public float floatSpeed = 0.7f;
    public float orbitRadius = 0.18f;
    public float sideAmplitude = 0.25f;
    public float forwardAmplitude = 0.25f;
    public float driftSpeed = 0.55f;

    private float phase;
    private Vector3 sideDirection;
    private Vector3 forwardDirection;
    private Vector3 moveDirection;
    private Vector3 previousWiggleOffset;

    void OnEnable()
    {
        basePosition = transform.position;
        phase = Random.Range(0f, 10f);
        ResetMotionDirection();
    }

    void Start()
    {
        basePosition = transform.position;
        ResetMotionDirection();
    }

    public void ResetMotionDirection()
    {
        sideDirection = new Vector3(Mathf.Cos(phase), 0f, Mathf.Sin(phase)).normalized;
        forwardDirection = new Vector3(-sideDirection.z, 0f, sideDirection.x).normalized;
        moveDirection = (forwardDirection + Vector3.up * 0.12f + sideDirection * 0.25f).normalized;
        previousWiggleOffset = Vector3.zero;
    }

    void Update()
    {
        float t = Time.time * floatSpeed + phase;
        Vector3 wiggleOffset =
            sideDirection * (Mathf.Sin(t * 0.73f) * sideAmplitude) +
            forwardDirection * (Mathf.Cos(t * 0.59f) * forwardAmplitude) +
            Vector3.up * (Mathf.Sin(t * 0.91f) * floatAmplitude);

        Vector3 wiggleDelta = wiggleOffset - previousWiggleOffset;
        previousWiggleOffset = wiggleOffset;

        transform.position += moveDirection * driftSpeed * Time.deltaTime + wiggleDelta;
        transform.Rotate(new Vector3(17f, 29f, 11f), 18f * Time.deltaTime, Space.Self);
    }
}

public class LobbySlowStraightMotion : MonoBehaviour
{
    public Vector3 travelAxis = Vector3.right;
    public float travelSpeed = 0.28f;
    public float rotationSpeed = 12f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        if (travelAxis.sqrMagnitude < 0.001f)
        {
            travelAxis = Vector3.right;
        }

        travelAxis.Normalize();
    }

    void Update()
    {
        transform.position += travelAxis * travelSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}

#if UNITY_EDITOR
[InitializeOnLoad]
public static class LobbyTutorialEditorBootstrap
{
    static LobbyTutorialEditorBootstrap()
    {
        EditorApplication.delayCall += EnsureLobbySystemInOpenScene;
    }

    static void EnsureLobbySystemInOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (UnityEngine.Object.FindObjectOfType<LobbyTutorialSystem>() != null)
        {
            return;
        }

        if (GameObject.Find("Sputnik1") == null && GameObject.Find("ExplorerSpaceship") == null)
        {
            return;
        }

        GameObject root = new GameObject("Runtime Lobby Tutorial System");
        root.AddComponent<LobbyTutorialSystem>();
        EditorSceneManager.MarkSceneDirty(root.scene);
    }
}
#endif
