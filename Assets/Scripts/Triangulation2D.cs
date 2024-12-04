using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class Triangulation2D : MonoBehaviour
{

    [SerializeField] private Camera _camera;
    [SerializeField] private GameObject _pointPrefab;

    private List<Vector3> _pointListPosition = new List<Vector3>();

    private void Start()
    {

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            AddPoint();
            List<Vector3> P = JarvisAlgorithm();

            for (int i = 1; i < P.Count; ++i)
            {
                Debug.DrawLine(P[i-1], P[i], Color.black, 500);
            }
            Debug.DrawLine(P[0], P[^1], Color.black, 500);
        }
    }

    private void AddPoint()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 mousePosToWorld = _camera.ScreenToWorldPoint(mousePosition);
        mousePosToWorld.z = 0;
        Instantiate(_pointPrefab, mousePosToWorld, Quaternion.identity);
        _pointListPosition.Add(mousePosToWorld);
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
    
}
