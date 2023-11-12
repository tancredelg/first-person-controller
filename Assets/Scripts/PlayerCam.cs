using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    [SerializeField] private Transform orientation;
    [SerializeField, Range(0.1f, 100f)] private float sensitivity = 20f;
    
    private Vector2 _rotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float deltaX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * 100 * sensitivity;
        float deltaY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * 100 * sensitivity;

        _rotation.x = Mathf.Clamp(_rotation.x - deltaY, -90, 90);
        _rotation.y += deltaX;

        transform.rotation = Quaternion.Euler(_rotation);
        orientation.rotation = Quaternion.Euler(0, _rotation.y, 0);
    }
}