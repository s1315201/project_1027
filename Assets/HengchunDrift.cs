using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// A self-contained arcade vignette. All scenery and characters are built locally.
public partial class HengchunDrift : MonoBehaviour
{
    enum Phase { Ready, Ride, Replay, Result, Correct, Crash }
    Phase phase;
    Camera cam;
    Transform scooter, frontWheel, rearWheel;
    Material asphalt, cream, brick, dark, teal, skin, white, blue;
    readonly List<Frame> recording = new List<Frame>();
    readonly List<Transform> smoke = new List<Transform>();
    struct Frame { public Vector3 p; public Quaternion q; public float slip, time; }
    float distance, speed, slip, driftPoints, replayTime, rideTime, clock, nextSmoke;
    int replayIndex, replayEnd;
    bool gasTouch, muted;
    AudioSource motor;
    Font font;
    Texture2D pixel;
    const float CornerStart = 84, Radius = 13, CornerEnd = CornerStart + Radius * Mathf.PI / 2;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch() { if (FindAnyObjectByType<HengchunDrift>() == null) new GameObject("HENGCHUN • DRIFT").AddComponent<HengchunDrift>(); }

    void Start()
    {
        Application.runInBackground=true; foreach (var c in FindObjectsByType<Camera>()) c.gameObject.SetActive(false);
        foreach (var l in FindObjectsByType<Light>()) l.gameObject.SetActive(false);
        asphalt = Mat("Road", "#363d43"); cream = Mat("Sand", "#cfad7d"); brick = Mat("Old coral stone", "#a57450");
        dark = Mat("Ink", "#182d35"); teal = Mat("Seafoam", "#5ad0b9"); skin = Mat("Sun kissed", "#dba77e"); white = Mat("Cotton", "#fff2d7"); blue = Mat("Slippers blue", "#167eae");
        RenderSettings.ambientLight = new Color(.57f,.60f,.68f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.17f,.20f,.29f); RenderSettings.fogDensity = .0045f;
        var sun = new GameObject("Late afternoon sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1,.78f,.52f); sun.intensity = .65f; sun.transform.rotation = Quaternion.Euler(38,-35,0); sun.shadows = LightShadows.Soft;
        cam = new GameObject("Ride and replay camera").AddComponent<Camera>(); cam.fieldOfView = 72; cam.gameObject.AddComponent<AudioListener>(); cam.nearClipPlane = .08f; cam.farClipPlane = 450; cam.backgroundColor = new Color(.10f,.15f,.24f); cam.clearFlags = CameraClearFlags.SolidColor;
        font = Font.CreateDynamicFontFromOSFont(new [] { "Microsoft JhengHei", "Arial" }, 32); environmentRoot=new GameObject("Hengchun and Kenting environment").transform; BuildTown(); BuildStreetLife(); AddFacadeDetails(); BuildScooter(); CustomizeScooter();
        font = Font.CreateDynamicFontFromOSFont(new [] { "Microsoft JhengHei", "Arial" }, 32);
        pixel = Texture2D.whiteTexture;
        SetupMission(); SetupRace(); motor = gameObject.AddComponent<AudioSource>(); motor.loop = true; motor.volume = .14f;
        var clip = AudioClip.Create("Two stroke",44100,1,44100,false); var samples = new float[44100];
        for(int i=0;i<samples.Length;i++) samples[i] = Mathf.Sin(i*2*Mathf.PI*65/44100)*.5f + Mathf.Sin(i*2*Mathf.PI*130/44100)*.2f;
        clip.SetData(samples,0); motor.clip = clip; motor.Play();
        ResetRide();
    }

    Material Mat(string n,string hex) { ColorUtility.TryParseHtmlString(hex,out var c); var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")); m.name=n; m.color=c; m.SetFloat("_Smoothness",.18f); return m; }
    Transform Shape(string n, PrimitiveType type, Vector3 p, Vector3 s, Material m, Transform parent=null)
    { var o=GameObject.CreatePrimitive(type); o.name=n; o.transform.SetParent(parent!=null?parent:environmentRoot,false); o.transform.localPosition=p; o.transform.localScale=s; o.GetComponent<Renderer>().sharedMaterial=m; var collider=o.GetComponent<Collider>(); if(collider && !SolidStreetShape(n)) { collider.enabled=false; Destroy(collider); } return o.transform; }
    Transform Box(string n,Vector3 p,Vector3 s,Material m,Transform parent=null) => Shape(n,PrimitiveType.Cube,p,s,m,parent);
    void Beam(string n, Vector3 a, Vector3 b,float width,Material m,Transform parent=null) { var t=Box(n,(a+b)/2,new Vector3(width,(b-a).magnitude,width),m,parent); t.up=(b-a).normalized; }
    void Label(string text,Vector3 p,float size,Color color,Quaternion rotation)
    { var o=new GameObject(text); o.transform.SetParent(environmentRoot,false); o.transform.localPosition=p; o.transform.rotation=rotation; var t=o.AddComponent<TextMesh>(); t.text=text; t.fontSize=64; t.characterSize=size*.62f; t.anchor=TextAnchor.MiddleCenter; t.color=color; var f=font; t.font=f; o.GetComponent<MeshRenderer>().sharedMaterial=WorldFontMaterial(f); }

    void BuildTown()
    {
        Random.InitState(27);
        Box("Southern earth",new Vector3(-40,-.22f,65),new Vector3(420,.4f,360),cream);
        Box("Old town straight",new Vector3(0,.01f,38),new Vector3(12,.08f,110),asphalt);
        Box("Night market street",new Vector3(-72,.01f,97),new Vector3(148,.08f,12),asphalt);
        // The bend is a genuine quarter-circle road, with raised pavements outside it.
        for(int i=0;i<32;i++) { float a=i/31f*Mathf.PI/2; var p=new Vector3(-13+Mathf.Cos(a)*13,.02f,84+Mathf.Sin(a)*13); var t=Box("Corner paving",p,new Vector3(12,.09f,1.1f),asphalt); t.rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0); }
        for(int z=-10;z<80;z+=9) Box("Road centre dash",new Vector3(0,.07f,z),new Vector3(.12f,.015f,3),white);
        for(int x=-135;x<-18;x+=9) Box("Road centre dash",new Vector3(x,.07f,97),new Vector3(3,.015f,.12f),white);
        var yellow=Mat("Faded yellow", "#edc557");
        for(int side=-1;side<=1;side+=2) { Box("Gutter",new Vector3(side*6.25f,.06f,35),new Vector3(.4f,.15f,102),dark); Box("Yellow curb",new Vector3(side*5.6f,.07f,35),new Vector3(.1f,.02f,102),yellow); }
        // Coral-stone wall, individual courses and battlements.
        for(int z=-12;z<96;z+=4) {
            Box("Coral stone city wall",new Vector3(9,1.7f,z),new Vector3(2.3f,3.4f,4),brick);
            for(int y=0;y<4;y++) for(int k=0;k<3;k++) Box("Stone joint",new Vector3(7.83f,.3f+y*.8f,z-1.8f+k*1.4f+(y%2)*.4f),new Vector3(.025f,.035f,1.3f),cream);
            Box("Wall cap",new Vector3(9,3.5f,z),new Vector3(2.6f,.25f,4),cream);
            Box("Battlement",new Vector3(9,3.94f,z),new Vector3(2.2f,.8f,1.7f),brick);
        }
        // Gatehouse frames the first-person approach.
        for(int side=-1;side<=1;side+=2) Box("Gate pier",new Vector3(side*5.7f,2.8f,64),new Vector3(2.7f,5.6f,4),brick);
        Box("City gate lintel",new Vector3(0,5.5f,64),new Vector3(14,2.1f,4),brick);
        Box("Gate stone plaque",new Vector3(0,5.5f,61.94f),new Vector3(3,.9f,.1f),cream);
        Label("南 門",new Vector3(0,5.5f,61.85f),.18f,new Color(.2f,.18f,.14f),Quaternion.identity);
        var roof=Mat("Terracotta", "#a14e36");
        Box("Gatehouse",new Vector3(0,7.3f,64),new Vector3(8,1.7f,3.6f),white);
        for(int i=0;i<5;i++) Box("Layered swallowtail roof",new Vector3(0,8.1f+i*.15f,64),new Vector3(10-i*.9f,.18f,5-i*.45f),roof);
        for(int z=-5;z<77;z+=12) { Shop(new Vector3(-10,0,z),false,z); Palm(new Vector3(-7.3f,0,z+5)); }
        for(int x=-28;x>-145;x-=11) { Shop(new Vector3(x,0,107),true,-x); Stall(new Vector3(x,0,89),-x); Palm(new Vector3(x-4,0,112)); }
        var red=Mat("Lantern red", "#f05236");
        for(int z=16;z<=76;z+=15) { Beam("Festival cable",new Vector3(-9,6,z),new Vector3(9,6,z),.035f,dark); for(int x=-6;x<=6;x+=3) { Shape("Paper lantern",PrimitiveType.Sphere,new Vector3(x,5.7f,z),new Vector3(.65f,.85f,.65f),red); Box("Lantern tassel",new Vector3(x,5.12f,z),new Vector3(.04f,.35f,.04f),yellow); } }
        for(int x=-30;x>-140;x-=15) { Beam("Market cable",new Vector3(x,6,88),new Vector3(x,6,107),.03f,dark); for(int z=90;z<106;z+=3) Shape("Night market lantern",PrimitiveType.Sphere,new Vector3(x,5.6f,z),new Vector3(.7f,.9f,.7f),red); }
        var sea=Mat("Ocean", "#438f99"); Box("Taiwan Strait",new Vector3(-95,-.12f,170),new Vector3(300,.1f,75),sea);
        for(int i=0;i<9;i++) Shape("Distant hills",PrimitiveType.Sphere,new Vector3(35+i*22,2,145+Random.Range(0,40)),new Vector3(45,22+Random.Range(0,28),45),Mat("Hill "+i,"#728d82"));
        for(int i=0;i<12;i++) Shape("Summer cloud",PrimitiveType.Sphere,new Vector3(Random.Range(-130,110),Random.Range(35,55),Random.Range(80,170)),new Vector3(22,5,9),white);
        Box("Turn arrow sign",new Vector3(2,2.9f,83),new Vector3(3,1.6f,.2f),yellow); Label("❮ ❮",new Vector3(2,2.9f,82.85f),.4f,Color.black,Quaternion.identity);
    }
    void Shop(Vector3 p,bool market,int seed)
    {
        seed=Mathf.Abs(seed); string[] colors={"#d4b79a","#79a99b","#ce8667","#ede0b8"}; var facade=Mat("Painted concrete",colors[seed%4]); float h=Random.Range(6,10);
        Box("Townhouse",p+Vector3.up*h/2,new Vector3(market?10:7,h,market?7:10),facade);
        if(market) {
            Box("Shop awning",p+new Vector3(0,3,-4),new Vector3(10,.25f,2),teal);
            for(int i=-1;i<=1;i++) Box("Upper window",p+new Vector3(i*2.7f,5,-3.55f),new Vector3(1.5f,1.8f,.1f),dark);
            string[] names={"墾丁大街","波霸奶茶","南國熱炒","海邊走走"}; Label(names[seed%4],p+new Vector3(0,3.9f,-3.62f),.3f,new Color(.16f,.23f,.22f),Quaternion.identity);
        } else {
            Box("Striped awning",p+new Vector3(4,3,0),new Vector3(2,.2f,10),teal);
            for(int i=-1;i<=1;i++) Box("Window",p+new Vector3(3.55f,5,i*2.6f),new Vector3(.1f,1.8f,1.5f),dark);
            Label(seed%2==0?"恆春・冰店":"南國機車行",p+new Vector3(3.62f,3.9f,0),.25f,new Color(.15f,.24f,.23f),Quaternion.Euler(0,90,0));
        }
    }
    void Stall(Vector3 p,int seed) {
        var coral=Mat("Stall vermilion","#e3774f"); Box("Food cart",p+Vector3.up*.8f,new Vector3(7,1.6f,2.5f),coral);
        for(int s=-1;s<=1;s+=2) Beam("Canopy post",p+new Vector3(s*3,0,0),p+new Vector3(s*3,3.3f,0),.1f,dark);
        for(int i=0;i<7;i++) Box("Canvas stripe",p+new Vector3(i-3,3.2f,0),new Vector3(.98f,.18f,3.2f),i%2==0?white:coral);
        Label(seed%2==0?"炭烤魷魚":"現打果汁",p+new Vector3(0,2.5f,1.3f),.23f,Color.white,Quaternion.identity);
        for(int i=0;i<5;i++) Shape("Market produce",PrimitiveType.Sphere,p+new Vector3(i-2,1.8f,0),Vector3.one*.4f,teal);
    }
    void Palm(Vector3 p) {
        var trunk=Mat("Palm trunk","#97795b"); var leaf=Mat("Palm leaf","#3f7860"); Beam("Palm trunk",p,p+new Vector3(.7f,8,0),.32f,trunk);
        for(int i=0;i<8;i++) { float a=i*Mathf.PI/4; var start=p+new Vector3(.7f,8,0); var end=start+new Vector3(Mathf.Cos(a)*3.2f,-.8f,Mathf.Sin(a)*3.2f); var t=Shape("Palm frond",PrimitiveType.Sphere,(start+end)/2,new Vector3(.65f,.18f,4.2f),leaf); t.rotation=Quaternion.LookRotation(end-start); }
    }
    void BuildScooter()
    {
        scooter=new GameObject("Two riders • island scooter").transform;
        Box("Seafoam body",new Vector3(0,.65f,0),new Vector3(.65f,.65f,1.65f),teal,scooter);
        Shape("Front fairing",PrimitiveType.Sphere,new Vector3(0,.95f,.7f),new Vector3(.75f,1.1f,.45f),teal,scooter);
        Box("Black saddle",new Vector3(0,1.05f,-.25f),new Vector3(.62f,.18f,1.2f),dark,scooter);
        var rubber=Mat("Rubber","#222a2b");
        frontWheel=Shape("Front tyre",PrimitiveType.Cylinder,new Vector3(0,.36f,.8f),new Vector3(.65f,.13f,.65f),rubber,scooter); frontWheel.localRotation=Quaternion.Euler(0,0,90);
        rearWheel=Shape("Rear tyre",PrimitiveType.Cylinder,new Vector3(0,.36f,-.75f),new Vector3(.65f,.15f,.65f),rubber,scooter); rearWheel.localRotation=Quaternion.Euler(0,0,90);
        Box("Handlebar",new Vector3(0,1.48f,.65f),new Vector3(1.1f,.09f,.12f),dark,scooter);
        Box("Dashboard",new Vector3(0,1.45f,.58f),new Vector3(.42f,.16f,.3f),dark,scooter);
        Shape("Headlamp",PrimitiveType.Sphere,new Vector3(0,1.35f,.93f),new Vector3(.35f,.22f,.1f),white,scooter);
        for(int s=-1;s<=1;s+=2) { Beam("Mirror stem",new Vector3(s*.48f,1.48f,.65f),new Vector3(s*.65f,1.83f,.7f),.035f,dark,scooter); Shape("Mirror",PrimitiveType.Sphere,new Vector3(s*.65f,1.85f,.7f),new Vector3(.24f,.16f,.08f),white,scooter); }
        Rider(.05f,false); Rider(-.65f,true);
    }
    void Rider(float z,bool girl) { DetailedRider(z,girl); }

    void ResetRide() { ResetRace(); }
    void Begin() { StartRace(); }
    bool Held(Key k) => Keyboard.current!=null && Keyboard.current[k].isPressed;
    bool Press(Key k) => Keyboard.current!=null && Keyboard.current[k].wasPressedThisFrame;
    Vector3 Route(float d) { return CoursePosition(d); }
    float Heading(float d) { return CourseHeading(d); }
    void Update() { RaceUpdate(); }
    GUIStyle Style(int size,Color c,FontStyle weight=FontStyle.Normal,TextAnchor align=TextAnchor.UpperLeft) => new GUIStyle(GUI.skin.label){font=font,fontSize=size,fontStyle=weight,normal={textColor=c},alignment=align};
    void Text(string s,float x,float y,float w,float h,int size,Color color,FontStyle weight=FontStyle.Normal,TextAnchor align=TextAnchor.UpperLeft) => GUI.Label(new Rect(x,y,w,h),s,Style(size,color,weight,align));
    void Panel(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,pixel);GUI.color=Color.white;}
    bool Button(string s,Rect r) { Panel(r,new Color(.05f,.17f,.18f,.94f)); return GUI.Button(r,s,new GUIStyle(GUI.skin.button){font=font,fontSize=19,normal={textColor=new Color(1,.94f,.8f),background=null},hover={textColor=Color.white,background=null},active={textColor=Color.white,background=null}}); }
    void OnGUI() { RaceGUI(); }
}


