using UnityEngine;

public class Rotation : MonoBehaviour
{
    public GameObject sphere;

    public float revolutionSpeed = 5f;
    public float rotationSpeed = 20f;

    void Update()
    {
        // Revolution around another object
        transform.RotateAround(
            sphere.transform.position,
            Vector3.up,
            revolutionSpeed * Time.deltaTime
        );

        // Rotation around itself
        transform.Rotate(
            Vector3.up,
            rotationSpeed * Time.deltaTime
        );
    }
}
