using System.Linq;
using UnityEngine;

public class CurveData
{
    public Vector2[] Verts => verts;
    public Vector2[] Normals => normals;
    public float[] UCoords => us;
    public int[] Lines => lines;
    public int Length => verts.Length;
    
    protected Vector2[] verts;
    protected Vector2[] normals;
    protected float[] us;
    protected int[] lines;

    public CurveData(Vector2[] vertices, bool closed = false)
    {
        verts = vertices;
        if(verts == null || verts.Length < 1)
            return;
        GenerateLines(closed);
        GenerateNormals();
        GenerateUCoords();
    }

    private void GenerateLines(bool closed)
    {
        lines = new int[(closed ? verts.Length : verts.Length - 1) * 2];
        int i = 0;
        for (int j = 0; i < (verts.Length - 1) * 2; i++)
        {
            lines[i] = i+j;
            j -= i % 2;
        }
        
        if(!closed)
            return;
        
        // stitch curve together
        lines[i] = verts.Length - 1;
        lines[i + 1] = 0;
    }

    private void GenerateNormals()
    {
        normals = new Vector2[verts.Length];
        
        // normals are approximated by rotating tangents by 90°
        Vector2 tangent = verts[1] - verts[0];
        normals[0] = new Vector2(-tangent.y, tangent.x).normalized;
        for (int i = 1; i < verts.Length; i ++)
        {
            tangent = verts[i] - verts[i - 1];
            normals[i] = new Vector2(-tangent.y, tangent.x).normalized;
        }
    }

    private void GenerateUCoords()
    {
        us = new float[verts.Length];
        for(int i = 0; i < verts.Length; i++)
        {
            us[i] = i / (float)verts.Length;
        }
    }
}