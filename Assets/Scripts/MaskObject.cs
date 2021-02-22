using System;
using UnityEngine;
using UnityEngine.Rendering;

public class MaskObject : BaseBehaviour
{
    private Transform[] Mesh_Transforms;
    private Mesh[]      Meshes;

    private void Awake()
    {
        MeshFilter[] Filters     = GetComponentsInChildren<MeshFilter>();
        int          Filters_Len = Filters.Length;
        int          Valid_Len   = 0;
        
        Mesh_Transforms = new Transform[Filters_Len];
        Meshes          = new Mesh[Filters_Len];

        for (int i = 0; i < Filters_Len; i++)
        {
            MeshFilter Filter = Filters[i];
            if(Filter == null)
                continue;

            Mesh Filter_Mesh = Filter.sharedMesh;
            if(Filter_Mesh == null)
                continue;

            Meshes[Valid_Len]          = Filter_Mesh;
            Mesh_Transforms[Valid_Len] = Filter.transform;

            Valid_Len++;
        }
        
        Array.Resize(ref Mesh_Transforms, Valid_Len);
        Array.Resize(ref Meshes, Valid_Len);
    }

    public void Render(CommandBuffer cmd, Material DepthMat)
    {
        int Meshes_Len = Meshes.Length;
        for(int i = 0; i < Meshes_Len; i++)
            cmd.DrawMesh(Meshes[i], Mesh_Transforms[i].localToWorldMatrix, DepthMat);
    }
}
