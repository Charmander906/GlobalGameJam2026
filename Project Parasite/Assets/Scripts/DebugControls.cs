using UnityEngine;
using UnityEngine.InputSystem;

public class DebugControls : MonoBehaviour
{
    public bool enableDebug = false;

    [HideInInspector]
    public bool viewDebug = false;

    void Start()
    {
        
    }

    void Update()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame) viewDebug = !viewDebug;
    }
}
