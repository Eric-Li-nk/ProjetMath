using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Voronoi2D
{
    private List<Vector2> _centers = new();
    private List<(Vector2, Vector2)> _voronoiEdges = new();
    private Dictionary<Sommet, List<Vector2>> _voronoiRegions = new();
    private const float EDGE_LENGTH = 10f;

    private bool IsDegenerate(List<Sommet> sommets)
    {
        if (sommets.Count < 3) return true;

        Vector2 direction = ((Vector2)sommets[1].p - (Vector2)sommets[0].p).normalized;
        for (int i = 2; i < sommets.Count; i++)
        {
            Vector2 currentDir = ((Vector2)sommets[i].p - (Vector2)sommets[0].p).normalized;
            if (!Mathf.Approximately(Mathf.Abs(Vector2.Dot(direction, currentDir)), 1f))
                return false;
        }
        return true;
    }

    private void GenerateParallelVoronoi(List<Arete> aretes)
    {
        foreach (var arete in aretes)
        {
            Vector2 midpoint = ((Vector2)arete.s1.p + (Vector2)arete.s2.p) * 0.5f;
            Vector2 perpendicular = Vector2.Perpendicular((Vector2)arete.s2.p - (Vector2)arete.s1.p).normalized;
            _voronoiEdges.Add((midpoint, midpoint + perpendicular * EDGE_LENGTH));
        }
    }

    public void GenerateVoronoi(List<Triangle> triangles, List<Arete> aretes, List<Sommet> sommets)
    {
        _centers.Clear();
        _voronoiEdges.Clear();
        _voronoiRegions.Clear();

        if (IsDegenerate(sommets))
        {
            GenerateParallelVoronoi(aretes);
            return;
        }

        Dictionary<Triangle, Vector2> circumcenters = new();
        foreach (var triangle in triangles)
        {
            Vector2 center = GeometryUtility.CentreCercleCirconscrit(triangle);
            circumcenters[triangle] = center;
            _centers.Add(center);
        }

        foreach (var arete in aretes)
        {

            if (arete.tg != null && arete.td != null)
            {
                // interne
                Vector2 center1 = circumcenters[arete.tg];
                Vector2 center2 = circumcenters[arete.td];
                if (Vector2.Distance(center1, center2) > 0.001f)
                {
                    _voronoiEdges.Add((center1, center2));
                }
            }
            else if (arete.tg != null || arete.td != null)
            {
                //Externe
                Vector2 edgeMiddle = ((Vector2)arete.s1.p + (Vector2)arete.s2.p) * 0.5f;
                Triangle triangle = arete.tg ?? arete.td;
                Vector2 center = circumcenters[triangle];
                Vector2 direction = (edgeMiddle - center).normalized;

                //algo qui check si le centre se trouve dans un des triangles
                bool isInsideAnyTriangle = false;
                foreach (var tri in triangles)
                {
                    Vector2 a = (Vector2)tri.sommets[0].p;
                    Vector2 b = (Vector2)tri.sommets[1].p;
                    Vector2 c = (Vector2)tri.sommets[2].p;

                    float denominator = ((b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y));
                    float u = ((b.y - c.y) * (center.x - c.x) + (c.x - b.x) * (center.y - c.y)) / denominator;
                    float v = ((c.y - a.y) * (center.x - c.x) + (a.x - c.x) * (center.y - c.y)) / denominator;
                    float w = 1 - u - v;

                    if (u >= 0 && v >= 0 && w >= 0)
                    {
                        isInsideAnyTriangle = true;
                        break;
                    }
                }

                if (!isInsideAnyTriangle)
                {
                    direction = -direction;
                }

                _voronoiEdges.Add((center, center + direction * EDGE_LENGTH));
            }
        }
    }

    public List<(Vector2, Vector2)> GetVoronoiEdges() => _voronoiEdges;
}