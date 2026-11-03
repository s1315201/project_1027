using System.Collections.Generic;
using UnityEngine;

public partial class HengchunDrift
{
    int cameraMode;
    readonly string[] cameraNames={"追車視角","騎士視角","高位視角"};
    readonly List<Renderer> cameraBlockers=new List<Renderer>();
    Renderer[] riderRenderers;
    void CycleCamera(){cameraMode=(cameraMode+1)%3;}
    void DrawCameraSwitch(float height,Color colour)
    {
        if(Button("C 切換 · "+cameraNames[cameraMode],new Rect(1160,height-72,250,45)))CycleCamera();
    }
    void CacheCameraBlockers()
    {
        cameraBlockers.Clear();
        foreach(var r in courseRoot.GetComponentsInChildren<Renderer>()){
            string n=r.name;
            if(n=="Townhouse"||n.StartsWith("Vendor cart")||n=="Stainless worktop"||n=="Striped canvas"||n=="Shop awning"||n=="Hanging cloth advertisement"||n=="Illuminated stall sign"||n=="Glowing menu lightbox")cameraBlockers.Add(r);
        }
        var all=new List<Renderer>();
        foreach(var t in scooter.GetComponentsInChildren<Transform>())if(t.name.StartsWith("小語")||t.name.StartsWith("阿辰"))all.AddRange(t.GetComponentsInChildren<Renderer>());
        riderRenderers=all.ToArray();
    }
    bool CameraInsideScenery(Vector3 point)
    {
        foreach(var r in cameraBlockers){if(!r||!r.gameObject.activeInHierarchy)continue;Bounds b=r.bounds;b.Expand(.5f);if(b.Contains(point))return true;}
        return false;
    }
    bool CameraSightClear(Vector3 eye,Vector3 look)
    {
        if(CameraInsideScenery(eye))return false;
        Vector3 delta=look-eye;float length=delta.magnitude;if(length<.01f)return true;
        var ray=new Ray(eye,delta/length);
        foreach(var r in cameraBlockers){
            if(!r||!r.gameObject.activeInHierarchy)continue;var b=r.bounds;b.Expand(.3f);
            if(b.IntersectRay(ray,out float hit)&&hit<length-.55f)return false;
        }
        return true;
    }
    Vector3 SafeCameraPosition(Vector3 desired,Vector3 look,bool failure)
    {
        if(CameraSightClear(desired,look))return desired;
        // Stay on the road instead of orbiting seven metres sideways into a food stall.
        foreach(float offset in new[]{-6f,6f,-10f,10f,0f}){
            Vector3 candidate=CoursePosition(Mathf.Clamp(distance+offset,0,courseLength))+Vector3.up*(failure?4.2f:3.8f);
            if(CameraSightClear(candidate,look))return candidate;
        }
        Vector3 overhead=CoursePosition(distance)+Vector3.up*7;
        return CameraInsideScenery(overhead)?CoursePosition(distance)+Vector3.up*11:overhead;
    }
    void UpdateRideCamera(float dt)
    {
        bool riding=phase==Phase.Ready||phase==Phase.Ride||phase==Phase.Correct;
        bool firstPerson=riding&&cameraMode==1;
        if(riderRenderers!=null)foreach(var r in riderRenderers)if(r)r.enabled=!firstPerson;
        var heading=Quaternion.Euler(0,rideHeading,0);
        Vector3 desired,look;float fov;
        if(riding){
            if(firstPerson){desired=scooter.position+heading*new Vector3(0,1.98f,.68f);look=desired+heading*new Vector3(0,-.1f,12);fov=72;}
            else if(cameraMode==2){desired=scooter.position+heading*new Vector3(0,8,-7);look=scooter.position+heading*new Vector3(0,.7f,3);fov=65;}
            else{desired=scooter.position+heading*new Vector3(0,3.1f,-5.4f);look=scooter.position+heading*new Vector3(0,1.3f,2);fov=68+speed*.2f;}
        }else if(phase==Phase.Result&&!crashed&&distance>=courseLength){
            desired=scooter.position+heading*new Vector3(0,3.8f,-12);look=scooter.position+Vector3.up*2.8f;fov=60;
        }else if(phase==Phase.Replay){
            desired=scooter.position+scooter.rotation*new Vector3(0,3.5f,-6);look=scooter.position+Vector3.up;fov=60;
        }else{
            desired=CoursePosition(Mathf.Max(0,distance-7))+Vector3.up*4.2f;look=scooter.position+Vector3.up*.75f;fov=62;
        }
        cam.transform.position=SafeCameraPosition(desired,look,!riding);
        cam.transform.LookAt(look);
        if(riding)cam.transform.rotation*=Quaternion.Euler(0,0,naturalBank*.06f);
        cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,fov,1-Mathf.Exp(-dt*5));
    }
}
