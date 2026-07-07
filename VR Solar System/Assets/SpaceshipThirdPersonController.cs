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

    [Header("Ship Controls")]
    [Tooltip("LEFT thumbstick: forward/back + sideways movement.")]
    public bool useLeftThumbstickForFlight = true;

    [Tooltip("RIGHT thumbstick left/right: turn spacecraft.")]
    public bool useRightThumbstickForTurning = true;

    [Tooltip("RIGHT B button (above A): move spacecraft UP.")]
    public bool rightBMovesUp = true;

    [Tooltip("RIGHT A button: move spacecraft DOWN.")]
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
    private bool shipIsTurningFromRightStick;

    // QuestJoystickMove is handled directly from this controller.
    // No inspector drag-and-drop or extra helper script is required.
    private QuestJoystickMove questJoystickMove;
    private bool questJoystickWasEnabledBeforeShipMode;

    // Initial-area boundary
    private Vector3 introFlightCentre;

    /// <summary>
    /// Kept public because your existing WormholeIntroTravel script may still
    /// check it. The ship will NOT travel through the wormhole in this version.
    /// </summary>
    public bool IsInShipMode => isShipMode;

    void Start()
    {
        ResolveReferences();

        if (spawnShipAtStart && shipSpawnPoint != null)
        {
            transform.SetPositionAndRotation(shipSpawnPoint.position, shipSpawnPoint.rotation);
        }

        introFlightCentre = transform.position;

        SetShipMode(startInShipMode, true);
    }

    void Update()
    {
        ResolveReferences();
        RefreshControllers();
        HandleLeftGripToggle();

        if (isShipMode)
        {
            UpdateShipFlight();
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

        bool rightBPressed = rightBMovesUp && ReadButton(
            XRNode.RightHand,
            InputDeviceCharacteristics.Right,
            CommonUsages.secondaryButton,
            ref rightController
        );

        bool rightAPressed = rightAMovesDown && ReadButton(
            XRNode.RightHand,
            InputDeviceCharacteristics.Right,
            CommonUsages.primaryButton,
            ref rightController
        );

        // Keyboard fallbacks are useful while testing in Unity Editor.
        bool keyboardUp = keyboardWAndAMoveVertical && Input.GetKey(KeyCode.W);
        bool keyboardDown = keyboardWAndAMoveVertical && Input.GetKey(KeyCode.A);

        float verticalInput = 0f;

        if (rightBPressed || keyboardUp)
        {
            verticalInput += 1f;
        }

        if (rightAPressed || keyboardDown)
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

        transform.position += transform.TransformDirection(currentLocalVelocity) * Time.unscaledDeltaTime;

        if (limitFlightToIntroArea)
        {
            KeepShipInsideIntroArea();
        }

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
            return;
        }

        for (int i = 0; i < hideVisualsWhilePiloting.Length; i++)
        {
            if (hideVisualsWhilePiloting[i] != null)
            {
                hideVisualsWhilePiloting[i].SetActive(visible);
            }
        }
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

    // ------------------------------------------------------------------------
    // Compatibility methods: your existing WormholeIntroTravel script may
    // still call these because an earlier version supported ship wormhole
    // travel. In THIS simplified version they intentionally do nothing.
    // ------------------------------------------------------------------------

    public bool BeginShipWormholeTravel(Transform ignoredShipLandingPoint)
    {
        if (showDebugLogs)
        {
            Debug.Log("Ship wormhole travel is disabled. Exit the ship with LEFT GRIP, then use Menu for player-only wormhole travel.");
        }

        return false;
    }

    public void SetShipWormholeProgress(float normalizedProgress)
    {
        // Intentionally empty: ship stays in the intro area.
    }

    public void CompleteShipWormholeTravel()
    {
        // Intentionally empty: ship stays in the intro area.
    }

    public void CancelShipWormholeTravel()
    {
        // Intentionally empty: ship stays in the intro area.
    }
}