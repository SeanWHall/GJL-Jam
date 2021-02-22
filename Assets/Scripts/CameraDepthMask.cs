using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraDepthMask : BaseBehaviour
{
    public Vector2Int Resolution = new Vector2Int(1024, 1024);
    public Shader     DepthShader;
    
    public override eUpdateFlags UpdateFlags => eUpdateFlags.RequireUpdate | eUpdateFlags.WhilePaused;
    public override int Priority => -100; //Last behaviour to run, so hopefully after everything has moved

    private List<MaskObject> m_MaskObjects = new List<MaskObject>();
    private RenderTexture    m_DepthMask_RT;
    private Material         m_DepthMask_Mat;
    private Camera           m_Camera;
    
    public override void OnUpdate(float DeltaTime)
    {
        if (DepthShader == null)
            return;

        if (m_Camera == null)
            m_Camera = GetComponent<Camera>();

        if (m_DepthMask_Mat == null)
            m_DepthMask_Mat = new Material(DepthShader);
        
        if (m_DepthMask_RT == null)
            m_DepthMask_RT = new RenderTexture(Resolution.x, Resolution.y, -1);

        int           Objects_Len = GetBehaviours(m_MaskObjects);
        CommandBuffer cmd         = CommandBufferPool.Get(ID);
        cmd.SetRenderTarget(m_DepthMask_RT);
        cmd.SetViewProjectionMatrices(m_Camera.worldToCameraMatrix, m_Camera.projectionMatrix);
        
        for(int i = 0; i < Objects_Len; i++)
            m_MaskObjects[i].Render(cmd, m_DepthMask_Mat);
        
        Graphics.ExecuteCommandBuffer(cmd);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (m_DepthMask_RT != null)
        {
            m_DepthMask_RT.Release();
            m_DepthMask_RT = null;
        }

        if (m_DepthMask_Mat != null)
        {
            Destroy(m_DepthMask_Mat);
            m_DepthMask_Mat = null;
        }
    }
}
