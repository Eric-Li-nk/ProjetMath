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

    [Header("Parámetros de creación de puntos")]
    [Tooltip("Distancia desde la cámara para definir el plano frontal de creación.")]
    public float creationDistance = 10f;

    [Header("Parámetros de cámara")]
    [Tooltip("Sensibilidad de la rotación al usar el botón derecho.")]
    public float rotationSpeed = 3f;
    [Tooltip("Velocidad de traslación (panning) al usar el botón del medio.")]
    public float panSpeed = 0.5f;
    [Tooltip("Velocidad de zoom con el scroll.")]
    public float zoomSpeed = 5f;

    // Variables para almacenar la orientación de la cámara.
    private float yaw;
    private float pitch;

    // Variables para la creación de puntos.
    private bool isCreatingPoint = false;
    private Vector3 initialWorldPos;
    private Vector3 initialMousePos;
    // Plano que se definirá al iniciar la creación del punto.
    private Plane creationPlane;

    void Start()
    {
        // Inicializa los ángulos con la rotación actual de la cámara.
        yaw = cam.transform.eulerAngles.y;
        pitch = cam.transform.eulerAngles.x;
    }

    void Update()
    {
        // --- Rotación de la cámara con botón derecho ---
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * rotationSpeed;
            pitch -= mouseY * rotationSpeed;
            // Limita el pitch para evitar rotaciones incómodas.
            pitch = Mathf.Clamp(pitch, -80f, 80f);

            cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // --- Movimiento de la cámara en el plano frontal con botón del medio (panning) ---
        if (Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Mueve la cámara en el plano frontal (definido por cam.transform.right y cam.transform.up)
            Vector3 move = (-mouseX * panSpeed * cam.transform.right) + (-mouseY * panSpeed * cam.transform.up);
            cam.transform.position += move;
        }

        // --- Zoom y deszoom con el scroll ---
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.001f)
        {
            // Se mueve la cámara a lo largo de su eje forward
            cam.transform.position += cam.transform.forward * scrollInput * zoomSpeed;
        }

        // --- Creación de puntos sobre el plano frontal con botón izquierdo ---
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            isCreatingPoint = true;
            initialMousePos = Input.mousePosition;

            // Se define el plano frontal:
            // - Normal: la dirección a la que mira la cámara.
            // - Punto de paso: a "creationDistance" unidades frente a la cámara.
            creationPlane = new Plane(cam.transform.forward, cam.transform.position + cam.transform.forward * creationDistance);

            // Calcula la intersección del rayo con el plano.
            Ray ray = cam.ScreenPointToRay(initialMousePos);
            if (creationPlane.Raycast(ray, out float enter))
            {
                initialWorldPos = ray.GetPoint(enter);
            }
        }

        if (Input.GetMouseButtonUp(0) && isCreatingPoint && !EventSystem.current.IsPointerOverGameObject())
        {
            // Al soltar el botón izquierdo se vuelve a proyectar el rayo en el mismo plano frontal.
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (creationPlane.Raycast(ray, out float enter))
            {
                Vector3 finalWorldPos = ray.GetPoint(enter);

                // Se crea el punto en la posición final.
                GameObject newPoint = Instantiate(pointPrefab, finalWorldPos, Quaternion.identity, pointsParent);
                newPoint.name = "Point " + pointsParent.childCount;
            }

            isCreatingPoint = false;
        }
    }

    // (Opcional) Puedes conservar también métodos para generar puntos aleatorios, etc.
}
