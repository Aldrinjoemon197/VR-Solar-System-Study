using UnityEngine;
using System.Collections.Generic;

// Shared half-sphere / half-shell mesh generation used by the remaining
// planet splitters (Mercury, Mars, Jupiter, Saturn, Uranus, Neptune).
// Earth (ExactHalfPlanetSplitter) and Venus (VenusHalfPlanetSplitter) keep
// their own copies of this math since they already shipped and work.
public static class PlanetSplitMeshUtility
{
    public static Mesh CreateOuterSurfaceHalfMesh(float radius, bool positiveXHalf, int segments, int rings)
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

        AddCurvedSurfaceTriangles(triangles, safeSegments, safeRings, 0, positiveXHalf);

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public static Mesh CreateHalfShellMesh(float outerRadius, float innerRadius, bool positiveXHalf, int segments, int rings)
    {
        Mesh mesh = new Mesh();
        mesh.name = positiveXHalf ? "Right Half Shell" : "Left Half Shell";

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

    public static Mesh CreateSolidHalfSphereMesh(float radius, bool positiveXHalf, int segments, int rings)
    {
        Mesh mesh = new Mesh();
        mesh.name = positiveXHalf ? "Right Solid Half Sphere" : "Left Solid Half Sphere";

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

    private static void AddHalfSphereVertices(List<Vector3> vertices, List<Vector2> uvs, float radius, int safeSegments, int safeRings, bool positiveXHalf)
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

    private static void AddCurvedSurfaceTriangles(List<int> triangles, int safeSegments, int safeRings, int startIndex, bool positiveXHalf)
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

    public static Material CreateLayerMaterial(Color color, bool transparent)
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

    public static GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 localRotation, Vector3 localScale, Material material)
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
            Object.Destroy(col);
        }

        return obj;
    }

    public static TextMesh CreateText(string name, Transform parent, Vector3 localPosition, float characterSize, TextAnchor anchor)
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
}
