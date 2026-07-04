using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class ExactHalfPlanetSplitter : MonoBehaviour
{
    [Header("Split Shape Detail")]
    public int segments = 64;
    public int rings = 32;

    [Header("Which Half Flies Away")]
    public bool rightHalfShouldFly = true;

    [Header("Split Motion")]
    public float smallOpeningDistance = 0.15f;
    public float flyingHalfImpulse = 350f;
    public float flyingHalfSpinImpulse = 40f;

    [Header("Tablet")]
    public bool showTabletAfterSplit = true;
    public float tabletOpenDelay = 0.25f;

    [Header("Earth Layer Ratios")]
    [Range(0.70f, 0.99f)]
    public float mantleOuterRatio = 0.92f;

    [Range(0.25f, 0.80f)]
    public float outerCoreOuterRatio = 0.55f;

    [Range(0.05f, 0.40f)]
    public float innerCoreOuterRatio = 0.20f;

    [Header("Layer Colors")]
    public Color crustCrossSectionColor = new Color(0.73f, 0.47f, 0.23f);
    public Color mantleColor = new Color(0.92f, 0.52f, 0.22f);
    public Color outerCoreColor = new Color(0.95f, 0.73f, 0.22f);
    public Color innerCoreColor = new Color(1.00f, 0.92f, 0.55f);

    [Header("Surface Skin")]
    public float outerSurfaceScaleBoost = 1.002f;

    [Header("Optional")]
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

        Debug.Log("Layered Earth split triggered on: " + gameObject.name);

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

        mantleOuterRatio = Mathf.Clamp(mantleOuterRatio, 0.70f, 0.99f);
        outerCoreOuterRatio = Mathf.Clamp(outerCoreOuterRatio, 0.25f, mantleOuterRatio - 0.05f);
        innerCoreOuterRatio = Mathf.Clamp(innerCoreOuterRatio, 0.05f, outerCoreOuterRatio - 0.05f);

        Material crustCrossSectionMat = CreateLayerMaterial(crustCrossSectionColor);
        Material mantleMat = CreateLayerMaterial(mantleColor);
        Material outerCoreMat = CreateLayerMaterial(outerCoreColor);
        Material innerCoreMat = CreateLayerMaterial(innerCoreColor);

        Material earthSurfaceMat = null;

        if (originalRenderer != null && originalRenderer.sharedMaterial != null)
        {
            earthSurfaceMat = new Material(originalRenderer.sharedMaterial);
        }

        Vector3 splitAxis = transform.right.normalized;

        GameObject leftHalf = CreateLayeredHalf(
            gameObject.name + "_Left_Half",
            radius,
            false,
            earthSurfaceMat,
            crustCrossSectionMat,
            mantleMat,
            outerCoreMat,
            innerCoreMat
        );

        GameObject rightHalf = CreateLayeredHalf(
            gameObject.name + "_Right_Half",
            radius,
            true,
            earthSurfaceMat,
            crustCrossSectionMat,
            mantleMat,
            outerCoreMat,
            innerCoreMat
        );

        float worldScale = Mathf.Max(
            transform.lossyScale.x,
            transform.lossyScale.y,
            transform.lossyScale.z
        );

        float opening = smallOpeningDistance * worldScale;

        EarthLayerInfoTarget infoTarget;

        if (rightHalfShouldFly)
        {
            leftHalf.transform.position = transform.position;
            rightHalf.transform.position = transform.position + splitAxis * opening;

            MakeStaticHalf(leftHalf);
            infoTarget = AddLayerInfoTarget(leftHalf, radius);
            MakeFlyingHalf(rightHalf, splitAxis);
        }
        else
        {
            rightHalf.transform.position = transform.position;
            leftHalf.transform.position = transform.position - splitAxis * opening;

            MakeStaticHalf(rightHalf);
            infoTarget = AddLayerInfoTarget(rightHalf, radius);
            MakeFlyingHalf(leftHalf, -splitAxis);
        }

        HideOriginalPlanet();

        if (showTabletAfterSplit && infoTarget != null)
        {
            StartCoroutine(OpenTabletAfterDelay(infoTarget));
        }

        Destroy(gameObject, 0.2f);
    }

    IEnumerator OpenTabletAfterDelay(EarthLayerInfoTarget infoTarget)
    {
        yield return null;

        if (tabletOpenDelay > 0f)
        {
            yield return new WaitForSeconds(tabletOpenDelay);
        }

        if (infoTarget != null)
        {
            infoTarget.SendMessage("ToggleLayerTable", SendMessageOptions.DontRequireReceiver);
        }
    }

    GameObject CreateLayeredHalf(
        string rootName,
        float radius,
        bool positiveXHalf,
        Material earthSurfaceMat,
        Material crustCrossSectionMat,
        Material mantleMat,
        Material outerCoreMat,
        Material innerCoreMat
    )
    {
        GameObject root = new GameObject(rootName);

        root.transform.position = transform.position;
        root.transform.rotation = transform.rotation;
        root.transform.localScale = transform.lossyScale;

        float crustOuter = radius;
        float mantleOuter = radius * mantleOuterRatio;
        float outerCoreOuter = radius * outerCoreOuterRatio;
        float innerCoreOuter = radius * innerCoreOuterRatio;

        CreateShellPart(
            "CrustCrossSection",
            root.transform,
            crustOuter,
            mantleOuter,
            positiveXHalf,
            crustCrossSectionMat
        );

        if (earthSurfaceMat != null)
        {
            CreateOuterSurfacePart(
                "EarthSurfaceTexture",
                root.transform,
                crustOuter * outerSurfaceScaleBoost,
                positiveXHalf,
                earthSurfaceMat
            );
        }

        CreateShellPart(
            "Mantle",
            root.transform,
            mantleOuter,
            outerCoreOuter,
            positiveXHalf,
            mantleMat
        );

        CreateShellPart(
            "OuterCore",
            root.transform,
            outerCoreOuter,
            innerCoreOuter,
            positiveXHalf,
            outerCoreMat
        );

        CreateSolidPart(
            "InnerCore",
            root.transform,
            innerCoreOuter,
            positiveXHalf,
            innerCoreMat
        );

        return root;
    }

    EarthLayerInfoTarget AddLayerInfoTarget(GameObject half, float radius)
    {
        SphereCollider collider = half.AddComponent<SphereCollider>();
        collider.isTrigger = false;
        collider.center = Vector3.zero;
        collider.radius = radius;

        EarthLayerInfoTarget infoTarget = half.AddComponent<EarthLayerInfoTarget>();

        infoTarget.sideOffset = 0.45f;
        infoTarget.upOffset = 0.10f;
        infoTarget.tableScale = 0.75f;

        infoTarget.crustColor = crustCrossSectionColor;
        infoTarget.mantleColor = mantleColor;
        infoTarget.outerCoreColor = outerCoreColor;
        infoTarget.innerCoreColor = innerCoreColor;

        return infoTarget;
    }

    void CreateShellPart(
        string name,
        Transform parent,
        float outerRadius,
        float innerRadius,
        bool positiveXHalf,
        Material material
    )
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

    void CreateSolidPart(
        string name,
        Transform parent,
        float radius,
        bool positiveXHalf,
        Material material
    )
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

    void CreateOuterSurfacePart(
        string name,
        Transform parent,
        float radius,
        bool positiveXHalf,
        Material material
    )
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

    void MakeStaticHalf(GameObject half)
    {
        Debug.Log(half.name + " stays in place.");
    }

    void MakeFlyingHalf(GameObject half, Vector3 flyDirection)
    {
        Rigidbody rb = half.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 1f;

        rb.AddForce(flyDirection.normalized * flyingHalfImpulse, ForceMode.Impulse);

        Vector3 spin = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        );

        rb.AddTorque(spin.normalized * flyingHalfSpinImpulse, ForceMode.Impulse);

        Debug.Log(half.name + " flies away.");
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

    Material CreateLayerMaterial(Color color)
    {
        Shader shader = Shader.Find("HDRP/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);

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

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.2f);
        }

        return mat;
    }

    Mesh CreateOuterSurfaceHalfMesh(float radius, bool positiveXHalf)
    {
        Mesh mesh = new Mesh();
        mesh.name = positiveXHalf ? "Right Outer Surface" : "Left Outer Surface";

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

        for (int i = 0; i < safeRings; i++)
        {
            for (int j = 0; j < safeSegments; j++)
            {
                int a = i * (safeSegments + 1) + j;
                int b = (i + 1) * (safeSegments + 1) + j;
                int c = i * (safeSegments + 1) + j + 1;
                int d = (i + 1) * (safeSegments + 1) + j + 1;

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
        mesh.name = positiveXHalf ? "Right Half Shell" : "Left Half Shell";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int safeSegments = Mathf.Max(12, segments);
        int safeRings = Mathf.Max(6, rings);

        int outerStart = vertices.Count;

        for (int i = 0; i <= safeRings; i++)
        {
            float theta = (Mathf.PI * 0.5f) * i / safeRings;

            float x = outerRadius * Mathf.Cos(theta);
            float ringRadius = outerRadius * Mathf.Sin(theta);

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

        for (int i = 0; i < safeRings; i++)
        {
            for (int j = 0; j < safeSegments; j++)
            {
                int a = outerStart + i * (safeSegments + 1) + j;
                int b = outerStart + (i + 1) * (safeSegments + 1) + j;
                int c = outerStart + i * (safeSegments + 1) + j + 1;
                int d = outerStart + (i + 1) * (safeSegments + 1) + j + 1;

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

        int innerStart = vertices.Count;

        for (int i = 0; i <= safeRings; i++)
        {
            float theta = (Mathf.PI * 0.5f) * i / safeRings;

            float x = innerRadius * Mathf.Cos(theta);
            float ringRadius = innerRadius * Mathf.Sin(theta);

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

        for (int i = 0; i < safeRings; i++)
        {
            for (int j = 0; j < safeSegments; j++)
            {
                int a = innerStart + i * (safeSegments + 1) + j;
                int b = innerStart + (i + 1) * (safeSegments + 1) + j;
                int c = innerStart + i * (safeSegments + 1) + j + 1;
                int d = innerStart + (i + 1) * (safeSegments + 1) + j + 1;

                if (positiveXHalf)
                {
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);

                    triangles.Add(c);
                    triangles.Add(d);
                    triangles.Add(b);
                }
                else
                {
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);

                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(d);
                }
            }
        }

        int outerCutStart = vertices.Count;

        for (int j = 0; j <= safeSegments; j++)
        {
            float phi = Mathf.PI * 2f * j / safeSegments;

            float y = outerRadius * Mathf.Cos(phi);
            float z = outerRadius * Mathf.Sin(phi);

            vertices.Add(new Vector3(0f, y, z));
            uvs.Add(new Vector2(
                0.5f + 0.5f * Mathf.Cos(phi),
                0.5f + 0.5f * Mathf.Sin(phi)
            ));
        }

        int innerCutStart = vertices.Count;

        for (int j = 0; j <= safeSegments; j++)
        {
            float phi = Mathf.PI * 2f * j / safeSegments;

            float y = innerRadius * Mathf.Cos(phi);
            float z = innerRadius * Mathf.Sin(phi);

            vertices.Add(new Vector3(0f, y, z));
            uvs.Add(new Vector2(
                0.5f + 0.5f * Mathf.Cos(phi),
                0.5f + 0.5f * Mathf.Sin(phi)
            ));
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
        mesh.name = positiveXHalf ? "Right Solid Half Sphere" : "Left Solid Half Sphere";

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
                uvs.Add(new Vector2((float)j / safeSegments, (float)i / safeRings));
            }
        }

        for (int i = 0; i < safeRings; i++)
        {
            for (int j = 0; j < safeSegments; j++)
            {
                int a = i * (safeSegments + 1) + j;
                int b = (i + 1) * (safeSegments + 1) + j;
                int c = i * (safeSegments + 1) + j + 1;
                int d = (i + 1) * (safeSegments + 1) + j + 1;

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
            uvs.Add(new Vector2(
                0.5f + 0.5f * Mathf.Cos(phi),
                0.5f + 0.5f * Mathf.Sin(phi)
            ));
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
}
