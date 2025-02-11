using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class Triangulation2D : MonoBehaviour
{

    [SerializeField] private Camera _camera;
    [SerializeField] private GameObject _pointPrefab;
    
    // On rangera les points générés dans un gameobject pour que ça soit plus lisible
    [SerializeField] private Transform _pointListTransform;

    [SerializeField] private LineRenderer _lineRenderer;

    [SerializeField] private MeshFilter _meshFilter;

    // Helper : to see the barycenter
    [SerializeField] private Transform barycenter;

    private List<Vector3> _pointListPosition = new ();

    public int algoIndex = 0;
    private bool usingDelaunay = true; // A CHANGER A FALSE
    
    private List<Sommet> _sommets;
    private List<Arete> _aretes;
    private List<Triangle> _triangles;
    
    [SerializeField] private LineRenderer _voronoiLineRenderer;
    private Voronoi2D _voronoiDiagram = new();
    
    private void Awake()
    {
        _sommets = new List<Sommet>();
        _aretes = new List<Arete>();
        _triangles = new List<Triangle>();
    }

    private void Start()
    {
        // DEBUG
        /*
        AddPoint(new Vector3(0,-2,0));
        AddPoint(new Vector3(0,1,0));
        AddPoint(new Vector3(0,3,0));
        AddPoint(new Vector3(0,-3,0));
        AddPoint(new Vector3(1,3,0));

        foreach (var p in _pointListPosition)
        {
            NoyauDelaunayAddPoint2D(p);
        }
        */
        // DEBUG
    }

    private void Update()
    {
        if ( Input.GetKeyDown(KeyCode.Mouse0) && !isMouseOverUI() )
        {
            RaycastHit raycastHit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out raycastHit))
            {
                GameObject point = raycastHit.collider.gameObject;
                if (usingDelaunay)
                    NoyauDelaunayDeletePoint2D(point.transform.position);
                Destroy(point);
            }
            else
            {
                AddPoint();
                if (!usingDelaunay && _pointListPosition.Count > 2)
                    CalculateConvexHull(algoIndex);
                if (usingDelaunay)
                    NoyauDelaunayAddPoint2D(_pointListPosition[^1]);
            }
        }
    }

    
    // Cours 4
    public void Triangulate2DIncremental()
    {
        // On réinitialise le nombre de triangles
        Triangle.counter = 0;
        
        List<Vector3> pList = new List<Vector3>(_pointListPosition);
        List<int> indices = new List<int>();
        _sommets = new List<Sommet>();
        _aretes = new List<Arete>();
        _triangles = new List<Triangle>();
        
        if (_pointListPosition.Count < 3)
        {
            Debug.LogError("Pas assez de points !");
            return;
        }
        
        pList = pList.OrderBy(value => value.x).ThenBy(value => value.y).ToList();
        
        int i = 1;
        Sommet s = new Sommet(0, pList[0]);
        _sommets.Add(s);
        
        // 2)
        // Problème rencontré, le cours ne prend pas en compte l'état où il n'y a pas 2 sommet initials aux même abscisse et différents ordonnée
        // 2.a)
        
        while (pList[i].x == pList[i-1].x)
        {
            s = new Sommet(i, pList[i]);            
            _sommets.Add(s);
            Arete a = new Arete(_sommets[i - 1], _sommets[i]);
            _aretes.Add(a);
            i++;
        }

        s = new Sommet(i, pList[i]);
        _sommets.Add(s);
        int initialAretesCount = _aretes.Count;
        for (int j = 0; j < initialAretesCount; ++j)
        {   
            CreateTriangle2D(_aretes[j], s);
            // On ajoute les index des sommets dans le sens horaire
            indices.AddRange(_triangles[0].GetIndices());
        }
        
        // Correction du problème évoqué plus haut
        // On crée un triangle initiale s'il n'y a pas encore de triangle
        if (_triangles.Count == 0)
        {
            Arete a = new Arete(_sommets[i - 1], _sommets[i]);
            _aretes.Add(a);
            i++;
            s = new Sommet(i, pList[i]);
            _sommets.Add(s);
            CreateTriangle2D(a, s);
            indices.AddRange(_triangles[0].GetIndices());
        }

        i++;

        // 3)
        
        while ( i < _pointListPosition.Count)
        {
            s = new Sommet(i, pList[i]);
            _sommets.Add(s);
            initialAretesCount = _aretes.Count;
            for (int j = 0; j < initialAretesCount; ++j)
            {
                Arete arete = _aretes[j];
                if (arete.HasInFront2D(s))
                {
                    CreateTriangle2D(arete, s);
                    indices.AddRange(_triangles[^1].GetIndices());
                }
            }
            i++;
        }
        
        _meshFilter.mesh.vertices = pList.ToArray();
        _meshFilter.mesh.triangles = indices.ToArray();
        _meshFilter.mesh.RecalculateNormals();
        
        // Debug: Pour voir si les triangle sont bien créer avec les arêtes dans l'ordre trigonométrique (r à g à b)
        //StartCoroutine(DebugTriangle());
    }
    // FOOTNOTE: Prendre en compte les points mit dans les triangles est possible mais ce sera plus une triangulation incrémentale 2D
     
    // Cours 4 p30
    public void DelaunayAreteFlipping()
    {
        // Liste des arêtes initiales
        List<Arete> aretes = new List<Arete>(_aretes);
        
        while (aretes.Count > 0)
        {
            Arete arete = aretes[0];
            aretes.Remove(arete);
            if (!IsLocalDelaunay(arete))
            {
                
                Triangle t2 = arete.tg;
                Triangle t1 = arete.td;
                
                Sommet s1 = arete.s1;
                Sommet s2 = arete.s2;
                
                Sommet s3 = t2.GetOppositePoint(arete);
                Sommet s4 = t1.GetOppositePoint(arete);
                
                int temp = t2.FindAreteIndex(arete);                
                Arete a3 = t2.aretes[(temp + 1) % 3];
                Arete a2 = t2.aretes[(temp + 2) % 3];

                temp = t1.FindAreteIndex(arete);
                Arete a1 = t1.aretes[(temp + 1) % 3];
                Arete a4 = t1.aretes[(temp + 2) % 3];
                
                arete.s1 = s3;
                arete.s2 = s4;
                
                t1.sommets = new []{ s3, s1, s4 };
                t1.aretes = new[] { arete, a2, a1 };

                t2.sommets = new[] { s3, s4, s2 };
                t2.aretes = new[] { arete, a4, a3 };

                s1.aretes.Remove(arete);
                s2.aretes.Remove(arete);
                s3.aretes.Add(arete);
                s4.aretes.Add(arete);
                
                if (a2.s1 == s3)
                    a2.tg = t1;
                else
                    a2.td = t1;
                if (a4.s1 == s4)
                    a4.tg = t2;
                else
                    a4.td = t2;

                aretes.AddRange(new []{a1, a2, a3, a4});

                List<int> indices = new List<int>(_meshFilter.mesh.triangles);
                
                indices[t1.index * 3] = s3.index;
                indices[t1.index * 3 + 1] = s4.index;
                indices[t1.index * 3 + 2] = s1.index;
                
                indices[t2.index * 3] = s3.index;
                indices[t2.index * 3 + 1] = s2.index;
                indices[t2.index * 3 + 2] = s4.index;

                _meshFilter.mesh.triangles = indices.ToArray();
                
            }
        }
        
    }

    // Si le point isolé du triangle opposé est contenu dans le cercle circonscrit du triangle initial, return false
    private bool IsLocalDelaunay(Arete a1)
    {
        if (a1.td == null || a1.tg == null)
            return true;

        Triangle t1 = a1.tg;
        Triangle t2 = a1.td;
        
        Sommet s2 = t2.GetOppositePoint(a1);

        Vector2 center1 = GeometryUtility.CentreCercleCirconscrit(t1);

        return Vector2.Distance(s2.p, center1) > Vector2.Distance(center1, t1.sommets[0].p);
    }

    private bool IsLocalDelaunay(Arete a1, Sommet s1)
    {
        Triangle t1 = a1.GetTriangleIncident();
        Vector2 center1 = GeometryUtility.CentreCercleCirconscrit(t1);
        return Vector2.Distance(s1.p, center1) > Vector2.Distance(center1, t1.sommets[0].p);
    }
    
    private bool IsLocalDelaunay(Vector2 a, Vector2 b, Vector2 c, Vector2 point)
    {
        Vector2 center1 = GeometryUtility.CentreCercleCirconscrit(a, b, c);
        return Vector2.Distance(point, center1) > Vector2.Distance(center1, a);
    }

    // Cours 4 p 31
    public void NoyauDelaunayAddPoint2D(Vector3 position)
    {
        Sommet s = new Sommet(_sommets.Count, position);
        _sommets.Add(s);
        
        // A | T ne contient pas de triangle
        if (_triangles.Count == 0)
        {
            // A-1 | T vide
            if (_sommets.Count == 1)
            {
                // Il n'y a rien ici, le point est déjà ajouté
            }
            // A-2 | T a 1 sommet
            else if (_sommets.Count == 2)
            {
                List<Sommet> temp = _sommets.OrderBy(s => s.p.x).ThenBy(s => s.p.y).ToList();
                Arete a = new Arete(temp[0], temp[1]);
                _aretes.Add(a);
            }
            // A-3 | tous les sommets colinéaires
            else
            {
                // A-3-1 | nouveau point P colinéaire à tous les sommets de T
                if (Vector3.Cross(_sommets[0].p - s.p, _sommets[1].p - s.p) == Vector3.zero)
                {
                    List<Sommet> temp = _sommets.OrderBy(s => s.p.x).ThenBy(s => s.p.y).ToList();
                    
                    // A-3-1-1 | Apparaît aux début de la chaîne
                    if (s == temp[0])
                    {
                        Arete a = new Arete(s, temp[1]);
                        _aretes.Add(a);
                    }
                    // A-3-1-2 | Apparaît à la fin de la chaîne
                    else if (s == temp[^1])
                    {
                        Arete a = new Arete(temp[^2], s);
                        _aretes.Add(a);
                    }
                    // A-3-1-3 | Apparaît au milieu de la chaîne
                    else
                    {
                        int i = temp.IndexOf(s);
                        Arete a1 = _aretes.Find(a => a.s1 == temp[i - 1]);
                        a1.s2 = s;
                        Arete a2 = new Arete(s, temp[i + 1]);
                        _aretes.Add(a2);
                    }
                }
                // A-3-2 | P n'est pas colinéaire aux sommets de T
                else
                {
                    List<int> indices = new List<int>();

                    int initialAreteCount = _aretes.Count;
                    for (int i = 0; i < initialAreteCount; i++)
                    {
                        CreateTriangle2D(_aretes[i], s);
                        indices.AddRange(_triangles[^1].GetIndices());
                    }
                    
                    Mesh mesh = _meshFilter.mesh;
                    mesh.vertices = _pointListPosition.ToArray();
                    mesh.triangles = indices.ToArray();
                    mesh.RecalculateNormals();
                }
            }
        }
        // B | T possède au moins un triangle
        else
        {
            Mesh mesh = _meshFilter.mesh;
            List<int> indices = new List<int>(mesh.triangles);
            List<Arete> L = new List<Arete>();

            bool insideTriangle = false;
            
            // Problème rencontré: Le cours ne prend pas en compte lorsque P est sur une arête
            // B-1-1 | P appartient a un triangle de T
            foreach (var triangle in _triangles)
            {
                if (triangle.ContainsPoint(s))
                {
                    insideTriangle = true;
                    L.AddRange(triangle.aretes);
                    DeleteTriangle(triangle, indices);
                    break;
                }
            }
            // B-1-2 | P hors de T
            if (!insideTriangle)
            {
                foreach (var arete in _aretes)
                {
                    if (arete.HasInFront2D(s))
                        L.Add(arete);
                }
            }

            // B-2 | tant que L non vide
            while (L.Count > 0)
            {
                Arete a = L[0];
                L.Remove(a);
                // B-2-1 | P ne respecte pas la règle de Delaunay avec une arête
                if (a.GetTriangleIncident() != null && !IsLocalDelaunay(a, s))
                {
                    Triangle t = a.GetTriangleIncident();
                    foreach (var arete in t.aretes)
                        if (arete != a)
                            L.Add(arete);
                    DeleteTriangle(t, indices);
                    DeleteArete(a);
                }
                // B-2-2 | P respecte la règle de Delaunay avec l'arête ou l'arête ne possède aucun triangle
                else
                {
                    Triangle t = CreateTriangle2D(a, s);
                    indices.AddRange(t.GetIndices());
                }
            }

            mesh.vertices = _pointListPosition.ToArray();
            mesh.triangles = indices.ToArray();
            mesh.RecalculateNormals();
        }
    }

    public void NoyauDelaunayDeletePoint2D(Vector3 position)
    {
        Mesh mesh = _meshFilter.mesh;
        List<int> indices = new List<int>(mesh.triangles);

        // A | T ne contient aucun triangles
        if (_triangles.Count == 0)
        {
            // A-1 | T a un seul sommet
            if (_sommets.Count == 1)
            {
                _sommets.Clear();
                _pointListPosition.Clear();
            }
            // A-2 | S est aux extrémités
            else
            {
                int index = _pointListPosition.IndexOf(position);
                // Premier
                if (index == 0)
                {
                    DeleteArete(_sommets[index].aretes[0]);
                    DeleteSommet(_sommets[index], indices);
                }
                // Dernier
                else if (index == _pointListPosition.Count - 1)
                {
                    _sommets.RemoveAt(index);
                    _pointListPosition.RemoveAt(index);
                }
                // Entre deux sommets
                else
                {
                    Sommet s = _sommets.Find(s => s.p == position);
                    Arete a1 = _aretes.Find(a => a.s2 == s);
                    Arete a2 = _aretes.Find(a => a.s1 == s);
                    a2.s1 = a1.s1;
                    DeleteArete(a1);
                    DeleteSommet(s, indices);
                }
            }
        }
        // B | T contient des triangles
        else
        {
            // B-1 | Suppression de toutes les arêtes et triangles lié au sommet s
            Sommet s = _sommets.Find(s => s.p == position);
            List<Arete> La1 = new List<Arete>(s.aretes);
            List<Triangle> Lt = new List<Triangle>();
            List<Arete> La2 = new List<Arete>();
            foreach (var arete in La1)
            {
                if (arete.tg != null && !Lt.Contains(arete.tg))
                    Lt.Add(arete.tg);
                if (arete.td != null && !Lt.Contains(arete.td))
                    Lt.Add(arete.td);
            }
            
            foreach(var triangle in Lt)
            {
                La2.Add(triangle.GetAreteOppose(s));
                DeleteTriangle(triangle, indices);
            }
            foreach (var arete in La1)
                DeleteArete(arete);
            
            DeleteSommet(s, indices);
            
            // B-2
            List<Sommet> uniqueSommet = new List<Sommet>();
            foreach (var arete in La2)
            {
                if (!uniqueSommet.Contains(arete.s1))
                    uniqueSommet.Add(arete.s1);
                if (!uniqueSommet.Contains(arete.s2))
                    uniqueSommet.Add(arete.s2);
            }
            
            // B-2-1 | La2 forme un polygone fermé (nb_sommets == nb_arêtes)
            if (uniqueSommet.Count == La2.Count)
            {
                while (La2.Count > 3)
                {
                    s = uniqueSommet[0];
                    List<Arete> temp = new List<Arete>(La2);
                    Arete a1 = temp.Find(a => a.s1 == s || a.s2 == s);
                    temp.Remove(a1);
                    Arete a2 = temp.Find(a => a.s1 == s || a.s2 == s);
                    Sommet s1 = a1.s1 != s ? a1.s1 : a1.s2;
                    Sommet s2 = a2.s1 != s ? a2.s1 : a2.s2;
                    List<Sommet> tempSommet = new List<Sommet>(uniqueSommet);
                    tempSommet.Remove(s);
                    tempSommet.Remove(s1);
                    tempSommet.Remove(s2);
                    bool isDelaunay = true;
                    foreach (var sommet in tempSommet)
                    {
                        if (!IsLocalDelaunay(s.p, s1.p, s2.p, sommet.p))
                        {
                            isDelaunay = false;
                            break;
                        }
                    }

                    if (isDelaunay)
                    {
                        uniqueSommet.RemoveAt(0);
                        Arete a3 = new Arete(s1, s2);
                        Triangle t = CreateTriangle2D(a3, s);
                        indices.AddRange(t.GetIndices());

                        La2.Remove(a1);
                        La2.Remove(a2);
                        La2.Add(a3);
                    }
                    else
                    {
                        Sommet temp2 = uniqueSommet[0];
                        uniqueSommet.RemoveAt(0);
                        uniqueSommet.Add(temp2);
                    }
                    
                }
                
                Arete arete = La2[0];
                Sommet smt = (La2[1].s1 != arete.s1 && La2[1].s1 != arete.s2) ? La2[1].s1 : La2[1].s2;
                Triangle t1 = CreateTriangle2D(arete, smt);
                indices.AddRange(t1.GetIndices());
            }
            // B-2-2 | La2 ne forme pas de polygone fermé
            else
            {
                while (uniqueSommet.Count > 0)
                {
                    s = uniqueSommet[0];
                    List<Arete> temp = new List<Arete>(La2);
                    Arete a1 = temp.Find(a => a.s1 == s || a.s2 == s);
                    temp.Remove(a1);
                    Arete a2 = temp.Find(a => a.s1 == s || a.s2 == s);
                    if (a2 == null)
                    {
                        uniqueSommet.RemoveAt(0);
                        continue;
                    }
                    Sommet s1 = a1.s1 != s ? a1.s1 : a1.s2;
                    Sommet s2 = a2.s1 != s ? a2.s1 : a2.s2;
                    List<Sommet> tempSommet = new List<Sommet>(uniqueSommet);
                    tempSommet.Remove(s);
                    tempSommet.Remove(s1);
                    tempSommet.Remove(s2);
                    bool isDelaunay = true;
                    foreach (var sommet in tempSommet)
                    {
                        if (!IsLocalDelaunay(s.p, s1.p, s2.p, sommet.p))
                        {
                            isDelaunay = false;
                            break;
                        }
                    }
                    if (isDelaunay)
                    {
                        uniqueSommet.RemoveAt(0);
                        Arete a3 = new Arete(s1, s2);
                        Triangle t = CreateTriangle2D(a3, s);
                        indices.AddRange(t.GetIndices());

                        La2.Remove(a1);
                        La2.Remove(a2);
                        La2.Add(a3);
                    }
                    else
                    {
                        Sommet temp2 = uniqueSommet[0];
                        uniqueSommet.RemoveAt(0);
                        uniqueSommet.Add(temp2);
                    }
                }
            }
            mesh.triangles = indices.ToArray();
            mesh.vertices = _pointListPosition.ToArray();
            mesh.RecalculateNormals();
        }
    }

    private void DeleteTriangle(Triangle triangle, List<int> indices)
    {
        Triangle.counter--;
        foreach (var t in _triangles)
        {
            if (t.index > triangle.index)
                t.index--;
        }
        foreach (var arete in triangle.aretes)
            arete.RemoveTriangle(triangle);
        indices.RemoveRange(triangle.index * 3, 3);
        _triangles.Remove(triangle);
    }

    private void DeleteArete(Arete arete)
    {
        arete.s1.aretes.Remove(arete);
        arete.s2.aretes.Remove(arete);
        _aretes.Remove(arete);
    }

    private void DeleteSommet(Sommet sommet, List<int> indices)
    {
        int index = sommet.index;
        _sommets.Remove(sommet);
        _pointListPosition.Remove(sommet.p);
        foreach (var s in _sommets)
        {
            if (s.index > index)
                s.index--;
        }

        for (int i = 0; i < indices.Count; i++)
        {
            if (indices[i] > index)
                indices[i]--;
        }

        foreach (Transform point in _pointListTransform)
        {
            String[] name = point.name.Split(" ");
            int number = Int32.Parse(name[1]);
            if (number > index)
                point.name = name[0] + " " + (number - 1);
        }
    }

    // Création d'un triangle pour la Triangulation 2D incrémentale
    // On assume que l'arete est toujours à gauche du sommet
    private Triangle CreateTriangle2D(Arete a1, Sommet s3)
    {
        List<Triangle> triangles = _triangles;
        List<Arete> aretes = _aretes;
        
        Arete a2 = null;
        Arete a3 = null;
        // On cherche si l'arête existe déjà
        foreach (var arete in _aretes)
        {
            if (arete.isEqual(a1.s1, s3))
                a2 = arete;
            else if (arete.isEqual(a1.s2, s3))
                a3 = arete;
        }

        if (a2 == null)
        {
            a2 = new Arete(a1.s1, s3);
            aretes.Add(a2);
        }
        if (a3 == null)
        {
            a3 = new Arete(a1.s2, s3);
            aretes.Add(a3);
        }
        s3.aretes.Add(a2);
        s3.aretes.Add(a3);

        // Méthode obsolète si on se contente juste du sens et non de où le sens commence
        // Affectation strict dans le sens trigonométrique (Prend le premier arête en partant de Vecteur3.right en tournant vers le sens trigo,
        // l'arête doit apparaître en entier (1 point en haut de vecteur3.right et 1 point en bas de vecteur.right sera dernier dans la liste))
        // nope, Cette méthode ne donne pas de bon résultat, prendre la moitié de l'arête non plus
        // On affecte les aretes au triangle dans le sens trigonométrique
        // Pour trier les aretes, on prend la valeur maximum des angles entre les points de l'arete et le barycentre et on les range dans l'ordre croissant
        //aList = aList.OrderBy(a => Mathf.Max(get360Angle(Vector3.right, a.s1.p - b),
        //    get360Angle(Vector3.right, a.s2.p - b))).ToArray();
        
        // On affecte les aretes au triangle dans le sens trigonométrique
        // Pour trier les aretes, on trie les sommets dans l'ordre trigonométrique d'abord puis on séléctione les arêtes qui correspondent aux sommets dans l'ordre donnée
        Vector3 b = GetBarycenter(a1.s1.p, a1.s2.p, s3.p);
        Sommet[] sList = { a1.s1, a1.s2, s3 };
        sList = sList.OrderBy(s => get360Angle(Vector3.right, s.p - b)).ToArray();
        List<Arete> aListtemp = new List<Arete>{ a1, a2, a3 };
        Arete[] aList = new Arete[3];
        
        for (int i = 0; i < 3; i++)
        {
            aList[i] = aListtemp.Find(a => a.s1 == sList[i] && a.s2 == sList[(i + 1) % 3] || a.s2 == sList[i] && a.s1 == sList[(i + 1) % 3]);
        }

        Triangle res = new Triangle(sList, aList);

            // On affecte le triangle aux aretes
        if (IsInFront2D(a1, s3)) 
            a1.td = res;
        else 
            a1.tg = res;
        if (IsInFront2D(a2, a1.s2))
            a2.td = res;
        else
            a2.tg = res;
        if (IsInFront2D(a3, a1.s1))
            a3.td = res;
        else
            a3.tg = res;
        
        triangles.Add(res);
        return res;
    }

    private Vector3 GetBarycenter(Vector3 a, Vector3 b, Vector3 c)
    {
        return (a + b + c) / 3;
    }
    
    // orienté: a -> b
    private bool IsInFront2D(Vector3 a, Vector3 b, Vector3 point)
    {
        
        Vector3 temp = a - b;
        Vector3 normal = new Vector3(-temp.y, temp.x, 0);
        //Debug.DrawLine((a+b)/2,(a+b)/2+normal, Color.blue, 90);
        return Vector3.Dot(normal, point - a) + Vector3.Dot(normal, point - b) > 0;
    }

    private bool IsInFront2D(Arete a1, Sommet s3)
    {
        return IsInFront2D(a1.s1.p, a1.s2.p, s3.p);
    }

    private void AddPoint()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 mousePosToWorld = _camera.ScreenToWorldPoint(mousePosition);
        mousePosToWorld.z = 0;

        AddPoint(mousePosToWorld);
    }

    private void AddPoint(Vector3 position)
    {
        if (_pointListPosition.Contains(position))
        {
            Debug.Log("Ce Point existe déjà");
            return;
        }
        GameObject point = Instantiate(_pointPrefab, position, Quaternion.identity, _pointListTransform);
        point.name = "Point " + _pointListPosition.Count;
        _pointListPosition.Add(position);
    }

    private void CalculateConvexHull(int value)
    {
        List<Vector3> convexHullPoints;
        switch (value)
        {
            case 0:
                convexHullPoints = JarvisAlgorithm();
                break;
            case 1:
                convexHullPoints = GrahamScanAlgorithm();
                break;
            default:
                Debug.LogError("Not using any algorithm");
                return;
        }
        
        _lineRenderer.positionCount = convexHullPoints.Count;
        _lineRenderer.SetPositions(convexHullPoints.ToArray());
    }

    private List<Vector3> JarvisAlgorithm()
    {
        List<Vector3> pList = _pointListPosition;
        
        // Recherche du point le plus en bas à gauche
        int i_0 = 0;
        float x_min = pList[i_0].x;
        float y_min = pList[i_0].y;

        for (int k = 1; k < pList.Count; k++)
        {
            if (pList[k].x < x_min || pList[k].x == x_min && pList[k].y < y_min)
            {
                i_0 = k;
                x_min = pList[k].x;
                y_min = pList[k].y;
            }
        }

        Vector3 v = new Vector3(0, -1, 0);
        List<Vector3> P = new List<Vector3>();
        int i = i_0;
        int j;
        
        do
        {
            P.Add(pList[i]); // ajout du point pivot à P
            
            // recherche du point suivant
            // initialisation de alpha_max et l_max avec le premier point d'indice différent de i
            if (i == 0)
                j = 1;
            else
                j = 0;

            float alpha_min = Vector3.Angle(v, pList[j] - pList[i]);
            float l_max = (pList[j] - pList[i]).magnitude;
            int i_new = j;
            
            // recherche du point le plus proche en angle de la droite
            for (j = i_new+1; j < pList.Count; j++)
            {
                if (j != i)
                {
                    float alpha = Vector3.Angle(v, pList[j] - pList[i]);
                    if (alpha_min > alpha || alpha_min == alpha && l_max < (pList[j] - pList[i]).magnitude)
                    {
                        alpha_min = alpha;
                        l_max = (pList[j] - pList[i]).magnitude;
                        i_new = j;
                    }
                }
            }
            // Mise à jor du pivot et du vecteur directeur
            v = new Vector3(pList[i_new].x - pList[i].x, pList[i_new].y - pList[i].y, 0);
            i = i_new;
            
        } while (i != i_0);

        return P;
    }
    
    private List<Vector3> GrahamScanAlgorithm()
    {
        // Debug : Affichage du Barycentre
        Vector3 B = getBarycenter(_pointListPosition);
        barycenter.position = B;

        // Triage des points dans l'ordre croissant en fonction de l'angle du point avec le barycentre dans le sens trigonométrique
        
        List<float> angles = new List<float>();
        List<Vector3> pointList = new List<Vector3>(_pointListPosition);
        
        foreach (var point in _pointListPosition)
            angles.Add(get360Angle(Vector3.right, point - B));
        
        LinkedList<Vector3> linkedListPoints = new LinkedList<Vector3>();

        for (int i = 0; i < _pointListPosition.Count; ++i)
        {
            int minIndex = 0;
            for (int j = 1; j < angles.Count; ++j)
                if (angles[j] < angles[minIndex]) minIndex = j;

            linkedListPoints.AddLast(pointList[minIndex]);
            
            angles.RemoveAt(minIndex);
            pointList.RemoveAt(minIndex);
        }
        
        // Cours 3 page 18
        // Utilisation d'une liste double chainée circulaire
        // Suppression des points non convexes pour avoir l'enveloppe convexe
        
        LinkedListNode<Vector3> p0 = linkedListPoints.First;
        LinkedListNode<Vector3> pivot = p0;
        bool avance;
        do
        {
            if (get360Angle(pivot.PreviousOrLast().Value - pivot.Value, pivot.NextOrFirst().Value - pivot.Value) > 180)
            {
                pivot = pivot.NextOrFirst();
                avance = true;
            }
            else
            {
                p0 = pivot.PreviousOrLast();
                linkedListPoints.Remove(pivot);
                pivot = p0;
                avance = false;
            }
        } while (pivot != p0 || avance == false);

        return linkedListPoints.ToList();
        
    }

    private Vector3 getBarycenter(List<Vector3> pList)
    {
        Vector3 res = Vector3.zero;
        foreach (Vector3 v in pList)
            res += v;
        return res / pList.Count;
    }

    private float get360Angle(Vector3 a, Vector3 b)
    {
        float res = Vector3.SignedAngle(a, b, Vector3.forward);
        if (res < 0)
            res += 360;
        return res;
    }

    
    // Various Helper for smooth interaction
    
    private bool isMouseOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    public void ChangeAlgorithm(int value)
    {
        algoIndex = value;
    }
    
    public void ToggleDelaunay(bool val)
    {
        usingDelaunay = val;
    }

    public void DebugTriangles()
    {
        StartCoroutine(DebugTriangle());
        int[] indices = _meshFilter.mesh.triangles;
        
        for (int i = 0; i < _meshFilter.mesh.triangles.Length; i += 3)
        {
            Debug.Log(indices[i] + " " + indices[i+1] + " " + indices[i+2]);
        }
        Debug.Log(" ============================== ");
        foreach (var triangle in _triangles)
        {
            Debug.Log(triangle.index + ", sommets: " + triangle.sommets[0].index + " " + triangle.sommets[1].index + " " + triangle.sommets[2].index);
        }
    }
    
    public IEnumerator DebugTriangle()
    {
        float duration = 2;
        foreach (var res in _triangles)
        {
            Debug.DrawLine(res.aretes[0].s1.p, res.aretes[0].s2.p, Color.red, duration);
            Debug.DrawLine(res.aretes[1].s1.p, res.aretes[1].s2.p, Color.green, duration);
            Debug.DrawLine(res.aretes[2].s1.p, res.aretes[2].s2.p, Color.blue, duration);
            yield return new WaitForSeconds(duration);
        }

    }

    public void GenerateAndDrawVoronoi()
    {
        _voronoiDiagram.GenerateVoronoi(_triangles, _aretes, _sommets);

        var edges = _voronoiDiagram.GetVoronoiEdges();
        var positions = new Vector3[edges.Count * 2];

        for (int i = 0; i < edges.Count; i++)
        {
            positions[i * 2] = edges[i].Item1;
            positions[i * 2 + 1] = edges[i].Item2;
        }

        _voronoiLineRenderer.positionCount = positions.Length;
        _voronoiLineRenderer.SetPositions(positions);
    }

    public void ClearAll()
    {
        _pointListPosition.Clear();

        foreach (Transform child in _pointListTransform)
        {
            Destroy(child.gameObject);
        }

        _sommets?.Clear();
        _aretes?.Clear();
        _triangles?.Clear();

        if (_lineRenderer != null)
        {
            _lineRenderer.positionCount = 0;
        }

        if (_voronoiLineRenderer != null)
        {
            _voronoiLineRenderer.positionCount = 0;
        }

        if (_meshFilter != null && _meshFilter.mesh != null)
        {
            _meshFilter.mesh.Clear();
        }

        Triangle.counter = 0;
    }

}
    
// Ajout de méthodes d'extensions à LinkedListNode qui permet d'avoir une liste double chainée circulaire
public static class CircularLinkedList
{
    public static LinkedListNode<T> NextOrFirst<T>(this LinkedListNode<T> current)
    {
        return current.Next ?? current.List.First;
    }

    public static LinkedListNode<T> PreviousOrLast<T>(this LinkedListNode<T> current)
    {
        return current.Previous ?? current.List.Last;
    }
}