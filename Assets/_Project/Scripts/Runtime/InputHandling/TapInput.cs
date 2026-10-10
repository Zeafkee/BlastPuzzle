using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BlastPuzzle.InputHandling
{
    public sealed class TapInput : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;

        public event Action<Vector3> Tapped;

        public bool Locked { get; set; }

        private void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        private void Update()
        {
            if (Locked) return;

            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 screen = pointer.position.ReadValue();
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
            Tapped?.Invoke(world);
        }
    }
}
