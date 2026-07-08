using UnityEngine;
using System.Collections.Generic;

public class AsteroidBeltGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform sun;
    public Transform mars;
    public Transform jupiter;

    [Header("Belt Look")]
    public int asteroidCount = 1800;
    public float beltInnerRadius = 0f;
    public float beltOuterRadius = 0f;
    public float verticalThickness = 3.2f;
    public float minAsteroidSize = 0.10f;
    public float maxAsteroidSize = 0.48f;
    public float orbitSpeed = 0.18f;
    public float spinSpeed = 22f;

    [Header("Generation")]
    public bool generateOnStart = true;
    public bool regenerateIfEmpty = true;
    public int randomSeed = 91724;

    private GameObject beltRoot;
    private readonly List<AsteroidVisual> asteroids = new List<AsteroidVisual>();
    private Material[] asteroidMaterials;

    void Start()
    {
        ResolveReferences();

        if (generateOnStart)
        {
            Generate();
        }
    }

    void Update()
    {
        if (regenerateIfEmpty && asteroids.Count == 0)
        {
            Generate();
        }

        if (sun == null)
        {
            return;
        }

        for (int i = 0; i < asteroids.Count; i++)
        {
            AsteroidVisual asteroid = asteroids[i];

            if (asteroid.transform == null)
            {
                continue;
            }

            float angle = asteroid.startAngle + Time.time * orbitSpeed * asteroid.orbitDirection / Mathf.Max(0.01f, asteroid.radius);
            Vector3 position = sun.position + new Vector3(
                Mathf.Cos(angle) * asteroid.radius,
                asteroid.heightOffset,
                Mathf.Sin(angle) * asteroid.radius
            );

            asteroid.transform.position = position;
            asteroid.transform.Rotate(asteroid.spinAxis, spinSpeed * asteroid.spinMultiplier * Time.deltaTime, Space.Self);
        }
    }

    public void Generate()
    {
        ResolveReferences();

        if (sun == null)
        {
            return;
        }

        if (beltRoot != null)
        {
            Destroy(beltRoot);
        }

        asteroids.Clear();
        CreateMaterials();

        float inner = beltInnerRadius;
        float outer = beltOuterRadius;

        if (inner <= 0f || outer <= 0f || outer <= inner)
        {
            float marsRadius = mars != null ? Vector3.Distance(sun.position, mars.position) : 35f;
            float jupiterRadius = jupiter != null ? Vector3.Distance(sun.position, jupiter.position) : marsRadius * 2.2f;
            inner = Mathf.Lerp(marsRadius, jupiterRadius, 0.30f);
            outer = Mathf.Lerp(marsRadius, jupiterRadius, 0.70f);
        }

        beltRoot = new GameObject("Procedural Asteroid Belt");

        Random.InitState(randomSeed);

        for (int i = 0; i < asteroidCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(inner, outer);
            float height = Random.Range(-verticalThickness, verticalThickness) * Random.Range(0.15f, 1f);
            float size = Random.Range(minAsteroidSize, maxAsteroidSize) * Random.Range(0.65f, 1.45f);

            GameObject asteroid = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            asteroid.name = "Asteroid_" + i;
            asteroid.transform.SetParent(beltRoot.transform);
            asteroid.transform.position = sun.position + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            asteroid.transform.rotation = Random.rotation;
            asteroid.transform.localScale = new Vector3(
                size * Random.Range(0.65f, 1.50f),
                size * Random.Range(0.45f, 1.15f),
                size * Random.Range(0.70f, 1.65f)
            );

            Renderer renderer = asteroid.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material = asteroidMaterials[Random.Range(0, asteroidMaterials.Length)];
            }

            Collider collider = asteroid.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }

            asteroids.Add(new AsteroidVisual
            {
                transform = asteroid.transform,
                radius = radius,
                heightOffset = height,
                startAngle = angle,
                orbitDirection = Random.value > 0.5f ? 1f : -1f,
                spinAxis = Random.onUnitSphere,
                spinMultiplier = Random.Range(0.25f, 2.2f)
            });
        }
    }

    void ResolveReferences()
    {
        if (sun == null)
        {
            sun = FindByName("sun");
        }

        if (mars == null)
        {
            mars = FindByName("mars");
        }

        if (jupiter == null)
        {
            jupiter = FindByName("jupiter");
        }
    }

    Transform FindByName(string key)
    {
        Transform[] all = FindObjectsOfType<Transform>();

        foreach (Transform t in all)
        {
            if (t != null && t.name.ToLower().Contains(key))
            {
                return t;
            }
        }

        return null;
    }

    void CreateMaterials()
    {
        if (asteroidMaterials != null && asteroidMaterials.Length > 0)
        {
            return;
        }

        asteroidMaterials = new Material[]
        {
            CreateAsteroidMaterial(new Color(0.24f, 0.22f, 0.20f)),
            CreateAsteroidMaterial(new Color(0.34f, 0.30f, 0.26f)),
            CreateAsteroidMaterial(new Color(0.18f, 0.17f, 0.16f)),
            CreateAsteroidMaterial(new Color(0.42f, 0.36f, 0.29f)),
            CreateAsteroidMaterial(new Color(0.30f, 0.28f, 0.31f))
        };
    }

    Material CreateAsteroidMaterial(Color color)
    {
        Shader shader = Shader.Find("HDRP/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.18f);
        }

        return material;
    }

    private struct AsteroidVisual
    {
        public Transform transform;
        public float radius;
        public float heightOffset;
        public float startAngle;
        public float orbitDirection;
        public Vector3 spinAxis;
        public float spinMultiplier;
    }
}
