using UnityEngine;

public class EarthLayerInfoTarget : MonoBehaviour
{
    [Header("Table Placement")]
    public float sideOffset = 1.0f;
    public float upOffset = 0.2f;
    public float tableScale = 1.0f;

    [Header("Table Colors")]
    public Color panelColor = new Color(0f, 0f, 0f, 0.55f);
    public Color textColor = Color.white;

    public Color crustColor = new Color(0.73f, 0.47f, 0.23f);
    public Color mantleColor = new Color(0.92f, 0.52f, 0.22f);
    public Color outerCoreColor = new Color(0.95f, 0.73f, 0.22f);
    public Color innerCoreColor = new Color(1.00f, 0.92f, 0.55f);

    private GameObject tableRoot;
    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;

        if (playerCamera == null)
        {
            playerCamera = FindAnyObjectByType<Camera>();
        }
    }

    void Update()
    {
        if (tableRoot != null && tableRoot.activeSelf)
        {
            KeepTableBesideEarth();
            FaceCamera();
        }
    }

    public void ToggleLayerTable()
    {
        if (tableRoot == null)
        {
            CreateLayerTable();
            tableRoot.SetActive(true);
        }
        else
        {
            tableRoot.SetActive(!tableRoot.activeSelf);
        }

        if (tableRoot != null && tableRoot.activeSelf)
        {
            KeepTableBesideEarth();
            FaceCamera();
        }
    }

    void CreateLayerTable()
    {
        tableRoot = new GameObject("Earth Layer Info Table");

        float finalScale = Mathf.Clamp(tableScale, 0.4f, 3.0f);

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        panel.name = "Transparent Table Panel";
        panel.transform.SetParent(tableRoot.transform);
        panel.transform.localPosition = Vector3.zero;
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = new Vector3(2.2f * finalScale, 1.55f * finalScale, 1f);

        Renderer panelRenderer = panel.GetComponent<Renderer>();
        panelRenderer.material = CreateTransparentMaterial(panelColor);

        Collider panelCollider = panel.GetComponent<Collider>();
        Destroy(panelCollider);

        CreateText(
            "Title",
            "EARTH LAYERS",
            new Vector3(-0.95f * finalScale, 0.55f * finalScale, -0.01f),
            0.055f * finalScale,
            textColor,
            tableRoot.transform,
            72
        );

        CreateLayerRow(
            "Crust",
            "Crust",
            crustColor,
            new Vector3(-0.88f * finalScale, 0.22f * finalScale, -0.01f),
            finalScale
        );

        CreateLayerRow(
            "Mantle",
            "Mantle",
            mantleColor,
            new Vector3(-0.88f * finalScale, 0.00f * finalScale, -0.01f),
            finalScale
        );

        CreateLayerRow(
            "OuterCore",
            "Outer Core",
            outerCoreColor,
            new Vector3(-0.88f * finalScale, -0.22f * finalScale, -0.01f),
            finalScale
        );

        CreateLayerRow(
            "InnerCore",
            "Inner Core",
            innerCoreColor,
            new Vector3(-0.88f * finalScale, -0.44f * finalScale, -0.01f),
            finalScale
        );

        tableRoot.SetActive(false);
    }

    void CreateLayerRow(
        string objectName,
        string label,
        Color color,
        Vector3 localPosition,
        float finalScale
    )
    {
        GameObject colorBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        colorBox.name = objectName + "_ColorBox";
        colorBox.transform.SetParent(tableRoot.transform);
        colorBox.transform.localPosition = localPosition;
        colorBox.transform.localRotation = Quaternion.identity;
        colorBox.transform.localScale = new Vector3(
            0.10f * finalScale,
            0.10f * finalScale,
            0.01f * finalScale
        );

        Renderer boxRenderer = colorBox.GetComponent<Renderer>();
        boxRenderer.material = CreateSolidMaterial(color);

        Collider boxCollider = colorBox.GetComponent<Collider>();
        Destroy(boxCollider);

        CreateText(
            objectName + "_Text",
            label,
            localPosition + new Vector3(0.18f * finalScale, -0.015f * finalScale, -0.01f),
            0.042f * finalScale,
            textColor,
            tableRoot.transform,
            64
        );
    }

    void CreateText(
        string objectName,
        string text,
        Vector3 localPosition,
        float characterSize,
        Color color,
        Transform parent,
        int fontSize
    )
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.identity;
        textObject.transform.localScale = Vector3.one;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.anchor = TextAnchor.MiddleLeft;
        textMesh.alignment = TextAlignment.Left;
    }

    void KeepTableBesideEarth()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;

            if (playerCamera == null)
            {
                playerCamera = FindAnyObjectByType<Camera>();
            }

            if (playerCamera == null)
            {
                return;
            }
        }

        Vector3 cameraRight = playerCamera.transform.right;
        Vector3 cameraUp = playerCamera.transform.up;

        float objectSize = Mathf.Max(
            transform.lossyScale.x,
            transform.lossyScale.y,
            transform.lossyScale.z
        );

        objectSize = Mathf.Max(objectSize, 1f);

        tableRoot.transform.position =
            transform.position +
            cameraRight * sideOffset * objectSize +
            cameraUp * upOffset * objectSize;
    }

    void FaceCamera()
    {
        if (playerCamera == null || tableRoot == null)
        {
            return;
        }

        Vector3 directionAwayFromCamera =
            tableRoot.transform.position - playerCamera.transform.position;

        tableRoot.transform.rotation =
            Quaternion.LookRotation(directionAwayFromCamera.normalized, playerCamera.transform.up);
    }

    Material CreateSolidMaterial(Color color)
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

        Material material = new Material(shader);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_UnlitColor"))
        {
            material.SetColor("_UnlitColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        return material;
    }

    Material CreateTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("HDRP/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
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

        if (material.HasProperty("_UnlitColor"))
        {
            material.SetColor("_UnlitColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_SurfaceType"))
        {
            material.SetFloat("_SurfaceType", 1f);
        }

        if (material.HasProperty("_BlendMode"))
        {
            material.SetFloat("_BlendMode", 0f);
        }

        material.renderQueue = 3000;
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_BLENDMODE_ALPHA");

        return material;
    }
}