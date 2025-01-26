using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

public class Triangulation2D : MonoBehaviour
{

    [SerializeField] private Camera _camera;
    [SerializeField] private GameObject _pointPrefab;
    // On rangera les points générés dans un gameobject pour que ça soit plus lisible
    [SerializeField] private Transform _pointListTransform;

    [SerializeField] private LineRenderer _lineRenderer;

    [SerializeField] private Transform barycenter;

    private List<Vector3> _pointListPosition = new List<Vector3>();

    public int algoIndex = 0;

    private void Start()
    {

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

    private void AddPoint()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 mousePosToWorld = _camera.ScreenToWorldPoint(mousePosition);
        mousePosToWorld.z = 0;

        if (_pointListPosition.Contains(mousePosToWorld))
        {
            Debug.Log("Ce Point existe déjà");
            return;
        }
        
        GameObject point = Instantiate(_pointPrefab, mousePosToWorld, Quaternion.identity, _pointListTransform);
        point.name = "Point " + _pointListPosition.Count;
        _pointListPosition.Add(mousePosToWorld);
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

    public void ChangeAlgorithm(int value)
    {
        algoIndex = value;
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
    // TO DO: Vérifier si un point est à l'intérieur d'abord
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

    // With a lower than b
    private bool isInFront(Vector3 a, Vector3 b, Vector3 point)
    {
        
        Vector3 temp = a - b;
        Vector3 normal = new Vector3(-temp.z, temp.y, temp.x);
        //Debug.DrawLine((a+b)/2,(a+b)/2+normal, Color.blue, 90);
        return Vector3.Dot(normal, point - a) + Vector3.Dot(normal, point - b) > 0;
    }

    private bool sameAbscisse(Vector3 a, Vector3 b)
    {
        return a.z == b.z;
    }

    private bool isMouseOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
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