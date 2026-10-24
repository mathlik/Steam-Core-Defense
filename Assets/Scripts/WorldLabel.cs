using UnityEngine;

/// <summary>
/// Поворачивает мировую надпись лицом к камере.
/// </summary>
public class WorldLabel : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        transform.forward = targetCamera.transform.forward;
    }
}
