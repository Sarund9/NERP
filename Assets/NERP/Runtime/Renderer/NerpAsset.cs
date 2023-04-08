using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static NerpRuntime.ShadowSettings;

namespace NerpRuntime
{
    [CreateAssetMenu(menuName ="Rendering/NERP/New Pipeline Asset")]
    public class NerpAsset : RenderPipelineAsset
    {
        [SerializeField]
        bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;

        [SerializeField]
        ShadowSettings shadows = default;

        protected override RenderPipeline CreatePipeline()
        {
            return new NonEuclideanRenderPipeline(
                useDynamicBatching,
                useGPUInstancing,
                useSRPBatcher,
                shadows);
        }
    }



    public class NonEuclideanRenderPipeline : RenderPipeline
    {
        CameraRenderer renderer = new();
        

        bool useDynamicBatching, useGPUInstancing;
        ShadowSettings shadowSettings;

        public NonEuclideanRenderPipeline(
            bool useDynamicBatching, bool useGPUInstancing,
            bool useSRPBatcher, ShadowSettings shadowSettings)
        {
            this.useDynamicBatching = useDynamicBatching;
            this.useGPUInstancing = useGPUInstancing;
            this.shadowSettings = shadowSettings;
            GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
            GraphicsSettings.lightsUseLinearIntensity = true;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                renderer.Render(
                    context, camera, useDynamicBatching, useGPUInstancing,
                    shadowSettings
                );
            }
        }
    }

    [Serializable]
    public class ShadowSettings
    {
        [Min(0f)]
        public float maxDistance = 100f;

        public Directional directional = new()
        {
            atlasSize = TextureSize._1024
        };


        // TYPES //

        public struct Directional
        {
            public TextureSize atlasSize;
        }

        public enum TextureSize
        {
            _256 = 256, _512 = 512, _1024 = 1024,
            _2048 = 2048, _4096 = 4096, _8192 = 8192
        }
        
    }

}
