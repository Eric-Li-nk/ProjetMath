using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathStruct
{
    
}

public class Sommet
{
    public int index;
    public Vector3 p { get; }
    public Arete a { get; set; }

    public Sommet(int index, Vector3 p)
    {
        this.index = index;
        this.p = p;
    }
}

public class Arete
{    
    // Arête orienté: s1 -> s2
    public Sommet s1 { get; set; }
    public Sommet s2 { get; set; }
    public Triangle tg { get; set; }
    public Triangle td { get; set; }

    public Arete(Sommet s1, Sommet s2)
    {
        this.s1 = s1;
        this.s2 = s2;
    }

    public bool isEqual(Sommet s3, Sommet s4)
    {
        return s1.p == s3.p && s2.p == s4.p || s1.p == s4.p && s2.p == s3.p;
    }

    public bool isInConvexHull()
    {
        return tg == null && td != null || tg != null && td == null;
    }

    public bool isInsideFigure()
    {
        return tg != null && td != null;
    }

    public bool HasInFront2D(Sommet s)
    {
        if (!isInConvexHull())
            return false;
        
        Vector3 point = s.p;
        Vector3 a = Vector3.zero;
        Vector3 b = Vector3.zero;
        if (tg != null)
        {
            a = s1.p;
            b = s2.p;
        }
        else if (td != null)
        {
            a = s2.p;
            b = s1.p;
        }
        else
        {
            Debug.LogError("Cet arête ne contient pas de triangle !!!");
        }
        Vector3 temp = a - b;
        Vector3 normal = new Vector3(-temp.y, temp.x, 0);
        //Debug.DrawLine((a+b)/2,(a+b)/2+normal, Color.blue, 90);
        return Vector3.Dot(normal, point - a) + Vector3.Dot(normal, point - b) > 0;
    }
    
}

public class Triangle
{
    public static int counter;
    public int index;
    public Sommet[] sommets;
    public Arete[] aretes;

    public Triangle(Sommet[] sommets, Arete[] aretes)
    {
        this.index = counter;
        this.sommets = sommets;
        this.aretes = aretes;
        counter++;
    }

    public int FindAreteIndex(Arete a)
    {
        for (int i = 0; i < aretes.Length; i++)
        {
            if (aretes[i] == a)
                return i;
        }
        return -1;
    }

    public Sommet GetOppositePoint(Arete a)
    {
        foreach (var s in sommets)
        {
            if (s != a.s1 && s != a.s2)
                return s;
        }
        Debug.LogError("Pas trouvé : " + sommets[0].index + " " + sommets[1].index + " " + sommets[2].index + " ");
        return null;
    }
}