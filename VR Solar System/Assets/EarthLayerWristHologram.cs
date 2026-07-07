using UnityEngine;
using UnityEngine.XR;

public class LeftHandLayerHologramProjector : MonoBehaviour
{
    public enum ActivationButton
    {
        LeftTrigger,
        LeftGrip,
        LeftPrimaryButton_X,
        LeftSecondaryButton_Y,
        RightTrigger,
        RightGrip,
        RightPrimaryButton_A,
        RightSecondaryButton_B,
        AlwaysOn
    }

    [Header("Required Reference")]
    [Tooltip("Drag RightHand Controller > GunMuzzle here.")]
    public Transform gunMuzzle;

    [Header("Activation")]
    [Tooltip("Hologram is visible only while this button is pressed.")]
    public ActivationButton activationButton = ActivationButton.LeftPrimaryButton_X;

    [Tooltip("Useful for testing in Unity editor. Hold left mouse button.")]
    public bool useMouseBackup = true;

    [Header("Raycast / Layer Detection")]
    public float maxDistance = 1000f;
    public LayerMask hitLayers = ~0;
    public bool autoAddCollidersToEarthLayers = true;
    public float colliderScanInterval = 0.5f;

    [Header("Projector Cube Placement")]
    [Tooltip("This is the small cube on the left hand/controller.")]
    public Vector3 projectorLocalPosition = new Vector3(0f, -0.035f, 0.065f);
    public Vector3 projectorLocalRotation = new Vector3(0f, 0f, 0f);
    public Vector3 projectorLocalScale = new Vector3(0.075f, 0.035f, 0.075f);

    [Header("Hologram Placement")]
    [Tooltip("Panel position above the projector cube.")]
    public Vector3 hologramLocalPosition = new Vector3(0f, 0.105f, 0.025f);
    public Vector3 hologramLocalRotation = new Vector3(65f, 0f, 0f);
    public Vector3 hologramLocalScale = new Vector3(0.45f, 0.45f, 0.45f);

    [Tooltip("Turn this ON if you want the panel to face the camera. If it rotates weirdly, turn it OFF.")]
    public bool hologramFacesCamera = false;

    [Header("Panel Size")]
    public Vector3 panelLocalScale = new Vector3(0.48f, 0.24f, 0.006f);

    [Header("Debug")]
    public bool showDebugRay = true;

    private GameObject projectorRoot;
    private GameObject hologramRoot;

    private TextMesh planetText;
    private TextMesh titleText;
    private TextMesh descriptionText;
    private TextMesh statusText;

    private Renderer panelRenderer;
    private Renderer colorDotRenderer;

    private Material projectorMat;
    private Material cyanMat;
    private Material panelMat;
    private Material layerColorMat;

    private float nextColliderScanTime;
    private string lastLayer = "";

    void Start()
    {
        DisableOldLayerDisplayScripts();
        CreateMaterials();
        BuildProjectorAndHologram();
        HideHologram();
    }

    void Update()
    {
        if (autoAddCollidersToEarthLayers && Time.time >= nextColliderScanTime)
        {
            nextColliderScanTime = Time.time + colliderScanInterval;
            AddMeshCollidersToEarthLayers();
        }

        if (!IsActivationPressed())
        {
            HideHologram();
            return;
        }

        if (gunMuzzle == null)
        {
            ShowPanel("SYSTEM", "NO MUZZLE", "Assign GunMuzzle", "WAITING", Color.red);
            return;
        }

        Vector3 start = gunMuzzle.position;
        Vector3 direction = gunMuzzle.forward.normalized;

        if (showDebugRay)
        {
            Debug.DrawRay(start, direction * 20f, Color.cyan);
        }

        if (TryDetectEarthLayer(
            start,
            direction,
            out string planetName,
            out string layerName,
            out string layerDescription,
            out Color layerColor))
        {
            ShowPanel(planetName, layerName, layerDescription, "IDENTIFIED", layerColor);
        }
        else
        {
            ShowPanel("SCANNING", "NO LAYER", "Point at a cut layer", "WAITING", new Color(0f, 0.8f, 1f));
        }

        FaceHologramToCamera();
    }

    bool IsActivationPressed()
    {
        if (activationButton == ActivationButton.AlwaysOn)
        {
            return true;
        }

        if (useMouseBackup && Input.GetMouseButton(0))
        {
            return true;
        }

        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        switch (activationButton)
        {
            case ActivationButton.LeftTrigger:
                return IsTriggerPressed(leftHand);

            case ActivationButton.LeftGrip:
                return IsGripPressed(leftHand);

            case ActivationButton.LeftPrimaryButton_X:
                return IsPrimaryPressed(leftHand);

            case ActivationButton.LeftSecondaryButton_Y:
                return IsSecondaryPressed(leftHand);

            case ActivationButton.RightTrigger:
                return IsTriggerPressed(rightHand);

            case ActivationButton.RightGrip:
                return IsGripPressed(rightHand);

            case ActivationButton.RightPrimaryButton_A:
                return IsPrimaryPressed(rightHand);

            case ActivationButton.RightSecondaryButton_B:
                return IsSecondaryPressed(rightHand);
        }

        return false;
    }

    bool IsTriggerPressed(InputDevice device)
    {
        if (!device.isValid) return false;

        bool pressedButton = false;
        float triggerValue = 0f;

        device.TryGetFeatureValue(CommonUsages.triggerButton, out pressedButton);
        device.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);

        return pressedButton || triggerValue > 0.15f;
    }

    bool IsGripPressed(InputDevice device)
    {
        if (!device.isValid) return false;

        bool pressedButton = false;
        float gripValue = 0f;

        device.TryGetFeatureValue(CommonUsages.gripButton, out pressedButton);
        device.TryGetFeatureValue(CommonUsages.grip, out gripValue);

        return pressedButton || gripValue > 0.15f;
    }

    bool IsPrimaryPressed(InputDevice device)
    {
        if (!device.isValid) return false;

        bool pressed = false;
        device.TryGetFeatureValue(CommonUsages.primaryButton, out pressed);
        return pressed;
    }

    bool IsSecondaryPressed(InputDevice device)
    {
        if (!device.isValid) return false;

        bool pressed = false;
        device.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed);
        return pressed;
    }

    bool TryDetectEarthLayer(
        Vector3 start,
        Vector3 direction,
        out string planetName,
        out string layerName,
        out string layerDescription,
        out Color layerColor
    )
    {
        planetName = "";
        layerName = "";
        layerDescription = "";
        layerColor = Color.white;

        RaycastHit[] hits = Physics.RaycastAll(
            start,
            direction,
            maxDistance,
            hitLayers,
            QueryTriggerInteraction.Ignore
        );

        bool found = false;
        float closestDistance = Mathf.Infinity;
        Transform playerRoot = transform.root;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            // Do not detect own player/controller/hand/gun/hologram.
            if (hit.collider.transform.root == playerRoot)
            {
                continue;
            }

            string hitPlanetName = GetPlanetNameFromLayerHit(hit.collider.transform, "");

            if (!GetLayerInfo(hit.collider.transform, hitPlanetName, out string foundName, out string foundDescription, out Color foundColor))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                planetName = hitPlanetName;
                layerName = foundName;
                layerDescription = foundDescription;
                layerColor = foundColor;
                found = true;
            }
        }

        return found;
    }

    string GetPlanetNameFromLayerHit(Transform hitTransform, string detectedLayerName)
    {
        Transform current = hitTransform;

        while (current != null)
        {
            string n = current.name.ToLower().Replace(" ", "").Replace("_", "");

            if (n.Contains("venus"))
            {
                return "VENUS";
            }

            if (n.Contains("earth"))
            {
                return "EARTH";
            }

            if (n.Contains("mercury"))
            {
                return "MERCURY";
            }

            if (n.Contains("mars"))
            {
                return "MARS";
            }

            if (n.Contains("jupiter"))
            {
                return "JUPITER";
            }

            if (n.Contains("saturn"))
            {
                return "SATURN";
            }

            if (n.Contains("uranus"))
            {
                return "URANUS";
            }

            if (n.Contains("neptune"))
            {
                return "NEPTUNE";
            }

            current = current.parent;
        }

        // Reliable fallback for the generated layer names in this project.
        string layer = detectedLayerName.ToLower();

        if (layer.Contains("co2") ||
            layer.Contains("sulfuric") ||
            layer.Contains("rocky mantle") ||
            layer.Contains("metallic"))
        {
            return "VENUS";
        }

        return "EARTH";
    }

    bool GetLayerInfo(
        Transform hitTransform,
        string planetName,
        out string layerName,
        out string layerDescription,
        out Color layerColor
    )
    {
        layerName = "";
        layerDescription = "";
        layerColor = Color.white;

        Transform current = hitTransform;

        while (current != null)
        {
            string n = current.name.ToLower().Replace(" ", "");

            // Gas giants (Jupiter, Saturn) are checked first: their generic
            // "Atmosphere"/"Core" object names would otherwise be misread as
            // Venus's CO2 atmosphere or Earth's core further down.
            if (planetName == "JUPITER" || planetName == "SATURN")
            {
                if (n.Contains("metallichydrogen"))
                {
                    layerName = "METALLIC H2";
                    layerDescription = "Metallic liquid hydrogen";
                    layerColor = new Color(0.55f, 0.55f, 0.75f);
                    return true;
                }

                if (n.Contains("molecularhydrogen"))
                {
                    layerName = "MOLECULAR H2";
                    layerDescription = "Liquid hydrogen layer";
                    layerColor = new Color(0.80f, 0.70f, 0.55f);
                    return true;
                }

                if (n.Contains("atmosphere"))
                {
                    layerName = "ATMOSPHERE";
                    layerDescription = "H2 / He cloud bands";
                    layerColor = new Color(0.90f, 0.82f, 0.60f);
                    return true;
                }

                if (n.Contains("core"))
                {
                    layerName = "CORE";
                    layerDescription = planetName == "JUPITER" ? "Dense rock/ice core" : "Rock and ice core";
                    layerColor = new Color(0.40f, 0.30f, 0.25f);
                    return true;
                }
            }

            // Ice giants (Uranus, Neptune).
            if (planetName == "URANUS" || planetName == "NEPTUNE")
            {
                if (n.Contains("icymantle"))
                {
                    layerName = "ICY MANTLE";
                    layerDescription = "Water-ammonia ices";
                    layerColor = planetName == "URANUS" ? new Color(0.25f, 0.55f, 0.65f) : new Color(0.18f, 0.30f, 0.60f);
                    return true;
                }

                if (n.Contains("atmosphere"))
                {
                    layerName = "ATMOSPHERE";
                    layerDescription = "H2 / He / methane";
                    layerColor = planetName == "URANUS" ? new Color(0.55f, 0.85f, 0.90f) : new Color(0.30f, 0.45f, 0.85f);
                    return true;
                }

                if (n.Contains("core"))
                {
                    layerName = "CORE";
                    layerDescription = "Small rocky core";
                    layerColor = new Color(0.33f, 0.29f, 0.27f);
                    return true;
                }
            }

            // Small rocky planets (Mercury, Mars).
            if (planetName == "MERCURY" || planetName == "MARS")
            {
                if (n.Contains("core"))
                {
                    layerName = "IRON CORE";
                    layerDescription = planetName == "MERCURY" ? "Huge solid iron core" : "Iron-sulfide core";
                    layerColor = planetName == "MERCURY" ? new Color(0.65f, 0.65f, 0.70f) : new Color(0.55f, 0.20f, 0.10f);
                    return true;
                }

                if (n.Contains("mantle"))
                {
                    layerName = "MANTLE";
                    layerDescription = "Rocky silicate layer";
                    layerColor = planetName == "MERCURY" ? new Color(0.45f, 0.30f, 0.20f) : new Color(0.80f, 0.40f, 0.15f);
                    return true;
                }

                if (n.Contains("crust"))
                {
                    layerName = "CRUST";
                    layerDescription = planetName == "MERCURY" ? "Thin rocky shell" : "Thin rusty crust";
                    layerColor = planetName == "MERCURY" ? new Color(0.55f, 0.50f, 0.45f) : new Color(0.72f, 0.35f, 0.20f);
                    return true;
                }
            }

            // Keep these short so they fit inside the hologram.
            if (n.Contains("carbondioxide") || n.Contains("co2") || n.Contains("atmosphere"))
            {
                layerName = "CO2 ATMOSPHERE";
                layerDescription = "Dense gas layer";
                layerColor = new Color(0.95f, 0.82f, 0.48f);
                return true;
            }

            if (n.Contains("sulfuric") || n.Contains("sulphuric") || n.Contains("cloud"))
            {
                layerName = "SULFURIC ACID";
                layerDescription = "Acid cloud layer";
                layerColor = new Color(1.00f, 0.86f, 0.38f);
                return true;
            }

            if (n.Contains("outercore"))
            {
                layerName = "OUTER CORE";
                layerDescription = "Liquid metal";
                layerColor = new Color(0.95f, 0.73f, 0.22f);
                return true;
            }

            if (n.Contains("innercore"))
            {
                layerName = "INNER CORE";
                layerDescription = "Solid centre";
                layerColor = new Color(1.00f, 0.92f, 0.55f);
                return true;
            }

            if (n.Contains("metalliccore"))
            {
                layerName = "METALLIC CORE";
                layerDescription = "Dense metal centre";
                layerColor = new Color(0.82f, 0.25f, 0.08f);
                return true;
            }

            if (n.Contains("crust"))
            {
                layerName = "CRUST";
                layerDescription = "Thin outer shell";
                layerColor = new Color(0.73f, 0.47f, 0.23f);
                return true;
            }

            if (n.Contains("rockymantle"))
            {
                layerName = "ROCKY MANTLE";
                layerDescription = "Hot rocky interior";
                layerColor = new Color(0.92f, 0.45f, 0.18f);
                return true;
            }

            if (n.Contains("mantle"))
            {
                layerName = "MANTLE";
                layerDescription = "Hot rocky layer";
                layerColor = new Color(0.92f, 0.52f, 0.22f);
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    void AddMeshCollidersToEarthLayers()
    {
        MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null || mf.sharedMesh == null)
            {
                continue;
            }

            if (!IsEarthLayerName(mf.gameObject.name))
            {
                continue;
            }

            // Skip flying half because it has Rigidbody.
            Rigidbody rootRb = mf.transform.root.GetComponent<Rigidbody>();
            if (rootRb != null)
            {
                continue;
            }

            MeshCollider meshCollider = mf.gameObject.GetComponent<MeshCollider>();

            if (meshCollider == null)
            {
                meshCollider = mf.gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = mf.sharedMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
        }
    }

    bool IsEarthLayerName(string objectName)
    {
        string n = objectName.ToLower().Replace(" ", "");

        return
            n.Contains("crust") ||
            n.Contains("mantle") ||
            n.Contains("rockymantle") ||
            n.Contains("icymantle") ||
            n.Contains("outercore") ||
            n.Contains("innercore") ||
            n.Contains("metalliccore") ||
            n.Contains("core") ||
            n.Contains("carbondioxide") ||
            n.Contains("co2") ||
            n.Contains("atmosphere") ||
            n.Contains("sulfuric") ||
            n.Contains("sulphuric") ||
            n.Contains("cloud") ||
            n.Contains("molecularhydrogen") ||
            n.Contains("metallichydrogen");
    }

    void BuildProjectorAndHologram()
    {
        projectorRoot = new GameObject("Left Hand Hologram Projector");
        projectorRoot.transform.SetParent(transform);
        projectorRoot.transform.localPosition = projectorLocalPosition;
        projectorRoot.transform.localRotation = Quaternion.Euler(projectorLocalRotation);
        projectorRoot.transform.localScale = Vector3.one;

        CreateCube(
            "Small Projector Cube",
            projectorRoot.transform,
            Vector3.zero,
            Vector3.zero,
            projectorLocalScale,
            projectorMat
        );

        CreateCube(
            "Projector Glow Strip",
            projectorRoot.transform,
            new Vector3(0f, 0.021f, 0f),
            Vector3.zero,
            new Vector3(projectorLocalScale.x * 0.75f, 0.006f, projectorLocalScale.z * 0.65f),
            cyanMat
        );

        hologramRoot = new GameObject("Left Hand Hologram Panel");
        hologramRoot.transform.SetParent(projectorRoot.transform);
        hologramRoot.transform.localPosition = hologramLocalPosition;
        hologramRoot.transform.localRotation = Quaternion.Euler(hologramLocalRotation);
        hologramRoot.transform.localScale = hologramLocalScale;

        GameObject panel = CreateCube(
            "Hologram Panel",
            hologramRoot.transform,
            Vector3.zero,
            Vector3.zero,
            panelLocalScale,
            panelMat
        );

        panelRenderer = panel.GetComponent<Renderer>();

        float halfWidth = panelLocalScale.x * 0.5f;
        float halfHeight = panelLocalScale.y * 0.5f;

        CreateCube(
            "Top Border",
            hologramRoot.transform,
            new Vector3(0f, halfHeight + 0.004f, -0.004f),
            Vector3.zero,
            new Vector3(panelLocalScale.x + 0.015f, 0.006f, 0.008f),
            cyanMat
        );

        CreateCube(
            "Bottom Border",
            hologramRoot.transform,
            new Vector3(0f, -halfHeight - 0.004f, -0.004f),
            Vector3.zero,
            new Vector3(panelLocalScale.x + 0.015f, 0.006f, 0.008f),
            cyanMat
        );

        CreateCube(
            "Left Border",
            hologramRoot.transform,
            new Vector3(-halfWidth - 0.004f, 0f, -0.004f),
            Vector3.zero,
            new Vector3(0.006f, panelLocalScale.y + 0.015f, 0.008f),
            cyanMat
        );

        CreateCube(
            "Right Border",
            hologramRoot.transform,
            new Vector3(halfWidth + 0.004f, 0f, -0.004f),
            Vector3.zero,
            new Vector3(0.006f, panelLocalScale.y + 0.015f, 0.008f),
            cyanMat
        );

        float leftX = -halfWidth + 0.035f;
        float rightX = halfWidth - 0.035f;
        float titleY = halfHeight - 0.038f;
        float layerY = halfHeight - 0.090f;
        float descriptionY = -0.020f;
        float statusY = -halfHeight + 0.030f;

        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = "Layer Colour Dot";
        dot.transform.SetParent(hologramRoot.transform);
        dot.transform.localPosition = new Vector3(leftX, layerY, -0.012f);
        dot.transform.localRotation = Quaternion.identity;
        dot.transform.localScale = new Vector3(0.018f, 0.018f, 0.018f);
        RemoveCollider(dot);

        colorDotRenderer = dot.GetComponent<Renderer>();
        colorDotRenderer.material = layerColorMat;

        // Panel information hierarchy:
        // 1) Planet name
        // 2) Layer name
        // 3) Short explanation
        planetText = CreateText(
            "Planet Heading Text",
            hologramRoot.transform,
            new Vector3(0f, titleY, -0.014f),
            0.0062f,
            TextAnchor.MiddleCenter
        );
        planetText.alignment = TextAlignment.Center;

        titleText = CreateText(
            "Layer Title Text",
            hologramRoot.transform,
            new Vector3(leftX + 0.028f, layerY, -0.014f),
            0.0072f,
            TextAnchor.MiddleLeft
        );

        descriptionText = CreateText(
            "Layer Description Text",
            hologramRoot.transform,
            new Vector3(0f, descriptionY, -0.014f),
            0.0049f,
            TextAnchor.MiddleCenter
        );
        descriptionText.alignment = TextAlignment.Center;

        statusText = CreateText(
            "Layer Status Text",
            hologramRoot.transform,
            new Vector3(0f, statusY, -0.014f),
            0.0048f,
            TextAnchor.MiddleCenter
        );
        statusText.alignment = TextAlignment.Center;
    }

    void ShowPanel(string planetName, string layerName, string description, string status, Color color)
    {
        if (hologramRoot == null)
        {
            return;
        }

        hologramRoot.SetActive(true);

        if (planetText != null)
        {
            planetText.text = FitSingleLine("PLANET: " + planetName, 24);
            planetText.color = new Color(0.50f, 1f, 1f);
        }

        titleText.text = FitSingleLine(layerName, 22);
        descriptionText.text = WrapText(description, 30, 2);
        statusText.text = FitSingleLine(status, 26);

        titleText.color = color;
        descriptionText.color = Color.white;
        statusText.color = new Color(0.50f, 1f, 1f);

        SetLayerVisualColor(color);

        string displayKey = planetName + " - " + layerName;

        if (lastLayer != displayKey)
        {
            lastLayer = displayKey;
            Debug.Log("Left hand hologram: " + displayKey);
        }
    }

    string FitSingleLine(string text, int maxCharacters)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxCharacters)
        {
            return text;
        }

        return text.Substring(0, Mathf.Max(0, maxCharacters - 1)) + ".";
    }

    string WrapText(string text, int maxCharactersPerLine, int maxLines)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        string[] words = text.Split(' ');
        string result = "";
        string line = "";
        int lines = 0;

        foreach (string word in words)
        {
            string nextLine = string.IsNullOrEmpty(line) ? word : line + " " + word;

            if (nextLine.Length > maxCharactersPerLine && !string.IsNullOrEmpty(line))
            {
                if (lines > 0)
                {
                    result += "\n";
                }

                result += line;
                lines++;
                line = word;

                if (lines >= maxLines)
                {
                    return result;
                }
            }
            else
            {
                line = nextLine;
            }
        }

        if (!string.IsNullOrEmpty(line) && lines < maxLines)
        {
            if (lines > 0)
            {
                result += "\n";
            }

            result += line;
        }

        return result;
    }

    void HideHologram()
    {
        if (hologramRoot != null)
        {
            hologramRoot.SetActive(false);
        }
    }

    TextMesh CreateText(
        string objectName,
        Transform parent,
        Vector3 localPosition,
        float characterSize,
        TextAnchor anchor
    )
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.identity;
        textObject.transform.localScale = Vector3.one;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.fontSize = 48;
        text.characterSize = characterSize;
        text.anchor = anchor;
        text.alignment = TextAlignment.Left;
        text.color = Color.white;
        text.text = "";

        return text;
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

    void FaceHologramToCamera()
    {
        if (!hologramFacesCamera || hologramRoot == null || !hologramRoot.activeSelf)
        {
            return;
        }

        Camera cam = Camera.main;

        if (cam == null)
        {
            return;
        }

        hologramRoot.transform.LookAt(cam.transform.position);
        hologramRoot.transform.Rotate(0f, 180f, 0f);
    }

    void CreateMaterials()
    {
        projectorMat = CreateMaterial(new Color(0.012f, 0.014f, 0.020f), false);
        cyanMat = CreateMaterial(new Color(0f, 0.85f, 1f), true);
        panelMat = CreateMaterial(new Color(0f, 0.70f, 1f, 0.35f), true);
        layerColorMat = CreateMaterial(new Color(0f, 0.75f, 1f), true);
    }

    void SetLayerVisualColor(Color color)
    {
        SetMaterialColor(layerColorMat, color, true);

        if (colorDotRenderer != null)
        {
            colorDotRenderer.material = layerColorMat;
        }

        if (panelRenderer != null)
        {
            Color panelColor = new Color(color.r, color.g, color.b, 0.35f);
            SetMaterialColor(panelRenderer.material, panelColor, true);
        }
    }

    Material CreateMaterial(Color color, bool emission)
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

        Material material = new Material(shader);
        SetMaterialColor(material, color, emission);
        return material;
    }

    void SetMaterialColor(Material material, Color color, bool emission)
    {
        if (material == null)
        {
            return;
        }

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

        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }
    }

    void RemoveCollider(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();

        if (col != null)
        {
            Destroy(col);
        }
    }

    void DisableOldLayerDisplayScripts()
    {
        MonoBehaviour[] scripts = FindObjectsOfType<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null)
            {
                continue;
            }

            string scriptName = script.GetType().Name;

            if (scriptName == "EarthLayerHoverLabels" || scriptName == "EarthLayerWristHologram")
            {
                script.enabled = false;
            }
        }
    }
}
