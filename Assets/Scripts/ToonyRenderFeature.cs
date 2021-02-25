using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ToonyRenderFeature : ScriptableRendererFeature
{
    public Texture2D LightRamp;
    public Texture2D SpecularRamp;
    public Texture2D SpecularMask;
    public Texture2D DirtMap;
    
    private ToonyRenderPass m_Pass;

    public override void Create()                                                                      => m_Pass = new ToonyRenderPass(this);
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) => renderer.EnqueuePass(m_Pass);
}

public class ToonyRenderPass : ScriptableRenderPass
{
    private ToonyRenderFeature Feature;

    public ToonyRenderPass(ToonyRenderFeature Feature)
    {
        this.Feature    = Feature;
        renderPassEvent = RenderPassEvent.BeforeRendering;
    }
    
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Shader.SetGlobalTexture("_LightRamp", Feature.LightRamp);
        Shader.SetGlobalTexture("_SpecularRamp", Feature.SpecularRamp);
        Shader.SetGlobalTexture("_SpecularMask", Feature.SpecularMask);
        Shader.SetGlobalTexture("_DirtMap", Feature.DirtMap);
    }
}
