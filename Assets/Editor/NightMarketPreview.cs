#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Editor-only art review shortcut; never included in the player build.
[InitializeOnLoad]
static class NightMarketPreview
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static NightMarketPreview(){EditorApplication.update+=Update;}
    [MenuItem("Tools/Night Market/Preview Finish _F8")]
    static void Finish(){SessionState.SetBool("NightMarket.PreviewFinish",true);if(!EditorApplication.isPlaying)EditorApplication.isPlaying=true;}
    static void Update()
    {
        if(!SessionState.GetBool("NightMarket.PreviewFinish",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        var game=UnityEngine.Object.FindAnyObjectByType<HengchunDrift>();if(!game)return;
        var type=game.GetType();var vehicle=type.GetField("scooter",Private).GetValue(game) as Transform;if(!vehicle)return;
        var root=type.GetField("finishFestival",Private).GetValue(game) as Transform;if(!root)return;
        SessionState.SetBool("NightMarket.PreviewFinish",false);
        type.GetField("distance",Private).SetValue(game,1000f);
        type.GetField("speed",Private).SetValue(game,0f);
        type.GetField("crashed",Private).SetValue(game,false);
        var phase=type.GetField("phase",Private);phase.SetValue(game,Enum.Parse(phase.FieldType,"Result"));
        vehicle.position=(Vector3)type.GetMethod("CoursePosition",Private).Invoke(game,new object[]{1000f});
        vehicle.rotation=Quaternion.Euler(0,(float)type.GetMethod("CourseHeading",Private).Invoke(game,new object[]{1000f}),0);
        var camera=type.GetField("cam",Private).GetValue(game) as Camera;
        camera.transform.position=vehicle.position+vehicle.rotation*new Vector3(0,3.8f,-12);
        camera.transform.LookAt(vehicle.position+Vector3.up*2.8f);
        var captureAt=EditorApplication.timeSinceStartup+1.5;
        EditorApplication.CallbackFunction capture=null;
        capture=()=>{if(EditorApplication.timeSinceStartup<captureAt)return;EditorApplication.update-=capture;ScreenCapture.CaptureScreenshot("Captures/finish-celebration.png");};
        EditorApplication.update+=capture;
    }
}
#endif

