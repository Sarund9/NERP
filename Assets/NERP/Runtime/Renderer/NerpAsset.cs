using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NerpRuntime
{
    [CreateAssetMenu(menuName ="Rendering/NERP/New Pipeline Asset")]
    public class NerpAsset : RenderPipelineAsset
    {
        [SerializeField]
        bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;

        protected override RenderPipeline CreatePipeline()
        {
            return new NonEuclideanRenderPipeline(
                useDynamicBatching,
                useGPUInstancing,
                useSRPBatcher);
        }
    }



    public class NonEuclideanRenderPipeline : RenderPipeline
    {
        CameraRender renderer = new();

        bool useDynamicBatching, useGPUInstancing;

        public NonEuclideanRenderPipeline(
            bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher)
        {
            this.useDynamicBatching = useDynamicBatching;
            this.useGPUInstancing = useGPUInstancing;
            GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                renderer.Render(context, camera, useDynamicBatching, useGPUInstancing);
            }
        }
    }
}
