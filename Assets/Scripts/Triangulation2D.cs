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
    
    private List<Sommet> _sommets;
    private List<Arete> _aretes;
    private List<Triangle> _triangles;

    private void Start()
    {
        // DEBUG
        AddPoint(new Vector3(0,-2,0));
        AddPoint(new Vector3(0,1,0));
        AddPoint(new Vector3(0,3,0));
        AddPoint(new Vector3(1,3,0));
        AddPoint(new Vector3(0,-3,0));
        // DEBUG
    }

    private void Update()
    {
        if ( Input.GetKeyDown(KeyCode.Mouse0) && !isMouseOverUI() )
        {
            AddPoint();
            if (_pointListPosition.Count > 2)
                CalculateConvexHull(algoIndex);
        }
    }

    
    // Cours 4
    public void Triangulate2DIncremental()
    {
        if (_pointListPosition.Count < 3)
        {
            Debug.LogError("Pas assez de points !");
            return;
        }
        
        List<Vector3> pList = new List<Vector3>(_pointListPosition);

        _sommets = new List<Sommet>();
        _aretes = new List<Arete>();
        _triangles = new List<Triangle>();
        // C'est possible d'utiliser l'un des algorithm pour calculer l'enveloppe convexe mais c'est plus coûteux, on calculera l'enveloppe convexe par incrémentation
        // On utilisera une liste d'arete plutot qu'un liste de sommet, plus simple
        List<Arete> _convexhull = new List<Arete>(); 

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
            _convexhull.Add(a);
            _aretes.Add(a);
            s.a = a;
            i++;
        }

        List<int> indices = new List<int>();

        s = new Sommet(i, pList[i]);
        _sommets.Add(s);
        int initialAretesCount = _aretes.Count;
        for (int j = 0; j < initialAretesCount; ++j)
        {   
            CreateTriangle2D(_triangles, _aretes, _aretes[j], s);
            
            // On ajoute les index des sommets dans le sens horaire
            indices.Add(i);
            indices.Add(_aretes[j].s1.index);
            indices.Add(_aretes[j].s2.index);
        }
        
        // Correction du problème évoqué plus haut
        // On crée un triangle initiale s'il n'y a pas encore de triangle
        if (_aretes.Count <= 0)
        {
            Arete a = new Arete(_sommets[i - 1], _sommets[i]);
            _aretes.Add(a);
            _sommets[i - 1].a = a;
            _sommets[i].a = a;
            i++;
            s = new Sommet(i, pList[i]);
            _sommets.Add(s);
            CreateTriangle2D(_triangles, _aretes, a, s);
            bool areteLeftFree = a.tg == null;
            if (!areteLeftFree)
            {
                indices.Add(a.s1.index);
                indices.Add(i);
                indices.Add(a.s2.index);
            }
            else
            {
                indices.Add(i);
                indices.Add(a.s1.index);
                indices.Add(a.s2.index);
            }
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
                bool areteLeftFree = arete.tg == null;
                if (arete.HasInFront2D(s))
                {
                    CreateTriangle2D(_triangles, _aretes, arete, s);
                    
                    if (!areteLeftFree)
                    {
                        indices.Add(i);
                        indices.Add(arete.s1.index);
                        indices.Add(arete.s2.index);
                    }
                    else
                    {
                        indices.Add(arete.s1.index);
                        indices.Add(i);
                        indices.Add(arete.s2.index);
                    }
                }
            }
            i++;
        }
        
        // CETTE VALEUR N'EST PAS UTILISÉE
        // un peu compliqué de colorer individuellement les triangles sans retoucher toucher aux shaders / faire dupliquer les vertices
        // On affectera une couleur random aux sommet pour que la triangulation soit plus visible (au lieu d'avoir un mur blanc)
        Color[] colors = new Color[_pointListPosition.Count];
        for (int k = 0; k < colors.Length; ++k)
            colors[k] = new Color(Random.value, Random.value, Random.value);

        _meshFilter.mesh.vertices = pList.ToArray();        
        _meshFilter.mesh.colors = colors;
        _meshFilter.mesh.triangles = indices.ToArray();
        _meshFilter.mesh.RecalculateNormals();
    }
    // FOOTNOTE: 

    
    // Création d'un triangle pour la Triangulation 2D incrémentale
    // On assume que l'arete est toujours à gauche du sommet
    private void CreateTriangle2D(List<Triangle> triangles, List<Arete> aretes, Arete a1, Sommet s3)
    {
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
        s3.a = a2;

        // Méthode obsolète si on se contente juste du sens et non de où le sens commence
        // Affectation strict dans le sens trigonométrique (Prend le premier arête en partant de Vecteur3.right en tournant vers le sens trigo,
        // l'arête doit apparaître en entier (1 point en haut de vecteur3.right et 1 point en bas de vecteur.right sera dernier dans la liste))

        // On affecte les aretes au triangle dans le sens trigonométrique
        // Pour trier les aretes, on prend la valeur maximum des angles entre les points de l'arete et le barycentre et on les range dans l'ordre croissant
        Vector3 b = GetBarycenter(a1.s1.p, a1.s2.p, s3.p);
        Arete[] aList = { a1, a2, a3 };
        aList = aList.OrderBy(a => Mathf.Max(get360Angle(Vector3.right, a.s1.p - b),
            get360Angle(Vector3.right, a.s2.p - b))).ToArray();

        Triangle res = new Triangle(aList[0], aList[1], aList[2]);

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