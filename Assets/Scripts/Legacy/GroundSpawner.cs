using UnityEngine;

public class GroundSpawner : MonoBehaviour
{
    public GameObject groundPrefab;

    public int groundCount = 20;

    public float spacing = 6f;

    void Start()
    {
        for (int i = 0; i < groundCount; i++)
        {
            Vector3 position = new Vector3(
                i * spacing,
                -3f,
                0f
            );

            Instantiate(
                groundPrefab,
                position,
                Quaternion.identity
            );
        }
    }
}