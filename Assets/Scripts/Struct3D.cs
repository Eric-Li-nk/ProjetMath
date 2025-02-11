using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Struct3D
{

}
public enum ColorType { Red, Blue, Violet }

public class Sommet3D
{
    public int index;
    public Vector3 p;
    public List<Edge3D> incidentEdges;
    public List<Face3D> incidentFaces;
    public ColorType color;  
    public Sommet3D(int index, Vector3 p)
    {
        this.index = index;
        this.p = p;
        incidentEdges = new List<Edge3D>();
        incidentFaces = new List<Face3D>();
        color = ColorType.Red;
    }
}

public class Edge3D
{
    public Sommet3D s1, s2;
    public List<Face3D> incidentFaces;
    public ColorType color;
    public Edge3D(Sommet3D s1, Sommet3D s2)
    {
        this.s1 = s1;
        this.s2 = s2;
        incidentFaces = new List<Face3D>();
        color = ColorType.Red;
        s1.incidentEdges.Add(this);
        s2.incidentEdges.Add(this);
    }
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
    public Face3D(Sommet3D s1, Sommet3D s2, Sommet3D s3)
    {
        this.s1 = s1;
        this.s2 = s2;
        this.s3 = s3;
        RecalculateNormal();
        CalculateEdges();
        s1.incidentFaces.Add(this);
        s2.incidentFaces.Add(this);
        s3.incidentFaces.Add(this);
        e1.incidentFaces.Add(this);
        e2.incidentFaces.Add(this);
        e3.incidentFaces.Add(this);
    }
    public void RecalculateNormal()
    {
        normal = Vector3.Cross(s2.p - s1.p, s3.p - s1.p).normalized;
    }
    public void CalculateEdges()
    {
        e1 = new Edge3D(s1, s2);
        e2 = new Edge3D(s2, s3);
        e3 = new Edge3D(s3, s1);
    }
    public bool IsVisible(Sommet3D p, float tol = 1e-6f)
    {
        float d = Vector3.Dot(normal, p.p - s1.p);
        return d > tol;
    }
    public void Flip()
    {
        Sommet3D temp = s2;
        s2 = s3;
        s3 = temp;
        RecalculateNormal();
        CalculateEdges();
    }
}




