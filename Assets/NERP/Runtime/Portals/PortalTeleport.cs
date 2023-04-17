using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NerpRuntime
{
    
    public class PortalTeleport : MonoBehaviour
    {

        Rigidbody rb;

        private void OnValidate()
        {
            if (!rb) rb = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            OnValidate();
        }

        public void Teleport(Portal through)
        {
            if (!through || !through.EndPortal)
                return;
            var m = NerpMath.TranslateTransform(
                transform.localToWorldMatrix,
                through.transform.worldToLocalMatrix,
                through.EndPortal.transform.localToWorldMatrix);

            transform.SetPositionAndRotation(m.GetPosition(), m.rotation);
            transform.localScale = m.lossyScale;
            rb.velocity = m.rotation * rb.velocity;
        }
    }
}
