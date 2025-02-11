using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointCloudGenerator3D : MonoBehaviour
{
    [Header("Referencias")]
    public Camera cam;
    public GameObject pointPrefab;
    public Transform pointsParent;

    [Header("Par�metros de creaci�n de puntos")]
    [Tooltip("Distancia desde la c�mara para definir el plano frontal de creaci�n.")]
    public float creationDistance = 10f;

    [Header("Par�metros de c�mara")]
    [Tooltip("Sensibilidad de la rotaci�n al usar el bot�n derecho.")]
    public float rotationSpeed = 3f;
    [Tooltip("Velocidad de traslaci�n (panning) al usar el bot�n del medio.")]
    public float panSpeed = 0.5f;
    [Tooltip("Velocidad de zoom con el scroll.")]
    public float zoomSpeed = 5f;

    // Variables para almacenar la orientaci�n de la c�mara.
    private float yaw;
    private float pitch;

    // Variables para la creaci�n de puntos.
    private bool isCreatingPoint = false;
    private Vector3 initialWorldPos;
    private Vector3 initialMousePos;
    // Plano que se definir� al iniciar la creaci�n del punto.
    private Plane creationPlane;

    void Start()
    {
        // Inicializa los �ngulos con la rotaci�n actual de la c�mara.
        yaw = cam.transform.eulerAngles.y;
        pitch = cam.transform.eulerAngles.x;
    }

    void Update()
    {
        // --- Rotaci�n de la c�mara con bot�n derecho ---
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * rotationSpeed;
            pitch -= mouseY * rotationSpeed;
            // Limita el pitch para evitar rotaciones inc�modas.
            pitch = Mathf.Clamp(pitch, -80f, 80f);

            cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // --- Movimiento de la c�mara en el plano frontal con bot�n del medio (panning) ---
        if (Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Mueve la c�mara en el plano frontal (definido por cam.transform.right y cam.transform.up)
            Vector3 move = (-mouseX * panSpeed * cam.transform.right) + (-mouseY * panSpeed * cam.transform.up);
            cam.transform.position += move;
        }

        // --- Zoom y deszoom con el scroll ---
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.001f)
        {
            // Se mueve la c�mara a lo largo de su eje forward
            cam.transform.position += cam.transform.forward * scrollInput * zoomSpeed;
        }

        // --- Creaci�n de puntos sobre el plano frontal con bot�n izquierdo ---
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            isCreatingPoint = true;
            initialMousePos = Input.mousePosition;

            // Se define el plano frontal:
            // - Normal: la direcci�n a la que mira la c�mara.
            // - Punto de paso: a "creationDistance" unidades frente a la c�mara.
            creationPlane = new Plane(cam.transform.forward, cam.transform.position + cam.transform.forward * creationDistance);

            // Calcula la intersecci�n del rayo con el plano.
            Ray ray = cam.ScreenPointToRay(initialMousePos);
            if (creationPlane.Raycast(ray, out float enter))
            {
                initialWorldPos = ray.GetPoint(enter);
            }
        }

        if (Input.GetMouseButtonUp(0) && isCreatingPoint && !EventSystem.current.IsPointerOverGameObject())
        {
            // Al soltar el bot�n izquierdo se vuelve a proyectar el rayo en el mismo plano frontal.
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (creationPlane.Raycast(ray, out float enter))
            {
                Vector3 finalWorldPos = ray.GetPoint(enter);

                // Se crea el punto en la posici�n final.
                GameObject newPoint = Instantiate(pointPrefab, finalWorldPos, Quaternion.identity, pointsParent);
                newPoint.name = "Point " + pointsParent.childCount;
            }

            isCreatingPoint = false;
        }
    }

    // (Opcional) Puedes conservar tambi�n m�todos para generar puntos aleatorios, etc.
}
