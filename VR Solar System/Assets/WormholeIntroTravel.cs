using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// WormholeIntroTravel
/// ALWAYS VISIBLE MULTICOLOR VERSION
///
/// Fix:
/// The wormhole is now attached directly to the VR Main Camera.
/// So even when XR Origin moves from IntroStartPoint to SolarSystemLandingPoint,
/// the rings stay in front of your eyes for the whole journey.
/// </summary>
public class WormholeIntroTravel : MonoBehaviour
{
    [Header("MUST ASSIGN")]
    public Transform playerRigToMove;       // XR Origin (VR)
    public Transform cameraTransform;       // Main Camera
    public Transform leftHandController;    // LeftHand Controller
    public Transform gunMuzzle;             // GunMuzzle

    [Header("START AND LANDING POINTS")]
    public Transform introStartPoint;
    public Transform landingPoint;

    public bool moveToIntroStartOnPlay = true;
    public bool forceRigExactlyToLandingPoint = true;
    public bool moveRigDuringWormhole = true;

    [Header("TRIGGER")]
    [Tooltip("ON = left controller three-line/Menu button starts the wormhole.")]
    public bool triggerWithLeftMenuButton = true;
    public bool triggerWithGunClickOnGloveButton = true;
    public float triggerThreshold = 0.45f;

    [Header("INTRO FREE MOVEMENT")]
    [Tooltip("ON = before pressing the wormhole button, you can freely move around the intro space.")]
    public bool allowIntroMovement = true;

    [Tooltip("ON = after reaching the solar system, this intro movement stops so it will not fight your other movement scripts.")]
    public bool stopIntroMovementAfterLanding = true;

    [Tooltip("Left thumbstick forward/back/left/right speed before wormhole.")]
    public float introMoveSpeed = 4.0f;

    [Tooltip("Right thumbstick left/right turning speed before wormhole.")]
    public float introTurnSpeed = 65.0f;

    [Tooltip("Right A = down, Right B = up speed before wormhole.")]
    public float introVerticalSpeed = 3.0f;

    public float introJoystickDeadzone = 0.15f;

    [Header("WORMHOLE TIME")]
    public float wormholeDuration = 3f;

    [Header("LEFT GLOVE CLICK BUTTON")]
    public Vector3 gloveButtonLocalPosition = new Vector3(0f, 0.075f, 0.055f);
    public Vector3 gloveButtonLocalRotation = new Vector3(25f, 0f, 0f);
    public Vector3 gloveButtonLocalScale = new Vector3(0.08f, 0.03f, 0.014f);

    [Header("ALWAYS VISIBLE MULTICOLOR WORMHOLE")]
    public int ringCount = 42;
    public int ringSegments = 96;
    public float ringStartDistance = 0.55f;
    public float ringEndDistance = 7.5f;
    public float ringRadius = 0.62f;
    public float ringWidth = 0.035f;
    public float ringTravelSpeed = 1.8f;
    public float ringSpinSpeed = 260f;
    public float ringPulseAmount = 0.18f;
    public float colorCycleSpeed = 0.45f;

    [Header("SPACE RIDE CAMERA MOTION")]
    public Transform cameraMotionRoot;      // usually Camera Offset
    public bool useCameraBob = true;
    public float bobAmount = 0.055f;
    public float bobSpeed = 2.4f;
    public float swayAmount = 0.025f;
    public float swaySpeed = 1.7f;

    [Header("DEBUG")]
    public bool showDebugLogs = true;

    private GameObject gloveButton;
    private GameObject wormholeRoot;

    private readonly List<LineRenderer> rings = new List<LineRenderer>();
    private readonly List<float> ringOffsets = new List<float>();
    private readonly List<Material> ringMaterials = new List<Material>();

    private bool travelling = false;
    private bool introMovementActive = true;
    private bool wasXPressed = false;
    private bool wasTriggerPressed = false;

    private float travelTimer = 0f;
    private Vector3 travelStartPosition;
    private Quaternion travelStartRotation;
    private Vector3 motionRootStartLocalPosition;

    void Start()
    {
        if (leftHandController == null)
        {
            leftHandController = transform;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraMotionRoot == null && cameraTransform != null && cameraTransform.parent != null)
        {
            cameraMotionRoot = cameraTransform.parent; // Camera Offset
        }

        if (cameraMotionRoot != null)
        {
            motionRootStartLocalPosition = cameraMotionRoot.localPosition;
        }

        if (playerRigToMove == null)
        {
            playerRigToMove = FindXROrigin();
        }

        CreateGloveButton();
        CreateWormholeLockedToCamera();
        SetWormholeVisible(false);

        if (moveToIntroStartOnPlay)
        {
            ForceMoveRigToIntroStart();
        }
    }

    void LateUpdate()
    {
        // LateUpdate keeps visuals after XR camera tracking updates.
        // This avoids the tunnel disappearing while the rig moves.
        if (travelling)
        {
            KeepWormholeLockedToCamera();
            UpdateWormholeVisuals();
            UpdateCameraBob();
        }
    }

    void Update()
    {
        if (!travelling)
        {
            UpdateIntroMovement();
            CheckTriggers();
            return;
        }

        UpdateTravelMovement();
    }

    void CheckTriggers()
    {
        if (triggerWithLeftMenuButton)
        {
            bool x = IsLeftMenuButtonPressed();

            if (x && !wasXPressed)
            {
                StartWormhole();
            }

            wasXPressed = x;
        }

        if (triggerWithGunClickOnGloveButton)
        {
            bool trigger = IsRightTriggerPressed();

            if (trigger && !wasTriggerPressed)
            {
                TryClickGloveButton();
            }

            wasTriggerPressed = trigger;
        }
    }

    public void StartWormhole()
    {
        if (travelling)
        {
            return;
        }

        if (playerRigToMove == null)
        {
            Debug.LogError("WormholeIntroTravel: Player Rig To Move missing. Drag XR Origin (VR).");
            return;
        }

        if (landingPoint == null)
        {
            Debug.LogError("WormholeIntroTravel: Landing Point missing. Drag SolarSystemLandingPoint.");
            return;
        }

        travelling = true;
        travelTimer = 0f;

        travelStartPosition = playerRigToMove.position;
        travelStartRotation = playerRigToMove.rotation;

        if (cameraMotionRoot != null)
        {
            motionRootStartLocalPosition = cameraMotionRoot.localPosition;
        }

        CreateWormholeLockedToCamera();
        SetWormholeVisible(true);
        SetGloveButtonVisible(false);

        if (showDebugLogs)
        {
            Debug.Log("WORMHOLE START. From: " + travelStartPosition + " To: " + landingPoint.position);
        }
    }

    void UpdateTravelMovement()
    {
        travelTimer += Time.unscaledDeltaTime;

        float t = Mathf.Clamp01(travelTimer / Mathf.Max(0.01f, wormholeDuration));
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        if (moveRigDuringWormhole && playerRigToMove != null && landingPoint != null)
        {
            ForceSetRigPosition(Vector3.Lerp(travelStartPosition, landingPoint.position, smoothT));
            playerRigToMove.rotation = Quaternion.Slerp(travelStartRotation, landingPoint.rotation, smoothT);
        }

        if (t >= 1f)
        {
            EndWormhole();
        }
    }

    void EndWormhole()
    {
        if (forceRigExactlyToLandingPoint && playerRigToMove != null && landingPoint != null)
        {
            ForceSetRigPosition(landingPoint.position);
            playerRigToMove.rotation = landingPoint.rotation;
        }

        if (cameraMotionRoot != null)
        {
            cameraMotionRoot.localPosition = motionRootStartLocalPosition;
        }

        SetWormholeVisible(false);
        SetGloveButtonVisible(true);

        if (stopIntroMovementAfterLanding)
        {
            introMovementActive = false;
        }

        travelling = false;

        if (showDebugLogs)
        {
            Debug.Log("WORMHOLE END. Rig final: " + playerRigToMove.position);
        }
    }

    void ForceMoveRigToIntroStart()
    {
        if (playerRigToMove == null || introStartPoint == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("WormholeIntroTravel: IntroStartPoint or PlayerRig missing. Cannot move to intro start.");
            }

            return;
        }

        ForceSetRigPosition(introStartPoint.position);
        playerRigToMove.rotation = introStartPoint.rotation;

        if (showDebugLogs)
        {
            Debug.Log("XR Origin moved to IntroStartPoint: " + introStartPoint.position);
        }
    }

    void ForceSetRigPosition(Vector3 position)
    {
        CharacterController cc = playerRigToMove.GetComponent<CharacterController>();
        bool ccWasEnabled = false;

        if (cc != null)
        {
            ccWasEnabled = cc.enabled;
            cc.enabled = false;
        }

        playerRigToMove.position = position;

        if (cc != null)
        {
            cc.enabled = ccWasEnabled;
        }
    }

    void CreateGloveButton()
    {
        if (leftHandController == null)
        {
            return;
        }

        if (gloveButton != null)
        {
            Destroy(gloveButton);
        }

        gloveButton = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gloveButton.name = "WormholeButton_LeftGlove";
        gloveButton.transform.SetParent(leftHandController, false);
        gloveButton.transform.localPosition = gloveButtonLocalPosition;
        gloveButton.transform.localRotation = Quaternion.Euler(gloveButtonLocalRotation);
        gloveButton.transform.localScale = gloveButtonLocalScale;

        Renderer r = gloveButton.GetComponent<Renderer>();

        if (r != null)
        {
            r.material = CreateMaterial(new Color(0f, 0.95f, 1f, 1f));
        }
    }

    void SetGloveButtonVisible(bool visible)
    {
        if (gloveButton != null)
        {
            gloveButton.SetActive(visible);
        }
    }

    void TryClickGloveButton()
    {
        if (gunMuzzle == null || gloveButton == null)
        {
            return;
        }

        RaycastHit hit;

        if (Physics.Raycast(gunMuzzle.position, gunMuzzle.forward, out hit, 200f, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider != null && hit.collider.gameObject == gloveButton)
            {
                StartWormhole();
            }
        }
    }

    void CreateWormholeLockedToCamera()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            return;
        }

        if (wormholeRoot != null)
        {
            Destroy(wormholeRoot);
        }

        rings.Clear();
        ringOffsets.Clear();
        ringMaterials.Clear();

        wormholeRoot = new GameObject("Always Visible Multicolor Wormhole");
        wormholeRoot.transform.SetParent(cameraTransform, false);
        wormholeRoot.transform.localPosition = Vector3.zero;
        wormholeRoot.transform.localRotation = Quaternion.identity;
        wormholeRoot.transform.localScale = Vector3.one;

        for (int i = 0; i < ringCount; i++)
        {
            GameObject ringObject = new GameObject("Wormhole_Ring_" + i);
            ringObject.transform.SetParent(wormholeRoot.transform, false);
            ringObject.transform.localPosition = Vector3.zero;
            ringObject.transform.localRotation = Quaternion.identity;
            ringObject.transform.localScale = Vector3.one;

            LineRenderer lr = ringObject.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = ringSegments;
            lr.widthMultiplier = ringWidth;
            lr.numCornerVertices = 4;
            lr.numCapVertices = 4;
            lr.alignment = LineAlignment.TransformZ;

            Material mat = CreateMaterial(Color.HSVToRGB((float)i / Mathf.Max(1, ringCount), 0.9f, 1f));
            lr.material = mat;

            Vector3[] points = new Vector3[ringSegments];

            for (int s = 0; s < ringSegments; s++)
            {
                float a = ((float)s / ringSegments) * Mathf.PI * 2f;
                points[s] = new Vector3(Mathf.Cos(a) * ringRadius, Mathf.Sin(a) * ringRadius, 0f);
            }

            lr.SetPositions(points);

            rings.Add(lr);
            ringMaterials.Add(mat);
            ringOffsets.Add((float)i / Mathf.Max(1, ringCount));
        }
    }

    void KeepWormholeLockedToCamera()
    {
        if (wormholeRoot == null)
        {
            return;
        }

        if (wormholeRoot.transform.parent != cameraTransform)
        {
            wormholeRoot.transform.SetParent(cameraTransform, false);
        }

        wormholeRoot.transform.localPosition = Vector3.zero;
        wormholeRoot.transform.localRotation = Quaternion.identity;
        wormholeRoot.transform.localScale = Vector3.one;
    }

    void UpdateWormholeVisuals()
    {
        if (wormholeRoot == null)
        {
            return;
        }

        float tunnelLength = Mathf.Max(0.1f, ringEndDistance - ringStartDistance);

        for (int i = 0; i < rings.Count; i++)
        {
            LineRenderer lr = rings[i];

            if (lr == null)
            {
                continue;
            }

            float offset = ringOffsets[i];

            // Important: there are always rings spread from near to far.
            // They never all leave the view at the same time.
            float travel = Time.unscaledTime * ringTravelSpeed * 0.18f;
            float loop = Mathf.Repeat(offset + travel, 1f);

            float z = Mathf.Lerp(ringEndDistance, ringStartDistance, loop);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 8f + i * 0.7f) * ringPulseAmount;
            float radius = ringRadius * Mathf.Lerp(0.65f, 1.35f, 1f - loop) * pulse;

            Transform rt = lr.transform;
            rt.localPosition = new Vector3(0f, 0f, z);
            rt.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * ringSpinSpeed + i * 11f);
            rt.localScale = new Vector3(radius / Mathf.Max(0.01f, ringRadius), radius / Mathf.Max(0.01f, ringRadius), 1f);

            float hue = Mathf.Repeat(Time.unscaledTime * colorCycleSpeed + offset, 1f);
            Color c = Color.HSVToRGB(hue, 0.95f, 1f);
            c.a = 1f;

            lr.startColor = c;
            lr.endColor = c;

            if (i < ringMaterials.Count && ringMaterials[i] != null)
            {
                ringMaterials[i].color = c;
            }

            lr.widthMultiplier = ringWidth * Mathf.Lerp(0.65f, 1.5f, 1f - loop);
            lr.enabled = true;
        }
    }

    void UpdateCameraBob()
    {
        if (!useCameraBob || cameraMotionRoot == null)
        {
            return;
        }

        float bob = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobAmount;
        float sway = Mathf.Sin(Time.unscaledTime * swaySpeed) * swayAmount;

        cameraMotionRoot.localPosition = motionRootStartLocalPosition + new Vector3(sway, bob, 0f);
    }

    void SetWormholeVisible(bool visible)
    {
        if (wormholeRoot != null)
        {
            wormholeRoot.SetActive(visible);
        }
    }

    void UpdateIntroMovement()
    {
        if (!allowIntroMovement || !introMovementActive || travelling)
        {
            return;
        }

        if (playerRigToMove == null)
        {
            return;
        }

        Transform cam = cameraTransform != null ? cameraTransform : Camera.main != null ? Camera.main.transform : null;

        if (cam == null)
        {
            return;
        }

        Vector2 leftStick = GetLeftJoystick();
        Vector2 rightStick = GetRightJoystick();

        Vector3 forward = cam.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = playerRigToMove.forward;
            forward.y = 0f;
        }

        forward.Normalize();

        Vector3 right = cam.right;
        right.y = 0f;

        if (right.sqrMagnitude < 0.001f)
        {
            right = playerRigToMove.right;
            right.y = 0f;
        }

        right.Normalize();

        Vector3 move = Vector3.zero;

        if (Mathf.Abs(leftStick.y) > introJoystickDeadzone)
        {
            move += forward * leftStick.y;
        }

        if (Mathf.Abs(leftStick.x) > introJoystickDeadzone)
        {
            move += right * leftStick.x;
        }

        if (IsRightAButtonHeld())
        {
            move += Vector3.down;
        }

        if (IsRightBButtonHeld())
        {
            move += Vector3.up;
        }

        if (move.sqrMagnitude > 0.001f)
        {
            move = Vector3.ClampMagnitude(move, 1f);
            Vector3 horizontal = new Vector3(move.x, 0f, move.z) * introMoveSpeed;
            Vector3 vertical = new Vector3(0f, move.y, 0f) * introVerticalSpeed;

            playerRigToMove.position += (horizontal + vertical) * Time.unscaledDeltaTime;
        }

        if (Mathf.Abs(rightStick.x) > introJoystickDeadzone)
        {
            float yaw = rightStick.x * introTurnSpeed * Time.unscaledDeltaTime;
            playerRigToMove.Rotate(0f, yaw, 0f, Space.World);
        }
    }

    Vector2 GetLeftJoystick()
    {
        InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (!left.isValid)
        {
            return Vector2.zero;
        }

        Vector2 axis = Vector2.zero;
        left.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);
        return axis;
    }

    Vector2 GetRightJoystick()
    {
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (!right.isValid)
        {
            return Vector2.zero;
        }

        Vector2 axis = Vector2.zero;
        right.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);
        return axis;
    }

    bool IsRightAButtonHeld()
    {
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (!right.isValid)
        {
            return false;
        }

        bool pressed = false;
        right.TryGetFeatureValue(CommonUsages.primaryButton, out pressed);
        return pressed;
    }

    bool IsRightBButtonHeld()
    {
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (!right.isValid)
        {
            return false;
        }

        bool pressed = false;
        right.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed);
        return pressed;
    }

    bool IsLeftMenuButtonPressed()
    {
        InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (!left.isValid)
        {
            return false;
        }

        bool pressed = false;
        left.TryGetFeatureValue(CommonUsages.menuButton, out pressed);

        return pressed;
    }

    bool IsRightTriggerPressed()
    {
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (!right.isValid)
        {
            return false;
        }

        bool button = false;
        float value = 0f;

        right.TryGetFeatureValue(CommonUsages.triggerButton, out button);
        right.TryGetFeatureValue(CommonUsages.trigger, out value);

        return button || value > triggerThreshold;
    }

    Transform FindXROrigin()
    {
        GameObject[] all = FindObjectsOfType<GameObject>();

        foreach (GameObject go in all)
        {
            string n = go.name.ToLower();

            if (n.Contains("xr origin") || n.Contains("xrorigin"))
            {
                return go.transform;
            }
        }

        return null;
    }

    Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    void OnDisable()
    {
        if (cameraMotionRoot != null)
        {
            cameraMotionRoot.localPosition = motionRootStartLocalPosition;
        }

        if (wormholeRoot != null)
        {
            Destroy(wormholeRoot);
        }
    }
}

