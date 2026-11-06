using UnityEngine;

// Preserved from the original 2D prototype (unused).
public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    private bool canMove = true;

    void Update()
    {
        if (!canMove)
            return;

        float horizontal = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal = -1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontal = 1f;
        }

        transform.position += Vector3.right * horizontal * speed * Time.deltaTime;
    }

    public void StopMoving()
    {
        canMove = false;
    }
}
