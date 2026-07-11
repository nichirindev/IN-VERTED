using UnityEngine;

public class Hover : MonoBehaviour
{
    private Vector3 desiredPos;
    private float startY;

    private void Awake()
    {
        desiredPos = transform.position;
        startY = transform.position.y;
    }

    private void Update()
    {
        desiredPos.y = startY + Mathf.PingPong(Time.time, 1f) - 0.5f;
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime);
        transform.Rotate(Vector3.up, 0.25f);
    }
}
