using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NerpRuntime
{
    public class PortalTranslateTest : MonoBehaviour
    {

        [SerializeField]
        Portal viewPortal;

        [SerializeField]
        Camera from;

        [SerializeField]
        Transform to;

        private void Update()
        {
            if (!viewPortal || !viewPortal.EndPortal || !from || !to) return;


            Matrix4x4 proj = default; // TODO: Camera params
            var viewMatrix = from.worldToCameraMatrix;

            viewPortal.Translate(viewMatrix, proj, out var newView, out var newProj);

            to.SetPositionAndRotation(newView.GetPosition(), newView.rotation);
            //to.localScale = newView. // TODO: Scale Extension
        }
    }
}
