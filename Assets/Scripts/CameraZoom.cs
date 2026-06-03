using UnityEngine;
using UnityEngine.InputSystem;

public class CameraZoom : MonoBehaviour
{
    public float minSize = 2.0f;
    public float maxSize = 50.0f;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool alt = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed ||
                   keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed ||
                   keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed;
        if (!alt) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.y.ReadValue() / 120f;
        if (Mathf.Approximately(scroll, 0)) return;

        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * 0.5f, minSize, maxSize);
    }
}
