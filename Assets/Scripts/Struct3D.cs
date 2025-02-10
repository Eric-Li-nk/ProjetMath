using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Struct3D
{

}
public class Sommet3D
{
    public int index;
    public Vector3 p;

    public Sommet3D(int index, Vector3 p)
    {
        this.index = index;
        this.p = p;
    }
}


public class Edge3D
{
    public Sommet3D s1, s2;

    public Edge3D(Sommet3D s1, Sommet3D s2)
    {
        this.s1 = s1;
        this.s2 = s2;
    }

    // Compara dos aristas considerando una tolerancia
    public bool IsEqual(Edge3D other, float tol = 1e-4f)
    {
        bool cond1 = (Vector3.Distance(s1.p, other.s1.p) < tol && Vector3.Distance(s2.p, other.s2.p) < tol);
        bool cond2 = (Vector3.Distance(s1.p, other.s2.p) < tol && Vector3.Distance(s2.p, other.s1.p) < tol);
        return cond1 || cond2;
    }
}


public class Face3D
{
    public Sommet3D s1, s2, s3;
    public Edge3D e1, e2, e3;
    public Vector3 normal;

    // Constructor
    public Face3D(Sommet3D s1, Sommet3D s2, Sommet3D s3)
    {
        this.s1 = s1;
        this.s2 = s2;
        this.s3 = s3;
        RecalculateNormal();
        CalculateEdges();
    }

    // Recalcula la normal de la cara usando el producto vectorial
    public void RecalculateNormal()
    {
        normal = Vector3.Cross(s2.p - s1.p, s3.p - s1.p).normalized;
    }

    // Crea los bordes de la cara
    public void CalculateEdges()
    {
        e1 = new Edge3D(s1, s2);
        e2 = new Edge3D(s2, s3);
        e3 = new Edge3D(s3, s1);
    }

    // Determina si el vértice p está "delante" de la cara usando una tolerancia
    public bool IsVisible(Sommet3D p, float tol = 1e-6f)
    {
        float d = Vector3.Dot(normal, p.p - s1.p);
        return d > tol;
    }

    // Invierte el orden de los vértices y recalcula la normal y los bordes
    public void Flip()
    {
        // Intercambia s2 y s3
        Sommet3D temp = s2;
        s2 = s3;
        s3 = temp;

        RecalculateNormal();
        CalculateEdges();
    }
}




