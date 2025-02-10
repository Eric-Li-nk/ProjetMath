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
            Vector2 edgeMiddle = ((Vector2)arete.s1.p + (Vector2)arete.s2.p) * 0.5f;

            if (arete.tg != null && arete.td != null)
            {
                // interne
                Vector2 center1 = circumcenters[arete.tg];
                Vector2 center2 = circumcenters[arete.td];
                if (Vector2.Distance(center1, center2) > 0.001f)
                {
                    _voronoiEdges.Add((center1, center2));
                }
                else if (arete.tg != null || arete.td != null)
                {
                    //Externe
                    Triangle triangle = arete.tg ?? arete.td;
                    Vector2 center = circumcenters[triangle];
                    Vector2 direction = (edgeMiddle - center).normalized;
                    _voronoiEdges.Add((center, center + direction * EDGE_LENGTH));
                }
            }
        }

            // regions
            foreach (var sommet in sommets)
        {
            var incidentTriangles = triangles.Where(t => t.sommets.Contains(sommet)).ToList();
            if (incidentTriangles.Count > 0)
            {
                var regionPoints = incidentTriangles.Select(t => circumcenters[t]).ToList();
                SortPointsClockwise(regionPoints, (Vector2)sommet.p);
                _voronoiRegions[sommet] = regionPoints;
            }
        }
    }

    private void SortPointsClockwise(List<Vector2> points, Vector2 center)
    {
        points.Sort((a, b) =>
        {
            float angleA = Mathf.Atan2(a.y - center.y, a.x - center.x);
            float angleB = Mathf.Atan2(b.y - center.y, b.x - center.x);
            return angleA.CompareTo(angleB);
        });
    }

    public List<(Vector2, Vector2)> GetVoronoiEdges() => _voronoiEdges;
    public Dictionary<Sommet, List<Vector2>> GetVoronoiRegions() => _voronoiRegions;
    public List<Vector2> GetCenters() => _centers;
}