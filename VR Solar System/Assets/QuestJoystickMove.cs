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

    private InputDevice leftController;
    private InputDevice rightController;
    private SpaceshipThirdPersonController spaceshipController;
    private float nextControllerSearchTime;

    void Update()
    {
        // Two safeguards:
        // 1. The ship controller sets this lock in Ship Mode.
        // 2. This script also independently checks the ship controller.
        if (IsLockedByShipMode || PlayerIsPilotingShip())
        {
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

        MoveWithLeftJoystick();
        TurnWithRightJoystick();
        MoveUpDownWithAandB();
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
        }

        if (bPressed)
        {
            transform.position += Vector3.up * verticalSpeed * Time.deltaTime;
        }
    }

    void OnDisable()
    {
        // Avoid a stuck lock after recompiling or leaving Play Mode.
        IsLockedByShipMode = false;
    }
}