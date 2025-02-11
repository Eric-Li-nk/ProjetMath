using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnveloppeConvex3D : MonoBehaviour
{
    [Header("Contenedor de puntos generados en runtime")]
    public Transform pointsParent;

    [Header("Material para la malla de la envolvente")]
    public Material hullMaterial;

    [Header("Material para dibujar edges en el Game (GL)")]
    public Material lineMaterial;

    private Mesh hullMesh;
    private GameObject hullObject;
    private List<Face3D> hullFaces = new List<Face3D>();
    private List<Transform> pointTransforms = new List<Transform>();

    public void RecomputeHull()
    {
        pointTransforms.Clear();
        foreach (Transform child in pointsParent)
        {
            pointTransforms.Add(child);
        }
        Debug.Log("[RecomputeHull] Puntos recogidos: " + pointTransforms.Count);
        if (pointTransforms.Count < 4)
        {
            Debug.LogWarning("Se necesitan al menos 4 puntos para calcular la envolvente convexa 3D.");
            return;
        }
        List<Sommet3D> sommets = new List<Sommet3D>();
        for (int i = 0; i < pointTransforms.Count; i++)
        {
            sommets.Add(new Sommet3D(i, pointTransforms[i].position));
            Debug.Log("[RecomputeHull] Punto " + i + ": " + sommets[i].p);
        }
        hullFaces.Clear();
        ComputeConvexHull3D(sommets);
        DrawHull();
        Debug.Log("[RecomputeHull] hullFaces.Count final: " + hullFaces.Count);
    }

    void ComputeConvexHull3D(List<Sommet3D> sommets)
    {
        float epsilon = 0.0001f;
        // Seleccionar el tetraedro inicial
        /*
        int i0 = 0;
        int i1 = -1;
        for (int i = 1; i < sommets.Count; i++)
        {
            if (Vector3.Distance(sommets[i].p, sommets[i0].p) > epsilon)
            {
                i1 = i;
                break;
            }
        }
        if (i1 == -1)
        {
            Debug.LogError("[ComputeConvexHull3D] Todos los puntos son identicos.");
            return;
        }
        int i2 = -1;
        for (int i = i1 + 1; i < sommets.Count; i++)
        {
            Vector3 cross = Vector3.Cross(sommets[i1].p - sommets[i0].p, sommets[i].p - sommets[i0].p);
            if (cross.magnitude > epsilon)
            {
                i2 = i;
                break;
            }
        }
        if (i2 == -1)
        {
            Debug.LogError("[ComputeConvexHull3D] Todos los puntos son colineales.");
            return;
        }
        int i3 = FindNonCoplanarIndex(sommets[i0], sommets[i1], sommets[i2], sommets, epsilon);
        if (i3 == -1)
        {
            Debug.LogError("[ComputeConvexHull3D] No se encontraron 4 puntos no coplanares.");
            return;
        }
        Debug.Log("[ComputeConvexHull3D] Tetraedro inicial: i0=" + i0 + ", i1=" + i1 + ", i2=" + i2 + ", i3=" + i3);

        // Crear las 4 caras del tetraedro inicial y corregir su orientacion
        Face3D baseFace = new Face3D(sommets[i0], sommets[i1], sommets[i2]);
        if (baseFace.IsVisible(sommets[i3]))
        {
            baseFace.Flip();
        }
        Face3D f1 = baseFace;
        Face3D f2 = new Face3D(sommets[i0], sommets[i1], sommets[i3]);
        if (f2.IsVisible(sommets[i2])) { f2.Flip(); }
        Face3D f3 = new Face3D(sommets[i0], sommets[i2], sommets[i3]);
        if (f3.IsVisible(sommets[i1])) { f3.Flip(); }
        Face3D f4 = new Face3D(sommets[i1], sommets[i2], sommets[i3]);
        if (f4.IsVisible(sommets[i0])) { f4.Flip(); }
        hullFaces.Add(f1);
        hullFaces.Add(f2);
        hullFaces.Add(f3);
        hullFaces.Add(f4);
        Debug.Log("[ComputeConvexHull3D] hullFaces.Count tras tetraedro inicial: " + hullFaces.Count);
        */

        int i0 = 0;
        int i1 = 1;
        int i2 = 2;
        int i3 = 3;

        // Crear la cara base con los vertices i0, i1 e i2
        Face3D f1 = new Face3D(sommets[i0], sommets[i1], sommets[i2]);
        // Se verifica la visibilidad del cuarto punto respecto a la cara base
        if (f1.IsVisible(sommets[i3]))
        {
            f1.Flip();  // Invertir la cara si el punto i3 esta delante
        }

        // Crear las otras 3 caras del tetraedro
        Face3D f2 = new Face3D(sommets[i0], sommets[i1], sommets[i3]);
        if (f2.IsVisible(sommets[i2])) { f2.Flip(); }

        Face3D f3 = new Face3D(sommets[i0], sommets[i2], sommets[i3]);
        if (f3.IsVisible(sommets[i1])) { f3.Flip(); }

        Face3D f4 = new Face3D(sommets[i1], sommets[i2], sommets[i3]);
        if (f4.IsVisible(sommets[i0])) { f4.Flip(); }

        // Anadir las 4 caras a la lista de envolvente
        hullFaces.Add(f1);
        hullFaces.Add(f2);
        hullFaces.Add(f3);
        hullFaces.Add(f4);

        // Imprimir en consola la cantidad de caras del tetraedro inicial
        Debug.Log("[ComputeConvexHull3D] Tetraedro inicial formado con indices: " + i0 + ", " + i1 + ", " + i2 + ", " + i3);

        // Insertar los demas puntos uno a uno usando el grafo de incidencias y marcaje de colores
        for (int i = 0; i < sommets.Count; i++)
        {
            if (i == i0 || i == i1 || i == i2 || i == i3)
                continue;
            Sommet3D p = sommets[i];

            List<Face3D> visibleFaces = new List<Face3D>();
            foreach (Face3D face in hullFaces)
            {
                if (face.IsVisible(p, 1e-6f))
                {
                    visibleFaces.Add(face);
                }
            }
            Debug.Log("[ComputeConvexHull3D] Punto " + p.index + ", visibleFaces.Count=" + visibleFaces.Count);
            if (visibleFaces.Count == 0)
            {
                Debug.Log("[ComputeConvexHull3D] Punto " + p.index + " dentro de la envolvente, no modifica hull.");
                continue;
            }

            // Calcular las aristas del horizonte: aquellas que aparecen en exactamente una cara visible.
            List<Edge3D> horizonEdges = new List<Edge3D>();
            foreach (Face3D face in visibleFaces)
            {
                CheckAndAddHorizonEdge(face.e1, visibleFaces, horizonEdges);
                CheckAndAddHorizonEdge(face.e2, visibleFaces, horizonEdges);
                CheckAndAddHorizonEdge(face.e3, visibleFaces, horizonEdges);
            }
            Debug.Log("[ComputeConvexHull3D] Punto " + p.index + ", horizonEdges.Count=" + horizonEdges.Count);

            // Para cada arista del horizonte, asignar color Violet (por que pertenece a una cara Blue y una Red)
            foreach (Edge3D edge in horizonEdges)
            {
                edge.color = ColorType.Violet;
            }
            // Eliminar las caras visibles de hullFaces
            foreach (Face3D face in visibleFaces)
            {
                hullFaces.Remove(face);
            }
            // Crear nuevas caras conectando el punto p con cada arista del horizonte.
            int facesRemoved = visibleFaces.Count;
            int facesAdded = 0;
            foreach (Edge3D edge in horizonEdges)
            {
                Face3D newFace = new Face3D(edge.s1, edge.s2, p);
                hullFaces.Add(newFace);
                facesAdded++;
            }
            Debug.Log("[ComputeConvexHull3D] Punto " + p.index + ": removed=" + facesRemoved + ", added=" + facesAdded + ", hullFaces.Count=" + hullFaces.Count);
        }
        CorrectFaceOrientations(hullFaces);
        Debug.Log("[ComputeConvexHull3D] hullFaces.Count tras CorrectFaceOrientations: " + hullFaces.Count);
    }

    /*int FindNonCoplanarIndex(Sommet3D s1, Sommet3D s2, Sommet3D s3, List<Sommet3D> sommets, float epsilon = 0.0001f)
    {
        Vector3 normal = Vector3.Cross(s2.p - s1.p, s3.p - s1.p).normalized;
        for (int i = 0; i < sommets.Count; i++)
        {
            if (sommets[i] == s1 || sommets[i] == s2 || sommets[i] == s3)
                continue;
            float d = Mathf.Abs(Vector3.Dot(normal, sommets[i].p - s1.p));
            if (d > epsilon)
            {
                Debug.Log("[FindNonCoplanarIndex] Punto " + sommets[i].index + " es no coplanar (dot=" + d + ").");
                return i;
            }
        }
        return -1;
    }*/

    void CorrectFaceOrientations(List<Face3D> faces)
    {
        HashSet<Vector3> uniqueVertices = new HashSet<Vector3>();
        foreach (Face3D face in faces)
        {
            uniqueVertices.Add(face.s1.p);
            uniqueVertices.Add(face.s2.p);
            uniqueVertices.Add(face.s3.p);
        }
        Vector3 globalCenter = Vector3.zero;
        foreach (Vector3 v in uniqueVertices)
        {
            globalCenter += v;
        }
        if (uniqueVertices.Count > 0)
            globalCenter /= uniqueVertices.Count;
        int flips = 0;
        foreach (Face3D face in faces)
        {
            Vector3 faceCentroid = (face.s1.p + face.s2.p + face.s3.p) / 3f;
            Vector3 dir = (faceCentroid - globalCenter).normalized;
            if (Vector3.Dot(face.normal, dir) < 0)
            {
                face.Flip();
                flips++;
            }
        }
        Debug.Log("[CorrectFaceOrientations] Caras flippeadas: " + flips);
    }

    void CheckAndAddHorizonEdge(Edge3D edge, List<Face3D> visibleFaces, List<Edge3D> horizonEdges, float tol = 1e-4f)
    {
        int count = 0;
        foreach (Face3D face in visibleFaces)
        {
            if (edge.IsEqual(face.e1, tol) || edge.IsEqual(face.e2, tol) || edge.IsEqual(face.e3, tol))
                count++;
        }
        if (count == 1 && !horizonEdges.Exists(e => e.IsEqual(edge, tol)))
        {
            horizonEdges.Add(edge);
        }
    }

    void DrawHull()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Dictionary<Vector3, int> vertexToIndex = new Dictionary<Vector3, int>();
        foreach (Face3D face in hullFaces)
        {
            Vector3[] faceVerts = new Vector3[] { face.s1.p, face.s2.p, face.s3.p };
            int[] indices = new int[3];
            for (int i = 0; i < 3; i++)
            {
                if (!vertexToIndex.ContainsKey(faceVerts[i]))
                {
                    vertexToIndex[faceVerts[i]] = vertices.Count;
                    vertices.Add(faceVerts[i]);
                }
                indices[i] = vertexToIndex[faceVerts[i]];
            }
            triangles.Add(indices[0]);
            triangles.Add(indices[1]);
            triangles.Add(indices[2]);
        }
        foreach (Face3D face in hullFaces)
        {
            Vector3 centroid = (face.s1.p + face.s2.p + face.s3.p) / 3f;
            Debug.DrawLine(centroid, centroid + face.normal * 2f, Color.red, 5f);
        }
        hullMesh = new Mesh();
        hullMesh.vertices = vertices.ToArray();
        hullMesh.triangles = triangles.ToArray();
        hullMesh.RecalculateNormals();
        if (hullObject != null)
        {
            Destroy(hullObject);
        }
        hullObject = new GameObject("ConvexHull3D");
        MeshFilter mf = hullObject.AddComponent<MeshFilter>();
        MeshRenderer mr = hullObject.AddComponent<MeshRenderer>();
        mf.mesh = hullMesh;
        mr.material = hullMaterial;
        Debug.Log("[DrawHull] hullFaces.Count=" + hullFaces.Count + ", Triangles en la malla=" + (triangles.Count / 3));
    }

    void OnRenderObject()
    {
        if (hullFaces == null || hullFaces.Count == 0 || lineMaterial == null)
            return;
        lineMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(Color.yellow);
        foreach (Face3D face in hullFaces)
        {
            // Se dibujan los tres edges de la cara
            GL.Vertex(face.s1.p);
            GL.Vertex(face.s2.p);
            GL.Vertex(face.s2.p);
            GL.Vertex(face.s3.p);
            GL.Vertex(face.s3.p);
            GL.Vertex(face.s1.p);
        }
        GL.End();
    }

}
