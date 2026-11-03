using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public partial class HengchunDrift
{
    int activeLeanDirection;
    bool leftLeanHeld,rightLeanHeld,touchLeft,touchRight,brakeTouch,touchLean;
    readonly List<List<Frame>> highlights=new List<List<Frame>>();
    readonly List<float> highlightQuality=new List<float>();
    readonly List<Crossing> crossings=new List<Crossing>();
    class Crossing { public Walker actor; public Transform root; public float d,age=-1; public Quaternion heading; public Vector3 centre; }

    void ResetChallengeState()
    {
        activeLeanDirection=0;leftLeanHeld=false;rightLeanHeld=false;touchLeft=false;touchRight=false;brakeTouch=false;touchLean=false;
        highlights.Clear();highlightQuality.Clear();
    }
    void SetLeanButton(int direction,bool held){if(direction<0)leftLeanHeld=held;else rightLeanHeld=held;}
    bool IsLeanInputZone()
    {
        if(phase!=Phase.Ride||distance>=800||bendIndex>=bends.Count)return false;
        var b=bends[bendIndex];return !b.released && (b.start-distance)/Mathf.Max(speed,1)<.65f;
    }
    void CaptureRelease(int direction,double inputTime)
    {
        SetLeanButton(direction,false);
        if(activeLeanDirection==direction)driftPresses.Enqueue(new DriftPress{direction=direction,release=true,stamp=Time.realtimeSinceStartupAsDouble+(inputTime-InputState.currentTime)});
    }
    public static bool MeetsLeanSpeed(float metresPerSecond) => metresPerSecond>=MinimumLeanSpeed-.00001f;
    public static bool IsExitReleaseValid(double secondsAfterExit) => secondsAfterExit>=-.000000001 && secondsAfterExit<=.200000001;
    void JudgeLeanPress(int direction,double stamp,double now)
    {
        if(phase==Phase.Correct){
            if(direction!=correctionDirection||correctionAge>1||!MeetsLeanSpeed(speed)){Crash(false);return;}
            phase=Phase.Ride;recoveryBlend=1;var recovered=bends[bendIndex];recovered.hit=true;recovered.entryTime=rideTime;recovered.entryScore=100;activeLeanDirection=direction;effects.PlayOneShot(skidClip);return;
        }
        if(phase!=Phase.Ride||bendIndex>=bends.Count)return;
        var b=bends[bendIndex];if(b.released)return;
        if(b.hit){if(direction!=activeLeanDirection)Crash(false);return;}
        double error=(distance-b.start)/Math.Max(speed,1)+(stamp-now);lastError=(float)error;
        if(!IsPrecise(error)||!MeetsLeanSpeed(speed)){Crash(false);return;}
        if(direction!=b.direction){if(recoveryUsed){Crash(false);return;}recoveryUsed=true;correctionDirection=b.direction;correctionAge=0;phase=Phase.Correct;effects.PlayOneShot(skidClip);return;}
        b.hit=true;b.entryTime=rideTime;b.entryScore=Mathf.RoundToInt(500*(1-Mathf.Abs(lastError)));activeLeanDirection=direction;
        feedbackUntil=clock+.65f;effects.PlayOneShot(skidClip,.45f);
    }
    void JudgeLeanRelease(int direction,double stamp,double now)
    {
        if(phase!=Phase.Ride||activeLeanDirection!=direction||bendIndex>=bends.Count)return;
        var b=bends[bendIndex];double error=(distance-b.end)/Math.Max(speed,1)+(stamp-now);lastError=(float)error;
        if(!IsExitReleaseValid(error)||!MeetsLeanSpeed(speed)){Crash(false);return;}
        b.released=true;activeLeanDirection=0;completedBends++;driftPoints+=b.entryScore;feedbackUntil=clock+.75f;
        CaptureLeanHighlight(b);
    }
    void UpdateLateralSteering(float dt)
    {
        rideHeading+=SteeringInput*(distance>=800?35f:30f)*Mathf.Clamp01(speed/3)*dt;
    }
    public bool IsOnDeliveryRoad(Vector3 position)
    {
        if(course.Count<2)return false;
        float closest=float.MaxValue;
        // Use actual road segments. This also handles a looping 360-degree bend.
        for(int i=0;i<course.Count-1;i++){
            Vector3 a=course[i],b=course[i+1],ab=b-a;float fraction=Mathf.Clamp01(Vector3.Dot(position-a,ab)/Mathf.Max(ab.sqrMagnitude,.0001f));
            Vector3 difference=position-(a+ab*fraction);difference.y=0;closest=Mathf.Min(closest,difference.sqrMagnitude);
        }
        return closest<=(RoadHalfWidth-.5f)*(RoadHalfWidth-.5f); // Keep the vehicle's outer edge inside the 11 m road.
    }
    void CaptureLeanHighlight(Bend bend)
    {
        var frames=new List<Frame>();foreach(var f in recording)if(f.time>=bend.entryTime-.3f)frames.Add(f);
        frames.Add(new Frame{p=scooter.position,q=scooter.rotation,time=rideTime,slip=slip});
        if(frames.Count<3)return;
        float quality=Mathf.Abs(bend.angle)+bend.entryScore*.15f;
        if(highlights.Count>=3){int worst=0;for(int i=1;i<highlightQuality.Count;i++)if(highlightQuality[i]<highlightQuality[worst])worst=i;if(quality<=highlightQuality[worst])return;highlights.RemoveAt(worst);highlightQuality.RemoveAt(worst);}
        highlights.Add(frames);highlightQuality.Add(quality);
    }
    void StartHighlightReplay()
    {
        if(activeLeanDirection!=0){Crash(false);return;}
        var selected=new List<Frame>();float timeline=0;
        foreach(var clip in highlights){float origin=clip[0].time;foreach(var f in clip){var frame=f;frame.time=timeline+f.time-origin;selected.Add(frame);}timeline=selected[selected.Count-1].time+.08f;}
        if(selected.Count<2){phase=Phase.Result;return;}
        recording.Clear();recording.AddRange(selected);replayIndex=0;replayEnd=recording.Count-1;replayTime=0;phase=Phase.Replay;
    }
    void DrawHoldControls(float h,float scale,Color color)
    {
        var left=new Rect(28,h-72,95,45);var right=new Rect(135,h-72,95,45);var gas=new Rect(242,h-72,95,45);
        Panel(left,new Color(.03f,leftLeanHeld?.4f:.15f,.16f,.8f));Panel(right,new Color(.03f,rightLeanHeld?.4f:.15f,.16f,.8f));Panel(gas,new Color(.03f,.15f,.15f,.8f));
        Text("A ←",left.x,left.y,95,45,19,color,FontStyle.Normal,TextAnchor.MiddleCenter);Text("D →",right.x,right.y,95,45,19,color,FontStyle.Normal,TextAnchor.MiddleCenter);Text("W ↑",gas.x,gas.y,95,45,19,color,FontStyle.Normal,TextAnchor.MiddleCenter);
        DrawExtraControls(h,color);
        if(Event.current.type!=EventType.Repaint)return;
        bool l=false,r=false,g=false;
        if(Pointer.current!=null&&Pointer.current.press.isPressed){var p=Pointer.current.position.ReadValue();var xy=new Vector2(p.x/scale,(Screen.height-p.y)/scale);l=left.Contains(xy);r=right.Contains(xy);g=gas.Contains(xy);}
        if(Touchscreen.current!=null)foreach(var touch in Touchscreen.current.touches)if(touch.press.isPressed){var p=touch.position.ReadValue();var xy=new Vector2(p.x/scale,(Screen.height-p.y)/scale);l|=left.Contains(xy);r|=right.Contains(xy);g|=gas.Contains(xy);}
        if(l&&!touchLeft)CapturePress(-1,InputState.currentTime);if(!l&&touchLeft)CaptureRelease(-1,InputState.currentTime);
        if(r&&!touchRight)CapturePress(1,InputState.currentTime);if(!r&&touchRight)CaptureRelease(1,InputState.currentTime);
        touchLeft=l;touchRight=r;gasTouch=g;
        UpdateExtraTouchControls(h,scale);
    }
    void DrawLeanExitCue(float height,Color ivory,Color mint)
    {
        if(activeLeanDirection==0||bendIndex>=bends.Count||phase!=Phase.Ride)return;
        var bend=bends[bendIndex];float remaining=(bend.end-distance)/Mathf.Max(speed,1);
        string cue=remaining>0?"壓住":"放開";
        Text((activeLeanDirection<0?"A ←  ":"D →  ")+cue,520,height*.27f,400,55,34,remaining>0?ivory:mint,FontStyle.Bold,TextAnchor.MiddleCenter);
        Text(Mathf.Max(0,remaining).ToString("0.000"),580,height*.27f+55,280,45,29,remaining>0?ivory:mint,FontStyle.Bold,TextAnchor.MiddleCenter);
    }
    void BuildChallengeHazards()
    {
        crossings.Clear();environmentRoot=courseRoot;
        for(int i=0;i<bends.Count-1;i+=2){
            float d=bends[i].end+32;if(d>=790)continue;
            if(d+28>=bends[i+1].start)continue;
            Vector3 point=CoursePosition(d);Quaternion q=Quaternion.Euler(0,CourseHeading(d),0);
            // Visible, solid rubbish leaves room to steer around on either side.
            for(int bag=0;bag<3;bag++){
                float side=i%4==0?-2.7f:2.7f;
                var trash=Shape("Trash bag",PrimitiveType.Sphere,point+q*new Vector3(side+bag*.28f,.3f,10+bag*.35f),new Vector3(.48f,.65f,.5f),M("#323c35"));
                Shape("Trash bag knot",PrimitiveType.Sphere,trash.position+Vector3.up*.36f,Vector3.one*.1f,dark);
            }
            if(i!=0 && i!=4)continue;
            for(int stripe=-4;stripe<=4;stripe++){var t=Box("Crosswalk stripe",point+q*new Vector3(stripe, .085f,0),new Vector3(.58f,.025f,3.5f),white);t.rotation=q;}
            var root=new GameObject("Pedestrian crossing event").transform;root.SetParent(courseRoot,false);root.localPosition=point;root.localRotation=q;environmentRoot=root;
            Person(new Vector3(6.5f,0,0),-90,63,false);var actor=walkers[walkers.Count-1];actor.root.name="老奶奶 · 等待過馬路";actor.travel=0;actor.root.localScale=Vector3.one*.92f;
            foreach(var renderer in actor.root.GetComponentsInChildren<Renderer>()){
                if(renderer.name=="Hair"||renderer.name=="Long hair")renderer.sharedMaterial=M("#b9b9b1");
                if(renderer.name=="Summer blouse")renderer.sharedMaterial=M("#8f677b");
            }
            Beam("Walking cane",new Vector3(.28f,.85f,.1f),new Vector3(.29f,.02f,.3f),.035f,M("#78573b"),actor.root);
            var hit=actor.root.gameObject.AddComponent<CapsuleCollider>();hit.height=1.65f;hit.center=Vector3.up*.82f;hit.radius=.25f;
            crossings.Add(new Crossing{actor=actor,root=root,d=d,centre=point,heading=q});environmentRoot=courseRoot;
        }
    }
    void AnimateChallengeHazards(float dt)
    {
        if(phase!=Phase.Ride&&phase!=Phase.Correct)return;
        foreach(var c in crossings){
            if(c.age<0&&distance>c.d-100)c.age=0;
            if(c.age<0)continue;c.age+=dt;
            float x=Mathf.Lerp(6.5f,-6.5f,Mathf.Clamp01(c.age/9.5f));c.actor.root.localPosition=new Vector3(x,0,0);c.actor.root.localRotation=Quaternion.Euler(6,-90,0);
            float swing=Mathf.Sin(c.age*5)*14;c.actor.leftLeg.localRotation=Quaternion.Euler(swing,0,0);c.actor.rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
        }
    }
}



