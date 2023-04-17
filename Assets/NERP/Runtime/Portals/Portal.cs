using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NerpRuntime
{
    [ExecuteInEditMode]
    public class Portal : MonoBehaviour
    {


        [SerializeField]
        bool render;

        BoxCollider trigger;

        enum Side
        {
            Front, // positive local Z
            Back,  // negative local Z
        }
        struct State
        {
            public Side side;
            public bool canCross;

            public State(Side side, bool insideBounds)
            {
                this.side = side;
                this.canCross = insideBounds;
            }
        }

        readonly Dictionary<PortalTeleport, State> teleportState = new();
        readonly HashSet<PortalTeleport> teleports = new();

        readonly Plane[] testplanes = new Plane[6];


        [field: SerializeField]
        public Portal EndPortal { get; private set; }

        [field: SerializeField]
        public Vector2 Extents { get; private set; } = Vector2.one * 2;

        public Vector2 Size => Extents * 2;

        public Bounds Bounds => CalcBounds();

        private Bounds CalcBounds()
        {
            var b = new Bounds(transform.position, transform.lossyScale * .03f);

            var p00 = new Vector3(-Extents.x, -Extents.y);
            var p01 = new Vector3(-Extents.x, Extents.y);
            var p10 = new Vector3(Extents.x, -Extents.y);
            var p11 = new Vector3(Extents.x, Extents.y);
            b.Encapsulate(transform.TransformPoint(p00));
            b.Encapsulate(transform.TransformPoint(p01));
            b.Encapsulate(transform.TransformPoint(p10));
            b.Encapsulate(transform.TransformPoint(p11));

            return b;
        }

        private void OnDrawGizmos()
        {
            // TODO: Move to Custom Editor
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new(.05f, .9f, .9f, .8f);
            var p00 = new Vector3(-Extents.x, -Extents.y);
            var p01 = new Vector3(-Extents.x, Extents.y);
            var p10 = new Vector3(Extents.x, -Extents.y);
            var p11 = new Vector3(Extents.x, Extents.y);
            Gizmos.DrawLine(p00, p01);
            Gizmos.DrawLine(p00, p10);
            Gizmos.DrawLine(p11, p01);
            Gizmos.DrawLine(p11, p10);

            Gizmos.color = new(.05f, .8f, .9f, .1f);
            Gizmos.DrawCube(Vector3.zero, Extents * 2);

            Gizmos.color = new(.9f, .3f, .05f, .7f);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 2);

            //Gizmos.matrix = Matrix4x4.identity;
            //Gizmos.color = new(.75f, .1f, .9f, .2f);
            //var b = CalcBounds();
            //Gizmos.DrawCube(b.center, b.size);
        }

        private void OnEnable()
        {
            PortalManager.Instance.Register(this);
        }
        private void OnDisable()
        {
            PortalManager.Instance.Unregister(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            var tel = other.GetComponent<PortalTeleport>();
            if (tel)
            {
                teleports.Add(tel);
                teleportState.Add(tel, new(GetSide(other.transform),
                    CanCross(tel.transform.position)));
            }
        }
        private void OnTriggerExit(Collider other)
        {
            var tel = other.GetComponent<PortalTeleport>();
            if (tel)
            {
                teleports.Remove(tel);
                teleportState.Remove(tel);
            }
        }

        private void OnValidate()
        {
            if (!trigger)
            {
                trigger = GetComponent<BoxCollider>();
            }
            if (trigger)
            {
                trigger.size = new(Size.x, Size.y, 1);
                trigger.center = Vector3.zero;
                trigger.isTrigger = true;
            }
        }

        Side GetSide(Transform other)
        {
            var at = transform.InverseTransformPoint(other.position);

            return at.z > 0 ? Side.Front : Side.Back;
        }
        bool CanCross(Vector3 objectPosition)
        {
            var localPosition = transform.InverseTransformPoint(objectPosition);

            return localPosition.x > -Extents.x && localPosition.y > -Extents.y
                && localPosition.x <  Extents.x && localPosition.y <  Extents.y;
        }

        private void Update()
        {
            if (!EndPortal) return;

            foreach (var tel in teleports)
            {
                // Object center must pass through portal
                var state = teleportState[tel];
                state.canCross = CanCross(tel.transform.position);
                
                var currentSide = GetSide(tel.transform);
                if (currentSide != state.side)
                {
                    if (state.canCross)
                        tel.Teleport(this);
                    else
                        state.side = currentSide;
                }
                teleportState[tel] = state;
            }
        }

        public bool InViewFrom(Camera camera)
        {
            if (!render) return false;
            GeometryUtility.CalculateFrustumPlanes(camera, testplanes);
            return GeometryUtility.TestPlanesAABB(testplanes, Bounds);
        }
        public bool InViewFrom(Matrix4x4 worldToProjectionMatrix)
        {
            if (!render) return false;
            GeometryUtility.CalculateFrustumPlanes(worldToProjectionMatrix, testplanes);
            return GeometryUtility.TestPlanesAABB(testplanes, Bounds);
        }

        
        public void Translate(
            Matrix4x4 currentView, Matrix4x4 currentProjection,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix)
        {
            if (!EndPortal)
            {
                viewMatrix = projectionMatrix = default;
                return;
            }

            // END ltw, start wtl, camera ltw
            viewMatrix = EndPortal.transform.localToWorldMatrix
                * transform.worldToLocalMatrix * currentView.inverse;

            // TODO: Oblique Projection Matrix
            projectionMatrix = currentProjection;
        }
    }

    public class PortalManager
    {
        public static PortalManager Instance { get; } = new();

        public List<Portal> AllPortals { get; } = new();

        public void Register(Portal portal)
        {
            AllPortals.Add(portal);
        }

        public void Unregister(Portal portal)
        {
            AllPortals.Remove(portal);
        }

    }
}
