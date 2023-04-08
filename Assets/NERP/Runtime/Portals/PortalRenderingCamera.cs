using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NerpRuntime
{
    [RequireComponent(typeof(Camera))]
    public class PortalRenderingCamera : MonoBehaviour
    {
        public static PortalRenderingCamera Instance { get; private set; }

        new Camera camera;

        public Camera Camera => camera;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void EditorInit()
        {
            FindObjectOfType<PortalRenderingCamera>().OnValidate();
        }
#endif

        public static bool IsInstance(Camera camera)
        {
            return Instance && Instance.camera == camera;
        }
        
        private void OnValidate()
        {
            Instance = this;
            if (!camera)
                camera = GetComponent<Camera>();
        }
        private void Awake()
        {
            OnValidate();
        }
    }
}
