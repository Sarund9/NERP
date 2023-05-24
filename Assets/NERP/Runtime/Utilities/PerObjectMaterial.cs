﻿using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterial : MonoBehaviour
{
    static int
        baseColorId = Shader.PropertyToID("_BaseColor"),
        cutoffId = Shader.PropertyToID("_Cutoff"),
        metallicId = Shader.PropertyToID("_Metallic"),
        smoothnessId = Shader.PropertyToID("_Smoothness"),
        emissionColorId = Shader.PropertyToID("_EmissionColor");

    static MaterialPropertyBlock block;

    [SerializeField]
    Color baseColor = Color.white;

    [SerializeField, Range(0f, 1f)]
    float cutoff = 0.5f, metallic = 0f, smoothness = 0.5f;

    [SerializeField, ColorUsage(false, true)]
    Color emissionColor = Color.black;

    void OnValidate()
    {
        block ??= new MaterialPropertyBlock();
        block.SetColor(baseColorId, baseColor);
        block.SetFloat(cutoffId, cutoff);
        block.SetFloat(metallicId, metallic);
        block.SetFloat(smoothnessId, smoothness);
        block.SetColor(emissionColorId, emissionColor);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

    void Awake() => OnValidate();
}
