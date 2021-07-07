
using UnityEngine;

public class UniqueMesh : MonoBehaviour
{
    [HideInInspector] int ownerID; // To ensure they have a unique mesh
    public Vector3[] verts;
    public Vector3[] normals;
    public int[] triangles;
    public Vector2[] uvs;
    
    MeshFilter _mf;
    MeshFilter mf { // Tries to find a mesh filter, adds one if it doesn't exist yet
        get{
            _mf = _mf == null ? GetComponent<MeshFilter>() : _mf;
            _mf = _mf == null ? gameObject.AddComponent<MeshFilter>() : _mf;
            return _mf;
        }
    }
    Mesh _mesh;
    public Mesh Mesh
    { // The mesh to edit
        get{
            bool isOwner = ownerID == gameObject.GetInstanceID();
            if( mf.sharedMesh == null || !isOwner ){
                mf.sharedMesh = _mesh = new UnityEngine.Mesh();
                ownerID = gameObject.GetInstanceID();
                _mesh.name = "Mesh [" + ownerID + "]";
            }

            verts = _mesh.vertices;
            normals = _mesh.normals;
            triangles = _mesh.triangles;
            uvs = _mesh.uv;
            return _mesh;
        }
    }
}
