using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class VenusHalfPlanetSplitter : MonoBehaviour
{
    [Header("Mesh Smoothness")]
    public int segments = 64;
    public int rings = 32;

    [Header("Which Half Flies Away")]
    public bool rightHalfShouldFly = true;

    [Header("Split Motion")]
    public float smallOpeningDistance = 0.15f;

    [Tooltip("How far the flying half visibly moves away. This is large so it goes out of view.")]
    public float flyingHalfMoveDistance = 22.0f;

    [Tooltip("How long the flying movement takes.")]
    public float flyingHalfMoveDuration = 2.2f;

    [Tooltip("How much the flying half rotates while moving away.")]
    public float flyingHalfSpinDegrees = 420f;

    [Tooltip("Destroy the flying half after it has gone far away.")]
    public bool destroyFlyingHalfAfterMove = true;

    [Header("Venus Layer Radius Ratios")]
    [Tooltip("Outer atmosphere radius compared to Venus radius.")]
    public float atmosphereOuterRatio = 1.08f;

    [Tooltip("Inner side of the carbon dioxide atmosphere shell.")]
    public float atmosphereInnerRatio = 1.035f;

    [Tooltip("Inner side of the sulfuric acid cloud shell.")]
    public float cloudInnerRatio = 1.005f;

    [Tooltip("Inner side of the crust shell.")]
    public float crustInnerRatio = 0.92f;

    [Tooltip("Inner side of the rocky mantle shell. Metallic core starts inside this.")]
    public float mantleInnerRatio = 0.36f;

    [Header("Layer Colors")]
    public Color carbonDioxideAtmosphereColor = new Color(0.95f, 0.82f, 0.48f, 0.28f);
    public Color sulfuricAcidCloudColor = new Color(1.00f, 0.86f, 0.38f, 0.45f);
    public Color crustColor = new Color(0.82f, 0.62f, 0.34f, 1f);
    public Color rockyMantleColor = new Color(0.92f, 0.45f, 0.18f, 1f);
    public Color metallicCoreColor = new Color(0.82f, 0.25f, 0.08f, 1f);

    [Header("Outer Surface")]
    public bool keepOriginalVenusTexture = true;

    [Tooltip("Venus texture is placed outside all generated shells so the original Venus image stays visible.")]
    public float outerSurfaceScaleBoost = 1.012f;

    [Header("Tablet")]
    public bool showTabletAfterSplit = true;
    public float tabletOpenDelay = 0.25f;
    public Vector3 tabletLocalOffset = new Vector3(0.85f, 0.25f, 0f);
    public float tabletScale = 0.65f;

    [Header("Optional Test")]
    public bool allowPKeyTest = false;

    private bool alreadySplit = false;

    void Update()
    {
        if (allowPKeyTest && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            SplitInHalf(transform.forward);
        }
    }

    public void SplitInHalf(Vector3 laserDirectionWorld)
    {
        if (alreadySplit)
        {
            return;
        }

        alreadySplit = true;

        Debug.Log("Venus split triggered on: " + gameObject.name);

        MeshFilter originalMeshFilter = GetComponent<MeshFilter>();
        Renderer originalRenderer = GetComponent<Renderer>();

        if (originalMeshFilter == null)
        {
            originalMeshFilter = GetComponentInChildren<MeshFilter>();
        }

        if (originalRenderer == null)
        {
            originalRenderer = GetComponentInChildren<Renderer>();
        }

        float radius = 0.5f;

        if (originalMeshFilter != null && originalMeshFilter.sharedMesh != null)
        {
            Bounds bounds = originalMeshFilter.sharedMesh.bounds;
            radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        }

        ClampRatios();

        Material atmosphereMat = CreateLayerMaterial(carbonDioxideAtmosphereColor, true);
        Material cloudMat = CreateLayerMaterial(sulfuricAcidCloudColor, true);
        Material crustMat = CreateLayerMaterial(crustColor, false);
        Material mantleMat = CreateLayerMaterial(rockyMantleColor, false);
        Material coreMat = CreateLayerMaterial(metallicCoreColor, false);

        Material venusSurfaceMat = null;

        if (keepOriginalVenusTexture && originalRenderer != null && originalRenderer.sharedMaterial != null)
        {
            venusSurfaceMat = new Material(originalRenderer.sharedMaterial);
        }

        Vector3 splitAxis = transform.right.normalized;

        GameObject leftHalf = CreateLayeredVenusHalf(
            gameObject.name + "_Left_Half",
            radius,
            false,
            venusSurfaceMat,
            atmosphereMat,
            cloudMat,
            crustMat,
            mantleMat,
            coreMat
        );

        GameObject rightHalf = CreateLayeredVenusHalf(
            gameObject.name + "_Right_Half",
            radius,
            true,
            venusSurfaceMat,
            atmosphereMat,
            cloudMat,
            crustMat,
            mantleMat,
            coreMat
        );

        float worldScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        float opening = smallOpeningDistance * worldScale;

        GameObject staticHalf;

        if (rightHalfShouldFly)
        {
            leftHalf.transform.position = transform.position;
            rightHalf.transform.position = transform.position + splitAxis * opening;

            staticHalf = leftHalf;
            MakeFlyingHalf(rightHalf, splitAxis);
        }
        else
        {
            rightHalf.transform.position = transform.position;
            leftHalf.transform.position = transform.position - splitAxis * opening;

            staticHalf = rightHalf;
            MakeFlyingHalf(leftHalf, -splitAxis);
        }

        AddStaticHalfCollider(staticHalf, radius * atmosphereOuterRatio);

        HideOriginalPlanet();

        if (showTabletAfterSplit)
        {
            StartCoroutine(CreateTabletAfterDelay(staticHalf));
        }

        Destroy(gameObject, 0.2f);
    }

    void ClampRatios()
    {
        atmosphereOuterRatio = Mathf.Clamp(atmosphereOuterRatio, 1.01f, 1.25f);
        atmosphereInnerRatio = Mathf.Clamp(atmosphereInnerRatio, 1.005f, atmosphereOuterRatio - 0.005f);
        cloudInnerRatio = Mathf.Clamp(cloudInnerRatio, 1.000f, atmosphereInnerRatio - 0.005f);
        crustInnerRatio = Mathf.Clamp(crustInnerRatio, 0.75f, 0.98f);
        mantleInnerRatio = Mathf.Clamp(mantleInnerRatio, 0.15f, crustInnerRatio - 0.05f);
    }

    GameObject CreateLayeredVenusHalf(
        string rootName,
        float radius,
        bool positiveXHalf,
        Material venusSurfaceMat,
        Material atmosphereMat,
        Material cloudMat,
        Material crustMat,
        Material mantleMat,
        Material coreMat
    )
    {
        GameObject root = new GameObject(rootName);

        root.transform.position = transform.position;
        root.transform.rotation = transform.rotation;
        root.transform.localScale = transform.lossyScale;

        float venusRadius = radius;
        float atmosphereOuter = radius * atmosphereOuterRatio;
        float atmosphereInner = radius * atmosphereInnerRatio;
        float cloudInner = radius * cloudInnerRatio;
        float crustInner = radius * crustInnerRatio;
        float mantleInner = radius * mantleInnerRatio;

        CreateShellPart(
            "CarbonDioxideAtmosphere",
            root.transform,
            atmosphereOuter,
            atmosphereInner,
            positiveXHalf,
            atmosphereMat
        );

        CreateShellPart(
            "SulfuricAcidCloudLayer",
            root.transform,
            atmosphereInner,
            cloudInner,
            positiveXHalf,
            cloudMat
        );

        CreateShellPart(
            "Crust",
            root.transform,
            venusRadius,
            crustInner,
            positiveXHalf,
            crustMat
        );

        CreateShellPart(
            "RockyMantle",
            root.transform,
            crustInner,
            mantleInner,
            positiveXHalf,
            mantleMat
        );

        CreateSolidPart(
            "MetallicCore",
            root.transform,
            mantleInner,
            positiveXHalf,
            coreMat
        );

        // IMPORTANT FIX:
        // Put the original Venus texture OUTSIDE all generated shells.
        // This keeps the original Venus image visible after splitting.
        if (venusSurfaceMat != null)
        {
            CreateOuterSurfacePart(
                "VenusSurfaceTexture",
                root.transform,
                atmosphereOuter * outerSurfaceScaleBoost,
                positiveXHalf,
                venusSurfaceMat
            );
        }

        return root;
    }

    IEnumerator CreateTabletAfterDelay(GameObject staticHalf)
    {
        yield return null;

        if (tabletOpenDelay > 0f)
        {
            yield return new WaitForSeconds(tabletOpenDelay);
        }

        if (staticHalf != null)
        {
            CreateVenusTablet(staticHalf.transform);
        }
    }

    void CreateVenusTablet(Transform parent)
    {
        GameObject tablet = new GameObject("Venus Layer Tablet");
        tablet.transform.SetParent(parent);
        tablet.transform.localPosition = tabletLocalOffset;
        tablet.transform.localRotation = Quaternion.identity;
        tablet.transform.localScale = Vector3.one * tabletScale;

        Material tabletMat = CreateLayerMaterial(new Color(0.02f, 0.025f, 0.035f, 0.88f), true);
        Material borderMat = CreateLayerMaterial(new Color(1f, 0.78f, 0.25f, 1f), true);

        CreateCube(
            "Tablet Background",
            tablet.transform,
            Vector3.zero,
            Vector3.zero,
            new Vector3(0.95f, 0.58f, 0.025f),
            tabletMat
        );

        CreateCube("Tablet Top Border", tablet.transform, new Vector3(0f, 0.305f, -0.015f), Vector3.zero, new Vector3(0.98f, 0.025f, 0.025f), borderMat);
        CreateCube("Tablet Bottom Border", tablet.transform, new Vector3(0f, -0.305f, -0.015f), Vector3.zero, new Vector3(0.98f, 0.025f, 0.025f), borderMat);
        CreateCube("Tablet Left Border", tablet.transform, new Vector3(-0.50f, 0f, -0.015f), Vector3.zero, new Vector3(0.025f, 0.60f, 0.025f), borderMat);
        CreateCube("Tablet Right Border", tablet.transform, new Vector3(0.50f, 0f, -0.015f), Vector3.zero, new Vector3(0.025f, 0.60f, 0.025f), borderMat);

        TextMesh text = CreateText(
            "Venus Tablet Text",
            tablet.transform,
            new Vector3(-0.43f, 0.20f, -0.04f),
            0.045f,
            TextAnchor.UpperLeft
        );

        text.text =
            "VENUS LAYERS\n" +
            "\n" +
            "CO2 Atmosphere\n" +
            "Sulfuric Acid Clouds\n" +
            "Crust\n" +
            "Rocky Mantle\n" +
            "Metallic Core";

        text.color = Color.white;

        VenusTabletBillboard billboard = tablet.AddComponent<VenusTabletBillboard>();
        billboard.keepUpright = true;
    }

    TextMesh CreateText(string name, Transform parent, Vector3 localPosition, float characterSize, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.identity;
        textObject.transform.localScale = Vector3.one;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.fontSize = 80;
        text.characterSize = characterSize;
        text.anchor = anchor;
        text.alignment = TextAlignment.Left;
        text.color = Color.white;

        return text;
    }

    void AddStaticHalfCollider(GameObject half, float radius)
    {
        SphereCollider collider = half.AddComponent<SphereCollider>();
        collider.isTrigger = false;
        collider.center = Vector3.zero;
        collider.radius = radius;
    }

    void MakeFlyingHalf(GameObject half, Vector3 flyDirection)
    {
        // IMPORTANT FIX:
        // The previous version used a coroutine on the original Venus object.
        // But the original Venus is destroyed after splitting, so that coroutine can stop early.
        // This mover is added to the NEW flying half itself, so it keeps moving even after the original is destroyed.
        VenusFlyingHalfMover mover = half.AddComponent<VenusFlyingHalfMover>();
        mover.Begin(
            flyDirection.normalized,
            flyingHalfMoveDistance,
            flyingHalfMoveDuration,
            flyingHalfSpinDegrees,
            destroyFlyingHalfAfterMove
        );

        Debug.Log(half.name + " will fly far away.");
    }

    void HideOriginalPlanet()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }
    }

    void CreateShellPart(string name, Transform parent, float outerRadius, float innerRadius, bool positiveXHalf, Material material)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent);
        part.transform.localPosition = Vector3.zero;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one;

        MeshFilter mf = part.AddComponent<MeshFilter>();
        MeshRenderer mr = part.AddComponent<MeshRenderer>();

        mf.mesh = CreateHalfShellMesh(outerRadius, innerRadius, positiveXHalf);
        mr.material = material;
    }

    void CreateSolidPart(string name, Transform parent, float radius, bool positiveXHalf, Material material)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent);
        part.transform.localPosition = Vector3.zero;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one;

        MeshFilter mf = part.AddComponent<MeshFilter>();
        MeshRenderer mr = part.AddComponent<MeshRenderer>();

        mf.mesh = CreateSolidHalfSphereMesh(radius, positiveXHalf);
        mr.material = material;
    }

    void CreateOuterSurfacePart(string name, Transform parent, float radius, bool positiveXHalf, Material material)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent);
        part.transform.localPosition = Vector3.zero;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one;

        MeshFilter mf = part.AddComponent<MeshFilter>();
        MeshRenderer mr = part.AddComponent<MeshRenderer>();

        mf.mesh = CreateOuterSurfaceHalfMesh(radius, positiveXHalf);
        mr.material = material;
    }

    GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 localRotation, Vector3 localScale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.Euler(localRotation);
        obj.transform.localScale = localScale;
        obj.GetComponent<Renderer>().material = material;

        Collider col = obj.GetComponent<Collider>();

        if (col != null)
        {
            Destroy(col);
        }

        return obj;
    }

    Material CreateLayerMaterial(Color color, bool transparent)
    {
        Shader shader = Shader.Find("HDRP/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);

        SetMaterialColor(mat, color);

        if (transparent)
        {
            mat.renderQueue = 3000;

            if (mat.HasProperty("_SurfaceType"))
            {
                mat.SetFloat("_SurfaceType", 1f);
            }

            if (mat.HasProperty("_BlendMode"))
            {
                mat.SetFloat("_BlendMode", 0f);
            }

            if (mat.HasProperty("_ZWrite"))
            {
                mat.SetFloat("_ZWrite", 0f);
            }

            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHABLEND_ON");
        }

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.2f);
        }

        return mat;
    }

    void SetMaterialColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        if (mat.HasProperty("_UnlitColor"))
        {
            mat.SetColor("_UnlitColor", color);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }
    }

    Mesh CreateOuterSurfaceHalfMesh(float radius, bool positiveXHalf)
    {
        Mesh mesh = new Mesh();
        mesh.name = positiveXHalf ? "Right Venus Outer Surface" : "Left Venus Outer Surface";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int safeSegments = Mathf.Max(12, segments);
        int safeRings = Mathf.Max(6, rings);

        for (int i = 0; i <= safeRings; i++)
        {
            float theta = (Mathf.PI * 0.5f) * i / safeRings;

            float x = radius * Mathf.Cos(theta);
            float ringRadius = radius * Mathf.Sin(theta);

            if (!positiveXHalf)
            {
                x = -x;
            }

            for (int j = 0; j <= safeSegments; j++)
            {
                float phi = Mathf.PI * 2f * j / safeSegments;

                float y = ringRadius * Mathf.Cos(phi);
                float z = ringRadius * Mathf.Sin(phi);

                vertices.Add(new Vector3(x, y, z));

                float u = (Mathf.Atan2(z, x) / (2f * Mathf.PI)) + 0.5f;
                float v = (y / (radius * 2f)) + 0.5f;

                uvs.Add(new Vector2(u, v));
            }
        }

        AddCurvedSurfaceTriangles(triangles, safeSegments, safeRings, 0, positiveXHalf);

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    Mesh CreateHalfShellMesh(float outerRadius, float innerRadius, bool positiveXHalf)
    {
        Mesh mesh = new Mesh();
        mesh.name = positiveXHalf ? "Right Venus Half Shell" : "Left Venus Half Shell";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int safeSegments = Mathf.Max(12, segments);
        int safeRings = Mathf.Max(6, rings);

        int outerStart = vertices.Count;
        AddHalfSphereVertices(vertices, uvs, outerRadius, safeSegments, safeRings, positiveXHalf);
        AddCurvedSurfaceTriangles(triangles, safeSegments, safeRings, outerStart, positiveXHalf);

        int innerStart = vertices.Count;
        AddHalfSphereVertices(vertices, uvs, innerRadius, safeSegments, safeRings, positiveXHalf);
        AddCurvedSurfaceTriangles(triangles, safeSegments, safeRings, innerStart, !positiveXHalf);

        int outerCutStart = vertices.Count;

        for (int j = 0; j <= safeSegments; j++)
        {
            float phi = Mathf.PI * 2f * j / safeSegments;
            float y = outerRadius * Mathf.Cos(phi);
            float z = outerRadius * Mathf.Sin(phi);

            vertices.Add(new Vector3(0f, y, z));
            uvs.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(phi), 0.5f + 0.5f * Mathf.Sin(phi)));
        }

        int innerCutStart = vertices.Count;

        for (int j = 0; j <= safeSegments; j++)
        {
            float phi = Mathf.PI * 2f * j / safeSegments;
            float y = innerRadius * Mathf.Cos(phi);
            float z = innerRadius * Mathf.Sin(phi);

            vertices.Add(new Vector3(0f, y, z));
            uvs.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(phi), 0.5f + 0.5f * Mathf.Sin(phi)));
        }

        for (int j = 0; j < safeSegments; j++)
        {
            int o0 = outerCutStart + j;
            int o1 = outerCutStart + j + 1;
            int i0 = innerCutStart + j;
            int i1 = innerCutStart + j + 1;

            triangles.Add(o0);
            triangles.Add(o1);
            triangles.Add(i0);

            triangles.Add(i0);
            triangles.Add(o1);
            triangles.Add(i1);

            triangles.Add(o0);
            triangles.Add(i0);
            triangles.Add(o1);

            triangles.Add(i0);
            triangles.Add(i1);
            triangles.Add(o1);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    Mesh CreateSolidHalfSphereMesh(float radius, bool positiveXHalf)
    {
        Mesh mesh = new Mesh();
        mesh.name = positiveXHalf ? "Right Venus Solid Core" : "Left Venus Solid Core";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int safeSegments = Mathf.Max(12, segments);
        int safeRings = Mathf.Max(6, rings);

        AddHalfSphereVertices(vertices, uvs, radius, safeSegments, safeRings, positiveXHalf);
        AddCurvedSurfaceTriangles(triangles, safeSegments, safeRings, 0, positiveXHalf);

        int centerIndex = vertices.Count;
        vertices.Add(Vector3.zero);
        uvs.Add(new Vector2(0.5f, 0.5f));

        int ringStartIndex = vertices.Count;

        for (int j = 0; j <= safeSegments; j++)
        {
            float phi = Mathf.PI * 2f * j / safeSegments;
            float y = radius * Mathf.Cos(phi);
            float z = radius * Mathf.Sin(phi);

            vertices.Add(new Vector3(0f, y, z));
            uvs.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(phi), 0.5f + 0.5f * Mathf.Sin(phi)));
        }

        for (int j = 0; j < safeSegments; j++)
        {
            int r0 = ringStartIndex + j;
            int r1 = ringStartIndex + j + 1;

            triangles.Add(centerIndex);
            triangles.Add(r0);
            triangles.Add(r1);

            triangles.Add(centerIndex);
            triangles.Add(r1);
            triangles.Add(r0);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    void AddHalfSphereVertices(List<Vector3> vertices, List<Vector2> uvs, float radius, int safeSegments, int safeRings, bool positiveXHalf)
    {
        for (int i = 0; i <= safeRings; i++)
        {
            float theta = (Mathf.PI * 0.5f) * i / safeRings;

            float x = radius * Mathf.Cos(theta);
            float ringRadius = radius * Mathf.Sin(theta);

            if (!positiveXHalf)
            {
                x = -x;
            }

            for (int j = 0; j <= safeSegments; j++)
            {
                float phi = Mathf.PI * 2f * j / safeSegments;

                float y = ringRadius * Mathf.Cos(phi);
                float z = ringRadius * Mathf.Sin(phi);

                vertices.Add(new Vector3(x, y, z));
                uvs.Add(new Vector2((float)j / safeSegments, (float)i / safeRings));
            }
        }
    }

    void AddCurvedSurfaceTriangles(List<int> triangles, int safeSegments, int safeRings, int startIndex, bool positiveXHalf)
    {
        for (int i = 0; i < safeRings; i++)
        {
            for (int j = 0; j < safeSegments; j++)
            {
                int a = startIndex + i * (safeSegments + 1) + j;
                int b = startIndex + (i + 1) * (safeSegments + 1) + j;
                int c = startIndex + i * (safeSegments + 1) + j + 1;
                int d = startIndex + (i + 1) * (safeSegments + 1) + j + 1;

                if (positiveXHalf)
                {
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);

                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(d);
                }
                else
                {
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);

                    triangles.Add(c);
                    triangles.Add(d);
                    triangles.Add(b);
                }
            }
        }
    }
}


public class VenusFlyingHalfMover : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 endPosition;

    private Quaternion startRotation;
    private Quaternion endRotation;

    private float duration = 2.2f;
    private float timer = 0f;
    private bool destroyAfterMove = true;
    private bool moving = false;

    public void Begin(
        Vector3 flyDirection,
        float moveDistance,
        float moveDuration,
        float spinDegrees,
        bool shouldDestroyAfterMove
    )
    {
        if (flyDirection.sqrMagnitude < 0.001f)
        {
            flyDirection = Vector3.right;
        }

        flyDirection.Normalize();

        startPosition = transform.position;
        endPosition = startPosition + flyDirection * moveDistance;

        startRotation = transform.rotation;

        Vector3 spinAxis = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        );

        if (spinAxis.sqrMagnitude < 0.001f)
        {
            spinAxis = Vector3.up;
        }

        spinAxis.Normalize();

        endRotation = Quaternion.AngleAxis(spinDegrees, spinAxis) * startRotation;

        duration = Mathf.Max(0.1f, moveDuration);
        destroyAfterMove = shouldDestroyAfterMove;
        timer = 0f;
        moving = true;
    }

    void Update()
    {
        if (!moving)
        {
            return;
        }

        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / duration);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        transform.position = Vector3.Lerp(startPosition, endPosition, smoothT);
        transform.rotation = Quaternion.Slerp(startRotation, endRotation, smoothT);

        if (t >= 1f)
        {
            moving = false;

            if (destroyAfterMove)
            {
                Destroy(gameObject);
            }
        }
    }
}

public class VenusTabletBillboard : MonoBehaviour
{
    public bool keepUpright = true;

    void LateUpdate()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            return;
        }

        transform.LookAt(cam.transform.position);
        transform.Rotate(0f, 180f, 0f);

        if (keepUpright)
        {
            Vector3 euler = transform.eulerAngles;
            euler.z = 0f;
            transform.eulerAngles = euler;
        }
    }
}