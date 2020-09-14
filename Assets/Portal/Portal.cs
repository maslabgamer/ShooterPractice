using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour {
    [Header("Main Settings")]
    public Portal linkedPortal;
    public MeshRenderer screen;
    public int recursionLimit = 5;

    Camera playerCam;
    Camera portalCam;
    RenderTexture viewTexture;

    List<PortalTraveler> trackedTravelers;
    MeshFilter screenMeshFilter;

    void Awake() {
        playerCam = Camera.main;
        portalCam = GetComponentInChildren<Camera>();
        portalCam.enabled = false; // Set to false as we want to control the camera
        trackedTravelers = new List<PortalTraveler>();
        screenMeshFilter = screen.GetComponent<MeshFilter>();
    }

    // Called after Update functions have been called
    private void LateUpdate() {
        HandleTravelers();
    }

    void HandleTravelers() {
        for (int i = 0; i < trackedTravelers.Count; i++) {
            PortalTraveler traveler = trackedTravelers[i];
            Transform travelerT = traveler.transform;
            var m = linkedPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * travelerT.localToWorldMatrix;

            Vector3 offsetFromPortal = travelerT.position - transform.position;
            int portalSide = System.Math.Sign(Vector3.Dot(offsetFromPortal, transform.forward));
            int portalSideOld = System.Math.Sign(Vector3.Dot(traveler.previousOffsetFromPortal, transform.forward));
            // Teleport the traveler if it has crossed from one side of the portal to the other
            if (portalSide != portalSideOld) {
                Vector3 positionOld = travelerT.position;
                Quaternion rotOld = travelerT.rotation;

                traveler.Teleport(transform, linkedPortal.transform, m.GetColumn(3), m.rotation);
                // Set traveler's clone to where it should be
                traveler.graphicsClone.transform.SetPositionAndRotation(positionOld, rotOld);

                // Can't rely on OnTriggerEnter/Exit to be called next frame because it depends on when FixedUpdate runs
                linkedPortal.OnTravelerEnterPortal(traveler);
                trackedTravelers.RemoveAt(i);
                i--;
            } else {
                traveler.graphicsClone.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
                traveler.previousOffsetFromPortal = offsetFromPortal;
            }
        }
    }

    void CreateViewTexture() {
        if (viewTexture == null || viewTexture.width != Screen.width || viewTexture.height != Screen.height) {
            if (viewTexture != null) {
                viewTexture.Release();
            }
            viewTexture = new RenderTexture(Screen.width, Screen.height, 0);
            // Render view from the portal camera to the view texture
            portalCam.targetTexture = viewTexture;
            // Display the view texture on the screen of the linked portal
            linkedPortal.screen.material.SetTexture("_MainTex", viewTexture);
        }
    }

    static bool VisibleFromCamera(Renderer renderer, Camera camera) {
        Plane[] frustrumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(frustrumPlanes, renderer.bounds);
    }

    // Called just beforep layer camera is rendered
    public void Render() {
        if (!VisibleFromCamera(linkedPortal.screen, playerCam)) {
            return;
        }
        CreateViewTexture();

        Matrix4x4 localToWorldMatrix = playerCam.transform.localToWorldMatrix;
        Vector3[] renderPositions = new Vector3[recursionLimit];
        Quaternion[] renderRotations = new Quaternion[recursionLimit];

        // Make portal cam position and rotation the same relative to this portal as player cam relative to the linked portal
        int startIndex = 0;
        portalCam.projectionMatrix = playerCam.projectionMatrix;
        for (int i = 0; i < recursionLimit; i++) {
            if (i > 0 && !CameraUtility.BoundsOverlap(screenMeshFilter, linkedPortal.screenMeshFilter, portalCam)) {
                // No need for recursive rendering if linked portal is not visible through this portal
                break;
            }

            localToWorldMatrix = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix * localToWorldMatrix;
            int renderOrderIndex = recursionLimit - i - 1;
            renderPositions[renderOrderIndex] = localToWorldMatrix.GetColumn(3);
            renderRotations[renderOrderIndex] = localToWorldMatrix.rotation;

            portalCam.transform.SetPositionAndRotation(renderPositions[renderOrderIndex], renderRotations[renderOrderIndex]);
            startIndex = renderOrderIndex;
        }

        // Hide screen so that camera can see through portal
        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        // Set displayMask of fragment shader to '1' to make it active and start drawing our new texture
        linkedPortal.screen.material.SetInt("displayMask", 0);

        // Render camera
        for (int i = 0; i < recursionLimit; i++) {
            portalCam.transform.SetPositionAndRotation(renderPositions[i], renderRotations[i]);
            SetNearClipPlane();
            portalCam.Render();

            if (i == startIndex) {
                linkedPortal.screen.material.SetInt("displayMask", 1);
            }
        }

        // Unhide the screen
        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    void OnTravelerEnterPortal(PortalTraveler traveler) {
        if (!trackedTravelers.Contains(traveler)) {
            traveler.EnterPortalThreshold();
            traveler.previousOffsetFromPortal = traveler.transform.position - transform.position;
            trackedTravelers.Add(traveler);
        }
    }

    void OnTriggerEnter(Collider other) {
        Debug.Log("other = " + other.name);
        var traveler = other.GetComponent<PortalTraveler>();
        if (traveler) {
            OnTravelerEnterPortal(traveler);
        }
    }

    void OnTriggerExit(Collider other) {
        var traveler = other.GetComponent<PortalTraveler>();
        if (traveler && trackedTravelers.Contains(traveler)) {
            traveler.ExitPortalThreshold();
            trackedTravelers.Remove(traveler);
        }
    }

    public void PrePortalRender() {
        foreach(var traveler in trackedTravelers) {
            UpdateSliceParams(traveler);
        }
    }

    public void PostPortalRender() {
        foreach(var traveler in trackedTravelers) {
            UpdateSliceParams(traveler);
        }
        ProtectScreenFromClipping();
    }

    // Sets the thickness of the portal screen so as not to clip the camera near plane when player goes through
    void ProtectScreenFromClipping() {
        float halfHeight = playerCam.nearClipPlane * Mathf.Tan(playerCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * playerCam.aspect;
        float dstToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, playerCam.nearClipPlane).magnitude;

        Transform screenT = screen.transform;
        bool camFacingSameDirAsPortal = Vector3.Dot(transform.forward, transform.position - playerCam.transform.position) > 0;
        screenT.localScale = new Vector3(screenT.localScale.x, screenT.localScale.y, dstToNearClipPlaneCorner);
        screenT.localPosition = Vector3.forward * dstToNearClipPlaneCorner * ((camFacingSameDirAsPortal) ? 0.5f : -0.5f);
    }

    // Use custom projection matrix to align portal camera's near clip plane with the surface of the portal
    // Note that this affects precision of the depth buffer, which can cause issues with effects like screenspace AO
    void SetNearClipPlane() {
        // Learning resource:
        // http://www.terathon.com/lengyel/Lengyel-Oblique.pdf
        Transform clipPlane = transform;
        int dot = System.Math.Sign(Vector3.Dot(clipPlane.forward, transform.position - portalCam.transform.position));

        Vector3 camSpacePos = portalCam.worldToCameraMatrix.MultiplyPoint(clipPlane.position);
        Vector3 camSpaceNormal = portalCam.worldToCameraMatrix.MultiplyVector(clipPlane.forward) * dot;
        float camSpaceDst = -Vector3.Dot(camSpacePos, camSpaceNormal);
        Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);

        // Update projection based on new clip plane
        // Calculate matrix with player cam so that player camera settings (fov, etc) are used
        portalCam.projectionMatrix = playerCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
    }

    void UpdateSliceParams(PortalTraveler traveler) {
        // Calculate slice normal
        int side = SideOfPortal(traveler.transform.position);
        Vector3 sliceNormal = transform.forward * -side;
        Vector3 cloneSliceNormal = linkedPortal.transform.forward * side;

        // Calculate slice center
        Vector3 slicePos = transform.position;
        Vector3 cloneSlicePos = linkedPortal.transform.position;

        for (int i = 0; i < traveler.originalMaterials.Length; i++) {
            traveler.originalMaterials[i].SetVector("sliceCenter", slicePos);
            traveler.originalMaterials[i].SetVector("sliceNormal", sliceNormal);

            traveler.cloneMaterials[i].SetVector("sliceCenter", cloneSlicePos);
            traveler.cloneMaterials[i].SetVector("sliceNormal", cloneSliceNormal);
        }
    }

    int SideOfPortal(Vector3 pos) {
        return System.Math.Sign(Vector3.Dot(pos - transform.position, transform.forward));
    }
}
