using UnityEngine;
using System.Collections.Generic;

// Scatters a field of procedurally-shaped rocks between Mars and Jupiter,
// where the real Main Asteroid Belt sits (roughly 2.2-3.2 AU from the Sun).
// This scene already places planets along X at ~30 units per AU (Mars sits
// at the 1.524 AU mark, Jupiter at the 5.203 AU mark, matching real ratios),
// so the belt is generated as a scattered disc around this object's own
// position rather than a perfect ring, to fit the same "flight corridor"
// layout the rest of the solar system uses.
public class AsteroidBeltGenerator : MonoBehaviour
{
    [Header("Belt Shape")]
    [Tooltip("How far the belt extends along local X, in both directions from this object's position.")]
    public float beltHalfLength = 15f;

    [Tooltip("Scatter radius on the Y/Z plane around this object's position.")]
    public float beltRingRadius = 16f;

    [Header("Asteroid Count / Size")]
    public int asteroidCount = 220;
    public float minScale = 0.35f;
    public float maxScale = 2.4f;

    [Header("Rock Shape")]
    public int rockSegments = 8;
    public int rockRings = 6;
    [Range(0f, 0.6f)]
    public float irregularity = 0.35f;

    [Header("Tumble")]
    public float minTumbleSpeed = 5f;
    public float maxTumbleSpeed = 40f;

    [Header("Randomness")]
    public int seed = 12345;

    private static readonly Color[] RockColorPalette =
    {
        new Color(0.42f, 0.39f, 0.36f),
        new Color(0.35f, 0.32f, 0.30f),
        new Color(0.48f, 0.44f, 0.38f),
        new Color(0.30f, 0.28f, 0.27f),
        new Color(0.55f, 0.50f, 0.44f)
    };

    void Start()
    {
        GenerateBelt();
    }

    public void GenerateBelt()
    {
        Random.State previousState = Random.state;
        Random.InitState(seed);

        Material[] rockMaterials = new Material[RockColorPalette.Length];
        for (int i = 0; i < RockColorPalette.Length; i++)
        {
            rockMaterials[i] = PlanetSplitMeshUtility.CreateLayerMaterial(RockColorPalette[i], false);
        }

        for (int i = 0; i < asteroidCount; i++)
        {
            float xOffset = Random.Range(-beltHalfLength, beltHalfLength);

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Random.value) * beltRingRadius;
            float yOffset = Mathf.Cos(angle) * radius;
            float zOffset = Mathf.Sin(angle) * radius;

            float scale = Mathf.Lerp(minScale, maxScale, Mathf.Pow(Random.value, 2f));

            GameObject rock = new GameObject("Asteroid_" + i);
            rock.transform.SetParent(transform);
            rock.transform.localPosition = new Vector3(xOffset, yOffset, zOffset);
            rock.transform.localRotation = Random.rotation;
            rock.transform.localScale = Vector3.one * scale;

            MeshFilter mf = rock.AddComponent<MeshFilter>();
            MeshRenderer mr = rock.AddComponent<MeshRenderer>();

            mf.mesh = CreateRockMesh(0.5f, rockSegments, rockRings, irregularity, Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f));
            mr.material = rockMaterials[Random.Range(0, rockMaterials.Length)];

            AsteroidTumble tumble = rock.AddComponent<AsteroidTumble>();
            tumble.axis = Random.onUnitSphere;
            tumble.degreesPerSecond = Random.Range(minTumbleSpeed, maxTumbleSpeed);
        }

        Random.state = previousState;
    }

    private Mesh CreateRockMesh(float radius, int segments, int rings, float noiseAmount, float noiseOffsetX, float noiseOffsetY)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Asteroid Rock";

        int safeSegments = Mathf.Max(5, segments);
        int safeRings = Mathf.Max(3, rings);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i <= safeRings; i++)
        {
            float theta = Mathf.PI * i / safeRings;
            float y = Mathf.Cos(theta);
            float ringRadius = Mathf.Sin(theta);

            for (int j = 0; j <= safeSegments; j++)
            {
                float phi = Mathf.PI * 2f * j / safeSegments;
                float x = ringRadius * Mathf.Cos(phi);
                float z = ringRadius * Mathf.Sin(phi);

                float noise = Mathf.PerlinNoise(
                    theta * 2.2f + noiseOffsetX,
                    phi * 2.2f + noiseOffsetY
                );

                float bump = 1f + (noise - 0.5f) * 2f * noiseAmount;

                vertices.Add(new Vector3(x, y, z) * radius * bump);
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

                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);

                triangles.Add(c);
                triangles.Add(b);
                triangles.Add(d);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}

public class AsteroidTumble : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float degreesPerSecond = 15f;

    void Update()
    {
        transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.World);
    }
}
