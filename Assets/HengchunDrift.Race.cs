using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public partial class HengchunDrift
{
    const float RaceSpeed=10, TimingWindow=.2f, MissionSeconds=180, MinimumLeanSpeed=24f/3.6f;
    class Bend { public float start,end,angle; public bool hit, released; public float entryTime, entryScore; public int direction => angle<0?-1:1; }
    readonly List<Vector3> course=new List<Vector3>();
    readonly List<float> courseYaw=new List<float>();
    readonly List<Bend> bends=new List<Bend>();
    struct DriftPress { public int direction; public double stamp; public bool release; }
    readonly Queue<DriftPress> driftPresses=new Queue<DriftPress>();
    InputAction leftDrift,rightDrift;
    Transform courseRoot,oldTown,rescueVan,rescuer,stretcher,vanDoor;
    AudioSource effects;
    AudioClip impactClip,sirenClip,skidClip;
    Texture2D radar;
    readonly Color32[] radarPixels=new Color32[128*128];
    float nextRadar, courseLength, crashAge, correctionAge, recoveryBlend, lastError, feedbackUntil;
    int bendIndex, correctionDirection, completedBends;
    bool recoveryUsed, crashed;
    double lastRaceWall, raceStartWall;
    Vector3 crashPosition;
    Quaternion crashRotation;
    Material ambulanceRed,ambulanceBlue;

    void SetupRace()
    {
        oldTown=environmentRoot;
        effects=gameObject.AddComponent<AudioSource>();effects.spatialBlend=0;effects.volume=.65f;
        impactClip=MakeEffect("Crash impact",.75f,0);skidClip=MakeEffect("Tyre slide",.65f,1);sirenClip=MakeEffect("Ambulance siren",7,2);
        radar=new Texture2D(128,128,TextureFormat.RGBA32,false);radar.filterMode=FilterMode.Bilinear;
        leftDrift=new InputAction("Left drift",InputActionType.Button,"<Keyboard>/a");leftDrift.AddBinding("<Keyboard>/leftArrow");
        rightDrift=new InputAction("Right drift",InputActionType.Button,"<Keyboard>/d");rightDrift.AddBinding("<Keyboard>/rightArrow");
        leftDrift.performed+=c=>CapturePress(-1,c.time);rightDrift.performed+=c=>CapturePress(1,c.time);
        leftDrift.canceled+=c=>CaptureRelease(-1,c.time);rightDrift.canceled+=c=>CaptureRelease(1,c.time);
        leftDrift.Enable();rightDrift.Enable();
        ImproveRiderMaterials();
    }
    void OnDestroy(){leftDrift?.Dispose();rightDrift?.Dispose();if(fontAtlasHooked)Font.textureRebuilt-=RefreshWorldFont;}
    void CapturePress(int direction,double inputTime)
    {
        // Input event time is mapped to realtime, retaining sub-frame press timing.
        double stamp=Time.realtimeSinceStartupAsDouble+(inputTime-InputState.currentTime);
        SetLeanButton(direction,true); if(phase==Phase.Correct || (LeanRequested && IsLeanInputZone()))driftPresses.Enqueue(new DriftPress{direction=direction,stamp=stamp});
    }
    AudioClip MakeEffect(string name,float seconds,int kind)
    {
        const int rate=22050;var values=new float[(int)(seconds*rate)];float phase=0;var rng=new System.Random(91+kind);
        for(int i=0;i<values.Length;i++){
            float t=i/(float)rate;float noise=(float)rng.NextDouble()*2-1;
            if(kind==2){float hz=650+250*Mathf.Sin(t*6);phase+=hz*2*Mathf.PI/rate;values[i]=Mathf.Sin(phase)*.35f;}
            else if(kind==1)values[i]=(noise*.55f+Mathf.Sin(t*2*Mathf.PI*1700)*.2f)*Mathf.Sin(Mathf.PI*t/seconds)*.55f;
            else values[i]=(noise*.6f+Mathf.Sin(t*2*Mathf.PI*72)*.4f)*Mathf.Exp(-t*8);
        }
        var clip=AudioClip.Create(name,values.Length,1,rate,false);clip.SetData(values,0);return clip;
    }
    void ResetRace()
    {
        if(oldTown)oldTown.gameObject.SetActive(false);
        if(courseRoot){courseRoot.gameObject.SetActive(false);Destroy(courseRoot.gameObject);}
        foreach(var t in smoke)if(t)Destroy(t.gameObject);smoke.Clear();walkers.Clear();
        courseRoot=new GameObject("Kenting night market · 1000 metres").transform;environmentRoot=courseRoot;
        GenerateCourse();BuildCourseScenery();
        ResetManualDrive();naturalBank=0;finaleDriftPoints=0;driftYaw=0;distance=0;speed=0;slip=0;rideTime=0;driftPoints=0;bendIndex=0;completedBends=0;recording.Clear();
        recoveryUsed=false;crashed=false;timedOut=false;recoveryBlend=0;crashAge=0;feedbackUntil=0;
        gasTouch=false;driftPresses.Clear();ResetChallengeState();phase=Phase.Ready;
        if(narrator)narrator.Stop();if(effects)effects.Stop();
        scooter.position=course[0];scooter.rotation=Quaternion.Euler(0,courseYaw[0],0);nextRadar=0;
    }
    void StartRace(){ResetRace();phase=Phase.Ride;raceStartWall=lastRaceWall=Time.realtimeSinceStartupAsDouble;}
    void GenerateCourse()
    {
        course.Clear();courseYaw.Clear();bends.Clear();Vector3 p=Vector3.zero;float yaw=0;course.Add(p);courseYaw.Add(yaw);
        // One metre per sample; gentle alternating blocks never intersect themselves.
        for(int metre=1;metre<=1000;metre++){
            bool finale=metre>800;
            int block=finale?(metre-801)/50:(metre-1)/100;
            int local=finale?(metre-801)%50:(metre-1)%100;
            int enter=finale?0:40, exit=finale?40:80;
            float angle=(block%2==0?1:-1)*(finale?65:36);
            if(local==enter)bends.Add(new Bend{start=metre-1,angle=angle});
            if(local>=enter&&local<exit)yaw+=angle/(exit-enter);
            p+=Quaternion.Euler(0,yaw,0)*Vector3.forward;course.Add(p);courseYaw.Add(yaw);
            if(local==exit-1)bends[bends.Count-1].end=metre;

        }
        courseLength=course.Count-1;
    }
    Vector3 CoursePosition(float d){if(course.Count<2)return Vector3.zero;int i=Mathf.Clamp(Mathf.FloorToInt(d),0,course.Count-2);return Vector3.Lerp(course[i],course[i+1],Mathf.Clamp01(d-i));}
    float CourseHeading(float d){if(courseYaw.Count<2)return 0;int i=Mathf.Clamp(Mathf.FloorToInt(d),0,courseYaw.Count-2);return Mathf.Lerp(courseYaw[i],courseYaw[i+1],Mathf.Clamp01(d-i));}
    void BuildCourseScenery()
    {
        Box("Peninsula ground",new Vector3(0,-.3f,0),new Vector3(12000,.4f,12000),M("#343e3b"));
        var road=new Mesh();road.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
        var verts=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        for(int i=0;i<course.Count;i+=2){var side=Quaternion.Euler(0,courseYaw[i],0)*Vector3.right*RoadHalfWidth;verts.Add(course[i]-side+Vector3.up*.025f);verts.Add(course[i]+side+Vector3.up*.025f);uv.Add(new Vector2(0,i*.1f));uv.Add(new Vector2(1,i*.1f));int n=verts.Count;if(n>2){triangles.Add(n-4);triangles.Add(n-2);triangles.Add(n-3);triangles.Add(n-3);triangles.Add(n-2);triangles.Add(n-1);}}
        road.SetVertices(verts);road.SetUVs(0,uv);road.SetTriangles(triangles,0);road.RecalculateNormals();
        var ro=new GameObject("Continuous delivery road");ro.transform.SetParent(courseRoot,false);ro.AddComponent<MeshFilter>().sharedMesh=road;ro.AddComponent<MeshRenderer>().sharedMaterial=asphalt;
        BuildDoubleYellowLines();
        BuildMarketPaving(); BuildVariedStreetSections();
        environmentRoot=courseRoot;
        BuildKentingEntrance();
        foreach(var b in bends){var marker=Box("Precision line",CoursePosition(b.start)+Vector3.up*.065f,new Vector3(10,.025f,.35f),M("#d4aa4f"));marker.rotation=Quaternion.Euler(0,CourseHeading(b.start),0);}
        var end=CoursePosition(courseLength);var goal=Box("Delivery finish",end+Vector3.up*.08f,new Vector3(RoadHalfWidth*2,.03f,1),teal);goal.rotation=Quaternion.Euler(0,CourseHeading(courseLength),0);
        BuildAmbulance(); ClearRoadObstructions(); BuildChallengeHazards(); BuildNightMarketCrowd(); BuildMarketObstacles(); BuildFinishCelebration(); BuildFabricObstacles(); CacheCameraBlockers(); Physics.SyncTransforms();
    }
    // Kept public and side-effect free for exact timing boundary verification.
    public static bool IsPrecise(double secondsError) => Math.Abs(secondsError)<=.200000001;
    void JudgePress(int direction,double stamp,double now) { JudgeLeanPress(direction,stamp,now); }
    void Crash(bool timeout)
    {
        if(phase==Phase.Crash||phase==Phase.Result)return;
        timedOut=timeout;crashed=true;phase=Phase.Crash;crashAge=0;speed=0;
        crashPosition=scooter.position;crashRotation=scooter.rotation;SetSafeCrashSlide();
        effects.Stop();effects.PlayOneShot(impactClip);effects.PlayOneShot(skidClip);
        if(narrator)narrator.Stop();
        rescueVan.gameObject.SetActive(true);rescueVan.position=crashPosition+Quaternion.Euler(0,CourseHeading(distance),0)*new Vector3(3.2f,0,-26);rescueVan.rotation=Quaternion.Euler(0,CourseHeading(distance),0);
    }
    void RaceUpdate()
    {
        if(!scooter||course.Count<2)return;
        float dt=Time.deltaTime;clock+=dt;AnimateStreetLife(dt);AnimateChallengeHazards(dt);AnimateNightMarketCrowd(dt);AnimateFinishCelebration();effects.mute=muted;if(narrator)narrator.mute=muted;
        if(Press(Key.C))CycleCamera();
        if(Press(Key.M))muted=!muted;if(Press(Key.R)){ResetRace();return;}
        if(phase==Phase.Ready&&Press(Key.Enter)){StartRace();return;}
        double now=Time.realtimeSinceStartupAsDouble;
        if(phase==Phase.Ride||phase==Phase.Correct){
            // Wall-clock seconds cannot be lengthened by low frame rate or timeScale.
            float elapsed=(float)(now-lastRaceWall);lastRaceWall=now;rideTime=Mathf.Min(MissionSeconds,(float)(now-raceStartWall));
            if(rideTime>=MissionSeconds){Crash(true);return;}
            if(phase==Phase.Ride){bool gas=(gasTouch||Held(Key.W)||Held(Key.UpArrow))&&!(Held(Key.S)||Held(Key.DownArrow)||brakeTouch);speed=Mathf.MoveTowards(speed,gas?RaceSpeed:0,elapsed*(Held(Key.S)||Held(Key.DownArrow)||brakeTouch?12:gas?2.5f:2));}
            if(phase==Phase.Ride&&!MoveManualDrive(elapsed)){Crash(false);return;}
            while(driftPresses.Count>0){var press=driftPresses.Dequeue();if(press.release)JudgeLeanRelease(press.direction,press.stamp,now);else JudgePress(press.direction,press.stamp,now);if(phase==Phase.Crash)break;}
            if(phase==Phase.Crash)return;
            if(phase==Phase.Correct){correctionAge+=elapsed;if(correctionAge>1){Crash(false);return;}}
            AdvanceOptionalBends();
            if(bendIndex<bends.Count){var b=bends[bendIndex];if(b.hit&&!b.released&&distance>b.end+speed*.2f){Crash(false);return;} if(b.hit&&!b.released&&speed<MinimumLeanSpeed){Crash(false);return;}}
            float target=bendIndex<bends.Count&&bends[bendIndex].hit&&distance>=bends[bendIndex].start&&distance<=bends[bendIndex].end?1:0;
            if(distance>=800)target=speed>3?Mathf.Abs(SteeringInput):0;
            slip=Mathf.MoveTowards(slip,target,dt*12);int side=bendIndex<bends.Count?bends[bendIndex].direction:1;
            recoveryBlend=Mathf.MoveTowards(recoveryBlend,0,dt*2);
            float wrong=phase==Phase.Correct?Mathf.Sin(correctionAge*Mathf.PI)*-side*65:recoveryBlend*side*45;
            float curve=Mathf.DeltaAngle(CourseHeading(distance-2),CourseHeading(distance+2))/4f;
            float targetBank=-SteeringInput*Mathf.Clamp(speed*2.1f,0,24);
            float steeringBank=((rightLeanHeld?1:0)-(leftLeanHeld?1:0))*-7f*Mathf.Clamp01(speed/5);
            naturalBank=Mathf.Lerp(naturalBank,targetBank+steeringBank,1-Mathf.Exp(-elapsed*4.5f));
            UpdateFinaleDrift(elapsed,curve);
            scooter.rotation=Quaternion.Euler(0,rideHeading-(distance>=800?driftYaw:side*slip*16)+wrong,naturalBank);
            UpdateTyreMarks(elapsed);
            if(recording.Count==0||rideTime-recording[recording.Count-1].time>=.04f){recording.Add(new Frame{p=scooter.position,q=scooter.rotation,time=rideTime,slip=slip});if(recording.Count>300)recording.RemoveAt(0);}
            if(slip>.3f&&clock>nextSmoke){nextSmoke=clock+.12f;var t=Shape("Tyre smoke",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.3f,white);t.position=scooter.TransformPoint(new Vector3(0,.2f,-.8f));smoke.Add(t);}
            if(distance>=courseLength){StartHighlightReplay();}
        }else driftPresses.Clear();
        if(phase==Phase.Crash)AnimateRescue(dt);
        if(phase==Phase.Replay){replayTime+=dt;float targetTime=recording[replayIndex].time+replayTime*.5f;int i=replayIndex;while(i<replayEnd&&recording[i].time<targetTime)i++;scooter.position=recording[i].p;scooter.rotation=recording[i].q;if(i<replayEnd){float blend=Mathf.InverseLerp(recording[i].time,recording[i+1].time,targetTime);scooter.position=Vector3.Lerp(recording[i].p,recording[i+1].p,blend);scooter.rotation=Quaternion.Slerp(recording[i].q,recording[i+1].q,blend);} if(i>=replayEnd)phase=Phase.Result;}
        for(int i=smoke.Count-1;i>=0;i--){var t=smoke[i];if(!t){smoke.RemoveAt(i);continue;}t.position+=Vector3.up*dt*.4f;t.localScale+=Vector3.one*dt;if(t.localScale.x>1.6f){Destroy(t.gameObject);smoke.RemoveAt(i);}}
        UpdateRideCamera(dt);

        if(motor){motor.mute=muted;motor.pitch=.6f+speed*.065f;motor.volume=phase==Phase.Ride?.13f:.025f;}
    }
    void BuildAmbulance()
    {
        rescueVan=new GameObject("Ambulance rescue animation").transform;rescueVan.SetParent(courseRoot,false);
        Box("Ambulance body",new Vector3(0,1.4f,0),new Vector3(2.1f,2.3f,4.6f),white,rescueVan);
        Box("Cab windscreen",new Vector3(0,1.85f,2.32f),new Vector3(1.8f,.8f,.04f),M("#426276"),rescueVan);
        for(int s=-1;s<=1;s+=2){Box("Red ambulance stripe",new Vector3(s*1.065f,1.1f,0),new Vector3(.03f,.28f,4.4f),M("#b93f39"),rescueVan);Box("Medical cross vertical",new Vector3(s*1.08f,1.9f,-.5f),new Vector3(.03f,.65f,.2f),M("#b93f39"),rescueVan);Box("Medical cross horizontal",new Vector3(s*1.09f,1.9f,-.5f),new Vector3(.03f,.2f,.65f),M("#b93f39"),rescueVan);for(int z=-1;z<=1;z+=2){var w=Shape("Van wheel",PrimitiveType.Cylinder,new Vector3(s*.95f,.45f,z*1.5f),new Vector3(.75f,.16f,.75f),dark,rescueVan);w.localRotation=Quaternion.Euler(0,0,90);}}
        ambulanceRed=Glow("#ff3333");ambulanceBlue=Glow("#337aff");Box("Red beacon",new Vector3(-.55f,2.65f,.4f),new Vector3(.75f,.2f,.45f),ambulanceRed,rescueVan);Box("Blue beacon",new Vector3(.55f,2.65f,.4f),new Vector3(.75f,.2f,.45f),ambulanceBlue,rescueVan);
        vanDoor=new GameObject("Opening rear ambulance door").transform;vanDoor.SetParent(rescueVan,false);vanDoor.localPosition=new Vector3(-1,1.4f,-2.34f);Box("Door panel",new Vector3(.5f,0,0),new Vector3(1,1.9f,.06f),white,vanDoor);
        rescuer=new GameObject("Paramedic").transform;rescuer.SetParent(rescueVan,false);Loft("Paramedic uniform",Vector3.up*.75f,new float[,]{{0,.19f,.14f},{.45f,.25f,.15f},{.7f,.1f,.1f}},M("#d87534"),rescuer);Shape("Paramedic head",PrimitiveType.Sphere,new Vector3(0,1.68f,0),new Vector3(.29f,.36f,.3f),skin,rescuer);
        for(int s=-1;s<=1;s+=2){Limb("Medic leg",new Vector3(s*.12f,.8f,0),new Vector3(s*.15f,.08f,0),.08f,dark,rescuer);Limb("Medic arm",new Vector3(s*.25f,1.35f,0),new Vector3(s*.28f,.94f,.2f),.065f,M("#d87534"),rescuer);}
        stretcher=new GameObject("Rescue stretcher").transform;stretcher.SetParent(rescueVan,false);Box("Stretcher mattress",Vector3.up*.65f,new Vector3(.7f,.15f,1.9f),teal,stretcher);for(int s=-1;s<=1;s+=2)Beam("Stretcher rail",new Vector3(s*.4f,.7f,-1),new Vector3(s*.4f,.7f,1),.045f,white,stretcher);
        rescueVan.gameObject.SetActive(false);
    }
    void AnimateRescue(float dt)
    {
        float before=crashAge;crashAge+=dt;float fall=Mathf.SmoothStep(0,1,Mathf.Clamp01(crashAge/1.1f));
        scooter.position=crashPosition+crashRotation*Vector3.forward*(fall*safeCrashSlide)+Vector3.down*(fall*.12f);scooter.rotation=crashRotation*Quaternion.Euler(0,fall*20,fall*78);
        var q=Quaternion.Euler(0,CourseHeading(distance),0);rescueVan.position=crashPosition+q*new Vector3(3.2f,0,Mathf.Lerp(-26,-4,Mathf.SmoothStep(0,1,(crashAge-1.3f)/3)));
        if(before<1.3f&&crashAge>=1.3f)effects.PlayOneShot(sirenClip,.6f);
        vanDoor.localRotation=Quaternion.Euler(0,Mathf.SmoothStep(0,105,Mathf.Clamp01((crashAge-4)/1)),0);
        float walk=Mathf.Clamp01((crashAge-4.5f)/2);rescuer.localPosition=new Vector3(Mathf.Lerp(-1.4f,-3.1f,walk),0,Mathf.Lerp(-2.5f,3,walk));rescuer.localRotation=Quaternion.Euler(Mathf.Sin(crashAge*6)*2,180,0);
        stretcher.localPosition=new Vector3(Mathf.Lerp(-1,-3,walk),0,Mathf.Lerp(-3,2,walk));
        if(crashAge>7){float lift=Mathf.Clamp01((crashAge-7)/1.3f);scooter.position+=Vector3.up*lift*.35f;rescuer.localRotation=Quaternion.Euler(-25*(1-lift),180,0);}
        if(crashAge>=9.5f){phase=Phase.Result;effects.Stop();}
    }
    void ImproveRiderMaterials()
    {
        ApplyComicMaterials(scooter);
    }

    Vector2 RadarPoint(Vector3 point)
    {
        var relative=Quaternion.Euler(0,-CourseHeading(distance),0)*(point-scooter.position);return new Vector2(64+relative.x*.8f,64+relative.z*.8f);
    }
    void RadarDot(Vector2 p,Color32 color,int radius)
    {
        int x=Mathf.RoundToInt(p.x),y=Mathf.RoundToInt(p.y);for(int a=-radius;a<=radius;a++)for(int b=-radius;b<=radius;b++){int u=x+a,v=y+b;if(u<0||v<0||u>=128||v>=128||(u-64)*(u-64)+(v-64)*(v-64)>58*58)continue;radarPixels[v*128+u]=color;}
    }
    void UpdateRadar()
    {
        for(int y=0;y<128;y++)for(int x=0;x<128;x++){float r=Vector2.Distance(new Vector2(x,y),new Vector2(64,64));radarPixels[y*128+x]=r>63?new Color32(0,0,0,0):r>59?new Color32(164,189,177,240):new Color32(10,25,31,222);}
        int from=Mathf.Max(0,(int)distance-100),to=Mathf.Min(course.Count-2,(int)distance+180);
        for(int i=from;i<to;i++){var a=RadarPoint(course[i]);var b=RadarPoint(course[i+1]);int n=Mathf.CeilToInt(Vector2.Distance(a,b));for(int j=0;j<=n;j++)RadarDot(Vector2.Lerp(a,b,j/(float)Mathf.Max(1,n)),i<distance?new Color32(78,94,95,255):new Color32(105,232,190,255),1);}
        if(bendIndex<bends.Count)RadarDot(RadarPoint(CoursePosition(bends[bendIndex].start)),new Color32(255,191,73,255),3);
        RadarDot(new Vector2(64,64),new Color32(255,250,225,255),3);RadarDot(new Vector2(64,70),new Color32(255,250,225,255),1);
        radar.SetPixels32(radarPixels);radar.Apply(false);
    }
    void RaceGUI()
    {
        if(!pixel||course.Count<2)return;float scale=Screen.width/1440f;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));float h=Screen.height/scale;
        Color ivory=new Color(1,.95f,.84f),mint=new Color(.5f,1,.8f);
        if(phase==Phase.Ready){
            DrawKentingOpening(h);
            DrawCameraSwitch(h,ivory);
            Text("墾丁夜行",44,h*.25f,480,80,58,ivory,FontStyle.Bold);Text("W 油門 · S 煞車 · A / D 閃避",48,h*.25f+87,500,40,23,mint);
            Text("1 公里夜市漫遊 · 最高 36 km/h · 限時 3 分鐘",48,h*.25f+130,500,35,18,ivory);
            if(Button("出發",new Rect(48,h*.25f+185,200,50)))StartRace();return;
        }
        DrawCameraSwitch(h,ivory);
        if(phase==Phase.Ride||phase==Phase.Correct){
            if(clock>nextRadar){nextRadar=clock+.08f;UpdateRadar();}GUI.DrawTexture(new Rect(24,22,146,146),radar);
            double remaining=Math.Max(0,MissionSeconds-rideTime);string timer=((int)remaining/60).ToString("00")+":"+((int)remaining%60).ToString("00")+"."+((int)(remaining*100)%100).ToString("00");
            Text(timer,1150,24,260,45,31,remaining<30?new Color(1,.4f,.3f):ivory,FontStyle.Bold,TextAnchor.MiddleRight);
            DrawCentreSpeedometer(h,ivory,mint);
            if(distance>=800)Text("連續漂移 · "+((int)finaleDriftPoints)+" 分",460,28,520,40,27,mint,FontStyle.Bold,TextAnchor.MiddleCenter);
            Text($"{distance:0} / 1000 m",1140,78,270,30,20,ivory,FontStyle.Bold,TextAnchor.MiddleRight);
            if(phase==Phase.Correct){Text((correctionDirection<0?"A ←":"D →")+"  回正",500,h*.36f,440,65,42,new Color(1,.65f,.3f),FontStyle.Bold,TextAnchor.MiddleCenter);}
            else if(LeanRequested && bendIndex<bends.Count&&!bends[bendIndex].hit){var b=bends[bendIndex];float seconds=(b.start-distance)/Mathf.Max(speed,1);if(seconds<3){Text((b.direction<0?"A ←":"D →")+"  "+Mathf.Abs(b.angle).ToString("0")+"°",520,h*.27f,400,60,38,ivory,FontStyle.Bold,TextAnchor.MiddleCenter);Text(Mathf.Max(0,seconds).ToString("0.000"),580,h*.27f+60,280,50,32,Mathf.Abs(seconds)<=.2f?mint:ivory,FontStyle.Bold,TextAnchor.MiddleCenter);Panel(new Rect(570,h*.27f+114,300,6),new Color(.2f,.25f,.25f));Panel(new Rect(570+Mathf.Clamp01(1-seconds/3)*294,h*.27f+108,6,18),mint);}}
            if(clock<feedbackUntil)Text(lastError.ToString("+0.000;-0.000;0.000")+" s",560,h*.48f,320,42,27,mint,FontStyle.Bold,TextAnchor.MiddleCenter);
            DrawLeanExitCue(h,ivory,mint); DrawHoldControls(h,scale,ivory);
        }
        if(phase==Phase.Crash)Text("挑戰失敗",500,40,440,65,40,ivory,FontStyle.Bold,TextAnchor.MiddleCenter);
        if(phase==Phase.Replay)Text("REPLAY",36,28,220,40,22,ivory);
        if(phase==Phase.Result){Panel(new Rect(40,h*.3f,330,235),new Color(.02f,.09f,.11f,.9f));Text(crashed?"挑戰失敗":"挑戰成功",65,h*.3f+20,285,55,35,ivory,FontStyle.Bold);Text(((int)driftPoints).ToString("00000"),65,h*.3f+83,285,60,45,mint);if(Button("再來",new Rect(65,h*.3f+162,265,48)))StartRace();}
    }
}






