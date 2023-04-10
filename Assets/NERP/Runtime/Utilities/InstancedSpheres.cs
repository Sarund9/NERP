using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class InstancedSpheres : MonoBehaviour
{

    readonly static int
        baseColorId = Shader.PropertyToID("_BaseColor"),
        metallicId = Shader.PropertyToID("_Metallic"),
        smoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    Mesh mesh = default;

    [SerializeField]
    Material material = default;

    [SerializeField, Range(1, 512)]
    int count = 128;
    [SerializeField]
    int seed = -312573;

    [SerializeField]
    Transform sortFrom;

    // Arrays
    Matrix4x4[] matrices;
    Vector4[] baseColors;
    float[]
        metallic,
        smoothness;

    MaterialPropertyBlock block;
    
    private void OnValidate()
    {
        Random.InitState(seed);

        matrices = new Matrix4x4[count];
        baseColors = new Vector4[count];
        metallic = new float[count];
        smoothness = new float[count];
        for (int i = 0; i < matrices.Length; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * 10f,
                Quaternion.Euler(
                    Random.value * 360f, Random.value * 360f, Random.value * 360f
                ),
                Vector3.one * Random.Range(0.5f, 1.5f)
            );
            baseColors[i] =
                new Vector4(Random.value, Random.value, Random.value, Random.Range(0.5f, 1f));
            metallic[i] = Random.value < 0.25f ? 1f : 0f;
            smoothness[i] = Random.Range(0.05f, 0.95f);
        }
        
    }

    void Awake()
    {
        OnValidate();
    }

    void Update()
    {
        if (!material.enableInstancing)
            return;
        if (block == null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(baseColorId, baseColors);
            block.SetFloatArray(metallicId, metallic);
            block.SetFloatArray(smoothnessId, smoothness);
        }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, count, block);
    }
}
