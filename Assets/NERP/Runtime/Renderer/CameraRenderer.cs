using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

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

        readonly Material stencilQuad = new(Shader.Find("NERP/Procedural/StencilQuad"));
        readonly Material stencilToDepth = new(Shader.Find("NERP/Procedural/StencilToDepth"));
        
        public void Render(ScriptableRenderContext context, Camera camera,
            bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
        {
            this.context = context;
            this.camera = camera;

            // Editor only
            PrepareBuffer();
            PrepareForSceneWindow();
            if (!Cull(shadowSettings.maxDistance))
            {
                return;
            }

            Setup();
            lighting.Setup(context, cullingResults, shadowSettings);
            // Scene
            DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
            // Dev
            DrawUnsupportedShaders();
            DrawGizmos();

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

            DrawScene();
        }

        void DrawScene()
        {
            // OPAQUES
            context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings
            );

            // RENDER PORTALS
            if (Camera.main == camera)
            {
                foreach (var portal in PortalManager.Instance.AllPortals)
                {
                    if (!portal.InViewFrom(camera))
                        continue;

                    // Draw Portal, sets Stencil
                    stencilQuad.SetInt("_StencilID", 1);
                    stencilQuad.SetVector("_PortalExtents", portal.Extents);
                    buffer.DrawProcedural(
                        portal.transform.localToWorldMatrix,
                        stencilQuad, 0,
                        MeshTopology.Quads, 4);

                    // Blit pass: set depth to 1, using stencil
                    stencilToDepth.SetInt("_StencilID", 1);
                    buffer.DrawProcedural(
                        Matrix4x4.identity,
                        stencilToDepth, 0,
                        MeshTopology.Quads, 4);

                    // Set to draw only on stencil 1
                    // Set camera parameters


                    // 
                }
            }
            ExecuteBuffer();

            // SKYBOX
            context.DrawSkybox(camera);

            // TODO: WATER

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            // TRANSPARENTS
            context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings
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
