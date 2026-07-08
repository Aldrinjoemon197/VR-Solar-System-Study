using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// SIMPLE INITIAL-AREA SPACESHIP CONTROLLER
///
/// Attach this ONE script to ExplorerSpaceship.
/// 
/// Intended game flow:
/// 1. Start in third-person spacecraft mode in the intro area.
/// 2. Fly the spacecraft only inside the intro flight zone.
/// 3. Press LEFT GRIP to exit the ship.
/// 4. Press the left Menu button to use the existing wormhole as a PLAYER-only
///    trip to the Solar System. The spacecraft stays behind in the intro area.
///
/// No manual drag-and-drop of other C# scripts is needed.
/// This script automatically pauses the known movement/turn scripts only
/// while the player is in spacecraft mode.
/// </summary>
[DefaultExecutionOrder(32000)]
public class SpaceshipThirdPersonController : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("Normally XR Origin (VR). If empty, the script finds it automatically.")]
    public Transform xrOriginRoot;

    [Tooltip("Normally XR Origin (VR) > Camera Offset > Main Camera. If empty, the script finds it automatically.")]
    public Transform xrHeadCamera;

    [Tooltip("Child of ExplorerSpaceship placed behind and above the ship. Its blue Z arrow should face the flight direction.")]
    public Transform thirdPersonCameraPoint;

    [Tooltip("Child of ExplorerSpaceship placed outside the ship. The player appears here after pressing LEFT X.")]
    public Transform shipExitPoint;

    [Tooltip("Use IntroStartPoint here. The ship starts at this point.")]
    public Transform shipSpawnPoint;

    [Header("Start State")]
    public bool startInShipMode = true;
    public bool spawnShipAtStart = true;

    [Header("Intro Flight Zone")]
    [Tooltip("Keeps the spacecraft in the initial intro area, away from the Solar System.")]
    public bool limitFlightToIntroArea = false;

    [Tooltip("Maximum horizontal distance the spacecraft can travel from its initial spawn position.")]
    public float introFlightRadius = 70f;

    [Tooltip("Maximum distance the spacecraft can move up or down from its initial spawn height.")]
    public float introVerticalRange = 30f;

    [Header("Smooth Ship Movement")]
    public float shipMoveSpeed = 25f;
    public float verticalMoveSpeed = 16f;
    public float shipTurnSpeed = 45f;
    public float movementAcceleration = 28f;
    public float movementDeceleration = 42f;
    public float turnAcceleration = 160f;
    public float turnDeceleration = 220f;

    [Range(0.05f, 0.5f)]
    public float deadzone = 0.15f;

    [Header("Ship Banking / Roll")]
    [Tooltip("ON = the ship leans smoothly while turning, like an aircraft banking left/right.")]
    public bool enableTurnBanking = true;

    [Tooltip("Maximum visual lean angle while turning.")]
    [Range(0f, 45f)]
    public float maxBankAngle = 18f;

    [Tooltip("How quickly the ship leans into a turn and returns level.")]
    public float bankSmoothSpeed = 5f;

    [Tooltip("Turn this ON if the ship banks the wrong way for your model.")]
    public bool invertBankDirection = false;

    [Header("Ship Pitch / Nose Tilt")]
    [Tooltip("ON = ship nose tilts up/down smoothly while moving vertically.")]
    public bool enableVerticalPitch = true;

    [Tooltip("Maximum nose up/down angle while moving vertically.")]
    [Range(0f, 35f)]
    public float maxPitchAngle = 12f;

    [Tooltip("How quickly the ship nose tilts and returns level.")]
    public float pitchSmoothSpeed = 4.5f;

    [Tooltip("Turn this ON if the ship pitches the wrong way for your model.")]
    public bool invertPitchDirection = false;

    [Header("Ship Controls")]
    [Tooltip("LEFT thumbstick: forward/back + sideways movement.")]
    public bool useLeftThumbstickForFlight = true;

    [Tooltip("RIGHT thumbstick left/right: turn spacecraft.")]
    public bool useRightThumbstickForTurning = true;

    [Tooltip("ON = B moves up and A moves down.")]
    public bool useBUpADown = true;

    [Tooltip("Legacy option. If A/B mapping is not forced, RIGHT B button moves spacecraft UP.")]
    public bool rightBMovesUp = true;

    [Tooltip("Legacy option. If A/B mapping is not forced, RIGHT A button moves spacecraft DOWN.")]
    public bool rightAMovesDown = true;

    [Tooltip("Editor-only fallback: keyboard W moves UP and keyboard A moves DOWN. This does not affect Quest controller controls.")]
    public bool keyboardWAndAMoveVertical = true;

    [Header("Mode Toggle")]
    [Tooltip("LEFT GRIP button toggles Ship Mode <-> Astronaut Mode.")]
    public bool toggleWithLeftGripButton = true;

    public float toggleCooldown = 0.45f;

    [Header("Pilot Visuals")]
    [Tooltip("Keep your already-working glove/gun visual objects here. They are hidden in Ship Mode and shown after LEFT X exit.")]
    public GameObject[] hideVisualsWhilePiloting;

    [Tooltip("If ON, visible renderers under XR Origin are hidden while piloting, but scripts and controller objects stay active.")]
    public bool autoHidePlayerRenderersWhilePiloting = true;

    [Header("Solid Bodies")]
    [Tooltip("ON = completely disables the experimental spaceship collision/push fix.")]
    public bool forceDisableSpaceshipCollisionFix = true;

    [Tooltip("ON = while flying, the spaceship cannot pass through planet bodies.")]
    public bool blockSpaceshipFromPlanets = true;

    [Tooltip("Extra clearance between the spaceship and planet surface.")]
    public float planetBlockPadding = 0.85f;

    [Tooltip("ON = adds colliders to the ship so the player cannot walk through it.")]
    public bool makeSpaceshipSolid = false;

    [Tooltip("Extra space around the ship used to slide the player away when walking into it.")]
    public float shipPushPadding = 0.45f;

    [Tooltip("How strongly the player is moved away from the ship surface.")]
    public float shipPushStrength = 10f;

    [Tooltip("ON = adds colliders to glove/gun renderers so they are not just empty visuals.")]
    public bool makeHandsAndGunSolid = true;

    [Header("Exact Ship / Camera Turn Sync")]
    [Tooltip("ON = while the right thumbstick is turning the ship, the camera yaw is aligned to ThirdPersonCameraPoint after the ship turns. This removes camera/ship turn lag.")]
    public bool lockCameraYawToShipWhileTurning = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Controller state
    private InputDevice leftController;
    private InputDevice rightController;
    private bool isShipMode;
    private bool wasLeftGripPressed;
    private float nextToggleTime;

    // Smooth movement state
    private Vector3 currentLocalVelocity;
    private float currentYawRate;
    private float currentBankAngle;
    private float currentPitchAngle;
    private bool shipIsTurningFromRightStick;

    // QuestJoystickMove is handled directly from this controller.
    // No inspector drag-and-drop or extra helper script is required.
    private QuestJoystickMove questJoystickMove;
    private bool questJoystickWasEnabledBeforeShipMode;

    // Initial-area boundary
    private Vector3 introFlightCentre;

    // Ship wormhole travel
    private bool shipWormholeTravelActive;
    private Vector3 shipWormholeStartPosition;
    private Quaternion shipWormholeStartRotation;
    private Transform shipWormholeLandingPoint;

    // Visual hiding keeps controls/scripts alive and restores original states.
    private readonly List<Renderer> autoHiddenPilotRenderers = new List<Renderer>();
    private readonly List<bool> autoHiddenPilotRendererStates = new List<bool>();
    private BoxCollider shipSolidCollider;
    private float nextSolidSetupTime;
    private readonly List<Transform> planetBlockTargets = new List<Transform>();
    private float nextPlanetBlockScanTime;

    /// <summary>
    /// Kept public because your existing WormholeIntroTravel script may still
    /// check it. The ship will NOT travel through the wormhole in this version.
    /// </summary>
    public bool IsInShipMode => isShipMode;

    void Start()
    {
        ProceduralSolarSystemAudio.Ensure();
        ResolveReferences();

        if (spawnShipAtStart && shipSpawnPoint != null)
        {
            transform.SetPositionAndRotation(shipSpawnPoint.position, shipSpawnPoint.rotation);
        }

        introFlightCentre = transform.position;
        ApplySpaceshipCollisionPreference();
        EnsureSpaceshipSolid();
        EnsureHandAndGunSolid();

        SetShipMode(startInShipMode, true);
    }

    void Update()
    {
        ResolveReferences();
        RefreshControllers();
        HandleLeftGripToggle();

        if (isShipMode && !shipWormholeTravelActive)
        {
            UpdateShipFlight();
        }

        if (Time.unscaledTime >= nextSolidSetupTime)
        {
            nextSolidSetupTime = Time.unscaledTime + 1.5f;
            ApplySpaceshipCollisionPreference();
            EnsureSpaceshipSolid();
            EnsureHandAndGunSolid();
        }

        UpdatePlayerShipCollision();
    }

    void ApplySpaceshipCollisionPreference()
    {
        if (!forceDisableSpaceshipCollisionFix)
        {
            return;
        }

        makeSpaceshipSolid = false;

        if (shipSolidCollider != null)
        {
            shipSolidCollider.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (!isShipMode)
        {
            return;
        }

        // First keep the real headset camera at the third-person point.
        FollowThirdPersonCameraPositionOnly();

        // Then, ONLY while the right thumbstick is actively turning the ship,
        // align the camera to the exact yaw of the same camera point.
        // There is no separate camera turn speed anywhere in this version.
        if (lockCameraYawToShipWhileTurning && shipIsTurningFromRightStick)
        {
            SyncCameraYawExactlyToThirdPersonPoint();
        }
    }

    void OnDisable()
    {
        // Never leave normal player movement disabled after a recompile,
        // disabling this component, or leaving Play Mode.
        RestoreQuestJoystickMoveAfterShipMode();
        SetPilotVisuals(true);
        ProceduralSolarSystemAudio.Ensure().SetShipThruster(false, 0f);
    }

    void ResolveReferences()
    {
        if (xrOriginRoot == null)
        {
            GameObject rig = GameObject.Find("XR Origin (VR)");

            if (rig != null)
            {
                xrOriginRoot = rig.transform;
            }
        }

        if (xrHeadCamera == null && xrOriginRoot != null)
        {
            Camera[] cameras = xrOriginRoot.GetComponentsInChildren<Camera>(true);

            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null)
                {
                    xrHeadCamera = cameras[i].transform;
                    break;
                }
            }
        }
    }

    void RefreshControllers()
    {
        if (!leftController.isValid)
        {
            leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        }

        if (!rightController.isValid)
        {
            rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }
    }

    void HandleLeftGripToggle()
    {
        if (!toggleWithLeftGripButton)
        {
            return;
        }

        // Meta Quest left controller grip/squeeze button.
        bool gripPressed = ReadButton(
            XRNode.LeftHand,
            InputDeviceCharacteristics.Left,
            CommonUsages.gripButton,
            ref leftController
        );

        if (gripPressed && !wasLeftGripPressed && Time.unscaledTime >= nextToggleTime)
        {
            SetShipMode(!isShipMode, false);
            nextToggleTime = Time.unscaledTime + toggleCooldown;
        }

        wasLeftGripPressed = gripPressed;
    }

    void SetShipMode(bool enableShipMode, bool force)
    {
        if (!force && isShipMode == enableShipMode)
        {
            return;
        }

        isShipMode = enableShipMode;
        currentLocalVelocity = Vector3.zero;
        currentYawRate = 0f;
        shipIsTurningFromRightStick = false;
        LevelShipAttitude();

        if (isShipMode)
        {
            // Directly pause normal astronaut movement during ship mode.
            // The component is restored automatically after LEFT GRIP exit.
            PauseQuestJoystickMoveForShipMode();
            SetPilotVisuals(false);
            PlaceCameraAtThirdPersonPointOnce();

            if (showDebugLogs)
            {
                Debug.Log("Ship Mode ON: ship can fly only inside the intro area.");
            }
        }
        else
        {
            if (shipExitPoint != null)
            {
                PlacePlayerCameraAt(shipExitPoint);
            }

            SetPilotVisuals(true);
            RestoreQuestJoystickMoveAfterShipMode();
            ProceduralSolarSystemAudio.Ensure().SetShipThruster(false, 0f);

            if (showDebugLogs)
            {
                Debug.Log("Astronaut Mode ON: normal player movement restored. Ship stays in the intro area.");
            }
        }
    }

    void UpdateShipFlight()
    {
        Vector2 leftStick = ReadStick(XRNode.LeftHand, InputDeviceCharacteristics.Left, ref leftController);
        Vector2 rightStick = ReadStick(XRNode.RightHand, InputDeviceCharacteristics.Right, ref rightController);

        float forwardInput = useLeftThumbstickForFlight && Mathf.Abs(leftStick.y) >= deadzone ? leftStick.y : 0f;
        float strafeInput = useLeftThumbstickForFlight && Mathf.Abs(leftStick.x) >= deadzone ? leftStick.x : 0f;
        float turnInput = useRightThumbstickForTurning && Mathf.Abs(rightStick.x) >= deadzone ? rightStick.x : 0f;

        bool rightBPressed = ReadButton(
            XRNode.RightHand,
            InputDeviceCharacteristics.Right,
            CommonUsages.secondaryButton,
            ref rightController
        );

        bool rightAPressed = ReadButton(
            XRNode.RightHand,
            InputDeviceCharacteristics.Right,
            CommonUsages.primaryButton,
            ref rightController
        );

        // Keyboard fallbacks are useful while testing in Unity Editor.
        bool keyboardUp = keyboardWAndAMoveVertical && Input.GetKey(KeyCode.W);
        bool keyboardDown = keyboardWAndAMoveVertical && Input.GetKey(KeyCode.A);

        float verticalInput = 0f;

        if ((useBUpADown ? rightBPressed : rightBMovesUp && rightBPressed) || keyboardUp)
        {
            verticalInput += 1f;
        }

        if ((useBUpADown ? rightAPressed : rightAMovesDown && rightAPressed) || keyboardDown)
        {
            verticalInput -= 1f;
        }

        Vector3 targetLocalVelocity = new Vector3(
            strafeInput * shipMoveSpeed,
            verticalInput * verticalMoveSpeed,
            forwardInput * shipMoveSpeed
        );

        float changeRate = targetLocalVelocity.sqrMagnitude > currentLocalVelocity.sqrMagnitude
            ? movementAcceleration
            : movementDeceleration;

        currentLocalVelocity = Vector3.MoveTowards(
            currentLocalVelocity,
            targetLocalVelocity,
            changeRate * Time.unscaledDeltaTime
        );

        Quaternion yawOnlyRotation = GetYawOnlyRotation();
        transform.position += (yawOnlyRotation * currentLocalVelocity) * Time.unscaledDeltaTime;

        if (limitFlightToIntroArea)
        {
            KeepShipInsideIntroArea();
        }

        BlockShipFromPlanets();

        float targetYawRate = turnInput * shipTurnSpeed;
        float yawChangeRate = Mathf.Abs(targetYawRate) > Mathf.Abs(currentYawRate)
            ? turnAcceleration
            : turnDeceleration;

        currentYawRate = Mathf.MoveTowards(
            currentYawRate,
            targetYawRate,
            yawChangeRate * Time.unscaledDeltaTime
        );

        float yawThisFrame = currentYawRate * Time.unscaledDeltaTime;
        shipIsTurningFromRightStick = Mathf.Abs(currentYawRate) > 0.00001f;

        if (Mathf.Abs(yawThisFrame) > 0.00001f)
        {
            // Turn only the spacecraft here.
            // Camera yaw is synchronised once in LateUpdate from the exact
            // ThirdPersonCameraPoint pose after this ship turn has happened.
            transform.Rotate(0f, yawThisFrame, 0f, Space.World);
        }

        UpdateShipAttitude(turnInput, verticalInput);

        float moveIntensity = currentLocalVelocity.magnitude / Mathf.Max(0.01f, shipMoveSpeed);
        float turnIntensity = Mathf.Abs(currentYawRate) / Mathf.Max(0.01f, shipTurnSpeed);
        ProceduralSolarSystemAudio.Ensure().SetShipThruster(
            moveIntensity > 0.02f || turnIntensity > 0.02f,
            Mathf.Max(moveIntensity, turnIntensity)
        );
    }

    void BlockShipFromPlanets()
    {
        if (!blockSpaceshipFromPlanets)
        {
            return;
        }

        Bounds shipBounds;

        if (!TryGetRendererBounds(transform, out shipBounds))
        {
            return;
        }

        float shipRadius = Mathf.Max(shipBounds.extents.x, shipBounds.extents.y, shipBounds.extents.z);
        RefreshPlanetBlockTargets();

        for (int i = 0; i < planetBlockTargets.Count; i++)
        {
            Transform candidate = planetBlockTargets[i];

            if (candidate == null || candidate == transform || candidate.IsChildOf(transform))
            {
                continue;
            }

            if (!LooksLikePlanet(candidate.name))
            {
                continue;
            }

            Bounds planetBounds;

            if (!TryGetRendererBounds(candidate, out planetBounds))
            {
                continue;
            }

            float planetRadius = Mathf.Max(planetBounds.extents.x, planetBounds.extents.y, planetBounds.extents.z);
            float minimumDistance = shipRadius + planetRadius + planetBlockPadding;
            Vector3 fromPlanet = transform.position - planetBounds.center;
            float distance = fromPlanet.magnitude;

            if (distance >= minimumDistance)
            {
                continue;
            }

            Vector3 pushDirection = distance > 0.001f ? fromPlanet.normalized : -transform.forward;
            transform.position = planetBounds.center + pushDirection * minimumDistance;
            currentLocalVelocity = Vector3.zero;

            if (showDebugLogs)
            {
                Debug.Log("Spaceship blocked by planet: " + candidate.name);
            }
        }
    }

    void RefreshPlanetBlockTargets()
    {
        if (Time.unscaledTime < nextPlanetBlockScanTime)
        {
            return;
        }

        nextPlanetBlockScanTime = Time.unscaledTime + 1f;
        planetBlockTargets.Clear();

        Transform[] transforms = FindObjectsOfType<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate == null || candidate == transform || candidate.IsChildOf(transform))
            {
                continue;
            }

            if (LooksLikePlanet(candidate.name))
            {
                planetBlockTargets.Add(candidate);
            }
        }
    }

    bool LooksLikePlanet(string objectName)
    {
        string n = objectName.ToLower();

        if (n.Contains("asteroid") || n.Contains("moon") || n.Contains("ring") || n.Contains("orbit"))
        {
            return false;
        }

        return n.Contains("mercury") ||
               n.Contains("venus") ||
               n.Contains("earth") ||
               n.Contains("mars") ||
               n.Contains("jupiter") ||
               n.Contains("saturn") ||
               n.Contains("uranus") ||
               n.Contains("neptune") ||
               n.Contains("sun");
    }

    void UpdateShipAttitude(float turnInput, float verticalInput)
    {
        if (!enableTurnBanking && !enableVerticalPitch)
        {
            LevelShipAttitude();
            return;
        }

        float bankDirection = invertBankDirection ? 1f : -1f;
        float targetBankAngle = enableTurnBanking ? turnInput * maxBankAngle * bankDirection : 0f;
        float bankBlend = 1f - Mathf.Exp(-bankSmoothSpeed * Time.unscaledDeltaTime);
        currentBankAngle = Mathf.Lerp(currentBankAngle, targetBankAngle, bankBlend);

        float pitchDirection = invertPitchDirection ? 1f : -1f;
        float targetPitchAngle = enableVerticalPitch ? verticalInput * maxPitchAngle * pitchDirection : 0f;
        float pitchBlend = 1f - Mathf.Exp(-pitchSmoothSpeed * Time.unscaledDeltaTime);
        currentPitchAngle = Mathf.Lerp(currentPitchAngle, targetPitchAngle, pitchBlend);

        ApplyShipAttitude();
    }

    void LevelShipAttitude()
    {
        currentBankAngle = 0f;
        currentPitchAngle = 0f;
        ApplyShipAttitude();
    }

    void ApplyShipAttitude()
    {
        Quaternion yawOnlyRotation = GetYawOnlyRotation();
        Quaternion pitch = Quaternion.AngleAxis(currentPitchAngle, Vector3.right);
        Quaternion bank = Quaternion.AngleAxis(currentBankAngle, Vector3.forward);
        transform.rotation = yawOnlyRotation * pitch * bank;
    }

    Quaternion GetYawOnlyRotation()
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

        if (flatForward.sqrMagnitude < 0.0001f)
        {
            return Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        return Quaternion.LookRotation(flatForward.normalized, Vector3.up);
    }

    void KeepShipInsideIntroArea()
    {
        Vector3 offset = transform.position - introFlightCentre;
        Vector3 horizontalOffset = new Vector3(offset.x, 0f, offset.z);

        if (horizontalOffset.magnitude > introFlightRadius)
        {
            horizontalOffset = horizontalOffset.normalized * introFlightRadius;
        }

        float clampedY = Mathf.Clamp(
            transform.position.y,
            introFlightCentre.y - introVerticalRange,
            introFlightCentre.y + introVerticalRange
        );

        transform.position = new Vector3(
            introFlightCentre.x + horizontalOffset.x,
            clampedY,
            introFlightCentre.z + horizontalOffset.z
        );
    }

    void PauseQuestJoystickMoveForShipMode()
    {
        if (questJoystickMove == null && xrOriginRoot != null)
        {
            questJoystickMove = xrOriginRoot.GetComponent<QuestJoystickMove>();
        }

        if (questJoystickMove == null)
        {
            questJoystickMove = FindObjectOfType<QuestJoystickMove>();
        }

        if (questJoystickMove == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("Spaceship controller could not find QuestJoystickMove. Ship turning still works.");
            }

            return;
        }

        questJoystickWasEnabledBeforeShipMode = questJoystickMove.enabled;
        questJoystickMove.enabled = false;
    }

    void RestoreQuestJoystickMoveAfterShipMode()
    {
        if (questJoystickMove != null && questJoystickWasEnabledBeforeShipMode)
        {
            questJoystickMove.enabled = true;
        }
    }

    void SyncCameraYawExactlyToThirdPersonPoint()
    {
        if (thirdPersonCameraPoint == null || xrOriginRoot == null || xrHeadCamera == null)
        {
            return;
        }

        Vector3 desiredForward = Vector3.ProjectOnPlane(thirdPersonCameraPoint.forward, Vector3.up);
        Vector3 currentForward = Vector3.ProjectOnPlane(xrHeadCamera.forward, Vector3.up);

        if (desiredForward.sqrMagnitude < 0.0001f || currentForward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // This is not a speed calculation. It places the camera at the same
        // yaw as the ship's own third-person camera point after each ship turn.
        float yawCorrection = Vector3.SignedAngle(currentForward, desiredForward, Vector3.up);
        xrOriginRoot.RotateAround(xrHeadCamera.position, Vector3.up, yawCorrection);
    }

    void SetPilotVisuals(bool visible)
    {
        if (hideVisualsWhilePiloting == null)
        {
            SetAutoHiddenPilotRenderers(visible);
            return;
        }

        for (int i = 0; i < hideVisualsWhilePiloting.Length; i++)
        {
            if (hideVisualsWhilePiloting[i] != null)
            {
                hideVisualsWhilePiloting[i].SetActive(visible);
            }
        }

        SetAutoHiddenPilotRenderers(visible);
    }

    void SetAutoHiddenPilotRenderers(bool visible)
    {
        if (!autoHidePlayerRenderersWhilePiloting || xrOriginRoot == null)
        {
            return;
        }

        if (!visible)
        {
            CachePilotRenderersToHide();

            for (int i = 0; i < autoHiddenPilotRenderers.Count; i++)
            {
                if (autoHiddenPilotRenderers[i] != null)
                {
                    autoHiddenPilotRenderers[i].enabled = false;
                }
            }

            return;
        }

        for (int i = 0; i < autoHiddenPilotRenderers.Count; i++)
        {
            if (autoHiddenPilotRenderers[i] != null && i < autoHiddenPilotRendererStates.Count)
            {
                autoHiddenPilotRenderers[i].enabled = autoHiddenPilotRendererStates[i];
            }
        }

        autoHiddenPilotRenderers.Clear();
        autoHiddenPilotRendererStates.Clear();
    }

    void EnsureSpaceshipSolid()
    {
        if (!makeSpaceshipSolid)
        {
            return;
        }

        Bounds bounds;

        if (!TryGetRendererBounds(transform, out bounds))
        {
            return;
        }

        if (shipSolidCollider == null)
        {
            shipSolidCollider = GetComponent<BoxCollider>();

            if (shipSolidCollider == null)
            {
                shipSolidCollider = gameObject.AddComponent<BoxCollider>();
            }
        }

        shipSolidCollider.isTrigger = false;
        shipSolidCollider.center = transform.InverseTransformPoint(bounds.center);
        shipSolidCollider.size = WorldSizeToLocalSize(bounds.size + Vector3.one * shipPushPadding);

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void UpdatePlayerShipCollision()
    {
        if (!makeSpaceshipSolid || isShipMode || xrOriginRoot == null || xrHeadCamera == null)
        {
            return;
        }

        Bounds shipBounds;

        if (!TryGetRendererBounds(transform, out shipBounds))
        {
            return;
        }

        shipBounds.Expand(shipPushPadding);
        Vector3 head = xrHeadCamera.position;

        if (!shipBounds.Contains(head))
        {
            return;
        }

        Vector3 closest = shipBounds.ClosestPoint(head);
        Vector3 push = head - closest;

        if (push.sqrMagnitude < 0.0001f)
        {
            Vector3 flat = head - shipBounds.center;
            flat.y = 0f;
            push = flat.sqrMagnitude > 0.0001f ? flat.normalized : transform.right;
        }

        push.y = 0f;

        if (push.sqrMagnitude < 0.0001f)
        {
            return;
        }

        xrOriginRoot.position += push.normalized * shipPushStrength * Time.unscaledDeltaTime;
    }

    void EnsureHandAndGunSolid()
    {
        if (!makeHandsAndGunSolid || xrOriginRoot == null)
        {
            return;
        }

        Renderer[] renderers = xrOriginRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !LooksLikePilotSolid(renderer.transform))
            {
                continue;
            }

            AddColliderToVisual(renderer);
        }
    }

    bool LooksLikePilotSolid(Transform item)
    {
        Transform current = item;

        while (current != null && current != xrOriginRoot)
        {
            string n = current.name.ToLower();

            if (n.Contains("hologram") || n.Contains("laser") || n.Contains("beam") || n.Contains("tablet"))
            {
                return false;
            }

            if (n.Contains("hand") ||
                n.Contains("glove") ||
                n.Contains("gun") ||
                n.Contains("controller"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    void AddColliderToVisual(Renderer renderer)
    {
        if (renderer.GetComponent<Collider>() != null)
        {
            return;
        }

        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();

        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            MeshCollider meshCollider = renderer.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = true;
            meshCollider.isTrigger = false;
        }
        else
        {
            BoxCollider box = renderer.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.center = Vector3.zero;
            box.size = Vector3.one * 0.12f;
        }

        Rigidbody rb = renderer.GetComponentInParent<Rigidbody>();

        if (rb == null)
        {
            rb = renderer.gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        bounds = new Bounds(root.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    Vector3 WorldSizeToLocalSize(Vector3 worldSize)
    {
        Vector3 scale = transform.lossyScale;

        return new Vector3(
            worldSize.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            worldSize.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            worldSize.z / Mathf.Max(0.001f, Mathf.Abs(scale.z))
        );
    }

    void CachePilotRenderersToHide()
    {
        autoHiddenPilotRenderers.Clear();
        autoHiddenPilotRendererStates.Clear();

        Renderer[] renderers = xrOriginRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            if (ShouldHidePilotRenderer(renderer.transform))
            {
                autoHiddenPilotRenderers.Add(renderer);
                autoHiddenPilotRendererStates.Add(renderer.enabled);
            }
        }
    }

    bool ShouldHidePilotRenderer(Transform rendererTransform)
    {
        Transform current = rendererTransform;

        while (current != null && current != xrOriginRoot)
        {
            string n = current.name.ToLower();

            if (n.Contains("hand") ||
                n.Contains("gun") ||
                n.Contains("muzzle") ||
                n.Contains("laser") ||
                n.Contains("glove") ||
                n.Contains("controller"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    void FollowThirdPersonCameraPositionOnly()
    {
        if (thirdPersonCameraPoint == null || xrOriginRoot == null || xrHeadCamera == null)
        {
            return;
        }

        Vector3 delta = thirdPersonCameraPoint.position - xrHeadCamera.position;
        xrOriginRoot.position += delta;
    }

    void PlaceCameraAtThirdPersonPointOnce()
    {
        if (thirdPersonCameraPoint == null || xrOriginRoot == null || xrHeadCamera == null)
        {
            return;
        }

        Vector3 delta = thirdPersonCameraPoint.position - xrHeadCamera.position;
        xrOriginRoot.position += delta;

        Vector3 desiredForward = Vector3.ProjectOnPlane(thirdPersonCameraPoint.forward, Vector3.up);
        Vector3 currentForward = Vector3.ProjectOnPlane(xrHeadCamera.forward, Vector3.up);

        if (desiredForward.sqrMagnitude > 0.0001f && currentForward.sqrMagnitude > 0.0001f)
        {
            float yawDelta = Vector3.SignedAngle(currentForward, desiredForward, Vector3.up);
            xrOriginRoot.RotateAround(xrHeadCamera.position, Vector3.up, yawDelta);
        }
    }

    void PlacePlayerCameraAt(Transform target)
    {
        if (xrOriginRoot == null || xrHeadCamera == null || target == null)
        {
            return;
        }

        Vector3 desiredForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
        Vector3 currentForward = Vector3.ProjectOnPlane(xrHeadCamera.forward, Vector3.up);

        if (desiredForward.sqrMagnitude > 0.0001f && currentForward.sqrMagnitude > 0.0001f)
        {
            float yawDelta = Vector3.SignedAngle(currentForward, desiredForward, Vector3.up);
            xrOriginRoot.RotateAround(xrHeadCamera.position, Vector3.up, yawDelta);
        }

        Vector3 delta = target.position - xrHeadCamera.position;
        xrOriginRoot.position += delta;
    }

    Vector2 ReadStick(XRNode node, InputDeviceCharacteristics hand, ref InputDevice cachedDevice)
    {
        Vector2 value = Vector2.zero;

        if (cachedDevice.isValid &&
            cachedDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out value))
        {
            return value;
        }

        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | hand,
            devices
        );

        for (int i = 0; i < devices.Count; i++)
        {
            if (devices[i].TryGetFeatureValue(CommonUsages.primary2DAxis, out value))
            {
                cachedDevice = devices[i];
                return value;
            }
        }

        return Vector2.zero;
    }

    bool ReadButton(
        XRNode node,
        InputDeviceCharacteristics hand,
        InputFeatureUsage<bool> button,
        ref InputDevice cachedDevice
    )
    {
        bool pressed = false;

        if (cachedDevice.isValid &&
            cachedDevice.TryGetFeatureValue(button, out pressed))
        {
            return pressed;
        }

        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | hand,
            devices
        );

        for (int i = 0; i < devices.Count; i++)
        {
            if (devices[i].TryGetFeatureValue(button, out pressed))
            {
                cachedDevice = devices[i];
                return pressed;
            }
        }

        return false;
    }

    public bool BeginShipWormholeTravel(Transform landingPoint)
    {
        if (!isShipMode || landingPoint == null)
        {
            return false;
        }

        currentLocalVelocity = Vector3.zero;
        currentYawRate = 0f;
        shipIsTurningFromRightStick = false;
        LevelShipAttitude();
        shipWormholeTravelActive = true;
        shipWormholeStartPosition = transform.position;
        shipWormholeStartRotation = transform.rotation;
        shipWormholeLandingPoint = landingPoint;

        if (showDebugLogs)
        {
            Debug.Log("Ship wormhole travel started. Ship and player will travel together.");
        }

        return true;
    }

    public void SetShipWormholeProgress(float normalizedProgress)
    {
        if (!shipWormholeTravelActive || shipWormholeLandingPoint == null)
        {
            return;
        }

        float t = Mathf.Clamp01(normalizedProgress);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        transform.position = Vector3.Lerp(
            shipWormholeStartPosition,
            shipWormholeLandingPoint.position,
            smoothT
        );

        transform.rotation = Quaternion.Slerp(
            shipWormholeStartRotation,
            shipWormholeLandingPoint.rotation,
            smoothT
        );
    }

    public void CompleteShipWormholeTravel()
    {
        if (shipWormholeLandingPoint != null)
        {
            transform.SetPositionAndRotation(
                shipWormholeLandingPoint.position,
                shipWormholeLandingPoint.rotation
            );
        }

        introFlightCentre = transform.position;
        shipWormholeTravelActive = false;
        shipWormholeLandingPoint = null;
    }

    public void CancelShipWormholeTravel()
    {
        if (shipWormholeTravelActive)
        {
            transform.SetPositionAndRotation(shipWormholeStartPosition, shipWormholeStartRotation);
        }

        shipWormholeTravelActive = false;
        shipWormholeLandingPoint = null;
    }
}
