using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

// Which planet this instance represents. Terrestrial planets (Mercury, Mars)
// get Crust/Mantle/Core. Gas giants (Jupiter, Saturn) get an extra metallic
// hydrogen layer. Ice giants (Uranus, Neptune) get an icy mantle instead of
// a rocky one.
public enum RemainingPlanetKind
{
    Mercury,
    Mars,
    Jupiter,
    Saturn,
    Uranus,
    Neptune
}

// Splits Mercury, Mars, Jupiter, Saturn, Uranus or Neptune into two halves
// and builds their internal layers, the same way ExactHalfPlanetSplitter
// does for Earth and VenusHalfPlanetSplitter does for Venus. The mesh math
// lives in PlanetSplitMeshUtility so it is not duplicated per planet.
public class PlanetHalfPlanetSplitter : MonoBehaviour
{
    [Header("Which Planet")]
    public RemainingPlanetKind planetKind = RemainingPlanetKind.Mars;

    [Header("Split Shape Detail")]
    public int segments = 64;
    public int rings = 32;

    [Header("Which Half Flies Away")]
    public bool rightHalfShouldFly = true;

    [Header("Split Motion")]
    public float smallOpeningDistance = 0.15f;
    public float flyingHalfMoveDistance = 22.0f;
    public float flyingHalfMoveDuration = 2.2f;
    public float flyingHalfSpinDegrees = 420f;
    public bool destroyFlyingHalfAfterMove = true;

    [Header("Surface Skin")]
    public bool keepOriginalTexture = true;
    public float outerSurfaceScaleBoost = 1.002f;

    [Header("Tablet")]
    public bool showTabletAfterSplit = true;
    public float tabletOpenDelay = 0.25f;
    public Vector3 tabletLocalOffset = new Vector3(0.85f, 0.25f, 0f);
    public float tabletScale = 0.65f;

    [Header("Optional Test")]
    public bool allowPKeyTest = false;

    private bool alreadySplit = false;

    private struct LayerSpec
    {
        public string objectName;
        public string label;
        public string description;
        public float outerRatio;
        public float innerRatio; // negative means this is the solid innermost layer
        public Color color;
        public bool transparent;

        public LayerSpec(string objectName, string label, string description, float outerRatio, float innerRatio, Color color, bool transparent)
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

        Debug.Log(planetKind + " split triggered on: " + gameObject.name);

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

        List<LayerSpec> layers = GetLayersForPlanet(planetKind);

        Material surfaceMat = null;

        if (keepOriginalTexture && originalRenderer != null && originalRenderer.sharedMaterial != null)
        {
            surfaceMat = new Material(originalRenderer.sharedMaterial);
        }

        Vector3 splitAxis = transform.right.normalized;

        GameObject leftHalf = CreateLayeredHalf(gameObject.name + "_Left_Half", radius, false, surfaceMat, layers);
        GameObject rightHalf = CreateLayeredHalf(gameObject.name + "_Right_Half", radius, true, surfaceMat, layers);

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

        AddStaticHalfCollider(staticHalf, radius);

        HideOriginalPlanet();

        if (showTabletAfterSplit)
        {
            StartCoroutine(CreateTabletAfterDelay(staticHalf, layers));
        }

        Destroy(gameObject, 0.2f);
    }

    private List<LayerSpec> GetLayersForPlanet(RemainingPlanetKind kind)
    {
        List<LayerSpec> layers = new List<LayerSpec>();

        switch (kind)
        {
            case RemainingPlanetKind.Mercury:
                layers.Add(new LayerSpec("Crust", "CRUST", "Thin rocky shell", 1.00f, 0.90f, new Color(0.55f, 0.50f, 0.45f), false));
                layers.Add(new LayerSpec("Mantle", "MANTLE", "Thin silicate layer", 0.90f, 0.83f, new Color(0.45f, 0.30f, 0.20f), false));
                layers.Add(new LayerSpec("Core", "IRON CORE", "Huge solid iron core", 0.83f, -1f, new Color(0.65f, 0.65f, 0.70f), false));
                break;

            case RemainingPlanetKind.Mars:
                layers.Add(new LayerSpec("Crust", "CRUST", "Thin rusty crust", 1.00f, 0.92f, new Color(0.72f, 0.35f, 0.20f), false));
                layers.Add(new LayerSpec("Mantle", "MANTLE", "Rocky silicate layer", 0.92f, 0.53f, new Color(0.80f, 0.40f, 0.15f), false));
                layers.Add(new LayerSpec("Core", "IRON CORE", "Iron-sulfide core", 0.53f, -1f, new Color(0.55f, 0.20f, 0.10f), false));
                break;

            case RemainingPlanetKind.Jupiter:
                layers.Add(new LayerSpec("Atmosphere", "ATMOSPHERE", "H2 / He cloud bands", 1.00f, 0.85f, new Color(0.90f, 0.80f, 0.65f, 0.35f), true));
                layers.Add(new LayerSpec("MolecularHydrogen", "MOLECULAR H2", "Liquid hydrogen layer", 0.85f, 0.50f, new Color(0.80f, 0.70f, 0.55f), false));
                layers.Add(new LayerSpec("MetallicHydrogen", "METALLIC H2", "Metallic liquid H2", 0.50f, 0.15f, new Color(0.55f, 0.55f, 0.75f), false));
                layers.Add(new LayerSpec("Core", "CORE", "Dense rock/ice core", 0.15f, -1f, new Color(0.40f, 0.30f, 0.25f), false));
                break;

            case RemainingPlanetKind.Saturn:
                layers.Add(new LayerSpec("Atmosphere", "ATMOSPHERE", "H2 / He cloud bands", 1.00f, 0.85f, new Color(0.90f, 0.85f, 0.65f, 0.35f), true));
                layers.Add(new LayerSpec("MolecularHydrogen", "MOLECULAR H2", "Liquid hydrogen layer", 0.85f, 0.55f, new Color(0.85f, 0.75f, 0.55f), false));
                layers.Add(new LayerSpec("MetallicHydrogen", "METALLIC H2", "Metallic liquid H2", 0.55f, 0.20f, new Color(0.60f, 0.58f, 0.70f), false));
                layers.Add(new LayerSpec("Core", "CORE", "Rock and ice core", 0.20f, -1f, new Color(0.40f, 0.32f, 0.28f), false));
                break;

            case RemainingPlanetKind.Uranus:
                layers.Add(new LayerSpec("Atmosphere", "ATMOSPHERE", "H2 / He / methane", 1.00f, 0.80f, new Color(0.55f, 0.85f, 0.90f, 0.35f), true));
                layers.Add(new LayerSpec("IcyMantle", "ICY MANTLE", "Water-ammonia ices", 0.80f, 0.20f, new Color(0.25f, 0.55f, 0.65f), false));
                layers.Add(new LayerSpec("Core", "CORE", "Small rocky core", 0.20f, -1f, new Color(0.35f, 0.30f, 0.28f), false));
                break;

            case RemainingPlanetKind.Neptune:
                layers.Add(new LayerSpec("Atmosphere", "ATMOSPHERE", "H2 / He / methane", 1.00f, 0.80f, new Color(0.30f, 0.45f, 0.85f, 0.35f), true));
                layers.Add(new LayerSpec("IcyMantle", "ICY MANTLE", "Water-ammonia ices", 0.80f, 0.25f, new Color(0.18f, 0.30f, 0.60f), false));
                layers.Add(new LayerSpec("Core", "CORE", "Small rocky core", 0.25f, -1f, new Color(0.32f, 0.28f, 0.26f), false));
                break;
        }

        return layers;
    }

    private GameObject CreateLayeredHalf(string rootName, float radius, bool positiveXHalf, Material surfaceMat, List<LayerSpec> layers)
    {
        GameObject root = new GameObject(rootName);

        root.transform.position = transform.position;
        root.transform.rotation = transform.rotation;
        root.transform.localScale = transform.lossyScale;

        for (int i = 0; i < layers.Count; i++)
        {
            LayerSpec layer = layers[i];
            Material mat = PlanetSplitMeshUtility.CreateLayerMaterial(layer.color, layer.transparent);
            float outer = radius * layer.outerRatio;

            if (layer.innerRatio < 0f)
            {
                CreateSolidPart(layer.objectName, root.transform, outer, positiveXHalf, mat);
            }
            else
            {
                float inner = radius * layer.innerRatio;
                CreateShellPart(layer.objectName, root.transform, outer, inner, positiveXHalf, mat);
            }
        }

        if (surfaceMat != null && layers.Count > 0)
        {
            float outerMost = radius * layers[0].outerRatio;

            CreateOuterSurfacePart(
                planetKind + "SurfaceTexture",
                root.transform,
                outerMost * outerSurfaceScaleBoost,
                positiveXHalf,
                surfaceMat
            );
        }

        return root;
    }

    private IEnumerator CreateTabletAfterDelay(GameObject staticHalf, List<LayerSpec> layers)
    {
        yield return null;

        if (tabletOpenDelay > 0f)
        {
            yield return new WaitForSeconds(tabletOpenDelay);
        }

        if (staticHalf != null)
        {
            CreateTablet(staticHalf.transform, layers);
        }
    }

    private void CreateTablet(Transform parent, List<LayerSpec> layers)
    {
        GameObject tablet = new GameObject(planetKind + " Layer Tablet");
        tablet.transform.SetParent(parent);
        tablet.transform.localPosition = tabletLocalOffset;
        tablet.transform.localRotation = Quaternion.identity;
        tablet.transform.localScale = Vector3.one * tabletScale;

        Material tabletMat = PlanetSplitMeshUtility.CreateLayerMaterial(new Color(0.02f, 0.025f, 0.035f, 0.88f), true);
        Material borderMat = PlanetSplitMeshUtility.CreateLayerMaterial(new Color(1f, 0.78f, 0.25f, 1f), true);

        PlanetSplitMeshUtility.CreateCube("Tablet Background", tablet.transform, Vector3.zero, Vector3.zero, new Vector3(0.95f, 0.58f, 0.025f), tabletMat);

        PlanetSplitMeshUtility.CreateCube("Tablet Top Border", tablet.transform, new Vector3(0f, 0.305f, -0.015f), Vector3.zero, new Vector3(0.98f, 0.025f, 0.025f), borderMat);
        PlanetSplitMeshUtility.CreateCube("Tablet Bottom Border", tablet.transform, new Vector3(0f, -0.305f, -0.015f), Vector3.zero, new Vector3(0.98f, 0.025f, 0.025f), borderMat);
        PlanetSplitMeshUtility.CreateCube("Tablet Left Border", tablet.transform, new Vector3(-0.50f, 0f, -0.015f), Vector3.zero, new Vector3(0.025f, 0.60f, 0.025f), borderMat);
        PlanetSplitMeshUtility.CreateCube("Tablet Right Border", tablet.transform, new Vector3(0.50f, 0f, -0.015f), Vector3.zero, new Vector3(0.025f, 0.60f, 0.025f), borderMat);

        TextMesh text = PlanetSplitMeshUtility.CreateText("Tablet Text", tablet.transform, new Vector3(-0.43f, 0.20f, -0.04f), 0.045f, TextAnchor.UpperLeft);

        string body = planetKind.ToString().ToUpper() + " LAYERS\n\n";

        for (int i = 0; i < layers.Count; i++)
        {
            body += layers[i].label + "\n";
        }

        text.text = body;
        text.color = Color.white;

        VenusTabletBillboard billboard = tablet.AddComponent<VenusTabletBillboard>();
        billboard.keepUpright = true;
    }

    private void AddStaticHalfCollider(GameObject half, float radius)
    {
        SphereCollider collider = half.AddComponent<SphereCollider>();
        collider.isTrigger = false;
        collider.center = Vector3.zero;
        collider.radius = radius;
    }

    private void MakeFlyingHalf(GameObject half, Vector3 flyDirection)
    {
        VenusFlyingHalfMover mover = half.AddComponent<VenusFlyingHalfMover>();
        mover.Begin(flyDirection.normalized, flyingHalfMoveDistance, flyingHalfMoveDuration, flyingHalfSpinDegrees, destroyFlyingHalfAfterMove);

        Debug.Log(half.name + " will fly far away.");
    }

    private void HideOriginalPlanet()
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

    private void CreateShellPart(string name, Transform parent, float outerRadius, float innerRadius, bool positiveXHalf, Material material)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent);
        part.transform.localPosition = Vector3.zero;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one;

        MeshFilter mf = part.AddComponent<MeshFilter>();
        MeshRenderer mr = part.AddComponent<MeshRenderer>();

        mf.mesh = PlanetSplitMeshUtility.CreateHalfShellMesh(outerRadius, innerRadius, positiveXHalf, segments, rings);
        mr.material = material;
    }

    private void CreateSolidPart(string name, Transform parent, float radius, bool positiveXHalf, Material material)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent);
        part.transform.localPosition = Vector3.zero;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one;

        MeshFilter mf = part.AddComponent<MeshFilter>();
        MeshRenderer mr = part.AddComponent<MeshRenderer>();

        mf.mesh = PlanetSplitMeshUtility.CreateSolidHalfSphereMesh(radius, positiveXHalf, segments, rings);
        mr.material = material;
    }

    private void CreateOuterSurfacePart(string name, Transform parent, float radius, bool positiveXHalf, Material material)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent);
        part.transform.localPosition = Vector3.zero;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one;

        MeshFilter mf = part.AddComponent<MeshFilter>();
        MeshRenderer mr = part.AddComponent<MeshRenderer>();

        mf.mesh = PlanetSplitMeshUtility.CreateOuterSurfaceHalfMesh(radius, positiveXHalf, segments, rings);
        mr.material = material;
    }
}
