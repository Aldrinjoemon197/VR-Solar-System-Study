using UnityEngine;
using System.Reflection;

public class BetterSciFiGunVisual : MonoBehaviour
{
    [Header("Build")]
    public bool buildOnStart = true;
    public bool removeOldVisualBeforeBuilding = true;

    [Header("Gun Placement")]
    public Vector3 gunLocalPosition = new Vector3(0f, -0.035f, 0.12f);
    public Vector3 gunLocalRotation = new Vector3(0f, 0f, 0f);
    public Vector3 gunLocalScale = new Vector3(1f, 1f, 1f);

    [Header("Muzzle")]
    public string muzzleName = "GunMuzzle";
    public Vector3 muzzleLocalPosition = new Vector3(0f, -0.03f, 0.42f);
    public Vector3 muzzleLocalRotation = new Vector3(0f, 0f, 0f);

    [Header("Colors")]
    public Color bodyColor = new Color(0.05f, 0.055f, 0.065f, 1f);
    public Color metalColor = new Color(0.30f, 0.32f, 0.34f, 1f);
    public Color darkMetalColor = new Color(0.01f, 0.012f, 0.015f, 1f);
    public Color greenGlowColor = new Color(0.0f, 1.0f, 0.12f, 1f);
    public Color blueGlowColor = new Color(0.0f, 0.65f, 1.0f, 1f);

    private const string gunVisualName = "SciFiLaserGun_Visual";

    void Awake()
    {
        DisableOldSimpleGunVisualToggle();
    }

    void Start()
    {
        if (buildOnStart)
        {
            BuildGun();
        }
    }

    [ContextMenu("Build / Rebuild Sci-Fi Gun")]
    public void BuildGun()
    {
        DisableOldSimpleGunVisualToggle();

        if (removeOldVisualBeforeBuilding)
        {
            RemoveExistingVisual();
        }

        Material bodyMat = CreateMaterial(bodyColor, false);
        Material metalMat = CreateMaterial(metalColor, false);
        Material darkMat = CreateMaterial(darkMetalColor, false);
        Material greenGlowMat = CreateMaterial(greenGlowColor, true);
        Material blueGlowMat = CreateMaterial(blueGlowColor, true);

        GameObject gunRoot = new GameObject(gunVisualName);
        gunRoot.transform.SetParent(transform);
        gunRoot.transform.localPosition = gunLocalPosition;
        gunRoot.transform.localRotation = Quaternion.Euler(gunLocalRotation);
        gunRoot.transform.localScale = gunLocalScale;

        // Main futuristic gun body
        CreateCube(
            "Main Angular Body",
            gunRoot.transform,
            new Vector3(0f, 0f, 0.04f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.075f, 0.052f, 0.22f),
            bodyMat
        );

        // Upper slide
        CreateCube(
            "Upper Slide",
            gunRoot.transform,
            new Vector3(0f, 0.035f, 0.045f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.068f, 0.025f, 0.23f),
            metalMat
        );

        // Rear block
        CreateCube(
            "Rear Power Block",
            gunRoot.transform,
            new Vector3(0f, 0.005f, -0.085f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.085f, 0.065f, 0.055f),
            darkMat
        );

        // Grip / handle
        CreateCube(
            "Angled Grip",
            gunRoot.transform,
            new Vector3(0f, -0.095f, -0.035f),
            new Vector3(18f, 0f, 0f),
            new Vector3(0.047f, 0.155f, 0.052f),
            bodyMat
        );

        // Grip lower plate
        CreateCube(
            "Grip Bottom Plate",
            gunRoot.transform,
            new Vector3(0f, -0.175f, -0.015f),
            new Vector3(18f, 0f, 0f),
            new Vector3(0.058f, 0.018f, 0.065f),
            metalMat
        );

        // Trigger guard
        CreateCube(
            "Trigger Guard Top",
            gunRoot.transform,
            new Vector3(0f, -0.045f, 0.02f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.052f, 0.010f, 0.060f),
            metalMat
        );

        CreateCube(
            "Trigger Guard Bottom",
            gunRoot.transform,
            new Vector3(0f, -0.078f, 0.04f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.052f, 0.010f, 0.055f),
            metalMat
        );

        CreateCube(
            "Trigger",
            gunRoot.transform,
            new Vector3(0f, -0.068f, 0.02f),
            new Vector3(20f, 0f, 0f),
            new Vector3(0.018f, 0.045f, 0.018f),
            darkMat
        );

        // Main barrel
        CreateCylinder(
            "Main Barrel",
            gunRoot.transform,
            new Vector3(0f, 0.01f, 0.215f),
            new Vector3(90f, 0f, 0f),
            new Vector3(0.028f, 0.115f, 0.028f),
            metalMat
        );

        // Inner glowing barrel
        CreateCylinder(
            "Green Energy Barrel",
            gunRoot.transform,
            new Vector3(0f, 0.01f, 0.245f),
            new Vector3(90f, 0f, 0f),
            new Vector3(0.016f, 0.125f, 0.016f),
            greenGlowMat
        );

        // Front nozzle
        CreateCylinder(
            "Front Nozzle",
            gunRoot.transform,
            new Vector3(0f, 0.01f, 0.355f),
            new Vector3(90f, 0f, 0f),
            new Vector3(0.040f, 0.018f, 0.040f),
            darkMat
        );

        // Glowing muzzle ring
        CreateCylinder(
            "Glowing Muzzle Ring",
            gunRoot.transform,
            new Vector3(0f, 0.01f, 0.383f),
            new Vector3(90f, 0f, 0f),
            new Vector3(0.047f, 0.006f, 0.047f),
            greenGlowMat
        );

        // Side armor panels
        CreateCube(
            "Left Side Armor",
            gunRoot.transform,
            new Vector3(-0.046f, 0.003f, 0.045f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.010f, 0.044f, 0.155f),
            metalMat
        );

        CreateCube(
            "Right Side Armor",
            gunRoot.transform,
            new Vector3(0.046f, 0.003f, 0.045f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.010f, 0.044f, 0.155f),
            metalMat
        );

        // Glowing side strips
        CreateCube(
            "Left Green Energy Strip",
            gunRoot.transform,
            new Vector3(-0.052f, 0.026f, 0.085f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.006f, 0.010f, 0.105f),
            greenGlowMat
        );

        CreateCube(
            "Right Green Energy Strip",
            gunRoot.transform,
            new Vector3(0.052f, 0.026f, 0.085f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.006f, 0.010f, 0.105f),
            greenGlowMat
        );

        // Top rail and sight
        CreateCube(
            "Top Rail",
            gunRoot.transform,
            new Vector3(0f, 0.061f, 0.035f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.040f, 0.012f, 0.170f),
            darkMat
        );

        CreateCube(
            "Rear Sight",
            gunRoot.transform,
            new Vector3(0f, 0.079f, -0.045f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.050f, 0.025f, 0.015f),
            metalMat
        );

        CreateCube(
            "Front Sight",
            gunRoot.transform,
            new Vector3(0f, 0.078f, 0.155f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.035f, 0.020f, 0.012f),
            metalMat
        );

        // Energy core
        CreateSphere(
            "Green Energy Core",
            gunRoot.transform,
            new Vector3(0f, 0.006f, 0.010f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.050f, 0.050f, 0.050f),
            greenGlowMat
        );

        // Small blue side lights
        CreateSphere(
            "Left Blue Light",
            gunRoot.transform,
            new Vector3(-0.052f, -0.008f, -0.040f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.018f, 0.018f, 0.018f),
            blueGlowMat
        );

        CreateSphere(
            "Right Blue Light",
            gunRoot.transform,
            new Vector3(0.052f, -0.008f, -0.040f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.018f, 0.018f, 0.018f),
            blueGlowMat
        );

        // Decorative cooling fins on top
        CreateCube(
            "Cooling Fin 1",
            gunRoot.transform,
            new Vector3(0f, 0.072f, 0.000f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.050f, 0.010f, 0.010f),
            metalMat
        );

        CreateCube(
            "Cooling Fin 2",
            gunRoot.transform,
            new Vector3(0f, 0.072f, 0.035f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.050f, 0.010f, 0.010f),
            metalMat
        );

        CreateCube(
            "Cooling Fin 3",
            gunRoot.transform,
            new Vector3(0f, 0.072f, 0.070f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.050f, 0.010f, 0.010f),
            metalMat
        );

        // Bottom energy battery
        CreateCube(
            "Bottom Battery Pack",
            gunRoot.transform,
            new Vector3(0f, -0.040f, 0.105f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.055f, 0.025f, 0.095f),
            darkMat
        );

        CreateCube(
            "Battery Green Line",
            gunRoot.transform,
            new Vector3(0f, -0.056f, 0.105f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0.045f, 0.006f, 0.080f),
            greenGlowMat
        );

        Transform muzzle = FindOrCreateMuzzle();
        AssignMuzzleToGunLaser(muzzle);

        Debug.Log("Better sci-fi gun visual created. Muzzle assigned to: " + muzzle.name);
    }

    Transform FindOrCreateMuzzle()
    {
        Transform existing = transform.Find(muzzleName);

        if (existing != null)
        {
            existing.localPosition = muzzleLocalPosition;
            existing.localRotation = Quaternion.Euler(muzzleLocalRotation);
            existing.localScale = Vector3.one;
            return existing;
        }

        GameObject muzzle = new GameObject(muzzleName);
        muzzle.transform.SetParent(transform);
        muzzle.transform.localPosition = muzzleLocalPosition;
        muzzle.transform.localRotation = Quaternion.Euler(muzzleLocalRotation);
        muzzle.transform.localScale = Vector3.one;

        return muzzle.transform;
    }

    void AssignMuzzleToGunLaser(Transform muzzle)
    {
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null)
            {
                continue;
            }

            string typeName = script.GetType().Name.ToLower();

            if (!typeName.Contains("quest") && !typeName.Contains("gun") && !typeName.Contains("laser"))
            {
                continue;
            }

            FieldInfo[] fields = script.GetType().GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            foreach (FieldInfo field in fields)
            {
                string fieldName = field.Name.ToLower();

                if (field.FieldType == typeof(Transform) && fieldName.Contains("muzzle"))
                {
                    field.SetValue(script, muzzle);
                }

                if (field.FieldType == typeof(bool) &&
                    fieldName.Contains("create") &&
                    fieldName.Contains("gun") &&
                    fieldName.Contains("visual"))
                {
                    field.SetValue(script, false);
                }
            }
        }
    }

    void DisableOldSimpleGunVisualToggle()
    {
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null)
            {
                continue;
            }

            FieldInfo[] fields = script.GetType().GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            foreach (FieldInfo field in fields)
            {
                string fieldName = field.Name.ToLower();

                if (field.FieldType == typeof(bool) &&
                    fieldName.Contains("create") &&
                    fieldName.Contains("gun") &&
                    fieldName.Contains("visual"))
                {
                    field.SetValue(script, false);
                }
            }
        }
    }

    void RemoveExistingVisual()
    {
        Transform existing = transform.Find(gunVisualName);

        if (existing == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(existing.gameObject);
        }
        else
        {
            DestroyImmediate(existing.gameObject);
        }
    }

    GameObject CreateCube(
        string objectName,
        Transform parent,
        Vector3 localPosition,
        Vector3 localRotation,
        Vector3 localScale,
        Material material
    )
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = objectName;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.Euler(localRotation);
        obj.transform.localScale = localScale;

        Renderer renderer = obj.GetComponent<Renderer>();
        renderer.material = material;

        RemoveCollider(obj);
        return obj;
    }

    GameObject CreateCylinder(
        string objectName,
        Transform parent,
        Vector3 localPosition,
        Vector3 localRotation,
        Vector3 localScale,
        Material material
    )
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = objectName;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.Euler(localRotation);
        obj.transform.localScale = localScale;

        Renderer renderer = obj.GetComponent<Renderer>();
        renderer.material = material;

        RemoveCollider(obj);
        return obj;
    }

    GameObject CreateSphere(
        string objectName,
        Transform parent,
        Vector3 localPosition,
        Vector3 localRotation,
        Vector3 localScale,
        Material material
    )
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = objectName;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.Euler(localRotation);
        obj.transform.localScale = localScale;

        Renderer renderer = obj.GetComponent<Renderer>();
        renderer.material = material;

        RemoveCollider(obj);
        return obj;
    }

    Material CreateMaterial(Color color, bool emission)
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

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", emission ? 0f : 0.7f);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", emission ? 0.9f : 0.55f);
        }

        if (emission)
        {
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.5f);
            }
        }

        return material;
    }

    void RemoveCollider(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();

        if (collider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }
    }
}