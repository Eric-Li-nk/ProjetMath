using System.Collections.Generic;
using UnityEngine;

public class DelaunayTriangulation3D : MonoBehaviour
{
    [Header("Contenedor de puntos (hijos del GameObject)")]
    public Transform pointsParent;

    [Header("Material para el Mesh Delaunay")]
    public Material delaunayMaterial;

    [Header("Tolerancia (para comparaciones)")]
    public float tolerance = 1e-6f;

    // Lista de tetraedros (resultado de la triangulacion de Delaunay en R3)
    private List<Tetrahedron3D> delaunayTetrahedra;


    // Representa un punto elevado en R4: (x,y,z, x^2+y^2+z^2)
    public class Vertex4D
    {
        public Vector4 pos;
        public int index;
        public Vertex4D(int index, Vector4 pos)
        {
            this.index = index;
            this.pos = pos;
        }
    }

    // Representa una faceta candidata (un 3-simplex en R4) de la envolvente convexa
    public class Facet4D
    {
        public Vertex4D p1, p2, p3, p4;
        public Facet4D(Vertex4D p1, Vertex4D p2, Vertex4D p3, Vertex4D p4)
        {
            List<Vertex4D> pts = new List<Vertex4D> { p1, p2, p3, p4 };
            pts.Sort((a, b) => a.index.CompareTo(b.index));
            this.p1 = pts[0];
            this.p2 = pts[1];
            this.p3 = pts[2];
            this.p4 = pts[3];
        }
    }

    // Representa un tetraedro en R3 (3-simplex)
    public class Tetrahedron3D
    {
        public Vector3 a, b, c, d;
        public Tetrahedron3D(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            this.a = a; this.b = b; this.c = c; this.d = d;
        }
    }

    float Determinant3(Vector3 u, Vector3 v, Vector3 w)
    {
        return u.x * (v.y * w.z - v.z * w.y)
             - u.y * (v.x * w.z - v.z * w.x)
             + u.z * (v.x * w.y - v.y * w.x);
    }

    Vector4 ComputeNormal4D(Vector4 p1, Vector4 p2, Vector4 p3, Vector4 p4)
    {
        Vector4 v = p2 - p1;
        Vector4 w = p3 - p1;
        Vector4 u = p4 - p1;
        float n0 = Determinant3(new Vector3(v.y, v.z, v.w),
                                new Vector3(w.y, w.z, w.w),
                                new Vector3(u.y, u.z, u.w));
        float n1 = -Determinant3(new Vector3(v.x, v.z, v.w),
                                 new Vector3(w.x, w.z, w.w),
                                 new Vector3(u.x, u.z, u.w));
        float n2 = Determinant3(new Vector3(v.x, v.y, v.w),
                                new Vector3(w.x, w.y, w.w),
                                new Vector3(u.x, u.y, u.w));
        float n3 = -Determinant3(new Vector3(v.x, v.y, v.z),
                                 new Vector3(w.x, w.y, w.z),
                                 new Vector3(u.x, u.y, u.z));
        return new Vector4(n0, n1, n2, n3);
    }

    public void ComputeDelaunay()
    {
        // 1. Elevar los puntos R3 a R4.
        List<Vertex4D> liftedPoints = new List<Vertex4D>();
        int idx = 0;
        foreach (Transform child in pointsParent)
        {
            Vector3 p = child.position;
            float quad = p.x * p.x + p.y * p.y + p.z * p.z;
            Vector4 p4 = new Vector4(p.x, p.y, p.z, quad);
            liftedPoints.Add(new Vertex4D(idx, p4));
            idx++;
        }
        int n = liftedPoints.Count;

        // Lista para almacenar las facetas "inferiores" (de la envolvente en R4)
        List<Facet4D> lowerFacets = new List<Facet4D>();

        // 2. Recorrer todas las combinaciones de 4 puntos (brute-force)
        for (int i = 0; i < n - 3; i++)
        {
            for (int j = i + 1; j < n - 2; j++)
            {
                for (int k = j + 1; k < n - 1; k++)
                {
                    for (int l = k + 1; l < n; l++)
                    {
                        Vertex4D p1 = liftedPoints[i];
                        Vertex4D p2 = liftedPoints[j];
                        Vertex4D p3 = liftedPoints[k];
                        Vertex4D p4 = liftedPoints[l];
                        Vector4 normal = ComputeNormal4D(p1.pos, p2.pos, p3.pos, p4.pos);
                        if (normal.sqrMagnitude < tolerance) continue;
                        if (normal.w > 0) normal = -normal;
                        float c = Vector4.Dot(normal, p1.pos);
                        bool isFacet = true;
                        for (int m = 0; m < n; m++)
                        {
                            if (m == i || m == j || m == k || m == l) continue;
                            float d = Vector4.Dot(normal, liftedPoints[m].pos) - c;
                            if (d > tolerance)
                            {
                                isFacet = false;
                                break;
                            }
                        }
                        if (!isFacet) continue;
                        lowerFacets.Add(new Facet4D(p1, p2, p3, p4));
                    }
                }
            }
        }
        Debug.Log("Numero de facetas inferiores encontradas: " + lowerFacets.Count);

        // 3. Proyectar cada faceta a R3 para obtener un tetraedro
        delaunayTetrahedra = new List<Tetrahedron3D>();
        foreach (Facet4D facet in lowerFacets)
        {
            Vector3 v1 = new Vector3(facet.p1.pos.x, facet.p1.pos.y, facet.p1.pos.z);
            Vector3 v2 = new Vector3(facet.p2.pos.x, facet.p2.pos.y, facet.p2.pos.z);
            Vector3 v3 = new Vector3(facet.p3.pos.x, facet.p3.pos.y, facet.p3.pos.z);
            Vector3 v4 = new Vector3(facet.p4.pos.x, facet.p4.pos.y, facet.p4.pos.z);
            delaunayTetrahedra.Add(new Tetrahedron3D(v1, v2, v3, v4));
        }
        Debug.Log("Triangulacion de Delaunay calculada: " + delaunayTetrahedra.Count + " tetraedros.");

        // 4. Construir y asignar un Mesh que pinte la superficie externa
        DrawDelaunayMesh();

        // 5. Dibujar los bordes en el juego
        DrawDelaunayEdges();
    }

    string GetVectorKey(Vector3 v)
    {
        float rx = Mathf.Round(v.x * 10000f) / 10000f;
        float ry = Mathf.Round(v.y * 10000f) / 10000f;
        float rz = Mathf.Round(v.z * 10000f) / 10000f;
        return rx.ToString("F4") + "_" + ry.ToString("F4") + "_" + rz.ToString("F4");
    }

    string FaceKey(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        List<string> keys = new List<string> { GetVectorKey(v1), GetVectorKey(v2), GetVectorKey(v3) };
        keys.Sort();
        return keys[0] + keys[1] + keys[2];
    }

    void DrawDelaunayMesh()
    {
        GameObject oldMesh = GameObject.Find("DelaunayMesh");
        if (oldMesh != null)
        {
            DestroyImmediate(oldMesh);
        }
        Dictionary<string, int> faceCount = new Dictionary<string, int>();
        foreach (Tetrahedron3D tet in delaunayTetrahedra)
        {
            Vector3[][] faces = new Vector3[][] {
                new Vector3[] { tet.a, tet.b, tet.c },
                new Vector3[] { tet.a, tet.b, tet.d },
                new Vector3[] { tet.a, tet.c, tet.d },
                new Vector3[] { tet.b, tet.c, tet.d }
            };
            foreach (Vector3[] face in faces)
            {
                string key = FaceKey(face[0], face[1], face[2]);
                if (faceCount.ContainsKey(key))
                    faceCount[key]++;
                else
                    faceCount[key] = 1;
            }
        }
        List<Vector3[]> boundaryFaces = new List<Vector3[]>();
        foreach (Tetrahedron3D tet in delaunayTetrahedra)
        {
            Vector3[][] faces = new Vector3[][] {
                new Vector3[] { tet.a, tet.b, tet.c },
                new Vector3[] { tet.a, tet.b, tet.d },
                new Vector3[] { tet.a, tet.c, tet.d },
                new Vector3[] { tet.b, tet.c, tet.d }
            };
            foreach (Vector3[] face in faces)
            {
                string key = FaceKey(face[0], face[1], face[2]);
                if (faceCount[key] == 1)
                    boundaryFaces.Add(face);
            }
        }
        Vector3 hullCenter = Vector3.zero;
        int pointCount = 0;
        foreach (Transform child in pointsParent)
        {
            hullCenter += child.position;
            pointCount++;
        }
        if (pointCount > 0)
            hullCenter /= pointCount;
        foreach (Vector3[] face in boundaryFaces)
        {
            Vector3 centroid = (face[0] + face[1] + face[2]) / 3f;
            Vector3 edge1 = face[1] - face[0];
            Vector3 edge2 = face[2] - face[0];
            Vector3 normal = Vector3.Cross(edge1, edge2).normalized;
            if (Vector3.Dot(normal, centroid - hullCenter) < 0)
            {
                Vector3 temp = face[1];
                face[1] = face[2];
                face[2] = temp;
            }
        }
        List<Vector3> meshVertices = new List<Vector3>();
        List<int> meshTriangles = new List<int>();
        Dictionary<string, int> vertexToIndex = new Dictionary<string, int>();
        foreach (Vector3[] face in boundaryFaces)
        {
            int i1, i2, i3;
            string key1 = GetVectorKey(face[0]);
            string key2 = GetVectorKey(face[1]);
            string key3 = GetVectorKey(face[2]);
            if (!vertexToIndex.ContainsKey(key1))
            {
                i1 = meshVertices.Count;
                meshVertices.Add(face[0]);
                vertexToIndex[key1] = i1;
            }
            else
            {
                i1 = vertexToIndex[key1];
            }
            if (!vertexToIndex.ContainsKey(key2))
            {
                i2 = meshVertices.Count;
                meshVertices.Add(face[1]);
                vertexToIndex[key2] = i2;
            }
            else
            {
                i2 = vertexToIndex[key2];
            }
            if (!vertexToIndex.ContainsKey(key3))
            {
                i3 = meshVertices.Count;
                meshVertices.Add(face[2]);
                vertexToIndex[key3] = i3;
            }
            else
            {
                i3 = vertexToIndex[key3];
            }
            meshTriangles.Add(i1);
            meshTriangles.Add(i2);
            meshTriangles.Add(i3);
        }
        Mesh mesh = new Mesh();
        mesh.vertices = meshVertices.ToArray();
        mesh.triangles = meshTriangles.ToArray();
        mesh.RecalculateNormals();
        GameObject delaunayObj = new GameObject("DelaunayMesh");
        delaunayObj.transform.position = Vector3.zero;
        MeshFilter mf = delaunayObj.AddComponent<MeshFilter>();
        MeshRenderer mr = delaunayObj.AddComponent<MeshRenderer>();
        mf.mesh = mesh;
        mr.material = delaunayMaterial;
        Debug.Log("Mesh Delaunay construido con " + (meshTriangles.Count / 3) + " triangulos.");
    }
    void DrawDelaunayEdges()
    {
        Dictionary<string, int> faceCount = new Dictionary<string, int>();
        foreach (Tetrahedron3D tet in delaunayTetrahedra)
        {
            Vector3[][] faces = new Vector3[][] {
                new Vector3[] { tet.a, tet.b, tet.c },
                new Vector3[] { tet.a, tet.b, tet.d },
                new Vector3[] { tet.a, tet.c, tet.d },
                new Vector3[] { tet.b, tet.c, tet.d }
            };
            foreach (Vector3[] face in faces)
            {
                string key = FaceKey(face[0], face[1], face[2]);
                if (faceCount.ContainsKey(key))
                    faceCount[key]++;
                else
                    faceCount[key] = 1;
            }
        }
        List<Vector3[]> boundaryFaces = new List<Vector3[]>();
        foreach (Tetrahedron3D tet in delaunayTetrahedra)
        {
            Vector3[][] faces = new Vector3[][] {
                new Vector3[] { tet.a, tet.b, tet.c },
                new Vector3[] { tet.a, tet.b, tet.d },
                new Vector3[] { tet.a, tet.c, tet.d },
                new Vector3[] { tet.b, tet.c, tet.d }
            };
            foreach (Vector3[] face in faces)
            {
                string key = FaceKey(face[0], face[1], face[2]);
                if (faceCount[key] == 1)
                    boundaryFaces.Add(face);
            }
        }
        Dictionary<string, (Vector3, Vector3)> edgeDict = new Dictionary<string, (Vector3, Vector3)>();
        foreach (Vector3[] face in boundaryFaces)
        {
            AddEdge(face[0], face[1], edgeDict);
            AddEdge(face[1], face[2], edgeDict);
            AddEdge(face[2], face[0], edgeDict);
        }
        List<Vector3> edgeVertices = new List<Vector3>();
        List<int> edgeIndices = new List<int>();
        int indexCounter = 0;
        foreach (var kvp in edgeDict)
        {
            Vector3 v1 = kvp.Value.Item1;
            Vector3 v2 = kvp.Value.Item2;
            edgeVertices.Add(v1);
            edgeVertices.Add(v2);
            edgeIndices.Add(indexCounter);
            edgeIndices.Add(indexCounter + 1);
            indexCounter += 2;
        }
        Mesh edgeMesh = new Mesh();
        edgeMesh.vertices = edgeVertices.ToArray();
        edgeMesh.SetIndices(edgeIndices.ToArray(), MeshTopology.Lines, 0);
        GameObject oldEdges = GameObject.Find("DelaunayEdges");
        if (oldEdges != null)
        {
            DestroyImmediate(oldEdges);
        }
        GameObject edgeObj = new GameObject("DelaunayEdges");
        edgeObj.transform.position = Vector3.zero;
        MeshFilter mfEdge = edgeObj.AddComponent<MeshFilter>();
        MeshRenderer mrEdge = edgeObj.AddComponent<MeshRenderer>();
        mfEdge.mesh = edgeMesh;
        mrEdge.material = delaunayMaterial;
        Debug.Log("Bordes Delaunay dibujados.");
    }

    void AddEdge(Vector3 v1, Vector3 v2, Dictionary<string, (Vector3, Vector3)> edgeDict)
    {
        string key1 = GetVectorKey(v1);
        string key2 = GetVectorKey(v2);
        string key = (string.Compare(key1, key2) < 0) ? key1 + "_" + key2 : key2 + "_" + key1;
        if (!edgeDict.ContainsKey(key))
        {
            edgeDict[key] = (v1, v2);
        }
    }

    void OnDrawGizmos()
    {
        if (delaunayTetrahedra == null) return;
        Gizmos.color = Color.magenta;
        foreach (Tetrahedron3D tet in delaunayTetrahedra)
        {
            Gizmos.DrawLine(tet.a, tet.b);
            Gizmos.DrawLine(tet.a, tet.c);
            Gizmos.DrawLine(tet.a, tet.d);
            Gizmos.DrawLine(tet.b, tet.c);
            Gizmos.DrawLine(tet.b, tet.d);
            Gizmos.DrawLine(tet.c, tet.d);
        }
    }
}