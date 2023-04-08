using Unity.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

namespace NerpRuntime
{
    public partial class CameraRender
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
            bool useDynamicBatching, bool useGPUInstancing)
        {
            this.context = context;
            this.camera = camera;

            // Editor only
            PrepareBuffer();
            PrepareForSceneWindow();
            if (!Cull())
            {
                return;
            }

            Setup();
            lighting.Setup(context, cullingResults);
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
        bool Cull()
        {
            if (camera.TryGetCullingParameters(out var p))
            {
                cullingResults = context.Cull(ref p);
                return true;
            }
            return false;
        }
    }
    public class Lighting
    {

        const string bufferName = "Lighting";
        const int maxDirLightCount = 4;

        static int
            dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
            dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
            dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

        static Vector4[]
            dirLightColors = new Vector4[maxDirLightCount],
            dirLightDirections = new Vector4[maxDirLightCount];

        CommandBuffer buffer = new CommandBuffer
        {
            name = bufferName
        };

        CullingResults cullingResults;

        public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
        {
            this.cullingResults = cullingResults;
            buffer.BeginSample(bufferName);
            SetupLights();
            buffer.EndSample(bufferName);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        void SetupLights()
        {
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            int dirLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                if (visibleLight.lightType == LightType.Directional)
                {
                    SetupDirectionalLight(dirLightCount++, ref visibleLight);
                    if (dirLightCount >= maxDirLightCount)
                    {
                        break;
                    }
                }
            }

            buffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
            buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
        }

        void SetupDirectionalLight(int index, ref VisibleLight visibleLight) {
            dirLightColors[index] = visibleLight.finalColor;
            dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        }
    }
}
