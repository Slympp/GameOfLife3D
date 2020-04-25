using UnityEngine;

public class CameraController : MonoBehaviour {
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float zoomSensitivity;
    [SerializeField] private Vector2 fovMinMax;

    private Transform _transform;
    private Camera _camera;

    private void Awake() {
        _transform = transform;
        _camera = Camera.main;
    }
    
    private void LateUpdate() {
        if (Input.GetMouseButton(1)) {
            _transform.RotateAround(Vector3.zero, Vector3.up, Input.GetAxis("Mouse X") * rotationSpeed);
            _transform.RotateAround(Vector3.zero, _transform.right, Input.GetAxis("Mouse Y") * -rotationSpeed);
        }

        float fov = _camera.fieldOfView;
        fov += Input.GetAxis("Mouse ScrollWheel") * -zoomSensitivity;
        fov = Mathf.Clamp(fov, fovMinMax.x, fovMinMax.y);
        _camera.fieldOfView = fov;
    }
}