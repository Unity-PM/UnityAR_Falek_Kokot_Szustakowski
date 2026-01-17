using UnityEngine;

public class rotate : MonoBehaviour
{
    public float speed = 50f;

    private void Update()
    {
        transform.Rotate(0, speed * Time.deltaTime, 0);
    }
}
