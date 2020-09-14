using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour {
    public float mouseSensitivity = 100.0f;

    public Transform playerBody;

    float xRotation = 0.0f;

    Portal[] portals;

    private void Awake() {
        portals = FindObjectsOfType<Portal>();
    }

    private void OnPreCull() {
        for (int i = 0; i < portals.Length; i++) {
            portals[i].PrePortalRender();
        }
        for (int i = 0; i < portals.Length; i++) {
            portals[i].Render();
        }
        for (int i = 0; i < portals.Length; i++) {
            portals[i].PostPortalRender();
        }
    }

    // Start is called before the first frame update
    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update() {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
