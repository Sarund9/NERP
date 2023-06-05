using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using static Codice.CM.Common.CmCallContext;

namespace NerpRuntime
{
    public partial class CameraRenderer
    {
        ScriptableRenderContext context;
        Camera camera;
        ShadowSettings shadowSettings;

        const string bufferName = "Render Camera";
        const int MAX_PORTAL_RECURSION = 4;

        CommandBuffer buffer = new CommandBuffer
        {
            name = bufferName
        };

        
        CullingResults[] cullingResultStack = new CullingResults[MAX_PORTAL_RECURSION];
        int currentCullingResult = 0;

        public ref CullingResults CullingResults => ref cullingResultStack[currentCullingResult];

        static ShaderTagId
            unlitShaderTagId = new("SRPDefaultUnlit"),
            litShaderTagId = new("NerpLit");

        readonly Lighting lighting = new();
        
        SortingSettings sortingSettings;
        DrawingSettings drawingSettings;
        FilteringSettings filteringSettings;

        Material incrementStencil, stencilToDepth, decrementStencil;

        MaterialPropertyBlock stencilQuadBlock;

        bool useDynamicBatching, useGPUInstancing;

        // DEBUG
        Matrix4x4 currentViewMatrix;

        static Matrix4x4 Plane(Vector3 position, Vector3 direction) =>
            Matrix4x4.TRS(position,
                Quaternion.LookRotation(direction, Vector3.up),
                new(1, 1, 0));
        static readonly Matrix4x4[] portalPlaneValues = new[]
        {
            Matrix4x4.identity,

            Plane(new(0, 0, 1), new(0, 0, -1)),
            Plane(new(0, 0, -1), new(0, 0, 1)),
            Plane(new(0, 1, 0), new(0, -1, 0)),
            Plane(new(0, -1, 0), new(0, 1, 0)),
            Plane(new(1, 0, 0), new(-1, 0, 0)),
            Plane(new(-1, 0, 0), new(1, 0, 0)),
        };
        GraphicsBuffer portalPlaneTable;

        public void Render(ScriptableRenderContext context, Camera camera,
            bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
        {
            this.context = context;
            this.camera = camera;
            this.shadowSettings = shadowSettings;
            this.useDynamicBatching = useDynamicBatching;
            this.useGPUInstancing = useGPUInstancing;

            if (incrementStencil == null)
            {
                incrementStencil = new(Shader.Find("NERP/Portals/IncrementStencilQuad"));
            }
            if (stencilToDepth == null)
            {
                stencilToDepth = new(Shader.Find("NERP/Procedural/StencilToDepth"));
            }
            if (decrementStencil == null)
            {
                decrementStencil = new(Shader.Find("NERP/Portals/DecrementStencilQuad"));
            }
            stencilQuadBlock ??= new();

            // Editor only
            PrepareBuffer();
            PrepareForSceneWindow();
            if (!Cull(camera, shadowSettings.maxDistance))
            {
                return;
            }

            buffer.BeginSample(SampleName);
            ExecuteBuffer();
            lighting.Setup(context, CullingResults, shadowSettings);
            buffer.EndSample(SampleName);
            Setup();
            // Scene
            DrawVisibleGeometry();
            // Dev
            DrawUnsupportedShaders();
            DrawGizmos();

            lighting.Cleanup();
            Submit();
        }

        public void Dispose()
        {
            portalPlaneTable?.Release();
            portalPlaneTable = null;
        }

        void Setup()
        {
            context.SetupCameraProperties(camera);
            CameraClearFlags flags = camera.clearFlags;
            buffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags == CameraClearFlags.Color,
                flags == CameraClearFlags.Color ?
                camera.backgroundColor.linear : Color.clear);
            buffer.BeginSample(SampleName);
            ExecuteBuffer();

            portalPlaneTable ??= new(GraphicsBuffer.Target.Structured, 7, 64);
            portalPlaneTable.SetData(portalPlaneValues, 0, 0, 7);
            buffer.SetGlobalBuffer("_PortalPlanes", portalPlaneTable);

            ExecuteBuffer();
        }

        void Submit()
        {
            buffer.EndSample(SampleName);
            ExecuteBuffer();
            context.Submit();
        }

        void DrawVisibleGeometry()
        {
            var stateBlock = new RenderStateBlock(RenderStateMask.Nothing)
            {
                //stencilState = new(true, compareFunction: CompareFunction.Equal),
            };

            DrawScene(
                ref stateBlock, camera.worldToCameraMatrix, camera.projectionMatrix);

        }



        /*
        MainCamera: 1
        Depth 1:    2
        Depth 2:    3

         */

        void DrawScene(
            ref RenderStateBlock block,
            in Matrix4x4 viewMatrix, in Matrix4x4 projectionMatrix,
            int stencil = 0)
        {
            sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque,
            };
            drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing,
                perObjectData =
                    PerObjectData.ReflectionProbes |
                    PerObjectData.Lightmaps | PerObjectData.ShadowMask |
                    PerObjectData.LightProbe | PerObjectData.OcclusionProbe |
                    PerObjectData.LightProbeProxyVolume |
                    PerObjectData.OcclusionProbeProxyVolume,
            };
            drawingSettings.SetShaderPassName(1, litShaderTagId);
            filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            // OPAQUES
            context.DrawRenderers(
                CullingResults, ref drawingSettings, ref filteringSettings, ref block
            );

            // RENDER PORTALS
            if (stencil < MAX_PORTAL_RECURSION && Camera.main == camera)
            {
                foreach (var portal in PortalManager.Instance.AllPortals)
                {
                    if (
                        !portal.InViewFrom(projectionMatrix * viewMatrix) ||
                        !portal.EndPortal || portal.EndPortal == portal)
                        continue;

                    buffer.BeginSample(portal.gameObject.name + " :: PrePass");

                    IncrementStencil(portal);

                    // Blit pass: set depth to 1, using stencil
                    stencilToDepth.SetInt("_StencilID", stencil + 1);
                    buffer.DrawProcedural(
                        Matrix4x4.identity,
                        stencilToDepth, 0,
                        MeshTopology.Quads, 4);

                    ExecuteBuffer();


                    // Set to draw only on stencil +1
                    var newBlock = block;
                    newBlock.stencilState = new StencilState(
                        compareFunction: CompareFunction.Equal);
                    newBlock.mask = RenderStateMask.Stencil;
                    newBlock.stencilReference = stencil + 1;

                    // Set camera parameters
                    portal.Translate(viewMatrix, projectionMatrix,
                        out var newView, out var newProj);

                    //buffer.SetViewProjectionMatrices(newView, newProj);
                    buffer.SetViewMatrix(newView.inverse);

                    buffer.EndSample(portal.gameObject.name + " :: PrePass");
                    ExecuteBuffer();

                    // TODO: New culling data
                    //camera.cullingMatrix = projectionMatrix * viewMatrix;
                    //currentCullingResult++;
                    //Cull(camera, shadowSettings.maxDistance);
                    //ExecuteBuffer();

                    

                    // Recursively Draw the Scene
                    DrawScene(ref newBlock,
                        newView, newProj,
                        stencil + 1);

                    //currentCullingResult--;

                    buffer.BeginSample(portal.gameObject.name + " :: PostPass");

                    //buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                    buffer.SetViewMatrix(viewMatrix);

                    // Re-set the stencil
                    DecrementStencil(portal);

                    buffer.EndSample(portal.gameObject.name + " :: PostPass");
                    ExecuteBuffer();

                }
            }

            //Debug.Log($"ROT: {viewMatrix.rotation.eulerAngles}");

            // TODO: How to draw skybox in Stencil Only

            // SKYBOX (Only drawn outside portals)
            if (stencil == 0)
                context.DrawSkybox(camera);

            // WATER

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            // TRANSPARENTS
            context.DrawRenderers(
                CullingResults, ref drawingSettings, ref filteringSettings, ref block
            );


            //context.Submit();
        }

        private void IncrementStencil(Portal portal)
        {
            Vector3 extents = portal.Extents;
            extents.z = .2f;
            stencilQuadBlock.SetVector("_PortalExtents", extents);
            stencilQuadBlock.SetVector("_PortalForward", portal.transform.forward);
            buffer.DrawProcedural(
                portal.transform.localToWorldMatrix,
                incrementStencil, 0,
                MeshTopology.Quads, 4, 7, stencilQuadBlock);
        }

        private void DecrementStencil(Portal portal)
        {
            Vector3 extents = portal.Extents;
            extents.z = .2f;
            stencilQuadBlock.SetVector("_PortalExtents", extents);
            stencilQuadBlock.SetVector("_PortalForward", portal.transform.forward);
            buffer.DrawProcedural(
                portal.transform.localToWorldMatrix,
                decrementStencil, 0,
                MeshTopology.Quads, 4, 7, stencilQuadBlock);
        }

        void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
        bool Cull(Camera camera, float maxShadowDistance)
        {
            if (camera.TryGetCullingParameters(out var p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
                
                CullingResults = context.Cull(ref p);
                return true;
            }
            return false;
        }
        bool Cull(Camera camera, float maxShadowDistance, Matrix4x4 view, Matrix4x4 proj)
        {
            var prs = new ScriptableCullingParameters
            {
                isOrthographic = camera.orthographic,
                cullingMatrix = camera.cullingMatrix,
                
            };
            // TODO: Try This
            //camera.cullingMatrix = projectionMatrix * viewMatrix;
            if (camera.TryGetCullingParameters(out var p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);

                p.cullingMatrix = camera.cullingMatrix;

                CullingResults = context.Cull(ref p);
                return true;
            }
            return false;
        }
    }
}
