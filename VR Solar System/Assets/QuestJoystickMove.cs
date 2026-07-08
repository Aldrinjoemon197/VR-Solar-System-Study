using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Normal astronaut movement.
///
/// While SpaceshipThirdPersonController is in Ship Mode, this component remains
/// enabled in the Inspector but ignores all joystick input. It resumes
/// immediately after the player exits the ship with the left grip button.
/// </summary>
public class QuestJoystickMove : MonoBehaviour
{
    // Used by SpaceshipThirdPersonController_LEFT_GRIP_SAFE_JOYSTICK_LOCK.
    // Keep this exact name: it fixes the CS0117 errors in that controller.
    public static bool IsLockedByShipMode = false;

    public Transform headCamera;

    public float moveSpeed = 12f;
    public float verticalSpeed = 6f;
    public float turnSpeed = 80f;
    public float turnDeadzone = 0.15f;

    [Header("Sun Block")]
    public Transform sun;
    public float sunBlockPadding = 1.2f;

    private InputDevice leftController;
    private InputDevice rightController;
    private SpaceshipThirdPersonController spaceshipController;
    private float nextControllerSearchTime;
    private bool movedThisFrame;
    private float nextSunSearchTime;

    void Start()
    {
        ProceduralSolarSystemAudio.Ensure();
        ResolveSun();
    }

    void Update()
    {
        // Two safeguards:
        // 1. The ship controller sets this lock in Ship Mode.
        // 2. This script also independently checks the ship controller.
        if (IsLockedByShipMode || PlayerIsPilotingShip())
        {
            ProceduralSolarSystemAudio.Ensure().SetPlayerThruster(false, 0f);
            return;
        }

        if (!leftController.isValid)
        {
            leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        }

        if (!rightController.isValid)
        {
            rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }

        movedThisFrame = false;
        MoveWithLeftJoystick();
        TurnWithRightJoystick();
        MoveUpDownWithAandB();
        BlockPlayerFromSun();
        ProceduralSolarSystemAudio.Ensure().SetPlayerThruster(movedThisFrame, 0.75f);
    }

    bool PlayerIsPilotingShip()
    {
        if (spaceshipController == null && Time.unscaledTime >= nextControllerSearchTime)
        {
            nextControllerSearchTime = Time.unscaledTime + 0.5f;

            SpaceshipThirdPersonController[] controllers =
                Resources.FindObjectsOfTypeAll<SpaceshipThirdPersonController>();

            for (int i = 0; i < controllers.Length; i++)
            {
                SpaceshipThirdPersonController candidate = controllers[i];

                if (candidate != null &&
                    candidate.enabled &&
                    candidate.gameObject.activeInHierarchy &&
                    candidate.gameObject.scene.IsValid())
                {
                    spaceshipController = candidate;
                    break;
                }
            }
        }

        return spaceshipController != null && spaceshipController.IsInShipMode;
    }

    void MoveWithLeftJoystick()
    {
        if (headCamera == null)
        {
            return;
        }

        if (!leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick))
        {
            return;
        }

        if (stick.magnitude < 0.15f)
        {
            return;
        }

        Vector3 forward = headCamera.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = headCamera.right;
        right.y = 0f;
        right.Normalize();

        Vector3 movement = forward * stick.y + right * stick.x;
        transform.position += movement * moveSpeed * Time.deltaTime;
        movedThisFrame = true;
    }

    void TurnWithRightJoystick()
    {
        if (headCamera == null)
        {
            return;
        }

        if (!rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick))
        {
            return;
        }

        float turnInput = stick.x;

        if (Mathf.Abs(turnInput) < turnDeadzone)
        {
            return;
        }

        float turnAmount = turnInput * turnSpeed * Time.deltaTime;
        transform.RotateAround(headCamera.position, Vector3.up, turnAmount);
        movedThisFrame = true;
    }

    void MoveUpDownWithAandB()
    {
        bool aPressed = false;
        bool bPressed = false;

        rightController.TryGetFeatureValue(CommonUsages.primaryButton, out aPressed);
        rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bPressed);

        if (aPressed)
        {
            transform.position += Vector3.down * verticalSpeed * Time.deltaTime;
            movedThisFrame = true;
        }

        if (bPressed)
        {
            transform.position += Vector3.up * verticalSpeed * Time.deltaTime;
            movedThisFrame = true;
        }
    }

    void BlockPlayerFromSun()
    {
        ResolveSun();

        if (sun == null || headCamera == null)
        {
            return;
        }

        float sunRadius = GetSunRadius();
        float minimumDistance = sunRadius + sunBlockPadding;
        Vector3 fromSun = headCamera.position - sun.position;
        float distance = fromSun.magnitude;

        if (distance >= minimumDistance)
        {
            return;
        }

        Vector3 pushDirection = distance > 0.001f ? fromSun.normalized : Vector3.back;
        Vector3 targetHeadPosition = sun.position + pushDirection * minimumDistance;
        transform.position += targetHeadPosition - headCamera.position;
    }

    void ResolveSun()
    {
        if (sun != null || Time.unscaledTime < nextSunSearchTime)
        {
            return;
        }

        nextSunSearchTime = Time.unscaledTime + 1f;
        Transform[] transforms = FindObjectsOfType<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate != null && candidate.name.ToLower().Contains("sun") && candidate.GetComponentInChildren<Renderer>() != null)
            {
                sun = candidate;
                return;
            }
        }
    }

    float GetSunRadius()
    {
        if (sun == null)
        {
            return 5f;
        }

        Renderer[] renderers = sun.GetComponentsInChildren<Renderer>();
        bool found = false;
        Bounds bounds = new Bounds(sun.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || !renderers[i].enabled)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderers[i].bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return found ? Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) : 5f;
    }

    void OnDisable()
    {
        // Avoid a stuck lock after recompiling or leaving Play Mode.
        IsLockedByShipMode = false;
        ProceduralSolarSystemAudio.Ensure().SetPlayerThruster(false, 0f);
    }
}
