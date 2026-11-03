#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class NightMarketCameraCheck
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static NightMarketCameraCheck(){EditorApplication.update+=Run;}
    [MenuItem("Tools/Night Market/Validate Cameras and Art")]
    static void Queue(){SessionState.SetBool("NightMarket.CameraCheck",true);if(!EditorApplication.isPlaying)EditorApplication.isPlaying=true;}
    static void Capture(Camera camera,string file)
    {
        var old=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());
        camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
    }
    static void Run()
    {
        if(!SessionState.GetBool("NightMarket.CameraCheck",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        var game=UnityEngine.Object.FindAnyObjectByType<HengchunDrift>();if(!game)return;
        var type=game.GetType();var vehicle=type.GetField("scooter",Private).GetValue(game) as Transform;
        var root=type.GetField("courseRoot",Private).GetValue(game) as Transform;
        if(!vehicle||!root||type.GetField("finishFestival",Private).GetValue(game)==null)return;
        SessionState.SetBool("NightMarket.CameraCheck",false);game.enabled=false;
        Action<string,object> set=(name,value)=>type.GetField(name,Private).SetValue(game,value);
        Func<string,object[],object> call=(name,args)=>type.GetMethod(name,Private).Invoke(game,args);
        var phase=type.GetField("phase",Private);var cam=type.GetField("cam",Private).GetValue(game) as Camera;
        try{
            float d=28,heading=(float)call("CourseHeading",new object[]{d});
            vehicle.position=(Vector3)call("CoursePosition",new object[]{d});vehicle.rotation=Quaternion.Euler(0,heading,0);
            set("rideHeading",heading);set("distance",d);set("speed",0f);set("crashed",false);phase.SetValue(game,Enum.Parse(phase.FieldType,"Ride"));
            var positions=new Vector3[3];
            for(int mode=0;mode<3;mode++){
                set("cameraMode",mode);call("UpdateRideCamera",new object[]{1f});positions[mode]=cam.transform.position;
                if((bool)call("CameraInsideScenery",new object[]{cam.transform.position}))throw new Exception("Camera mode "+mode+" inside scenery.");
                Capture(cam,"Captures/view-"+mode+".png");
            }
            if(Vector3.Distance(positions[0],positions[1])<1||Vector3.Distance(positions[0],positions[2])<1)throw new Exception("Camera modes are not distinct.");
            set("cameraMode",0);set("crashed",true);phase.SetValue(game,Enum.Parse(phase.FieldType,"Crash"));
            foreach(var r in root.GetComponentsInChildren<Renderer>())if(r.name.StartsWith("Vendor cart")&&r.gameObject.activeInHierarchy){
                vehicle.position=r.bounds.center;float closest=float.MaxValue,nearest=0;
                for(int i=0;i<800;i++){float error=((Vector3)call("CoursePosition",new object[]{(float)i})-vehicle.position).sqrMagnitude;if(error<closest){closest=error;nearest=i;}}
                set("distance",nearest);break;
            }
            call("UpdateRideCamera",new object[]{1f});
            if((bool)call("CameraInsideScenery",new object[]{cam.transform.position}))throw new Exception("Failure camera remained inside the cart.");
            float failDistance=(float)type.GetField("distance",Private).GetValue(game);
            vehicle.position=(Vector3)call("CoursePosition",new object[]{failDistance})+Quaternion.Euler(0,(float)call("CourseHeading",new object[]{failDistance}),0)*new Vector3(2.8f,0,0);
            call("UpdateRideCamera",new object[]{1f});Capture(cam,"Captures/failure-safe-camera.png");
            vehicle.position=(Vector3)call("CoursePosition",new object[]{28f});vehicle.rotation=Quaternion.Euler(0,heading,0);
            foreach(var r in vehicle.GetComponentsInChildren<Renderer>())r.enabled=true;
            cam.transform.position=vehicle.position+Quaternion.Euler(0,heading,0)*new Vector3(-2.4f,2.25f,3.2f);cam.transform.LookAt(vehicle.position+Vector3.up*1.55f);cam.fieldOfView=42;
            Capture(cam,"Captures/realistic-riders.png");
            File.WriteAllText("Validation/camera-art-result.txt","PASS: all three camera modes outside scenery and distinct; failure camera outside scenery even with vehicle inside cart. Captured chase, rider, high view and rider portrait.\n");
        }catch(Exception error){File.WriteAllText("Validation/camera-art-result.txt","FAIL: "+error);Debug.LogException(error);}
        finally{set("cameraMode",0);call("ResetRace",Array.Empty<object>());game.enabled=true;}
    }
}
#endif
