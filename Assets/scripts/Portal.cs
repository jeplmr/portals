using System.Collections; 
using System.Collections.Generic; 
using UnityEngine;


public class Portal : MonoBehaviour
{
    public Portal linkedPortal; 
    public MeshRenderer screen; 
    Camera playerCam; 
    Camera portalCam; 
    RenderTexture viewTexture; 

    private List<PortalTraveller> trackedTravellers; 

    void Awake(){
        playerCam = Camera.main; 
        portalCam = GetComponentInChildren<Camera>(); 
        portalCam.enabled = false; 
        trackedTravellers = new List<PortalTraveller>(); 
    }
    
    void Update(){
        CreateViewTexture(); 
        Render(); 
    }

    void LateUpdate(){
        for(int i = 0; i < trackedTravellers.Count; i++){
            PortalTraveller traveller = trackedTravellers[i];
            Transform travellerT = traveller.transform;

            Vector3 offsetFromPortal = travellerT.position - transform.position; 
            int portalSide = System.Math.Sign(Vector3.Dot(offsetFromPortal, transform.forward)); 
            int portalSideOld = System.Math.Sign(Vector3.Dot(traveller.previousOffsetFromPortal, transform.forward)); 
            //Teleport the traveller if it has crossed from one side of the portal to the other
            if(portalSide != portalSideOld){
                var m = linkedPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * travellerT.localToWorldMatrix; 
                traveller.Teleport(transform, linkedPortal.transform, m.GetColumn(3), m.rotation); 

                linkedPortal.OnTravellerEnterPortal(traveller);
                trackedTravellers.RemoveAt(i);
                i--;  
            } else {
                traveller.previousOffsetFromPortal = offsetFromPortal; 
            }
        }
    }

    void OnTravellerEnterPortal(PortalTraveller traveller){
        if(!trackedTravellers.Contains (traveller)){
            traveller.EnterPortalThreshold(); 
            traveller.previousOffsetFromPortal = traveller.transform.position - transform.position; 
            trackedTravellers.Add(traveller); 
        }
    }

    
    void OnTriggerEnter(Collider other){
        var traveller = other.GetComponent<PortalTraveller>();
        if(traveller){
            OnTravellerEnterPortal(traveller); 
        }
    }

    void OnTriggerExit(Collider other){
        var traveller = other.GetComponent<PortalTraveller>();
        if(traveller && trackedTravellers.Contains (traveller)){
            traveller.ExitPortalThreshold(); 
            trackedTravellers.Remove(traveller); 
        }   
    }

    void CreateViewTexture(){
        if(viewTexture == null || viewTexture.width != Screen.width || viewTexture.height != Screen.height){
            if(viewTexture != null){
                viewTexture.Release(); 
            }
            viewTexture = new RenderTexture(Screen.width, Screen.height, 0);
            portalCam.targetTexture = viewTexture; 
            linkedPortal.screen.material.SetTexture("_MainTex", viewTexture); 
        }
    }

    static bool VisibleFromCamera(Renderer renderer, Camera camera){
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds); 
    }

    //Called just before player camera is rendered
    public void Render(){
        if(!VisibleFromCamera(linkedPortal.screen, playerCam)){
            return; 
        }
        screen.enabled = false; 
        CreateViewTexture(); 

        //Make portal cam position and rotation the same relative to this portal as player cam relative to linked portal
        var m = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix * playerCam.transform.localToWorldMatrix; 
        portalCam.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation); 

        //render the camera
        portalCam.Render(); 
        screen.enabled = true; 
    }

}
