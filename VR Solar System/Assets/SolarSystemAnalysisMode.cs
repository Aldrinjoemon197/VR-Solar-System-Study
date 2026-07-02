using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class SolarSystemAnalysisMode : MonoBehaviour
{
    private enum AnalysisPage
    {
        Menu,
        Distance,
        Compare,
        CompareCharts,
        Moons
    }

    private enum AnalysisButton
    {
        None,
        Distance,
        Compare,
        Moons,
        GasCharts,
        Exit,
        Back
    }

    [Header("Required References")]
    [Tooltip("Drag RightHand Controller > GunMuzzle here.")]
    public Transform gunMuzzle;

    [Tooltip("Drag your Sun object here. Planet orbit movement uses this as the center.")]
    public Transform sun;

    [Tooltip("Leave empty if this script is on LeftHand Controller.")]
    public Transform hologramParent;

    [Header("Controls")]
    [Tooltip("Left controller Y button toggles Analysis Mode.")]
    public bool toggleWithLeftY = true;

    [Tooltip("Right trigger selects menu buttons and planets.")]
    public bool selectWithRightTrigger = true;

    [Tooltip("Prevents accidental half-trigger presses from causing missed/repeated selections.")]
    public float triggerSelectPressThreshold = 0.45f;

    [Tooltip("Small delay after each selection, to avoid double-click glitches.")]
    public float selectionCooldownSeconds = 0.18f;

    [Tooltip("Useful for testing in editor. Keyboard Y toggles, Mouse Right selects.")]
    public bool useKeyboardMouseBackup = true;

    [Header("Pause Settings")]
    [Tooltip("ON = Time.timeScale becomes 0 while analysis mode is active.")]
    public bool pauseTimeScale = true;

    [Tooltip("ON = QuestHandGunLaser is disabled during analysis mode, so right trigger does not split planets.")]
    public bool disableGunScriptInAnalysisMode = true;

    [Tooltip("ON = existing layer hologram projector is disabled during analysis mode to avoid overlapping screens.")]
    public bool disableLayerHologramInAnalysisMode = true;

    [Header("Planet Detection")]
    public LayerMask planetHitLayers = ~0;

    [Tooltip("Automatically adds SphereCollider to known planet objects if missing.")]
    public bool autoAddPlanetColliders = true;

    [Tooltip("Radius used if auto-added SphereCollider has no good size.")]
    public float defaultPlanetColliderRadius = 0.5f;

    [Tooltip("Bigger value makes planet selection easier from far away.")]
    public float planetSelectionSphereRadius = 0.80f;

    [Tooltip("When selecting a new planet, ignore the currently selected planet if another planet is under the pointer.")]
    public bool preferNewPlanetOverActivePlanet = false;

    [Tooltip("ON = when A-B is already selected in Distance Mode, aiming at a third planet shifts cleanly to B-C.")]
    public bool stronglyPreferThirdPlanetWhenDistancePairFull = true;

    [Tooltip("Higher value makes third-planet shifting easier when the old second planet is still close to the ray.")]
    public float distanceShiftThirdPlanetTolerance = 1.2f;

    [Header("Planet Orbit Movement")]
    [Tooltip("When ON, selected planet stays on its orbit circle around Sun.")]
    public bool keepPlanetOnOrbitCircle = true;

    [Tooltip("If ray-plane dragging feels wrong, turn OFF and use joystick movement instead.")]
    public bool moveSelectedPlanetByAimingRay = true;

    [Tooltip("Right joystick horizontal speed when moving selected planet around orbit.")]
    public float joystickOrbitSpeedDegrees = 90f;

    [Tooltip("Lower = smoother/slower planet dragging with trigger. Try 1.5 to 3.")]
    public float planetMoveSmoothSpeed = 1.0f;

    [Header("Distance Display")]
    [Tooltip("Keep 1 if you want Unity units. Example: 10 means 1 Unity unit = 10 million km.")]
    public float unityUnitToMillionKm = 1f;

    [Tooltip("ON = display both Unity units and approximate million km.")]
    public bool showMillionKmAlso = false;

    [Header("Hologram Placement")]
    public Vector3 hologramLocalPosition = new Vector3(0f, 0.22f, 0.10f);
    public Vector3 hologramLocalRotation = new Vector3(65f, 0f, 0f);
    public Vector3 hologramLocalScale = new Vector3(0.38f, 0.38f, 0.38f);

    [Header("Hand-Based Hologram Placement")]
    [Tooltip("Drag LeftHand Controller > AstronautHand or Group6942 here. This keeps both holograms above the glove.")]
    public Transform handVisualRoot;

    [Tooltip("ON = place hologram using the actual glove renderer bounds instead of guessing local position.")]
    public bool useHandBoundsForHologram = false;

    [Tooltip("How high above the top of the glove the hologram floats.")]
    public float hologramHeightAboveHand = 0.32f;

    [Tooltip("Pulls the hologram slightly toward your face/camera so it does not pass through the glove.")]
    public float hologramTowardCamera = 0.18f;

    [Tooltip("Side shift if needed. Usually keep 0.")]
    public float hologramSideOffset = 0f;

    [Tooltip("Keeps the panel readable by facing the camera.")]
    public bool handPlacedHologramFacesCamera = true;

    [Header("BEST FIX: Shared Hologram Anchor")]
    [Tooltip("Create one Empty object above the left glove and drag it here. Use the same anchor for both hologram scripts.")]
    public Transform sharedHologramAnchor;

    [Tooltip("ON = use Shared Hologram Anchor. This makes all pages appear in the same position.")]
    public bool useSharedHologramAnchor = true;

    [Tooltip("ON = hologram always faces the VR Main Camera.")]
    public bool sharedAnchorFacesCamera = true;

    [Tooltip("Small world offset from the anchor if needed. Usually keep zero.")]
    public Vector3 sharedAnchorWorldOffset = Vector3.zero;

    [Tooltip("If the hologram faces the wrong side, use 180. Normal value is 0.")]
    public float sharedAnchorYawCorrection = 0f;

    [Tooltip("ON forces the hologram high every time Play starts, even if old Inspector values were stored.")]
    public bool forceHologramHighAboveHand = false;

    [Tooltip("Visible floating position above the hand, not too high and not through the hand.")]
    public Vector3 forcedHighHologramLocalPosition = new Vector3(0f, 0.55f, 0.22f);

    [Header("Analysis Ray")]
    public bool showAnalysisRay = true;

    [Tooltip("How far selection ray can hit planets/buttons.")]
    public float analysisRayDistance = 1000f;

    [Tooltip("How long the visible cyan pointer looks when it does not hit anything.")]
    public float analysisRayVisibleLength = 80f;

    public float analysisRayRadius = 0.012f;
    public Color analysisRayColor = new Color(0f, 0.9f, 1f, 1f);

    [Header("Distance Line")]
    public bool showDistanceLineBetweenSelectedPlanets = true;
    public float distanceLineRadius = 0.16f;

    [Tooltip("Arrow cone size at both ends of the distance line.")]
    public float distanceArrowLength = 1.80f;
    public float distanceArrowRadius = 0.75f;

    public float distanceLabelCharacterSize = 0.35f;

    [Tooltip("Zero means the km text stays exactly in the middle of the line.")]
    public Vector3 distanceLabelOffset = new Vector3(0f, 0f, 0f);

    [Tooltip("ON forces a very thick distance measurement line at runtime.")]
    public bool forceVeryThickDistanceLine = true;

    [Header("Dotted Distance Line")]
    public bool useDottedDistanceLine = true;

    [Tooltip("How many dash pieces make the dotted line.")]
    public int distanceLineDashCount = 24;

    [Tooltip("0.5 means each dash takes half of each segment, leaving gaps.")]
    public float distanceLineDashFill = 0.45f;

    [Header("Saturn Ring Fix")]
    [Tooltip("Drag your Saturn ring object here if it is not already a child of Saturn.")]
    public Transform saturnRingObject;

    [Tooltip("Also tries to auto-move objects with names like SaturnRing / saturn ring.")]
    public bool autoMoveSaturnRingObjects = true;

    [Header("Analysis Flight Movement")]
    [Tooltip("Drag XR Origin (VR) here. If empty, script tries to find XR Origin automatically.")]
    public Transform playerRigToMove;

    [Tooltip("Usually Main Camera. If empty, Camera.main is used.")]
    public Transform flightDirectionReference;

    [Tooltip("Left controller thumbstick speed: forward/back/left/right.")]
    public float analysisFlySpeed = 2.0f;

    [Tooltip("A button down and B button up speed.")]
    public float analysisVerticalSpeed = 1.5f;

    [Tooltip("Left thumbstick moves player in Analysis Mode.")]
    public bool leftThumbstickMovesPlayer = true;

    [Tooltip("Right controller A button moves player down.")]
    public bool rightAButtonMovesDown = true;

    [Tooltip("Right controller B button moves player up. If you said V, this is usually the B button on Quest.")]
    public bool rightBButtonMovesUp = true;
    [Header("Right Thumbstick Camera Rotation")]
    [Tooltip("ON = right thumbstick rotates the view while in analysis mode.")]
    public bool rightThumbstickRotatesCamera = true;

    [Tooltip("Optional. Drag XR Origin (VR) > Camera Offset here for up/down camera rotation. If empty, script tries to find it.")]
    public Transform cameraPitchTarget;

    [Tooltip("Left/right turning speed for the right thumbstick.")]
    public float cameraYawSpeed = 75f;

    [Tooltip("Up/down look speed for the right thumbstick.")]
    public float cameraPitchSpeed = 55f;

    [Tooltip("Lowest downward/upward pitch angle.")]
    public float cameraPitchMin = -70f;

    [Tooltip("Highest upward/downward pitch angle.")]
    public float cameraPitchMax = 70f;

    [Tooltip("Small joystick deadzone to avoid camera drifting.")]
    public float cameraRotationDeadzone = 0.18f;

    [Tooltip("ON = right stick rotates only one direction at a time: left/right OR up/down, never diagonal/all-around.")]
    public bool cameraRotationDominantAxisOnly = true;

    [Header("Compare Mode Gas Bar Charts")]
    [Tooltip("Bars animate from 0 to the target percentage when planets are selected in Compare Mode.")]
    public bool showCompareGasCharts = true;

    [Tooltip("How fast the gas bars grow toward the final percentage.")]
    public float compareChartAnimationSpeed = 95f;

    [Tooltip("Maximum height of each gas bar inside the hologram chart.")]
    public float compareChartBarMaxHeight = 0.055f;

    [Header("Compact Gas Chart Layout")]
    [Tooltip("Overall size of the two gas charts. Keep below 1 to stay inside the hologram.")]
    public float compareChartPageScale = 0.72f;

    [Tooltip("Small planet name below each chart.")]
    public float compareChartPlanetNameSize = 0.0032f;

    [Tooltip("Small gas-label size below each bar.")]
    public float compareChartGasLabelSize = 0.0021f;

    [Tooltip("Small percentage-number size above each bar.")]
    public float compareChartValueSize = 0.0022f;

    [Header("Moons Mode")]
    [Tooltip("Shows a MOONS page in Analysis Mode. Aim at a planet and press the right trigger.")]
    public bool enableMoonsMode = true;

    [Tooltip("Fixed educational total shown in the hologram for the eight planets in this project.")]
    public int totalMoonsAroundEightPlanets = 422;

    [Header("Ratio-Based Visual Moon Models")]
    [Tooltip("ON = uses a clean visual ratio instead of trying to draw every real moon.")]
    public bool useRatioBasedVisualMoonCounts = true;

    [Tooltip("Visual moons for Jupiter. The hologram still shows the full scientific count.")]
    public int jupiterVisualMoonCount = 20;

    [Tooltip("Visual moons for Saturn. The hologram still shows the full scientific count.")]
    public int saturnVisualMoonCount = 30;

    [Tooltip("Visual moons for Uranus. Kept smaller than Jupiter/Saturn but larger than Neptune.")]
    public int uranusVisualMoonCount = 12;

    [Tooltip("Visual moons for Neptune.")]
    public int neptuneVisualMoonCount = 8;

    [Tooltip("Fallback cap if Ratio-Based Visual Moon Counts is turned OFF.")]
    public int maxVisualMoons = 30;

    [Tooltip("ON = creates every moon model for a planet. Not recommended for Saturn in Quest VR because it can look crowded.")]
    public bool visualizeEveryMoon = false;

    [Tooltip("Speed of the visual moon orbits in degrees per second.")]
    public float moonOrbitSpeedDegrees = 24f;

    [Tooltip("Orbit radius relative to the selected planet visual radius.")]
    public float moonOrbitRadiusMultiplier = 2.1f;

    [Tooltip("Additional orbit spacing for each ring of moon models.")]
    public float moonOrbitRingSpacingMultiplier = 0.85f;

    [Tooltip("Size of the visual moon models relative to the selected planet.")]
    public float moonVisualSizeMultiplier = 0.09f;

    [Header("Moon Surface Appearance")]
    [Tooltip("Drag your uploaded moon texture image here. The same texture will be wrapped onto every visual moon sphere.")]
    public Texture2D moonSurfaceTexture;

    [Tooltip("ON = apply the moon texture to all visual moons instead of plain white spheres.")]
    public bool useMoonTextureOnVisualMoons = true;

    [Tooltip("Brightness tint for the moon texture. Keep near white.")]
    public Color moonTextureTint = Color.white;

    [Tooltip("Optional texture tiling if you want to scale the moon texture.")]
    public Vector2 moonTextureTiling = Vector2.one;

    [Tooltip("ON = use an opaque, non-emissive material so the moon texture remains visible instead of becoming a bright white ball.")]
    public bool useOpaqueMoonSurfaceMaterial = true;

    [Header("Closer Jupiter and Saturn Moon Orbits")]
    [Tooltip("Base orbit distance for Jupiter's visual moons, relative to Jupiter's radius.")]
    public float jupiterMoonOrbitRadiusMultiplier = 1.45f;

    [Tooltip("Base orbit distance for Saturn's visual moons, relative to Saturn's radius.")]
    public float saturnMoonOrbitRadiusMultiplier = 1.65f;

    [Tooltip("Tighter spacing between Jupiter/Saturn moon rings. Lower values bring the rings closer together.")]
    public float giantPlanetMoonOrbitRingSpacingMultiplier = 0.42f;

    [Tooltip("ON = show subtle orbit paths around the selected planet.")]
    public bool showMoonOrbitPaths = true;

    private bool analysisModeOn = false;
    private bool wasLeftYPressed = false;
    private bool wasRightTriggerPressed = false;
    private float nextAllowedSelectionTime = 0f;

    private float previousTimeScale = 1f;
    private float cameraPitchAngle = 0f;
    private bool cameraPitchInitialized = false;

    private AnalysisPage currentPage = AnalysisPage.Menu;

    private GameObject hologramRoot;
    private GameObject menuRoot;
    private GameObject distanceRoot;
    private GameObject compareRoot;
    private GameObject compareChartPageRoot;
    private GameObject compareShowChartsButton;
    private GameObject compareBackButton;

    private GameObject moonsRoot;
    private Transform moonSelectedPlanet;
    private readonly List<MoonVisualData> moonVisuals = new List<MoonVisualData>();
    private readonly List<LineRenderer> moonOrbitPaths = new List<LineRenderer>();
    private Material moonVisualMaterial;
    private Material moonOrbitPathMaterial;

    private TextMesh titleText;
    private TextMesh bodyText;
    private TextMesh footerText;

    private GameObject analysisRayObject;
    private Material analysisRayMaterial;

    private GameObject distanceLineObject;
    private GameObject distanceArrowAObject;
    private GameObject distanceArrowBObject;
    private List<GameObject> distanceDashObjects = new List<GameObject>();
    private TextMesh distanceLabelText;
    private Material distanceLineMaterial;

    private Material panelMaterial;
    private Material borderMaterial;
    private Material buttonMaterial;
    private Material selectedButtonMaterial;
    private Material textGlowMaterial;

    private GameObject selectedPlanetIndicatorRoot;
    private LineRenderer selectedPlanetRingXY;
    private LineRenderer selectedPlanetRingXZ;
    private LineRenderer selectedPlanetRingYZ;
    private Material selectedPlanetIndicatorMaterial;

    private GameObject comparePlanetAIndicatorRoot;
    private GameObject comparePlanetBIndicatorRoot;

    private Transform distancePlanetA;
    private Transform distancePlanetB;
    private Transform activeMovePlanet;

    private Transform comparePlanetA;
    private Transform comparePlanetB;

    private GameObject compareChartLeftRoot;
    private GameObject compareChartRightRoot;
    private TextMesh compareChartLeftTitle;
    private TextMesh compareChartRightTitle;
    private Transform[] compareLeftBars;
    private Transform[] compareRightBars;
    private TextMesh[] compareLeftBarValues;
    private TextMesh[] compareRightBarValues;
    private float[] compareLeftCurrentValues;
    private float[] compareRightCurrentValues;
    private float[] compareLeftTargetValues;
    private float[] compareRightTargetValues;
    private Transform compareChartShownPlanetA;
    private Transform compareChartShownPlanetB;
    private readonly string[] compareGasLabels = new string[] { "CO2", "O2", "N2", "CH4", "H2", "OTH" };

    private Dictionary<Transform, float> orbitRadiusByPlanet = new Dictionary<Transform, float>();
    private Dictionary<Transform, float> orbitYOffsetByPlanet = new Dictionary<Transform, float>();

    private List<MonoBehaviour> disabledGunScripts = new List<MonoBehaviour>();
    private List<MonoBehaviour> disabledLayerHologramScripts = new List<MonoBehaviour>();

    void Start()
    {
        if (hologramParent == null)
        {
            hologramParent = transform;
        }

        CreateMaterials();
        BuildHologram();
        CreateAnalysisRay();
        CreateDistanceLineVisuals();
        CreateSelectedPlanetIndicator();

        HideHologram();
        HideDistanceLine();
        HideSelectedPlanetIndicator();
        CreateMoonModeVisuals();
        HideMoonModeVisuals();

        if (autoAddPlanetColliders)
        {
            AddCollidersToKnownPlanets();
        }
    }

    void Update()
    {
        if (autoAddPlanetColliders)
        {
            AddCollidersToKnownPlanets();
        }

        HandleToggleInput();

        if (!analysisModeOn)
        {
            HideAnalysisRay();
            return;
        }

        UpdateAnalysisRay();

        UpdateAnalysisFlightMovement();
        UpdateAnalysisCameraRotation();

        HandleSelectInput();

        if (currentPage == AnalysisPage.Distance)
        {
            HideComparePlanetIndicators();

            UpdateDistancePlanetMovement();
            UpdateDistanceText();
            UpdateDistanceLineVisual();
            UpdateSelectedPlanetIndicator();
        }
        else if (currentPage == AnalysisPage.Compare)
        {
            HideDistanceLine();
            HideSelectedPlanetIndicator();
            UpdateComparePlanetIndicators();
        }
        else if (currentPage == AnalysisPage.CompareCharts)
        {
            HideDistanceLine();
            HideSelectedPlanetIndicator();
            HideComparePlanetIndicators();
            UpdateCompareChartVisuals();
            HideMoonModeVisuals();
        }
        else if (currentPage == AnalysisPage.Moons)
        {
            HideDistanceLine();
            HideComparePlanetIndicators();
            UpdateSelectedPlanetIndicator();
            UpdateMoonModeVisuals();
        }
        else
        {
            HideDistanceLine();
            HideSelectedPlanetIndicator();
            HideComparePlanetIndicators();
            HideMoonModeVisuals();
        }
    }

    void HandleToggleInput()
    {
        bool leftYPressed = IsLeftYPressed();

        if (leftYPressed && !wasLeftYPressed)
        {
            ToggleAnalysisMode();
        }

        wasLeftYPressed = leftYPressed;
    }

    bool IsLeftYPressed()
    {
        if (!toggleWithLeftY)
        {
            return false;
        }

        if (useKeyboardMouseBackup && Input.GetKeyDown(KeyCode.Y))
        {
            return true;
        }

        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (!leftHand.isValid)
        {
            return false;
        }

        bool yPressed = false;
        leftHand.TryGetFeatureValue(CommonUsages.secondaryButton, out yPressed);

        return yPressed;
    }

    void HandleSelectInput()
    {
        bool rightTriggerPressed = IsRightTriggerPressed();

        // Important for Quest trigger: release must be detected before the next click.
        if (!rightTriggerPressed)
        {
            wasRightTriggerPressed = false;
            return;
        }

        if (!wasRightTriggerPressed && Time.unscaledTime >= nextAllowedSelectionTime)
        {
            if (currentPage == AnalysisPage.Menu)
            {
                HandleMenuSelection();
            }
            else if (currentPage == AnalysisPage.Distance)
            {
                HandleDistancePlanetSelection();
            }
            else if (currentPage == AnalysisPage.Compare)
            {
                HandleComparePlanetSelection();
            }
            else if (currentPage == AnalysisPage.CompareCharts)
            {
                HandleCompareChartPageSelection();
            }
            else if (currentPage == AnalysisPage.Moons)
            {
                HandleMoonPlanetSelection();
            }

            nextAllowedSelectionTime = Time.unscaledTime + selectionCooldownSeconds;
        }

        wasRightTriggerPressed = true;
    }

    bool IsRightTriggerPressed()
    {
        if (!selectWithRightTrigger)
        {
            return false;
        }

        if (useKeyboardMouseBackup && Input.GetMouseButtonDown(1))
        {
            return true;
        }

        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (!rightHand.isValid)
        {
            return false;
        }

        bool triggerButton = false;
        float triggerValue = 0f;

        rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out triggerButton);
        rightHand.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);

        return triggerButton || triggerValue >= triggerSelectPressThreshold;
    }

    bool IsRightTriggerHeld()
    {
        if (useKeyboardMouseBackup && Input.GetMouseButton(1))
        {
            return true;
        }

        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (!rightHand.isValid)
        {
            return false;
        }

        bool triggerButton = false;
        float triggerValue = 0f;

        rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out triggerButton);
        rightHand.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);

        return triggerButton || triggerValue > 0.15f;
    }

    Vector2 GetLeftJoystick()
    {
        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (!leftHand.isValid)
        {
            return Vector2.zero;
        }

        Vector2 axis = Vector2.zero;
        leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);

        return axis;
    }

    Vector2 GetRightJoystick()
    {
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (!rightHand.isValid)
        {
            return Vector2.zero;
        }

        Vector2 axis = Vector2.zero;
        rightHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);

        return axis;
    }

    void UpdateAnalysisFlightMovement()
    {
        if (!leftThumbstickMovesPlayer && !rightAButtonMovesDown && !rightBButtonMovesUp)
        {
            return;
        }

        Transform rig = GetPlayerRigToMove();

        if (rig == null)
        {
            return;
        }

        Transform reference = GetFlightDirectionReference();

        if (reference == null)
        {
            return;
        }

        Vector3 forward = reference.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = rig.forward;
            forward.y = 0f;
        }

        forward.Normalize();

        Vector3 right = reference.right;
        right.y = 0f;

        if (right.sqrMagnitude < 0.001f)
        {
            right = rig.right;
            right.y = 0f;
        }

        right.Normalize();

        Vector2 stick = GetLeftJoystick();

        Vector3 move = Vector3.zero;

        if (leftThumbstickMovesPlayer)
        {
            move += forward * stick.y;
            move += right * stick.x;
        }

        if (rightAButtonMovesDown && IsRightAButtonHeld())
        {
            move += Vector3.down;
        }

        if (rightBButtonMovesUp && IsRightBButtonHeld())
        {
            move += Vector3.up;
        }

        if (useKeyboardMouseBackup)
        {
            if (Input.GetKey(KeyCode.UpArrow)) move += forward;
            if (Input.GetKey(KeyCode.DownArrow)) move -= forward;
            if (Input.GetKey(KeyCode.RightArrow)) move += right;
            if (Input.GetKey(KeyCode.LeftArrow)) move -= right;
            if (Input.GetKey(KeyCode.A)) move += Vector3.down;
            if (Input.GetKey(KeyCode.V)) move += Vector3.up;
        }

        if (move.sqrMagnitude < 0.001f)
        {
            return;
        }

        move = Vector3.ClampMagnitude(move, 1f);

        Vector3 horizontalMove = new Vector3(move.x, 0f, move.z) * analysisFlySpeed;
        Vector3 verticalMove = new Vector3(0f, move.y, 0f) * analysisVerticalSpeed;

        rig.position += (horizontalMove + verticalMove) * Time.unscaledDeltaTime;
    }

    void UpdateAnalysisCameraRotation()
    {
        if (!rightThumbstickRotatesCamera)
        {
            return;
        }

        Vector2 axis = GetRightJoystick();

        if (cameraRotationDominantAxisOnly &&
            Mathf.Abs(axis.x) >= cameraRotationDeadzone &&
            Mathf.Abs(axis.y) >= cameraRotationDeadzone)
        {
            if (Mathf.Abs(axis.x) >= Mathf.Abs(axis.y))
            {
                axis.y = 0f;
            }
            else
            {
                axis.x = 0f;
            }
        }

        if (Mathf.Abs(axis.x) < cameraRotationDeadzone && Mathf.Abs(axis.y) < cameraRotationDeadzone)
        {
            return;
        }

        Transform rig = GetPlayerRigToMove();

        if (rig == null)
        {
            return;
        }

        // Left / right = rotate the whole XR Origin around world Y.
        if (Mathf.Abs(axis.x) >= cameraRotationDeadzone)
        {
            float yaw = axis.x * cameraYawSpeed * Time.unscaledDeltaTime;
            rig.Rotate(0f, yaw, 0f, Space.World);
        }

        // Up / down = pitch the Camera Offset, not the tracked Main Camera directly.
        if (Mathf.Abs(axis.y) >= cameraRotationDeadzone)
        {
            Transform pitchTarget = GetCameraPitchTarget();

            if (pitchTarget == null)
            {
                return;
            }

            if (!cameraPitchInitialized)
            {
                cameraPitchAngle = NormalizeAngle(pitchTarget.localEulerAngles.x);
                cameraPitchInitialized = true;
            }

            cameraPitchAngle -= axis.y * cameraPitchSpeed * Time.unscaledDeltaTime;
            cameraPitchAngle = Mathf.Clamp(cameraPitchAngle, cameraPitchMin, cameraPitchMax);

            Vector3 euler = pitchTarget.localEulerAngles;
            euler.x = cameraPitchAngle;
            pitchTarget.localEulerAngles = euler;
        }
    }

    Transform GetCameraPitchTarget()
    {
        if (cameraPitchTarget != null)
        {
            return cameraPitchTarget;
        }

        if (flightDirectionReference != null && flightDirectionReference.parent != null)
        {
            return flightDirectionReference.parent;
        }

        Camera cam = Camera.main;

        if (cam != null && cam.transform.parent != null)
        {
            return cam.transform.parent;
        }

        return null;
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }

        while (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }

    Transform GetPlayerRigToMove()
    {
        if (playerRigToMove != null)
        {
            return playerRigToMove;
        }

        Transform current = transform;

        while (current != null)
        {
            string n = current.name.ToLower();

            if (n.Contains("xr origin") || n.Contains("xrorigin"))
            {
                return current;
            }

            current = current.parent;
        }

        // Fallback for your hierarchy:
        // LeftHand Controller -> Camera Offset -> XR Origin (VR)
        if (transform.parent != null && transform.parent.parent != null)
        {
            return transform.parent.parent;
        }

        return transform;
    }

    Transform GetFlightDirectionReference()
    {
        if (flightDirectionReference != null)
        {
            return flightDirectionReference;
        }

        Camera cam = Camera.main;

        if (cam != null)
        {
            return cam.transform;
        }

        if (gunMuzzle != null)
        {
            return gunMuzzle;
        }

        return transform;
    }

    bool IsRightAButtonHeld()
    {
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (!rightHand.isValid)
        {
            return false;
        }

        bool pressed = false;
        rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out pressed);
        return pressed;
    }

    bool IsRightBButtonHeld()
    {
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (!rightHand.isValid)
        {
            return false;
        }

        bool pressed = false;
        rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed);
        return pressed;
    }

    void ToggleAnalysisMode()
    {
        if (analysisModeOn)
        {
            ExitAnalysisMode();
        }
        else
        {
            EnterAnalysisMode();
        }
    }

    void EnterAnalysisMode()
    {
        analysisModeOn = true;
        currentPage = AnalysisPage.Menu;

        previousTimeScale = Time.timeScale;

        if (pauseTimeScale)
        {
            Time.timeScale = 0f;
        }

        if (disableGunScriptInAnalysisMode)
        {
            DisableGunScripts();
        }

        if (disableLayerHologramInAnalysisMode)
        {
            DisableLayerHologramScripts();
        }

        ResetSelections();

        ShowMenuPage();

        Debug.Log("ANALYSIS MODE ON");
    }

    void ExitAnalysisMode()
    {
        analysisModeOn = false;

        if (pauseTimeScale)
        {
            Time.timeScale = previousTimeScale;
        }

        EnableGunScripts();
        EnableLayerHologramScripts();

        ResetSelections();

        HideHologram();
        HideAnalysisRay();
        HideDistanceLine();
        HideSelectedPlanetIndicator();

        Debug.Log("ANALYSIS MODE OFF");
    }

    void ResetSelections()
    {
        distancePlanetA = null;
        distancePlanetB = null;
        activeMovePlanet = null;

        wasRightTriggerPressed = false;
        nextAllowedSelectionTime = Time.unscaledTime + selectionCooldownSeconds;

        HideDistanceLine();
        HideSelectedPlanetIndicator();

        comparePlanetA = null;
        comparePlanetB = null;
        HideComparePlanetIndicators();
        ResetCompareChartAnimation();
        ResetMoonMode();
    }

    void HandleMenuSelection()
    {
        AnalysisButton button = RaycastMenuButton();

        Debug.Log("Analysis Mode button pressed: " + button);

        if (button == AnalysisButton.Distance)
        {
            currentPage = AnalysisPage.Distance;
            ResetSelections();
            ShowDistancePage();
        }
        else if (button == AnalysisButton.Compare)
        {
            currentPage = AnalysisPage.Compare;
            ResetSelections();
            ShowComparePage();
        }
        else if (button == AnalysisButton.Moons && enableMoonsMode)
        {
            currentPage = AnalysisPage.Moons;
            ResetSelections();
            ShowMoonsPage();
        }
        else if (button == AnalysisButton.Exit)
        {
            ExitAnalysisMode();
        }
    }

    AnalysisButton RaycastMenuButton()
    {
        if (gunMuzzle == null)
        {
            Debug.LogWarning("Analysis Mode: Gun Muzzle is not assigned.");
            return AnalysisButton.None;
        }

        // Method 1: try to hit the real button cube colliders.
        RaycastHit[] hits = Physics.RaycastAll(
            gunMuzzle.position,
            gunMuzzle.forward,
            analysisRayDistance,
            ~0,
            QueryTriggerInteraction.Collide
        );

        float closest = Mathf.Infinity;
        AnalysisButton selected = AnalysisButton.None;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            string n = hit.collider.gameObject.name.ToLower();

            if (!n.Contains("analysisbutton"))
            {
                continue;
            }

            if (hit.distance < closest)
            {
                closest = hit.distance;

                if (n.Contains("distance"))
                {
                    selected = AnalysisButton.Distance;
                }
                else if (n.Contains("moons"))
                {
                    selected = AnalysisButton.Moons;
                }
                else if (n.Contains("gascharts") || n.Contains("gas_chart") || n.Contains("showcharts"))
                {
                    selected = AnalysisButton.GasCharts;
                }
                else if (n.Contains("back"))
                {
                    selected = AnalysisButton.Back;
                }
                else if (n.Contains("compare"))
                {
                    selected = AnalysisButton.Compare;
                }
                else if (n.Contains("exit"))
                {
                    selected = AnalysisButton.Exit;
                }
            }
        }

        if (selected != AnalysisButton.None)
        {
            return selected;
        }

        // Method 2: easier VR fallback.
        // This checks where the laser touches the hologram panel itself.
        // It avoids needing to hit the tiny button collider perfectly.
        return RaycastMenuButtonByPanelArea();
    }

    AnalysisButton RaycastMenuButtonByPanelArea()
    {
        if (gunMuzzle == null || hologramRoot == null)
        {
            return AnalysisButton.None;
        }

        Ray ray = new Ray(gunMuzzle.position, gunMuzzle.forward);

        // hologramRoot is a GameObject, so use hologramRoot.transform.
        Plane panelPlane = new Plane(hologramRoot.transform.forward, hologramRoot.transform.position);

        float enter = 0f;

        if (!panelPlane.Raycast(ray, out enter))
        {
            return AnalysisButton.None;
        }

        if (enter < 0f || enter > analysisRayDistance)
        {
            return AnalysisButton.None;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector3 localPoint = hologramRoot.transform.InverseTransformPoint(hitPoint);

        float x = localPoint.x;
        float y = localPoint.y;

        // Bigger invisible click areas around the visible buttons.

        // Main menu layout: DISTANCE and COMPARE on top;
        // MOONS and EXIT below.
        if (currentPage == AnalysisPage.Menu &&
            x >= -0.245f && x <= -0.025f && y >= -0.075f && y <= 0.005f)
        {
            return AnalysisButton.Distance;
        }

        if (currentPage == AnalysisPage.Menu &&
            x >= 0.025f && x <= 0.245f && y >= -0.075f && y <= 0.005f)
        {
            return AnalysisButton.Compare;
        }

        if (currentPage == AnalysisPage.Menu && enableMoonsMode &&
            x >= -0.245f && x <= -0.025f && y >= -0.145f && y <= -0.075f)
        {
            return AnalysisButton.Moons;
        }

        // Compare detail page: show gas-chart button area.
        if (currentPage == AnalysisPage.Compare &&
            comparePlanetA != null && comparePlanetB != null &&
            x >= -0.145f && x <= 0.145f && y >= -0.135f && y <= -0.075f)
        {
            return AnalysisButton.GasCharts;
        }

        // Compare chart page: back button area.
        if (currentPage == AnalysisPage.CompareCharts &&
            x >= -0.125f && x <= 0.125f && y >= -0.160f && y <= -0.100f)
        {
            return AnalysisButton.Back;
        }

        // EXIT button area only belongs to the main menu.
        if (currentPage == AnalysisPage.Menu &&
            x >= 0.025f && x <= 0.245f && y >= -0.145f && y <= -0.075f)
        {
            return AnalysisButton.Exit;
        }

        return AnalysisButton.None;
    }

    void HandleDistancePlanetSelection()
    {
        Transform hitPlanet = RaycastPlanet();

        if (hitPlanet == null)
        {
            Debug.Log("Distance Mode: no planet hit. Aim directly at a planet.");
            return;
        }

        RegisterPlanetOrbit(hitPlanet);

        // OLD SHIFTING RULE RESTORED:
        // Pick A, then B -> line shows A-B.
        // Pick C -> old A is removed, old B becomes A, C becomes B.
        // This means you can continue comparing planets without clearing manually.

        if (distancePlanetA == null)
        {
            distancePlanetA = hitPlanet;
            activeMovePlanet = hitPlanet;
            Debug.Log("Distance first planet selected: " + PlanetDisplayName(hitPlanet));
        }
        else if (distancePlanetB == null)
        {
            if (hitPlanet == distancePlanetA)
            {
                activeMovePlanet = hitPlanet;
                Debug.Log("Distance first planet still selected: " + PlanetDisplayName(hitPlanet));
            }
            else
            {
                distancePlanetB = hitPlanet;
                activeMovePlanet = hitPlanet;
                Debug.Log("Distance second planet selected: " + PlanetDisplayName(hitPlanet));
            }
        }
        else
        {
            if (hitPlanet == distancePlanetA)
            {
                // Keep same pair, just make first planet movable again.
                activeMovePlanet = hitPlanet;
                Debug.Log("Distance first planet active: " + PlanetDisplayName(hitPlanet));
            }
            else if (hitPlanet == distancePlanetB)
            {
                // Keep same pair, just make second planet movable again.
                activeMovePlanet = hitPlanet;
                Debug.Log("Distance second planet active: " + PlanetDisplayName(hitPlanet));
            }
            else
            {
                // Third different planet shifts the pair.
                distancePlanetA = distancePlanetB;
                distancePlanetB = hitPlanet;
                activeMovePlanet = hitPlanet;

                Debug.Log("Distance shifted: " +
                    PlanetDisplayName(distancePlanetA) + " -> " +
                    PlanetDisplayName(distancePlanetB));
            }
        }

        UpdateDistanceText();
        UpdateDistanceLineVisual();
        UpdateSelectedPlanetIndicator();
    }

    void ClearDistanceSelection()
    {
        distancePlanetA = null;
        distancePlanetB = null;
        activeMovePlanet = null;

        HideDistanceLine();
        HideSelectedPlanetIndicator();

        if (currentPage == AnalysisPage.Distance)
        {
            SetText(
                "DISTANCE MODE",
                "Pick first planet\nPick second planet\nThird shifts pair",
                "A-B, then B-C, then C-D"
            );
        }
    }

    void HandleComparePlanetSelection()
    {
        AnalysisButton button = RaycastMenuButton();

        if (button == AnalysisButton.GasCharts && comparePlanetA != null && comparePlanetB != null)
        {
            currentPage = AnalysisPage.CompareCharts;
            ShowCompareChartPage();
            return;
        }

        Transform hitPlanet = RaycastPlanet();

        if (hitPlanet == null)
        {
            return;
        }

        if (comparePlanetA == null)
        {
            comparePlanetA = hitPlanet;
        }
        else if (comparePlanetA == hitPlanet)
        {
            // Keep first selected.
        }
        else if (comparePlanetB == null)
        {
            comparePlanetB = hitPlanet;
        }
        else
        {
            // Only two planets shown. Selecting another replaces the second.
            comparePlanetB = hitPlanet;
        }

        ShowCompareText();
        UpdateComparePlanetIndicators();
    }

    void HandleCompareChartPageSelection()
    {
        AnalysisButton button = RaycastMenuButton();

        if (button == AnalysisButton.Back)
        {
            currentPage = AnalysisPage.Compare;
            ShowComparePage();
        }
    }

    void HandleMoonPlanetSelection()
    {
        Transform hitPlanet = RaycastPlanet();

        if (hitPlanet == null)
        {
            SetText(
                "MOONS MODE",
                "Aim at a planet + RT",
                "Select a planet to visualize its moons"
            );
            return;
        }

        moonSelectedPlanet = hitPlanet;
        activeMovePlanet = hitPlanet;

        BuildMoonVisualsForPlanet(hitPlanet);
        UpdateSelectedPlanetIndicator();
        UpdateMoonModeText();
    }

    Transform RaycastPlanet()
    {
        if (gunMuzzle == null)
        {
            return null;
        }

        Ray ray = new Ray(gunMuzzle.position, gunMuzzle.forward);

        List<RaycastHit> allHits = new List<RaycastHit>();

        RaycastHit[] directHits = Physics.RaycastAll(
            ray.origin,
            ray.direction,
            analysisRayDistance,
            planetHitLayers,
            QueryTriggerInteraction.Ignore
        );

        if (directHits != null)
        {
            allHits.AddRange(directHits);
        }

        RaycastHit[] sphereHits = Physics.SphereCastAll(
            ray.origin,
            planetSelectionSphereRadius,
            ray.direction,
            analysisRayDistance,
            planetHitLayers,
            QueryTriggerInteraction.Ignore
        );

        if (sphereHits != null)
        {
            allHits.AddRange(sphereHits);
        }

        Transform playerRoot = transform.root;

        Transform bestAnyPlanet = null;
        float bestAnyScore = Mathf.Infinity;

        Transform bestNewPlanet = null;
        float bestNewScore = Mathf.Infinity;

        Transform bestDistanceShiftPlanet = null;
        float bestDistanceShiftScore = Mathf.Infinity;

        foreach (RaycastHit hit in allHits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.transform.root == playerRoot)
            {
                continue;
            }

            Transform planet = FindPlanetTransform(hit.collider.transform);

            if (planet == null)
            {
                continue;
            }

            float score = GetRayPlanetAimScore(ray, planet, hit.distance);

            if (score < bestAnyScore)
            {
                bestAnyScore = score;
                bestAnyPlanet = planet;
            }

            if (planet != activeMovePlanet && score < bestNewScore)
            {
                bestNewScore = score;
                bestNewPlanet = planet;
            }

            if (currentPage == AnalysisPage.Distance &&
                distancePlanetA != null &&
                distancePlanetB != null &&
                planet != distancePlanetA &&
                planet != distancePlanetB &&
                score < bestDistanceShiftScore)
            {
                bestDistanceShiftScore = score;
                bestDistanceShiftPlanet = planet;
            }
        }

        // Distance-mode old shifting rule helper:
        // If A-B is already selected and a third planet is under/near the pointer,
        // prefer that third planet so the transition becomes B-C smoothly.
        if (stronglyPreferThirdPlanetWhenDistancePairFull &&
            currentPage == AnalysisPage.Distance &&
            distancePlanetA != null &&
            distancePlanetB != null &&
            bestDistanceShiftPlanet != null)
        {
            if (bestAnyPlanet == null ||
                bestDistanceShiftScore <= bestAnyScore + Mathf.Max(0.05f, distanceShiftThirdPlanetTolerance))
            {
                return bestDistanceShiftPlanet;
            }
        }

        if (preferNewPlanetOverActivePlanet && bestNewPlanet != null && bestAnyPlanet == activeMovePlanet)
        {
            return bestNewPlanet;
        }

        return bestAnyPlanet;
    }

    float GetRayPlanetAimScore(Ray ray, Transform planet, float hitDistance)
    {
        Vector3 toPlanet = planet.position - ray.origin;
        float forwardDistance = Vector3.Dot(toPlanet, ray.direction);

        if (forwardDistance < 0f)
        {
            return Mathf.Infinity;
        }

        Vector3 closestPoint = ray.origin + ray.direction * forwardDistance;
        float sideDistance = Vector3.Distance(planet.position, closestPoint);

        // Main priority is how close the ray passes to planet center.
        // Small distance weight keeps nearer targets slightly preferred.
        return sideDistance + hitDistance * 0.003f;
    }

    Transform FindPlanetTransform(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            if (IsKnownPlanetName(current.name))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    bool IsKnownPlanetName(string objectName)
    {
        string n = objectName.ToLower();

        // Avoid split generated halves being selected as base planet controls.
        if (n.Contains("_left_half") || n.Contains("_right_half"))
        {
            return false;
        }

        return
            n.Contains("mercury") ||
            n.Contains("venus") ||
            n.Contains("earth") ||
            n.Contains("mars") ||
            n.Contains("jupiter") ||
            n.Contains("saturn") ||
            n.Contains("uranus") ||
            n.Contains("neptune");
    }

    void RegisterPlanetOrbit(Transform planet)
    {
        if (planet == null || sun == null)
        {
            return;
        }

        if (!orbitRadiusByPlanet.ContainsKey(planet))
        {
            Vector3 offset = planet.position - sun.position;
            Vector2 flat = new Vector2(offset.x, offset.z);
            orbitRadiusByPlanet[planet] = Mathf.Max(0.01f, flat.magnitude);
            orbitYOffsetByPlanet[planet] = offset.y;
        }
    }

    void UpdateDistancePlanetMovement()
    {
        if (activeMovePlanet == null || sun == null)
        {
            return;
        }

        RegisterPlanetOrbit(activeMovePlanet);

        if (!IsRightTriggerHeld())
        {
            return;
        }

        Vector3 oldPosition = activeMovePlanet.position;

        // Right thumbstick is now reserved for camera rotation.
        // Planet movement uses right trigger + aiming ray only.
        MovePlanetByGunRay(activeMovePlanet);

        Vector3 movementDelta = activeMovePlanet.position - oldPosition;

        if (movementDelta.sqrMagnitude > 0.000001f)
        {
            MoveExtraObjectsWithPlanet(activeMovePlanet, movementDelta);
        }
    }

    void MovePlanetByGunRay(Transform planet)
    {
        if (planet == null || gunMuzzle == null || sun == null)
        {
            return;
        }

        float y = sun.position.y + orbitYOffsetByPlanet[planet];

        Plane orbitPlane = new Plane(Vector3.up, new Vector3(0f, y, 0f));
        Ray ray = new Ray(gunMuzzle.position, gunMuzzle.forward);

        if (!orbitPlane.Raycast(ray, out float enter))
        {
            return;
        }

        Vector3 point = ray.GetPoint(enter);
        Vector3 fromSun = point - sun.position;
        fromSun.y = 0f;

        if (fromSun.sqrMagnitude < 0.001f)
        {
            return;
        }

        fromSun.Normalize();

        float radius = orbitRadiusByPlanet[planet];
        float yOffset = orbitYOffsetByPlanet[planet];

        Vector3 targetPosition = sun.position + fromSun * radius + Vector3.up * yOffset;
        MovePlanetSmoothly(planet, targetPosition);
    }

    void MovePlanetByRightJoystick(Transform planet)
    {
        if (planet == null || sun == null)
        {
            return;
        }

        Vector2 axis = GetRightJoystick();

        if (Mathf.Abs(axis.x) < 0.1f)
        {
            return;
        }

        Vector3 offset = planet.position - sun.position;
        float radius = orbitRadiusByPlanet[planet];
        float yOffset = orbitYOffsetByPlanet[planet];

        float currentAngle = Mathf.Atan2(offset.z, offset.x);
        float deltaAngle = axis.x * joystickOrbitSpeedDegrees * Mathf.Deg2Rad * Time.unscaledDeltaTime;

        float newAngle = currentAngle + deltaAngle;

        Vector3 newOffset = new Vector3(
            Mathf.Cos(newAngle) * radius,
            yOffset,
            Mathf.Sin(newAngle) * radius
        );

        Vector3 targetPosition = sun.position + newOffset;
        MovePlanetSmoothly(planet, targetPosition);
    }

    void MovePlanetSmoothly(Transform planet, Vector3 targetPosition)
    {
        if (planet == null)
        {
            return;
        }

        float t = Mathf.Clamp01(planetMoveSmoothSpeed * Time.unscaledDeltaTime);
        planet.position = Vector3.Lerp(planet.position, targetPosition, t);
    }

    void MoveExtraObjectsWithPlanet(Transform planet, Vector3 delta)
    {
        if (planet == null)
        {
            return;
        }

        string planetName = PlanetDisplayName(planet).ToLower();

        if (planetName.Contains("saturn"))
        {
            MoveSaturnRingWithPlanet(planet, delta);
        }
    }

    void MoveSaturnRingWithPlanet(Transform planet, Vector3 delta)
    {
        if (saturnRingObject != null && !saturnRingObject.IsChildOf(planet))
        {
            saturnRingObject.position += delta;
        }

        if (!autoMoveSaturnRingObjects)
        {
            return;
        }

        Transform[] allTransforms = FindObjectsOfType<Transform>();

        foreach (Transform t in allTransforms)
        {
            if (t == null || t == planet)
            {
                continue;
            }

            if (t.IsChildOf(planet))
            {
                continue;
            }

            string n = t.name.ToLower();

            bool looksLikeSaturnRing =
                (n.Contains("saturn") && n.Contains("ring")) ||
                n.Contains("saturnring") ||
                n.Contains("saturn_ring");

            if (!looksLikeSaturnRing)
            {
                continue;
            }

            t.position += delta;
        }
    }

    void UpdateDistanceText()
    {
        if (currentPage != AnalysisPage.Distance)
        {
            return;
        }

        string aName = PlanetDisplayName(distancePlanetA);
        string bName = PlanetDisplayName(distancePlanetB);

        if (distancePlanetA == null && distancePlanetB == null)
        {
            SetText(
                "DISTANCE MODE",
                "Pick first planet\nThen pick second planet\nThird planet shifts pair",
                "Example: Earth-Jupiter, then Jupiter-Mars"
            );
            return;
        }

        if (distancePlanetA != null && distancePlanetB == null)
        {
            SetText(
                "DISTANCE MODE",
                "First: " + aName + "\nPick second planet",
                "RT: select second"
            );
            return;
        }

        float distance = Vector3.Distance(distancePlanetA.position, distancePlanetB.position);
        string distanceText = BuildDistanceString(distance);

        SetText(
            aName + " - " + bName,
            distanceText + "\nNext planet shifts the pair",
            "Old rule: A-B -> B-C"
        );
    }

    string BuildDistanceString(float unityDistance)
    {
        float millionKm = unityDistance * unityUnitToMillionKm;
        float km = millionKm * 1000000f;

        if (showMillionKmAlso)
        {
            return "Distance: " + km.ToString("0") + " km\n" +
                   "(" + unityDistance.ToString("0.00") + " Unity)";
        }

        return "Distance: " + km.ToString("0") + " km";
    }

    void CreateSelectedPlanetIndicator()
    {
        selectedPlanetIndicatorMaterial = CreateMaterial(new Color(0f, 1f, 0.25f, 1f), true);

        selectedPlanetIndicatorRoot = new GameObject("Selected Planet Green Indicator");
        selectedPlanetRingXY = CreateIndicatorRing("Selected Ring XY", selectedPlanetIndicatorRoot.transform);
        selectedPlanetRingXZ = CreateIndicatorRing("Selected Ring XZ", selectedPlanetIndicatorRoot.transform);
        selectedPlanetRingYZ = CreateIndicatorRing("Selected Ring YZ", selectedPlanetIndicatorRoot.transform);

        selectedPlanetRingXY.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        selectedPlanetRingXZ.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        selectedPlanetRingYZ.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        comparePlanetAIndicatorRoot = CreateCompareIndicatorRoot("Compare Planet A Green Indicator");
        comparePlanetBIndicatorRoot = CreateCompareIndicatorRoot("Compare Planet B Green Indicator");

        HideSelectedPlanetIndicator();
        HideComparePlanetIndicators();
    }

    GameObject CreateCompareIndicatorRoot(string objectName)
    {
        GameObject root = new GameObject(objectName);

        LineRenderer ringXY = CreateIndicatorRing(objectName + " Ring XY", root.transform);
        LineRenderer ringXZ = CreateIndicatorRing(objectName + " Ring XZ", root.transform);
        LineRenderer ringYZ = CreateIndicatorRing(objectName + " Ring YZ", root.transform);

        ringXY.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        ringXZ.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ringYZ.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        root.SetActive(false);
        return root;
    }

    LineRenderer CreateIndicatorRing(string objectName, Transform parent)
    {
        GameObject ringObject = new GameObject(objectName);
        ringObject.transform.SetParent(parent);
        ringObject.transform.localPosition = Vector3.zero;
        ringObject.transform.localRotation = Quaternion.identity;
        ringObject.transform.localScale = Vector3.one;

        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 96;
        line.widthMultiplier = 0.035f;
        line.material = selectedPlanetIndicatorMaterial;

        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
        }

        return line;
    }

    void UpdateSelectedPlanetIndicator()
    {
        if (activeMovePlanet == null || selectedPlanetIndicatorRoot == null)
        {
            HideSelectedPlanetIndicator();
            return;
        }

        float radius = GetPlanetVisualRadius(activeMovePlanet) * 1.28f;
        radius = Mathf.Max(radius, 0.35f);

        selectedPlanetIndicatorRoot.transform.position = activeMovePlanet.position;
        selectedPlanetIndicatorRoot.transform.rotation = Quaternion.identity;
        selectedPlanetIndicatorRoot.transform.localScale = Vector3.one * radius;

        selectedPlanetIndicatorRoot.SetActive(true);
    }

    void HideSelectedPlanetIndicator()
    {
        if (selectedPlanetIndicatorRoot != null)
        {
            selectedPlanetIndicatorRoot.SetActive(false);
        }
    }

    void UpdateComparePlanetIndicators()
    {
        SetIndicatorOnPlanet(comparePlanetAIndicatorRoot, comparePlanetA);
        SetIndicatorOnPlanet(comparePlanetBIndicatorRoot, comparePlanetB);
    }

    void SetIndicatorOnPlanet(GameObject indicatorRoot, Transform planet)
    {
        if (indicatorRoot == null)
        {
            return;
        }

        if (planet == null)
        {
            indicatorRoot.SetActive(false);
            return;
        }

        float radius = GetPlanetVisualRadius(planet) * 1.30f;
        radius = Mathf.Max(radius, 0.35f);

        indicatorRoot.transform.position = planet.position;
        indicatorRoot.transform.rotation = Quaternion.identity;
        indicatorRoot.transform.localScale = Vector3.one * radius;
        indicatorRoot.SetActive(true);
    }

    void HideComparePlanetIndicators()
    {
        if (comparePlanetAIndicatorRoot != null)
        {
            comparePlanetAIndicatorRoot.SetActive(false);
        }

        if (comparePlanetBIndicatorRoot != null)
        {
            comparePlanetBIndicatorRoot.SetActive(false);
        }
    }

    float GetPlanetVisualRadius(Transform planet)
    {
        if (planet == null)
        {
            return 0.5f;
        }

        Renderer renderer = planet.GetComponentInChildren<Renderer>();

        if (renderer == null)
        {
            return 0.5f;
        }

        return Mathf.Max(renderer.bounds.extents.x, renderer.bounds.extents.y, renderer.bounds.extents.z);
    }

    void CreateDistanceLineVisuals()
    {
        distanceLineMaterial = CreateMaterial(new Color(0f, 1f, 1f, 1f), true);

        distanceLineObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        distanceLineObject.name = "Analysis Distance Line";

        Collider lineCollider = distanceLineObject.GetComponent<Collider>();

        if (lineCollider != null)
        {
            Destroy(lineCollider);
        }

        Renderer lineRenderer = distanceLineObject.GetComponent<Renderer>();

        if (lineRenderer != null)
        {
            lineRenderer.material = distanceLineMaterial;
        }

        distanceArrowAObject = CreateDistanceArrowObject("Analysis Distance Arrow A");
        distanceArrowBObject = CreateDistanceArrowObject("Analysis Distance Arrow B");
        CreateDistanceDashObjects();

        GameObject labelObject = new GameObject("Analysis Distance Label");
        distanceLabelText = labelObject.AddComponent<TextMesh>();
        distanceLabelText.fontSize = 64;
        distanceLabelText.characterSize = distanceLabelCharacterSize;
        distanceLabelText.anchor = TextAnchor.MiddleCenter;
        distanceLabelText.alignment = TextAlignment.Center;
        distanceLabelText.color = Color.white;
        distanceLabelText.text = "";

        HideDistanceLine();
    }

    void UpdateDistanceLineVisual()
    {
        if (!showDistanceLineBetweenSelectedPlanets ||
            distancePlanetA == null ||
            distancePlanetB == null ||
            distanceLineObject == null ||
            distanceLabelText == null)
        {
            HideDistanceLine();
            return;
        }

        Vector3 start = distancePlanetA.position;
        Vector3 end = distancePlanetB.position;

        float actualLineRadius = forceVeryThickDistanceLine ? 0.45f : distanceLineRadius;

        if (useDottedDistanceLine)
        {
            distanceLineObject.SetActive(false);
            UpdateDistanceDashes(start, end, actualLineRadius);
        }
        else
        {
            HideDistanceDashes();
            SetCylinderBetween(distanceLineObject, start, end, actualLineRadius);
            distanceLineObject.SetActive(true);
        }

        Vector3 direction = end - start;
        float lineLength = direction.magnitude;

        if (lineLength > 0.001f)
        {
            direction.Normalize();

            float arrowOffset = Mathf.Min(0.6f, lineLength * 0.25f);

            SetConeArrow(
                distanceArrowAObject,
                start + direction * arrowOffset,
                direction,
                distanceArrowLength,
                distanceArrowRadius
            );

            SetConeArrow(
                distanceArrowBObject,
                end - direction * arrowOffset,
                -direction,
                distanceArrowLength,
                distanceArrowRadius
            );
        }

        float unityDistance = Vector3.Distance(start, end);
        Vector3 middle = (start + end) * 0.5f;

        distanceLabelText.transform.position = middle + distanceLabelOffset;
        distanceLabelText.text = BuildDistanceString(unityDistance).Replace("Distance: ", "");

        Camera cam = Camera.main;

        if (cam != null)
        {
            distanceLabelText.transform.LookAt(cam.transform.position);
            distanceLabelText.transform.Rotate(0f, 180f, 0f);
        }

        distanceLabelText.gameObject.SetActive(true);
    }

    GameObject CreateDistanceArrowObject(string objectName)
    {
        GameObject arrow = new GameObject(objectName);

        MeshFilter meshFilter = arrow.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = arrow.AddComponent<MeshRenderer>();

        meshFilter.mesh = CreateConeMesh(24);

        if (distanceLineMaterial != null)
        {
            meshRenderer.material = distanceLineMaterial;
        }

        arrow.SetActive(false);
        return arrow;
    }

    Mesh CreateConeMesh(int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Distance Arrow Cone Mesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Cone points along local +Y.
        int tipIndex = vertices.Count;
        vertices.Add(new Vector3(0f, 0.5f, 0f));

        int baseCenterIndex = vertices.Count;
        vertices.Add(new Vector3(0f, -0.5f, 0f));

        int baseStartIndex = vertices.Count;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segments;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            vertices.Add(new Vector3(x, -0.5f, z));
        }

        for (int i = 0; i < segments; i++)
        {
            int a = baseStartIndex + i;
            int b = baseStartIndex + i + 1;

            // Side triangle
            triangles.Add(tipIndex);
            triangles.Add(a);
            triangles.Add(b);

            // Base triangle
            triangles.Add(baseCenterIndex);
            triangles.Add(b);
            triangles.Add(a);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    void SetConeArrow(GameObject arrow, Vector3 position, Vector3 direction, float length, float radius)
    {
        if (arrow == null)
        {
            return;
        }

        if (direction.sqrMagnitude < 0.001f)
        {
            arrow.SetActive(false);
            return;
        }

        arrow.transform.position = position;
        arrow.transform.up = direction.normalized;
        arrow.transform.localScale = new Vector3(radius, length, radius);
        arrow.SetActive(true);
    }

    void CreateDistanceDashObjects()
    {
        int count = Mathf.Max(1, distanceLineDashCount);

        for (int i = 0; i < count; i++)
        {
            GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dash.name = "Analysis Distance Dotted Dash " + i;

            Collider collider = dash.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = dash.GetComponent<Renderer>();

            if (renderer != null && distanceLineMaterial != null)
            {
                renderer.material = distanceLineMaterial;
            }

            dash.SetActive(false);
            distanceDashObjects.Add(dash);
        }
    }

    void EnsureDistanceDashCount(int count)
    {
        count = Mathf.Max(1, count);

        while (distanceDashObjects.Count < count)
        {
            GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dash.name = "Analysis Distance Dotted Dash " + distanceDashObjects.Count;

            Collider collider = dash.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = dash.GetComponent<Renderer>();

            if (renderer != null && distanceLineMaterial != null)
            {
                renderer.material = distanceLineMaterial;
            }

            dash.SetActive(false);
            distanceDashObjects.Add(dash);
        }
    }

    void UpdateDistanceDashes(Vector3 start, Vector3 end, float radius)
    {
        Vector3 full = end - start;
        float length = full.magnitude;

        if (length < 0.001f)
        {
            HideDistanceDashes();
            return;
        }

        Vector3 direction = full.normalized;
        int count = Mathf.Max(1, distanceLineDashCount);
        float fill = Mathf.Clamp(distanceLineDashFill, 0.1f, 0.9f);

        EnsureDistanceDashCount(count);

        float segmentLength = length / count;
        float dashLength = segmentLength * fill;

        for (int i = 0; i < distanceDashObjects.Count; i++)
        {
            GameObject dash = distanceDashObjects[i];

            if (dash == null)
            {
                continue;
            }

            if (i >= count)
            {
                dash.SetActive(false);
                continue;
            }

            float centerDistance = segmentLength * i + segmentLength * 0.5f;
            Vector3 dashCenter = start + direction * centerDistance;

            Vector3 dashStart = dashCenter - direction * dashLength * 0.5f;
            Vector3 dashEnd = dashCenter + direction * dashLength * 0.5f;

            SetCylinderBetween(dash, dashStart, dashEnd, radius);
            dash.SetActive(true);
        }
    }

    void HideDistanceDashes()
    {
        foreach (GameObject dash in distanceDashObjects)
        {
            if (dash != null)
            {
                dash.SetActive(false);
            }
        }
    }

    void ForceHideDistanceObjectsByName()
    {
        // Extra safety: if Unity kept an old dash/line object alive, hide it too.
        Transform[] allTransforms = FindObjectsOfType<Transform>();

        foreach (Transform t in allTransforms)
        {
            if (t == null)
            {
                continue;
            }

            string n = t.gameObject.name;

            if (n.StartsWith("Analysis Distance"))
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    void HideDistanceLine()
    {
        if (distanceLineObject != null)
        {
            distanceLineObject.SetActive(false);
        }

        HideDistanceDashes();
        ForceHideDistanceObjectsByName();

        if (distanceArrowAObject != null)
        {
            distanceArrowAObject.SetActive(false);
        }

        if (distanceArrowBObject != null)
        {
            distanceArrowBObject.SetActive(false);
        }

        if (distanceLabelText != null)
        {
            distanceLabelText.gameObject.SetActive(false);
        }
    }

    void ShowCompareText()
    {
        bool hasPair = comparePlanetA != null && comparePlanetB != null;

        if (compareShowChartsButton != null)
        {
            compareShowChartsButton.SetActive(hasPair);
        }

        if (comparePlanetA == null && comparePlanetB == null)
        {
            SetText(
                "COMPARE MODE",
                "RT: pick first\nRT: pick second\nThen open gas charts",
                "Left stick fly | A down | B up"
            );
            return;
        }

        if (comparePlanetA != null && comparePlanetB == null)
        {
            SetText(
                "COMPARE MODE",
                "Selected: " + PlanetDisplayName(comparePlanetA) + "\nPick second planet",
                "Left stick fly | A down | B up"
            );
            return;
        }

        PlanetInfo a = GetPlanetInfo(comparePlanetA);
        PlanetInfo b = GetPlanetInfo(comparePlanetB);

        string similarities = BuildSimilarities(a, b);
        string differences = BuildDifferences(a, b);

        SetText(
            a.displayName + " vs " + b.displayName,
            "SIM: " + similarities + "\nDIFF: " + differences,
            "Aim GAS CHARTS + RT"
        );
    }

    string BuildSimilarities(PlanetInfo a, PlanetInfo b)
    {
        List<string> sims = new List<string>();

        sims.Add("orbit Sun");

        if (a.type == b.type)
        {
            sims.Add("both " + a.type);
        }

        if (a.hasAtmosphere && b.hasAtmosphere)
        {
            sims.Add("atmosphere");
        }

        if (a.hasMoons && b.hasMoons)
        {
            sims.Add("moons");
        }

        return JoinShort(sims, 2);
    }

    string BuildDifferences(PlanetInfo a, PlanetInfo b)
    {
        if (a.displayName == b.displayName)
        {
            return "same planet selected";
        }

        List<string> diffs = new List<string>();

        if (a.type != b.type)
        {
            diffs.Add(a.type + " vs " + b.type);
        }

        diffs.Add(a.shortFact);
        diffs.Add(b.shortFact);

        return JoinShort(diffs, 2);
    }

    string JoinShort(List<string> items, int maxItems)
    {
        string result = "";

        int count = Mathf.Min(maxItems, items.Count);

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                result += "; ";
            }

            result += items[i];
        }

        return result;
    }

    PlanetInfo GetPlanetInfo(Transform planet)
    {
        string n = PlanetDisplayName(planet).ToLower();

        if (n.Contains("mercury"))
        {
            return new PlanetInfo("Mercury", "rocky", false, false, "smallest planet");
        }

        if (n.Contains("venus"))
        {
            return new PlanetInfo("Venus", "rocky", true, false, "dense CO2 atmosphere");
        }

        if (n.Contains("earth"))
        {
            return new PlanetInfo("Earth", "rocky", true, true, "liquid water");
        }

        if (n.Contains("mars"))
        {
            return new PlanetInfo("Mars", "rocky", true, true, "thin CO2 atmosphere");
        }

        if (n.Contains("jupiter"))
        {
            return new PlanetInfo("Jupiter", "gas giant", true, true, "largest planet");
        }

        if (n.Contains("saturn"))
        {
            return new PlanetInfo("Saturn", "gas giant", true, true, "large ring system");
        }

        if (n.Contains("uranus"))
        {
            return new PlanetInfo("Uranus", "ice giant", true, true, "tilted rotation");
        }

        if (n.Contains("neptune"))
        {
            return new PlanetInfo("Neptune", "ice giant", true, true, "very strong winds");
        }

        return new PlanetInfo(PlanetDisplayName(planet), "planet", false, false, "unknown details");
    }

    string PlanetDisplayName(Transform planet)
    {
        if (planet == null)
        {
            return "None";
        }

        string n = planet.name;

        if (n.ToLower().Contains("mercury")) return "Mercury";
        if (n.ToLower().Contains("venus")) return "Venus";
        if (n.ToLower().Contains("earth")) return "Earth";
        if (n.ToLower().Contains("mars")) return "Mars";
        if (n.ToLower().Contains("jupiter")) return "Jupiter";
        if (n.ToLower().Contains("saturn")) return "Saturn";
        if (n.ToLower().Contains("uranus")) return "Uranus";
        if (n.ToLower().Contains("neptune")) return "Neptune";

        return n;
    }

    void ShowMenuPage()
    {
        ShowHologram();

        menuRoot.SetActive(true);
        distanceRoot.SetActive(false);
        compareRoot.SetActive(false);

        if (compareChartPageRoot != null) compareChartPageRoot.SetActive(false);
        HideMoonModeVisuals();

        SetText(
            "ANALYSIS MODE",
            "Aim DISTANCE, COMPARE, or MOONS + RT",
            "Left stick fly | A down | B up"
        );
    }

    void ShowDistancePage()
    {
        ShowHologram();

        menuRoot.SetActive(false);
        distanceRoot.SetActive(true);
        compareRoot.SetActive(false);

        if (compareChartPageRoot != null) compareChartPageRoot.SetActive(false);
        HideMoonModeVisuals();

        UpdateDistanceText();
    }

    void ShowComparePage()
    {
        ShowHologram();

        menuRoot.SetActive(false);
        distanceRoot.SetActive(false);
        compareRoot.SetActive(true);

        if (compareChartPageRoot != null)
        {
            compareChartPageRoot.SetActive(false);
        }

        HideMoonModeVisuals();

        ShowCompareText();
        UpdateComparePlanetIndicators();
    }

    void ShowCompareChartPage()
    {
        ShowHologram();

        menuRoot.SetActive(false);
        distanceRoot.SetActive(false);
        compareRoot.SetActive(false);

        if (compareChartPageRoot != null)
        {
            compareChartPageRoot.SetActive(true);
        }

        // The normal hologram text is intentionally hidden on this page.
        // Chart-specific text is small and built inside the chart page instead.
        SetText("", "", "");

        UpdateCompareChartVisuals(true);
    }

    void ShowMoonsPage()
    {
        ShowHologram();

        menuRoot.SetActive(false);
        distanceRoot.SetActive(false);
        compareRoot.SetActive(false);

        if (compareChartPageRoot != null)
        {
            compareChartPageRoot.SetActive(false);
        }

        if (moonsRoot != null)
        {
            moonsRoot.SetActive(true);
        }

        UpdateMoonModeText();
    }

    void UpdateMoonModeText()
    {
        if (moonSelectedPlanet == null)
        {
            SetText(
                "MOONS MODE",
                "Aim at a planet + RT\nMoons will appear and orbit",
                "Total moons around the 8 planets: " + totalMoonsAroundEightPlanets
            );
            return;
        }

        int totalMoonCount = GetKnownMoonCount(moonSelectedPlanet);
        string planetName = PlanetDisplayName(moonSelectedPlanet);

        SetText(
            planetName.ToUpper() + " MOONS",
            "Total moons in the 8-planet system: " + totalMoonsAroundEightPlanets +
            "\n" + planetName + ": " + totalMoonCount + " moons",
            GetMoonFact(moonSelectedPlanet)
        );
    }

    void SetText(string title, string body, string footer)
    {
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
        if (footerText != null) footerText.text = footer;
    }

    bool UseSharedAnchorForHologramPosition()
    {
        if (!useSharedHologramAnchor || sharedHologramAnchor == null || hologramRoot == null)
        {
            return false;
        }

        hologramRoot.transform.position = sharedHologramAnchor.position + sharedAnchorWorldOffset;

        Camera cam = Camera.main;

        if (sharedAnchorFacesCamera && cam != null)
        {
            Vector3 toCamera = cam.transform.position - hologramRoot.transform.position;

            if (toCamera.sqrMagnitude > 0.0001f)
            {
                // Face the VR camera directly.
                // If it faces backwards in your project, set Shared Anchor Yaw Correction to 180 in Inspector.
                hologramRoot.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
                hologramRoot.transform.Rotate(0f, sharedAnchorYawCorrection, 0f, Space.Self);
            }
        }
        else
        {
            hologramRoot.transform.rotation = sharedHologramAnchor.rotation;
        }

        return true;
    }

    void UpdateHologramPositionFromHand()
    {
        if (UseSharedAnchorForHologramPosition())
        {
            return;
        }

        if (!useHandBoundsForHologram || hologramRoot == null)
        {
            return;
        }

        Transform handRoot = handVisualRoot != null ? handVisualRoot : hologramParent;

        if (handRoot == null)
        {
            return;
        }

        Bounds handBounds;

        if (!TryGetRendererBounds(handRoot, out handBounds))
        {
            return;
        }

        Camera cam = Camera.main;

        Vector3 topOfHand = new Vector3(
            handBounds.center.x,
            handBounds.max.y,
            handBounds.center.z
        );

        Vector3 towardCamera = Vector3.zero;
        Vector3 cameraRight = Vector3.zero;

        if (cam != null)
        {
            towardCamera = cam.transform.position - topOfHand;
            towardCamera.y = 0f;

            if (towardCamera.sqrMagnitude < 0.0001f)
            {
                towardCamera = -cam.transform.forward;
                towardCamera.y = 0f;
            }

            towardCamera.Normalize();

            cameraRight = cam.transform.right;
            cameraRight.y = 0f;

            if (cameraRight.sqrMagnitude > 0.0001f)
            {
                cameraRight.Normalize();
            }
        }
        else
        {
            towardCamera = -transform.forward;
            towardCamera.y = 0f;

            if (towardCamera.sqrMagnitude > 0.0001f)
            {
                towardCamera.Normalize();
            }

            cameraRight = transform.right;
            cameraRight.y = 0f;

            if (cameraRight.sqrMagnitude > 0.0001f)
            {
                cameraRight.Normalize();
            }
        }

        Vector3 finalPosition =
            topOfHand +
            Vector3.up * hologramHeightAboveHand +
            towardCamera * hologramTowardCamera +
            cameraRight * hologramSideOffset;

        hologramRoot.transform.position = finalPosition;

        if (handPlacedHologramFacesCamera && cam != null)
        {
            hologramRoot.transform.LookAt(cam.transform.position);
            hologramRoot.transform.Rotate(0f, 180f, 0f);
        }
    }

    bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds(root.position, Vector3.zero);

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        bool found = false;

        foreach (Renderer r in renderers)
        {
            if (r == null || !r.enabled)
            {
                continue;
            }

            if (!found)
            {
                bounds = r.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return found;
    }

    void ShowHologram()
    {
        if (hologramRoot != null)
        {
            UpdateHologramPositionFromHand();
            hologramRoot.SetActive(true);
        }
    }

    void HideHologram()
    {
        if (hologramRoot != null)
        {
            hologramRoot.SetActive(false);
        }
    }

    void BuildHologram()
    {
        hologramRoot = new GameObject("Solar System Analysis Hologram");
        hologramRoot.transform.SetParent(hologramParent);
        if (forceHologramHighAboveHand)
        {
            hologramLocalPosition = forcedHighHologramLocalPosition;
        }

        hologramRoot.transform.localPosition = hologramLocalPosition;
        hologramRoot.transform.localRotation = Quaternion.Euler(hologramLocalRotation);
        hologramRoot.transform.localScale = hologramLocalScale;
        UpdateHologramPositionFromHand();

        CreateCube(
            "Analysis Panel Background",
            hologramRoot.transform,
            Vector3.zero,
            Vector3.zero,
            new Vector3(0.54f, 0.34f, 0.010f),
            panelMaterial,
            false
        );

        CreateCube("Analysis Panel Top Border", hologramRoot.transform, new Vector3(0f, 0.175f, -0.006f), Vector3.zero, new Vector3(0.56f, 0.010f, 0.010f), borderMaterial, false);
        CreateCube("Analysis Panel Bottom Border", hologramRoot.transform, new Vector3(0f, -0.175f, -0.006f), Vector3.zero, new Vector3(0.56f, 0.010f, 0.010f), borderMaterial, false);
        CreateCube("Analysis Panel Left Border", hologramRoot.transform, new Vector3(-0.28f, 0f, -0.006f), Vector3.zero, new Vector3(0.010f, 0.35f, 0.010f), borderMaterial, false);
        CreateCube("Analysis Panel Right Border", hologramRoot.transform, new Vector3(0.28f, 0f, -0.006f), Vector3.zero, new Vector3(0.010f, 0.35f, 0.010f), borderMaterial, false);

        titleText = CreateText("Analysis Title", hologramRoot.transform, new Vector3(-0.245f, 0.125f, -0.018f), 0.010f);
        bodyText = CreateText("Analysis Body", hologramRoot.transform, new Vector3(-0.245f, 0.045f, -0.018f), 0.0062f);
        footerText = CreateText("Analysis Footer", hologramRoot.transform, new Vector3(-0.245f, -0.135f, -0.018f), 0.0055f);

        menuRoot = new GameObject("Analysis Menu Buttons");
        menuRoot.transform.SetParent(hologramRoot.transform);
        menuRoot.transform.localPosition = Vector3.zero;
        menuRoot.transform.localRotation = Quaternion.identity;
        menuRoot.transform.localScale = Vector3.one;

        CreateButton("AnalysisButton_Distance", "DISTANCE", menuRoot.transform, new Vector3(-0.150f, -0.035f, -0.020f));
        CreateButton("AnalysisButton_Compare", "COMPARE", menuRoot.transform, new Vector3(0.150f, -0.035f, -0.020f));
        CreateButton("AnalysisButton_Moons", "MOONS", menuRoot.transform, new Vector3(-0.150f, -0.105f, -0.020f));
        CreateButton("AnalysisButton_Exit", "EXIT", menuRoot.transform, new Vector3(0.150f, -0.105f, -0.020f));

        distanceRoot = new GameObject("Distance Page Root");
        distanceRoot.transform.SetParent(hologramRoot.transform);
        distanceRoot.transform.localPosition = Vector3.zero;
        distanceRoot.transform.localRotation = Quaternion.identity;
        distanceRoot.transform.localScale = Vector3.one;

        compareRoot = new GameObject("Compare Page Root");
        compareRoot.transform.SetParent(hologramRoot.transform);
        compareRoot.transform.localPosition = Vector3.zero;
        compareRoot.transform.localRotation = Quaternion.identity;
        compareRoot.transform.localScale = Vector3.one;

        compareShowChartsButton = CreateButton("AnalysisButton_GasCharts", "GAS CHARTS", compareRoot.transform, new Vector3(0f, -0.100f, -0.020f));
        compareShowChartsButton.SetActive(false);

        compareChartPageRoot = new GameObject("Compare Chart Page Root");
        compareChartPageRoot.transform.SetParent(hologramRoot.transform);
        compareChartPageRoot.transform.localPosition = Vector3.zero;
        compareChartPageRoot.transform.localRotation = Quaternion.identity;
        compareChartPageRoot.transform.localScale = Vector3.one;

        CreateCompareCharts();
        compareBackButton = CreateButton("AnalysisButton_BackToCompare", "BACK", compareChartPageRoot.transform, new Vector3(0f, -0.135f, -0.020f));

        menuRoot.SetActive(false);
        distanceRoot.SetActive(false);
        compareRoot.SetActive(false);
        compareChartPageRoot.SetActive(false);
    }

    void CreateCompareCharts()
    {
        compareLeftBars = new Transform[compareGasLabels.Length];
        compareRightBars = new Transform[compareGasLabels.Length];
        compareLeftBarValues = new TextMesh[compareGasLabels.Length];
        compareRightBarValues = new TextMesh[compareGasLabels.Length];
        compareLeftCurrentValues = new float[compareGasLabels.Length];
        compareRightCurrentValues = new float[compareGasLabels.Length];
        compareLeftTargetValues = new float[compareGasLabels.Length];
        compareRightTargetValues = new float[compareGasLabels.Length];

        GameObject chartHeaderObject = new GameObject("CompareChart_SmallHeader");
        chartHeaderObject.transform.SetParent(compareChartPageRoot.transform);
        chartHeaderObject.transform.localPosition = new Vector3(0f, 0.120f, -0.020f);
        chartHeaderObject.transform.localRotation = Quaternion.identity;
        chartHeaderObject.transform.localScale = Vector3.one;

        TextMesh chartHeader = chartHeaderObject.AddComponent<TextMesh>();
        chartHeader.text = "ATMOSPHERIC GASES (%)";
        chartHeader.fontSize = 40;
        chartHeader.characterSize = 0.0045f;
        chartHeader.anchor = TextAnchor.MiddleCenter;
        chartHeader.alignment = TextAlignment.Center;
        chartHeader.color = Color.white;

        compareChartLeftRoot = CreateSingleCompareChart("CompareChart_Left", compareChartPageRoot.transform, new Vector3(-0.125f, 0.010f, -0.020f), true);
        compareChartRightRoot = CreateSingleCompareChart("CompareChart_Right", compareChartPageRoot.transform, new Vector3(0.125f, 0.010f, -0.020f), false);

        compareChartLeftRoot.transform.localScale = Vector3.one * compareChartPageScale;
        compareChartRightRoot.transform.localScale = Vector3.one * compareChartPageScale;

        ResetCompareChartAnimation();
    }

    GameObject CreateSingleCompareChart(string objectName, Transform parent, Vector3 localPosition, bool isLeft)
    {
        GameObject chartRoot = new GameObject(objectName);
        chartRoot.transform.SetParent(parent);
        chartRoot.transform.localPosition = localPosition;
        chartRoot.transform.localRotation = Quaternion.identity;
        chartRoot.transform.localScale = Vector3.one;

        CreateCube(
            objectName + "_Background",
            chartRoot.transform,
            new Vector3(0f, 0f, 0.002f),
            Vector3.zero,
            new Vector3(0.185f, 0.128f, 0.004f),
            panelMaterial,
            false
        );

        CreateCube(objectName + "_AxisBottom", chartRoot.transform, new Vector3(0f, -0.030f, -0.002f), Vector3.zero, new Vector3(0.145f, 0.0025f, 0.004f), borderMaterial, false);
        CreateCube(objectName + "_AxisLeft", chartRoot.transform, new Vector3(-0.070f, 0.004f, -0.002f), Vector3.zero, new Vector3(0.0025f, 0.072f, 0.004f), borderMaterial, false);

        // Small planet name BELOW the graph, as requested.
        TextMesh chartTitle = CreateText(objectName + "_Title", chartRoot.transform, new Vector3(0f, -0.057f, -0.010f), compareChartPlanetNameSize);
        chartTitle.fontSize = 36;
        chartTitle.anchor = TextAnchor.MiddleCenter;
        chartTitle.alignment = TextAlignment.Center;
        chartTitle.text = isLeft ? "Planet A" : "Planet B";

        float startX = -0.058f;
        float spacing = 0.023f;
        float baseY = -0.028f;

        for (int i = 0; i < compareGasLabels.Length; i++)
        {
            float x = startX + spacing * i;

            GameObject bar = CreateCube(
                objectName + "_Bar_" + compareGasLabels[i],
                chartRoot.transform,
                new Vector3(x, baseY + 0.001f, -0.005f),
                Vector3.zero,
                new Vector3(0.014f, 0.002f, 0.012f),
                CreateMaterial(GetGasColor(i), false),
                false
            );

            TextMesh valueText = CreateText(objectName + "_Value_" + compareGasLabels[i], chartRoot.transform, new Vector3(x, baseY + 0.008f, -0.013f), compareChartValueSize);
            valueText.fontSize = 28;
            valueText.anchor = TextAnchor.MiddleCenter;
            valueText.alignment = TextAlignment.Center;
            valueText.text = "0";

            TextMesh labelText = CreateText(objectName + "_Label_" + compareGasLabels[i], chartRoot.transform, new Vector3(x, -0.042f, -0.013f), compareChartGasLabelSize);
            labelText.fontSize = 26;
            labelText.anchor = TextAnchor.MiddleCenter;
            labelText.alignment = TextAlignment.Center;
            labelText.text = compareGasLabels[i];

            if (isLeft)
            {
                compareLeftBars[i] = bar.transform;
                compareLeftBarValues[i] = valueText;
                compareChartLeftTitle = chartTitle;
            }
            else
            {
                compareRightBars[i] = bar.transform;
                compareRightBarValues[i] = valueText;
                compareChartRightTitle = chartTitle;
            }
        }

        return chartRoot;
    }

    Color GetGasColor(int index)
    {
        switch (index)
        {
            case 0: return new Color(0.95f, 0.35f, 0.35f, 1f);
            case 1: return new Color(0.35f, 0.80f, 1.00f, 1f);
            case 2: return new Color(0.45f, 0.55f, 1.00f, 1f);
            case 3: return new Color(0.45f, 0.95f, 0.55f, 1f);
            case 4: return new Color(1.00f, 0.85f, 0.25f, 1f);
            default: return new Color(0.85f, 0.85f, 0.90f, 1f);
        }
    }

    void ResetCompareChartAnimation()
    {
        compareChartShownPlanetA = null;
        compareChartShownPlanetB = null;

        ResetChartArray(compareLeftCurrentValues);
        ResetChartArray(compareRightCurrentValues);
        ResetChartArray(compareLeftTargetValues);
        ResetChartArray(compareRightTargetValues);

        ApplyChartVisuals(compareLeftBars, compareLeftBarValues, compareLeftCurrentValues);
        ApplyChartVisuals(compareRightBars, compareRightBarValues, compareRightCurrentValues);

        if (compareChartLeftTitle != null) compareChartLeftTitle.text = "Planet A";
        if (compareChartRightTitle != null) compareChartRightTitle.text = "Planet B";

        if (compareChartLeftRoot != null) compareChartLeftRoot.SetActive(false);
        if (compareChartRightRoot != null) compareChartRightRoot.SetActive(false);
    }

    void ResetChartArray(float[] array)
    {
        if (array == null) return;
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = 0f;
        }
    }

    void UpdateCompareChartVisuals(bool forceRefresh = false)
    {
        bool chartPageVisible = currentPage == AnalysisPage.CompareCharts &&
                                compareChartPageRoot != null &&
                                compareChartPageRoot.activeInHierarchy;

        if (!showCompareGasCharts || !chartPageVisible)
        {
            if (compareChartLeftRoot != null) compareChartLeftRoot.SetActive(false);
            if (compareChartRightRoot != null) compareChartRightRoot.SetActive(false);
            return;
        }

        if (compareChartLeftRoot != null) compareChartLeftRoot.SetActive(true);
        if (compareChartRightRoot != null) compareChartRightRoot.SetActive(true);

        if (forceRefresh || comparePlanetA != compareChartShownPlanetA)
        {
            compareChartShownPlanetA = comparePlanetA;
            ResetChartArray(compareLeftCurrentValues);
            SetChartTargetsForPlanet(comparePlanetA, compareLeftTargetValues);
            if (compareChartLeftTitle != null)
            {
                compareChartLeftTitle.text = comparePlanetA != null ? PlanetDisplayName(comparePlanetA) : "Planet A";
            }
        }

        if (forceRefresh || comparePlanetB != compareChartShownPlanetB)
        {
            compareChartShownPlanetB = comparePlanetB;
            ResetChartArray(compareRightCurrentValues);
            SetChartTargetsForPlanet(comparePlanetB, compareRightTargetValues);
            if (compareChartRightTitle != null)
            {
                compareChartRightTitle.text = comparePlanetB != null ? PlanetDisplayName(comparePlanetB) : "Planet B";
            }
        }

        AnimateChartValues(compareLeftCurrentValues, compareLeftTargetValues);
        AnimateChartValues(compareRightCurrentValues, compareRightTargetValues);

        ApplyChartVisuals(compareLeftBars, compareLeftBarValues, compareLeftCurrentValues);
        ApplyChartVisuals(compareRightBars, compareRightBarValues, compareRightCurrentValues);
    }

    void AnimateChartValues(float[] current, float[] target)
    {
        if (current == null || target == null) return;

        float step = compareChartAnimationSpeed * Time.unscaledDeltaTime;

        for (int i = 0; i < current.Length; i++)
        {
            current[i] = Mathf.MoveTowards(current[i], target[i], step);
        }
    }

    void SetChartTargetsForPlanet(Transform planet, float[] targetArray)
    {
        ResetChartArray(targetArray);

        if (planet == null || targetArray == null)
        {
            return;
        }

        float[] values = GetAtmospherePercentages(planet);

        for (int i = 0; i < targetArray.Length && i < values.Length; i++)
        {
            targetArray[i] = values[i];
        }
    }

    float[] GetAtmospherePercentages(Transform planet)
    {
        string n = PlanetDisplayName(planet).ToLower();

        if (n.Contains("mercury")) return new float[] { 0f, 0f, 0f, 0f, 0f, 100f };
        if (n.Contains("venus"))   return new float[] { 96.5f, 0f, 3.5f, 0f, 0f, 0f };
        if (n.Contains("earth"))   return new float[] { 0.04f, 21.0f, 78.0f, 0.0002f, 0f, 0.96f };
        if (n.Contains("mars"))    return new float[] { 95.3f, 0.13f, 2.7f, 0.0004f, 0f, 1.87f };
        if (n.Contains("jupiter")) return new float[] { 0f, 0f, 0f, 0.3f, 89.8f, 9.9f };
        if (n.Contains("saturn"))  return new float[] { 0f, 0f, 0f, 0.4f, 96.3f, 3.3f };
        if (n.Contains("uranus"))  return new float[] { 0f, 0f, 0f, 2.3f, 82.5f, 15.2f };
        if (n.Contains("neptune")) return new float[] { 0f, 0f, 0f, 1.5f, 80.0f, 18.5f };

        return new float[] { 0f, 0f, 0f, 0f, 0f, 100f };
    }

    void ApplyChartVisuals(Transform[] bars, TextMesh[] values, float[] amounts)
    {
        if (bars == null || values == null || amounts == null) return;

        float baseY = -0.028f;
        float minHeight = 0.002f;

        for (int i = 0; i < bars.Length && i < amounts.Length; i++)
        {
            if (bars[i] == null) continue;

            float height = Mathf.Max(minHeight, (Mathf.Clamp(amounts[i], 0f, 100f) / 100f) * compareChartBarMaxHeight);
            Vector3 scale = bars[i].localScale;
            scale.y = height;
            bars[i].localScale = scale;

            Vector3 pos = bars[i].localPosition;
            pos.y = baseY + height * 0.5f;
            bars[i].localPosition = pos;

            if (values[i] != null)
            {
                values[i].text = amounts[i] >= 1f ? Mathf.RoundToInt(amounts[i]).ToString() : amounts[i].ToString("0.0");
                values[i].transform.localPosition = new Vector3(pos.x, baseY + height + 0.007f, -0.013f);
            }
        }
    }

    void CreateMoonModeVisuals()
    {
        if (moonsRoot != null)
        {
            return;
        }

        moonsRoot = new GameObject("Analysis Moons Visuals");
        moonVisualMaterial = CreateMoonSurfaceMaterial();
        ApplyMoonTextureToMoonMaterial();
        moonOrbitPathMaterial = CreateMaterial(new Color(0.15f, 0.85f, 1f, 0.55f), true);
    }

    Material CreateMoonSurfaceMaterial()
    {
        Shader shader = null;

        // A lit opaque shader shows the lunar texture properly and avoids the glowing
        // white look caused by the old hologram/emission material.
        if (useOpaqueMoonSurfaceMaterial)
        {
            shader = Shader.Find("HDRP/Lit");

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
        }

        // Fallbacks for projects that use a different rendering pipeline.
        if (shader == null)
        {
            shader = Shader.Find("HDRP/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);

        // Moons should be normal opaque objects, not transparent glowing holograms.
        material.renderQueue = 2000;

        if (material.HasProperty("_SurfaceType"))
        {
            material.SetFloat("_SurfaceType", 0f);
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", moonTextureTint);
        if (material.HasProperty("_UnlitColor")) material.SetColor("_UnlitColor", moonTextureTint);
        if (material.HasProperty("_Color")) material.SetColor("_Color", moonTextureTint);

        if (material.HasProperty("_EmissionColor"))
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }

        return material;
    }

    void ApplyMoonTextureToMoonMaterial()
    {
        if (moonVisualMaterial == null)
        {
            return;
        }

        Color tint = moonTextureTint;
        if (tint.a <= 0f)
        {
            tint.a = 1f;
        }

        if (moonVisualMaterial.HasProperty("_BaseColor")) moonVisualMaterial.SetColor("_BaseColor", tint);
        if (moonVisualMaterial.HasProperty("_UnlitColor")) moonVisualMaterial.SetColor("_UnlitColor", tint);
        if (moonVisualMaterial.HasProperty("_Color")) moonVisualMaterial.SetColor("_Color", tint);

        Texture textureToApply = useMoonTextureOnVisualMoons ? moonSurfaceTexture : null;

        // Different Unity pipelines use different property names.
        // Setting all supported names makes this work with HDRP, URP, Standard,
        // and Unlit shaders.
        SetMoonTextureProperty("_BaseColorMap", textureToApply);
        SetMoonTextureProperty("_UnlitColorMap", textureToApply);
        SetMoonTextureProperty("_BaseMap", textureToApply);
        SetMoonTextureProperty("_MainTex", textureToApply);

        if (textureToApply != null)
        {
            moonVisualMaterial.mainTexture = textureToApply;
        }

        if (moonVisualMaterial.HasProperty("_EmissionColor"))
        {
            moonVisualMaterial.DisableKeyword("_EMISSION");
            moonVisualMaterial.SetColor("_EmissionColor", Color.black);
        }
    }

    void SetMoonTextureProperty(string propertyName, Texture texture)
    {
        if (moonVisualMaterial == null || !moonVisualMaterial.HasProperty(propertyName))
        {
            return;
        }

        moonVisualMaterial.SetTexture(propertyName, texture);
        moonVisualMaterial.SetTextureScale(propertyName, moonTextureTiling);
    }

    float GetMoonOrbitBaseMultiplier(Transform planet)
    {
        string n = PlanetDisplayName(planet).ToLower();

        if (n.Contains("jupiter"))
        {
            return jupiterMoonOrbitRadiusMultiplier;
        }

        if (n.Contains("saturn"))
        {
            return saturnMoonOrbitRadiusMultiplier;
        }

        return moonOrbitRadiusMultiplier;
    }

    float GetMoonOrbitRingSpacingMultiplier(Transform planet)
    {
        string n = PlanetDisplayName(planet).ToLower();

        if (n.Contains("jupiter") || n.Contains("saturn"))
        {
            return giantPlanetMoonOrbitRingSpacingMultiplier;
        }

        return moonOrbitRingSpacingMultiplier;
    }

    void ResetMoonMode()
    {
        moonSelectedPlanet = null;
        ClearMoonVisuals();
        HideMoonModeVisuals();
    }

    void HideMoonModeVisuals()
    {
        if (moonsRoot != null)
        {
            moonsRoot.SetActive(false);
        }
    }

    void ClearMoonVisuals()
    {
        moonVisuals.Clear();
        moonOrbitPaths.Clear();

        if (moonsRoot == null)
        {
            return;
        }

        for (int i = moonsRoot.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(moonsRoot.transform.GetChild(i).gameObject);
        }
    }

    void BuildMoonVisualsForPlanet(Transform planet)
    {
        CreateMoonModeVisuals();
        ApplyMoonTextureToMoonMaterial();
        ClearMoonVisuals();

        if (moonsRoot != null)
        {
            moonsRoot.SetActive(true);
        }

        if (planet == null)
        {
            return;
        }

        int knownMoonCount = GetKnownMoonCount(planet);

        if (knownMoonCount <= 0)
        {
            return;
        }

        int visibleMoonCount = visualizeEveryMoon
            ? knownMoonCount
            : GetVisualMoonCountForPlanet(planet, knownMoonCount);

        float planetRadius = Mathf.Max(0.25f, GetPlanetVisualRadius(planet));
        float visualMoonSize = Mathf.Clamp(
            planetRadius * moonVisualSizeMultiplier,
            0.04f,
            Mathf.Max(0.08f, planetRadius * 0.22f)
        );

        int moonsPerRing = 8;
        int ringCount = Mathf.CeilToInt((float)visibleMoonCount / moonsPerRing);

        if (showMoonOrbitPaths)
        {
            for (int ring = 0; ring < ringCount; ring++)
            {
                float ringRadius = planetRadius *
                    (GetMoonOrbitBaseMultiplier(planet) +
                     ring * GetMoonOrbitRingSpacingMultiplier(planet));

                CreateMoonOrbitPath(ringRadius, ring * 14f);
            }
        }

        for (int i = 0; i < visibleMoonCount; i++)
        {
            int ringIndex = i / moonsPerRing;
            int positionInRing = i % moonsPerRing;

            float ringRadius = planetRadius *
                (GetMoonOrbitBaseMultiplier(planet) +
                 ringIndex * GetMoonOrbitRingSpacingMultiplier(planet));

            GameObject moonObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moonObject.name = "Moon Visual " + (i + 1) + " of " + knownMoonCount;

            Collider moonCollider = moonObject.GetComponent<Collider>();
            if (moonCollider != null)
            {
                Destroy(moonCollider);
            }

            Renderer moonRenderer = moonObject.GetComponent<Renderer>();
            if (moonRenderer != null)
            {
                moonRenderer.material = moonVisualMaterial;
            }

            moonObject.transform.SetParent(moonsRoot.transform, true);

            float variation = 0.72f + (i % 4) * 0.10f;
            moonObject.transform.localScale = Vector3.one * visualMoonSize * variation;

            MoonVisualData moonData = new MoonVisualData();
            moonData.transform = moonObject.transform;
            moonData.ringRadius = ringRadius;
            moonData.startAngle = (360f / Mathf.Max(1, moonsPerRing)) * positionInRing + ringIndex * 19f;
            moonData.angularSpeed = moonOrbitSpeedDegrees * (1f + ringIndex * 0.25f) * (i % 2 == 0 ? 1f : -1f);
            moonData.inclination = -22f + ((i * 17) % 44);

            moonVisuals.Add(moonData);
        }
    }

    void CreateMoonOrbitPath(float radius, float inclination)
    {
        GameObject pathObject = new GameObject("Moon Orbit Path");
        pathObject.transform.SetParent(moonsRoot.transform, true);

        LineRenderer line = pathObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = 72;
        line.widthMultiplier = 0.012f;
        line.material = moonOrbitPathMaterial;

        moonOrbitPaths.Add(line);
    }

    void UpdateMoonModeVisuals()
    {
        if (moonsRoot == null || currentPage != AnalysisPage.Moons)
        {
            return;
        }

        moonsRoot.SetActive(true);

        if (moonSelectedPlanet == null)
        {
            return;
        }

        Vector3 center = moonSelectedPlanet.position;
        float time = Time.unscaledTime;

        for (int i = 0; i < moonVisuals.Count; i++)
        {
            MoonVisualData moon = moonVisuals[i];

            if (moon.transform == null)
            {
                continue;
            }

            float angle = moon.startAngle + time * moon.angularSpeed;
            Vector3 localOrbitPoint = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * moon.ringRadius,
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * moon.ringRadius
            );

            Quaternion tilt = Quaternion.AngleAxis(moon.inclination, Vector3.right);
            moon.transform.position = center + tilt * localOrbitPoint;
        }

        if (showMoonOrbitPaths)
        {
            int pathIndex = 0;
            int visualCount = moonVisuals.Count;
            int ringCount = Mathf.CeilToInt((float)visualCount / 8f);
            float planetRadius = Mathf.Max(0.25f, GetPlanetVisualRadius(moonSelectedPlanet));

            for (int ring = 0; ring < ringCount && pathIndex < moonOrbitPaths.Count; ring++)
            {
                float radius = planetRadius *
                    (GetMoonOrbitBaseMultiplier(moonSelectedPlanet) +
                     ring * GetMoonOrbitRingSpacingMultiplier(moonSelectedPlanet));

                LineRenderer pathLine = moonOrbitPaths[pathIndex];
                UpdateMoonOrbitPath(pathLine, radius, ring * 14f);
                pathIndex++;
            }
        }
    }

    void UpdateMoonOrbitPath(LineRenderer line, float radius, float inclination)
    {
        if (line == null || moonSelectedPlanet == null)
        {
            return;
        }

        Vector3 center = moonSelectedPlanet.position;
        Quaternion tilt = Quaternion.AngleAxis(inclination, Vector3.right);

        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / line.positionCount;
            Vector3 localPoint = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            line.SetPosition(i, center + tilt * localPoint);
        }
    }

    int GetVisualMoonCountForPlanet(Transform planet, int knownMoonCount)
    {
        if (knownMoonCount <= 0)
        {
            return 0;
        }

        if (!useRatioBasedVisualMoonCounts)
        {
            return Mathf.Min(knownMoonCount, Mathf.Max(1, maxVisualMoons));
        }

        string n = PlanetDisplayName(planet).ToLower();

        // Inner planets stay exact for simple learning.
        if (n.Contains("earth")) return 1;
        if (n.Contains("mars")) return 2;

        // Compact visual ratio:
        // Saturn 30 > Jupiter 20 > Uranus 12 > Neptune 8.
        if (n.Contains("jupiter")) return Mathf.Min(knownMoonCount, Mathf.Max(1, jupiterVisualMoonCount));
        if (n.Contains("saturn")) return Mathf.Min(knownMoonCount, Mathf.Max(1, saturnVisualMoonCount));
        if (n.Contains("uranus")) return Mathf.Min(knownMoonCount, Mathf.Max(1, uranusVisualMoonCount));
        if (n.Contains("neptune")) return Mathf.Min(knownMoonCount, Mathf.Max(1, neptuneVisualMoonCount));

        return Mathf.Min(knownMoonCount, Mathf.Max(1, maxVisualMoons));
    }

    int GetKnownMoonCount(Transform planet)
    {
        string n = PlanetDisplayName(planet).ToLower();

        if (n.Contains("mercury")) return 0;
        if (n.Contains("venus")) return 0;
        if (n.Contains("earth")) return 1;
        if (n.Contains("mars")) return 2;
        if (n.Contains("jupiter")) return 101;
        if (n.Contains("saturn")) return 274;
        if (n.Contains("uranus")) return 28;
        if (n.Contains("neptune")) return 16;

        return 0;
    }

    string GetMoonFact(Transform planet)
    {
        string n = PlanetDisplayName(planet).ToLower();

        if (n.Contains("mercury") || n.Contains("venus"))
        {
            return "No known natural satellites";
        }

        if (n.Contains("earth")) return "Earth's Moon is its only natural satellite";
        if (n.Contains("mars")) return "Phobos and Deimos orbit Mars";
        if (n.Contains("jupiter")) return "Jupiter has a very large and diverse moon system";
        if (n.Contains("saturn")) return "Saturn currently has the most confirmed moons";
        if (n.Contains("uranus")) return "Its moons are mostly named after literary characters";
        if (n.Contains("neptune")) return "Triton is Neptune's largest moon";

        return "";
    }

    GameObject CreateButton(string objectName, string label, Transform parent, Vector3 localPosition)
    {
        GameObject button = CreateCube(
            objectName,
            parent,
            localPosition,
            Vector3.zero,
            new Vector3(0.22f, 0.060f, 0.035f),
            buttonMaterial,
            true
        );

        TextMesh buttonText = CreateText(
            objectName + "_Text",
            button.transform,
            new Vector3(0f, -0.002f, -0.026f),
            0.017f
        );

        buttonText.fontSize = 96;
        buttonText.anchor = TextAnchor.MiddleCenter;
        buttonText.alignment = TextAlignment.Center;
        buttonText.text = label;
        return button;
    }

    TextMesh CreateText(string objectName, Transform parent, Vector3 localPosition, float characterSize)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.identity;
        textObject.transform.localScale = Vector3.one;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.fontSize = 48;
        text.characterSize = characterSize;
        text.anchor = TextAnchor.UpperLeft;
        text.alignment = TextAlignment.Left;
        text.color = Color.white;
        text.text = "";

        return text;
    }

    GameObject CreateCube(string objectName, Transform parent, Vector3 localPosition, Vector3 localRotation, Vector3 localScale, Material material, bool keepCollider)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = objectName;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.Euler(localRotation);
        obj.transform.localScale = localScale;

        Renderer renderer = obj.GetComponent<Renderer>();
        renderer.material = material;

        if (!keepCollider)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        return obj;
    }

    void CreateAnalysisRay()
    {
        analysisRayMaterial = CreateMaterial(analysisRayColor, true);

        analysisRayObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        analysisRayObject.name = "Analysis Mode Cyan Selection Ray";

        Collider col = analysisRayObject.GetComponent<Collider>();
        if (col != null) Destroy(col);

        analysisRayObject.GetComponent<Renderer>().material = analysisRayMaterial;
        analysisRayObject.SetActive(false);
    }

    void UpdateAnalysisRay()
    {
        if (!showAnalysisRay || gunMuzzle == null || analysisRayObject == null)
        {
            HideAnalysisRay();
            return;
        }

        Vector3 start = gunMuzzle.position;
        Vector3 direction = gunMuzzle.forward.normalized;
        Vector3 end = start + direction * analysisRayVisibleLength;

        RaycastHit[] hits = Physics.RaycastAll(start, direction, analysisRayDistance, ~0, QueryTriggerInteraction.Collide);

        float closest = Mathf.Infinity;
        bool found = false;

        Transform playerRoot = transform.root;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;

            bool isButton = hit.collider.gameObject.name.ToLower().Contains("analysisbutton");
            bool isPlanet = FindPlanetTransform(hit.collider.transform) != null;

            if (!isButton && !isPlanet)
            {
                continue;
            }

            if (!isButton && hit.collider.transform.root == playerRoot)
            {
                continue;
            }

            if (hit.distance < closest)
            {
                closest = hit.distance;
                end = hit.point;
                found = true;
            }
        }

        if (!found)
        {
            end = start + direction * analysisRayVisibleLength;
        }

        SetCylinderBetween(analysisRayObject, start, end, analysisRayRadius);
        analysisRayObject.SetActive(true);
    }

    void HideAnalysisRay()
    {
        if (analysisRayObject != null)
        {
            analysisRayObject.SetActive(false);
        }
    }

    void SetCylinderBetween(GameObject cylinder, Vector3 start, Vector3 end, float radius)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;

        if (length < 0.001f)
        {
            return;
        }

        cylinder.transform.position = start + direction * 0.5f;
        cylinder.transform.up = direction.normalized;
        cylinder.transform.localScale = new Vector3(radius, length * 0.5f, radius);
    }

    void AddCollidersToKnownPlanets()
    {
        Transform[] allTransforms = FindObjectsOfType<Transform>();

        foreach (Transform t in allTransforms)
        {
            if (t == null)
            {
                continue;
            }

            if (!IsKnownPlanetName(t.name))
            {
                continue;
            }

            if (t.GetComponent<Collider>() != null)
            {
                continue;
            }

            SphereCollider collider = t.gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = false;

            Renderer r = t.GetComponentInChildren<Renderer>();

            if (r != null)
            {
                float radius = Mathf.Max(r.bounds.extents.x, r.bounds.extents.y, r.bounds.extents.z);
                float scale = Mathf.Max(t.lossyScale.x, t.lossyScale.y, t.lossyScale.z);

                if (scale > 0.001f)
                {
                    collider.radius = Mathf.Max(defaultPlanetColliderRadius, radius / scale);
                }
                else
                {
                    collider.radius = defaultPlanetColliderRadius;
                }
            }
            else
            {
                collider.radius = defaultPlanetColliderRadius;
            }
        }
    }

    void DisableGunScripts()
    {
        disabledGunScripts.Clear();

        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();

        foreach (MonoBehaviour script in allScripts)
        {
            if (script == null) continue;

            if (script.GetType().Name == "QuestHandGunLaser" && script.enabled)
            {
                script.enabled = false;
                disabledGunScripts.Add(script);
            }
        }
    }

    void EnableGunScripts()
    {
        foreach (MonoBehaviour script in disabledGunScripts)
        {
            if (script != null)
            {
                script.enabled = true;
            }
        }

        disabledGunScripts.Clear();
    }

    void DisableLayerHologramScripts()
    {
        disabledLayerHologramScripts.Clear();

        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();

        foreach (MonoBehaviour script in allScripts)
        {
            if (script == null) continue;

            string n = script.GetType().Name;

            if ((n == "LeftHandLayerHologramProjector" || n == "EarthLayerWristHologram") && script.enabled)
            {
                // Do not disable this analysis script.
                if (script == this)
                {
                    continue;
                }

                script.enabled = false;
                disabledLayerHologramScripts.Add(script);
            }
        }
    }

    void EnableLayerHologramScripts()
    {
        foreach (MonoBehaviour script in disabledLayerHologramScripts)
        {
            if (script != null)
            {
                script.enabled = true;
            }
        }

        disabledLayerHologramScripts.Clear();
    }

    void CreateMaterials()
    {
        panelMaterial = CreateMaterial(new Color(0f, 0.55f, 1f, 0.35f), true);
        borderMaterial = CreateMaterial(new Color(0f, 0.9f, 1f, 1f), true);
        buttonMaterial = CreateMaterial(new Color(0.04f, 0.16f, 0.22f, 0.95f), true);
        selectedButtonMaterial = CreateMaterial(new Color(0f, 0.8f, 1f, 1f), true);
        textGlowMaterial = CreateMaterial(Color.white, true);
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

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_UnlitColor")) material.SetColor("_UnlitColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);

        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }

        material.renderQueue = 3000;

        return material;
    }

    void OnDisable()
    {
        if (analysisModeOn)
        {
            ExitAnalysisMode();
        }
    }

    private class MoonVisualData
    {
        public Transform transform;
        public float ringRadius;
        public float startAngle;
        public float angularSpeed;
        public float inclination;
    }

    private struct PlanetInfo
    {
        public string displayName;
        public string type;
        public bool hasAtmosphere;
        public bool hasMoons;
        public string shortFact;

        public PlanetInfo(string displayName, string type, bool hasAtmosphere, bool hasMoons, string shortFact)
        {
            this.displayName = displayName;
            this.type = type;
            this.hasAtmosphere = hasAtmosphere;
            this.hasMoons = hasMoons;
            this.shortFact = shortFact;
        }
    }
}