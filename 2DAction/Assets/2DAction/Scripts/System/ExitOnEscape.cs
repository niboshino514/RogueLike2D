using UnityEngine;
using UnityEngine.InputSystem;

namespace System
{
    public class ExitOnEscape : MonoBehaviour
    {
        void Update()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            }
        }
    }
}