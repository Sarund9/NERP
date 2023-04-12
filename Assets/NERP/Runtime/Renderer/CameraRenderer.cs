using System;
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

        const string bufferName = "Render Camera";

        CommandBuffer buffer = new CommandBuffer
        {
            name = bufferName
        };

        CullingResults cullingResults;

        static ShaderTagId
            unlitShaderTagId = new("SRPDefaultUnlit"),
            litShaderTagId = new("NerpLit");

        readonly Lighting lighting = new();
        
        SortingSettings sortingSettings;
        DrawingSettings drawingSettings;
        FilteringSettings filteringSettings;

        Material stencilQuad, stencilToDepth, depthQuad;

        // DEBUG
        Matrix4x4 currentViewMatrix;

        public void Render(ScriptableRenderContext context, Camera camera,
            bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
        {
            this.context = context;
            this.camera = camera;

            if (stencilQuad == null)
            {
                stencilQuad = new(Shader.Find("NERP/Procedural/StencilQuad"));
            }
            if (stencilToDepth == null)
            {
                stencilToDepth = new(Shader.Find("NERP/Procedural/StencilToDepth"));
            }
            if (depthQuad == null)
            {
                depthQuad = new(Shader.Find("NERP/Procedural/DepthQuad"));
            }

            // Editor only
            PrepareBuffer();
            PrepareForSceneWindow();
            if (!Cull(shadowSettings.maxDistance))
            {
                return;
            }

            buffer.BeginSample(SampleName);
            ExecuteBuffer();
            lighting.Setup(context, cullingResults, shadowSettings);
            buffer.EndSample(SampleName);
            Setup();
            // Scene
            DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
            // Dev
            DrawUnsupportedShaders();
            DrawGizmos();

            lighting.Cleanup();
            Submit();
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
        }

        void Submit()
        {
            buffer.EndSample(SampleName);
            ExecuteBuffer();
            context.Submit();
        }

        void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
        {
            sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque,
            };
            drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing,
            };
            drawingSettings.SetShaderPassName(1, litShaderTagId);
            filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            var stateBlock = new RenderStateBlock(RenderStateMask.Nothing)
            {
                //stencilState = new(true, compareFunction: CompareFunction.Equal),
            };

            

            DrawScene(ref stateBlock, camera.worldToCameraMatrix, camera.projectionMatrix);
        }

        void DrawScene(
            ref RenderStateBlock block,
            Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix,
            int depth = 1, int stencil = 1)
        {
            // OPAQUES
            context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings, ref block
            );

            // RENDER PORTALS
            if (depth > 0 && Camera.main == camera)
            {
                foreach (var portal in PortalManager.Instance.AllPortals)
                {
                    if (!portal.InViewFrom(projectionMatrix * viewMatrix)
                        || !portal.EndPortal || portal.EndPortal == portal)
                        continue;

                    buffer.BeginSample(portal.gameObject.name + " :: PrePass");

                    // Draw Portal, sets Stencil
                    stencilQuad.SetInt("_StencilID", stencil);
                    stencilQuad.SetVector("_PortalExtents", portal.Extents);
                    buffer.DrawProcedural(
                        portal.transform.localToWorldMatrix,
                        stencilQuad, 0,
                        MeshTopology.Quads, 4);
                    
                    // Blit pass: set depth to 1, using stencil
                    stencilToDepth.SetInt("_StencilID", stencil);
                    buffer.DrawProcedural(
                        Matrix4x4.identity,
                        stencilToDepth, 0,
                        MeshTopology.Quads, 4);
                    
                    // Set to draw only on stencil 1
                    var newBlock = block;
                    newBlock.stencilState = new StencilState(
                        compareFunction: CompareFunction.Equal);
                    newBlock.mask = RenderStateMask.Stencil;
                    newBlock.stencilReference = stencil;

                    // Set camera parameters
                    portal.Translate(viewMatrix, projectionMatrix,
                        out var newView, out var newProj);

                    //buffer.SetViewProjectionMatrices(newView, newProj);
                    buffer.SetViewMatrix(newView.inverse);

                    buffer.EndSample(portal.gameObject.name + " :: PrePass");
                    ExecuteBuffer();


                    // Recursively Draw the Scene
                    DrawScene(ref newBlock, viewMatrix, projectionMatrix, depth - 1, stencil + 1);

                    buffer.BeginSample(portal.gameObject.name + " :: PostPass");

                    //buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                    buffer.SetViewMatrix(viewMatrix);

                    // Re-set the stencil
                    depthQuad.SetInt("_StencilID", stencil - 1);
                    depthQuad.SetVector("_PortalExtents", portal.Extents);
                    buffer.DrawProcedural(
                        portal.transform.localToWorldMatrix,
                        depthQuad, 0,
                        MeshTopology.Quads, 4);

                    buffer.EndSample(portal.gameObject.name + " :: PostPass");
                    ExecuteBuffer();

                }
            }

            //Debug.Log($"ROT: {viewMatrix.rotation.eulerAngles}");

            // TODO: How to draw skybox in Stencil Only

            // SKYBOX
            //context.DrawSkybox(camera);

            // WATER

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            // TRANSPARENTS
            context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings, ref block
            );
        }

        void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
        bool Cull(float maxShadowDistance)
        {
            if (camera.TryGetCullingParameters(out var p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
                cullingResults = context.Cull(ref p);
                return true;
            }
            return false;
        }
    }
}
