using System.Collections.Generic;
using UnityEngine;

public class VoronoiAnimator : MonoBehaviour
{
    [SerializeField] private Triangulation2D _triangulation;
    [SerializeField] private float _movementSpeed = 1f;
    [SerializeField] private float _radius = 0.5f;

    private float _time;
    private bool _isAnimating = false;
    private List<Vector3> _originalPositions = new List<Vector3>();
    private List<GameObject> _pointObjects = new List<GameObject>();
    private List<Sommet> _points = new List<Sommet>();

    private void Update()
    {
        if (!_isAnimating) return;

        _time += Time.deltaTime;

        List<Sommet> updatedPoints = new List<Sommet>();

        for (int i = 0; i < _points.Count; i++)
        {
            Vector3 originalPos = _originalPositions[i];

            //mouvement circulaire
            float angle = _time * _movementSpeed + (2 * Mathf.PI * i / _points.Count);
            Vector3 newPos = originalPos + new Vector3(
                Mathf.Cos(angle) * _radius,
                Mathf.Sin(angle) * _radius,
                0
            );

            //update gameobjects et structures
            if (i < _pointObjects.Count)
            {
                _pointObjects[i].transform.position = newPos;
            }

            Sommet newSommet = new Sommet(_points[i].index, newPos);
            newSommet.aretes = new List<Arete>(_points[i].aretes);
            updatedPoints.Add(newSommet);
        }

        //update les positions dans triangulation2D
        _points = updatedPoints;

        _triangulation._pointListPosition.Clear();
        foreach (var point in _points)
        {
            _triangulation._pointListPosition.Add(point.p);
        }

        //recaclcul des algos
        _triangulation.Triangulate2DIncremental();
        _triangulation.DelaunayAreteFlipping();
        _triangulation.GenerateAndDrawVoronoi();
    }

    public void ToggleAnimation()
    {
        _isAnimating = !_isAnimating;

        if (_isAnimating)
        {
            Debug.Log("Starting animation");
            _originalPositions.Clear();
            _points.Clear();
            _pointObjects.Clear();

            foreach (Transform child in _triangulation._pointListTransform)
            {
                _pointObjects.Add(child.gameObject);
                _originalPositions.Add(child.position);
            }

            foreach (var sommet in _triangulation._sommets)
            {
                var newSommet = new Sommet(sommet.index, sommet.p);
                newSommet.aretes = new List<Arete>(sommet.aretes);
                _points.Add(newSommet);
            }
        }
        else
        {
            Debug.Log("Ending animation");

            for (int i = 0; i < _pointObjects.Count; i++)
            {
                _pointObjects[i].transform.position = _originalPositions[i];
            }

            List<Sommet> resetPoints = new List<Sommet>();

            //Récupérer les positions initiales
            for (int i = 0; i < _points.Count; i++)
            {
                var newSommet = new Sommet(_points[i].index, _originalPositions[i]);
                newSommet.aretes = new List<Arete>(_points[i].aretes);
                resetPoints.Add(newSommet);
            }

            _points = resetPoints;

            _triangulation._pointListPosition.Clear();
            foreach (var point in _points)
            {
                _triangulation._pointListPosition.Add(point.p);
            }

            _triangulation.Triangulate2DIncremental();
            _triangulation.DelaunayAreteFlipping();
            _triangulation.GenerateAndDrawVoronoi();
        }
    }
}