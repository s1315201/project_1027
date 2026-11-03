#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class NightMarketDrivingCheck
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static NightMarketDrivingCheck(){EditorApplication.update+=RunPending;}
    [MenuItem("Tools/Night Market/Validate Manual Driving")]
    static void Queue(){SessionState.SetBool("NightMarket.DrivingCheck",true);if(!EditorApplication.isPlaying)EditorApplication.isPlaying=true;}
    static void RunPending()
    {
        if(!SessionState.GetBool("NightMarket.DrivingCheck",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        var game=UnityEngine.Object.FindAnyObjectByType<HengchunDrift>();if(!game)return;
        var t=game.GetType();var vehicle=t.GetField("scooter",Private).GetValue(game) as Transform;
        if(!vehicle||t.GetField("finishFestival",Private).GetValue(game)==null)return;
        SessionState.SetBool("NightMarket.DrivingCheck",false);game.enabled=false;
        Action<string,object> set=(name,value)=>t.GetField(name,Private).SetValue(game,value);
        Func<string,object[],object> call=(name,args)=>t.GetMethod(name,Private).Invoke(game,args);
        try{
            call("ResetManualDrive",Array.Empty<object>());
            Vector3 start=(Vector3)call("CoursePosition",new object[]{800f});
            float heading=(float)call("CourseHeading",new object[]{800f});
            vehicle.position=start;vehicle.rotation=Quaternion.Euler(0,heading,0);
            set("rideHeading",heading);set("distance",800f);set("speed",10f);set("leftLeanHeld",false);set("rightLeanHeld",false);
            for(int i=0;i<3;i++)if(!(bool)call("MoveManualDrive",new object[]{.2f}))throw new Exception("Straight movement unexpectedly blocked in drift sector.");
            Vector3 expected=start+Quaternion.Euler(0,heading,0)*Vector3.forward*6;
            if(Vector3.Distance(vehicle.position,expected)>.02f)throw new Exception("Vehicle still follows the road without steering.");
            if(Mathf.Abs(Mathf.DeltaAngle(heading,(float)t.GetField("rideHeading",Private).GetValue(game)))>.01f)throw new Exception("Heading changed without player input.");
            set("rightLeanHeld",true);set("slip",1f);
            for(int i=0;i<60;i++){
                if(!(bool)call("MoveManualDrive",new object[]{1f/60}))throw new Exception("Controlled right turn blocked in drift sector.");
                vehicle.rotation=Quaternion.Euler(0,(float)t.GetField("rideHeading",Private).GetValue(game),0);
                call("UpdateFinaleDrift",new object[]{1f/60,0f});call("UpdateTyreMarks",new object[]{1f/60});
            }
            float turned=Mathf.DeltaAngle(heading,(float)t.GetField("rideHeading",Private).GetValue(game));
            if(Mathf.Abs(turned-35)>.1f)throw new Exception("Right input did not steer the vehicle by expected amount.");
            var marks=t.GetField("tyreMarks",Private).GetValue(game);
            int count=(int)marks.GetType().GetProperty("Count").GetValue(marks);
            if(count==0)throw new Exception("Held drift did not create skid marks.");
            set("rightLeanHeld",false);call("UpdateTyreMarks",new object[]{.1f});
            if((bool)t.GetField("tyreWasMarking",Private).GetValue(game))throw new Exception("Marks continued after steering release.");
            File.WriteAllText("Validation/manual-driving-result.txt","PASS: no-input movement stays straight on a curved road; right input turns 35 degrees in one second; held drift creates tyre marks; releasing steering stops tyre marks.\n");
            Debug.Log("Night market manual-driving checks passed.");
        }catch(Exception error){File.WriteAllText("Validation/manual-driving-result.txt","FAIL: "+error);Debug.LogException(error);}
        finally{call("ResetRace",Array.Empty<object>());game.enabled=true;}
    }
}
#endif
