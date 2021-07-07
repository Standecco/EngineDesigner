using System;
using System.Collections;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[RequireComponent(typeof(BellNozzle))]
[RequireComponent(typeof(UniqueMesh))]
public class CurveRevolver : MonoBehaviour
{
    public int edgeNumber = 36;
    public LineRenderer[] edges;

    private void Start()
    {
        edges = new LineRenderer[edgeNumber];

        //StartCoroutine(AddLineRenderers());
    }

    private IEnumerator AddLineRenderers()
    {
        for (int i = 0; i < edgeNumber; i++)
        {
            var obj = new GameObject("Edge Renderer");
            edges[i] = obj.AddComponent<LineRenderer>() as LineRenderer;
            //yield return new WaitForFixedUpdate();
        }
        
        edges = Resources.FindObjectsOfTypeAll<GameObject>().Select(g => g.GetComponent<LineRenderer>()).ToArray();
        yield break;
    }
    
    private void Update()
    {
        CurveData curve = GetComponent<BellNozzle>().curveData;
        UniqueMesh meshData = GetComponent<UniqueMesh>();

        OrientedPoint[] path = new OrientedPoint[edgeNumber];
        OrientedPoint[] reversePath = new OrientedPoint[edgeNumber];

        for (int i = 0; i < edgeNumber; i++)
        {
            float t = i / (float) edgeNumber * 2 * Mathf.PI;

            var v1 = new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t));
            var v2 = new Vector3(-Mathf.Sin(t), 0, Mathf.Cos(t));

            path[i] = new OrientedPoint(Vector3.zero, Quaternion.LookRotation(v1, v2));
        }

        Extrude(meshData.Mesh, curve, path);
    }
    public void Extrude(Mesh mesh, CurveData curve, OrientedPoint[] path)
    {
        int vertsInCurve = curve.Length;
        int edgeLoops = path.Length;
        int vertCount = vertsInCurve * edgeLoops;
        int triCount = curve.Lines.Length * edgeLoops;
        int triIndexCount = triCount * 3;

        var triangleIndices = new int[triIndexCount];
        var vertices = new Vector3[vertCount];
        var normals = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangleIndices;
        mesh.uv = uvs;

        for( int i = 0; i < path.Length; i++ ) {
            int offset = i * vertsInCurve;
            for( int j = 0; j < vertsInCurve; j++ ) {
                int id = offset + j;
                vertices[id] = path[i].LocalToWorld( curve.Verts[j] );
                normals[id] = path[i].LocalToWorldDirection( curve.Normals[j] );
                uvs[id] = new Vector2( curve.UCoords[j], i / (float)edgeLoops );
                
                //edges[i].positionCount = vertsInCurve;
                //edges[i].SetPosition(j, vertices[id]);
            }
        }
        int ti = 0;
        for( int i = 0; i < edgeLoops; i++ ) {
            int offset = i * vertsInCurve;
            for ( int l = 0; l < curve.Lines.Length; l += 2 ) {
                int a = offset + curve.Lines[l] + vertsInCurve;
                int b = offset + curve.Lines[l];
                int c = offset + curve.Lines[l+1];
                int d = offset + curve.Lines[l+1] + vertsInCurve;
                a = a < vertCount ? a : a - vertCount;
                b = b < vertCount ? b : b - vertCount;
                c = c < vertCount ? c : c - vertCount;
                d = d < vertCount ? d : d - vertCount;
                triangleIndices[ti] = a; 	ti++;
                triangleIndices[ti] = b; 	ti++;
                triangleIndices[ti] = c; 	ti++;
                triangleIndices[ti] = c; 	ti++;
                triangleIndices[ti] = d; 	ti++;
                triangleIndices[ti] = a; 	ti++;
            }
        }
        
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangleIndices;
        mesh.uv = uvs;
    }
}