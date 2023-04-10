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
        [Min(0.001f)]
        public float maxDistance = 100f;

        [Range(0.001f, 1f)]
        public float distanceFade = 0.1f;

        public Directional directional = new()
        {
            atlasSize = TextureSize._1024,
            filter = FilterMode.PCF2x2,
            cascadeCount = 4,
            cascadeRatio1 = 0.1f,
            cascadeRatio2 = 0.25f,
            cascadeRatio3 = 0.5f,
            cascadeFade = 0.1f,
            cascadeBlend = Directional.CascadeBlendMode.Hard
        };

        public enum FilterMode
        {
            PCF2x2, PCF3x3, PCF5x5, PCF7x7
        }

        // TYPES //
        [Serializable]
        public struct Directional
        {
            public TextureSize atlasSize;

            public FilterMode filter;

            [Range(1, 4)]
            public int cascadeCount;

            [Range(0f, 1f)]
            public float cascadeRatio1, cascadeRatio2, cascadeRatio3;

            [Range(0.001f, 1f)]
            public float cascadeFade;

            public Vector3 CascadeRatios =>
                new(cascadeRatio1, cascadeRatio2, cascadeRatio3);


            public enum CascadeBlendMode
            {
                Hard, Soft, Dither
            }

            public CascadeBlendMode cascadeBlend;
        }

        public enum TextureSize
        {
            _256 = 256, _512 = 512, _1024 = 1024,
            _2048 = 2048, _4096 = 4096, _8192 = 8192
        }
        
    }

}
