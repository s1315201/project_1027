using UnityEngine;

public partial class HengchunDrift
{
    Transform environmentRoot;
    AudioSource narrator;
    AudioClip narrationClip;
    int turnDirection=-1;
    bool timedOut;
    float Mirror => -turnDirection;
    void SetupMission()
    {
        narrator=gameObject.AddComponent<AudioSource>();narrator.spatialBlend=0;narrator.volume=.95f;
        narrationClip=Resources.Load<AudioClip>("Audio/MissionIntro");
        if(!narrationClip)Debug.LogWarning("MissionIntro narration audio is missing.");
        Box("集合站 finish marker",new Vector3(-91,.1f,97),new Vector3(1,.04f,10),white);
        Box("集合站 sign",new Vector3(-94,3.6f,104),new Vector3(5,.9f,.2f),M("#e8d2a3"));
        Label("集合站",new Vector3(-94,3.6f,103.85f),.28f,new Color(.1f,.2f,.2f),Quaternion.identity);
    }
    void SetTurnDirection(int direction)
    {
        turnDirection=direction<0?-1:1;
        if(!environmentRoot)return;
        environmentRoot.localScale=new Vector3(Mirror,1,1);
        // Counter-reflect glyphs so shop signs stay legible on either route.
        foreach(var text in environmentRoot.GetComponentsInChildren<TextMesh>()){
            var s=text.transform.localScale;s.x=Mathf.Abs(s.x)*Mirror;text.transform.localScale=s;
            if(text.name=="❮ ❮")text.text=turnDirection<0?"<<":">>";
        }
    }
    string MissionClock()
    {
        int remaining=Mathf.CeilToInt(Mathf.Max(0,180-rideTime));return (remaining/60).ToString("00")+":"+(remaining%60).ToString("00");
    }
    void MapLine(Vector2 a,Vector2 b,Color color,float width)
    {
        var matrix=GUI.matrix;var delta=b-a;
        GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg),Vector3.one);
        Panel(new Rect(0,-width/2,delta.magnitude,width),color);GUI.matrix=matrix;
    }
    Vector2 MapPoint(Vector3 p)
    {
        return new Vector2((turnDirection<0?252:58)+p.x*1.95f,289-p.z*1.55f);
    }
    void DrawMissionMap()
    {
        Panel(new Rect(28,104,250,218),new Color(.025f,.09f,.12f,.94f));
        for(int i=0;i<5;i++)MapLine(new Vector2(39,151+i*31),new Vector2(266,151+i*31),new Color(.18f,.27f,.29f,.7f),1);
        Text("集合站  /  路線導航",42,114,225,26,16,new Color(.9f,.91f,.8f));
        float end=CornerEnd+78;
        for(int i=0;i<80;i++){
            float a=end*i/80,b=end*(i+1)/80;MapLine(MapPoint(Route(a)),MapPoint(Route(b)),new Color(.29f,.4f,.42f),9);
            if(b>=distance)MapLine(MapPoint(Route(a)),MapPoint(Route(b)),new Color(.48f,1,.79f),4);
        }
        var finish=MapPoint(Route(end));Panel(new Rect(finish.x-5,finish.y-5,10,10),new Color(1,.72f,.3f));
        var pos=MapPoint(Route(distance));Panel(new Rect(pos.x-5,pos.y-5,10,10),Color.white);
        Text(distance<CornerEnd?(turnDirection<0?"下一彎：左轉":"下一彎：右轉"):"直行到集合站",42,291,225,26,16,new Color(.55f,1,.82f),FontStyle.Bold);
    }
    void DrawMissionClock()
    {
        var color=rideTime>150?new Color(1,.4f,.3f):new Color(1,.95f,.83f);
        Panel(new Rect(1070,103,335,84),new Color(.025f,.09f,.12f,.86f));
        Text("趕到墾丁大街集合站",1086,111,300,24,16,color);
        Text(MissionClock(),1086,134,300,48,38,color,FontStyle.Bold,TextAnchor.MiddleRight);
        if(narrator&&narrator.isPlaying && rideTime<8){
            Text("「妹，坐穩啦！三分鐘內趕到集合站！」",345,Screen.height/(Screen.width/1440f)-68,740,32,20,new Color(1,.95f,.83f),FontStyle.Normal,TextAnchor.MiddleCenter);
        }
    }

    // A smooth loft gives the tank top a waist, chest, shoulder and neck profile.
    Transform Loft(string name,Vector3 position,float[,] rings,Material material,Transform parent)
    {
        const int sides=24;int rows=rings.GetLength(0);var v=new Vector3[rows*(sides+1)];var uv=new Vector2[v.Length];var tris=new int[(rows-1)*sides*6];
        for(int y=0;y<rows;y++)for(int i=0;i<=sides;i++){
            float a=i*Mathf.PI*2/sides;int n=y*(sides+1)+i;v[n]=new Vector3(Mathf.Cos(a)*rings[y,1],rings[y,0],Mathf.Sin(a)*rings[y,2]);uv[n]=new Vector2(i/(float)sides,y/(float)(rows-1));
        }
        int ti=0;for(int y=0;y<rows-1;y++)for(int i=0;i<sides;i++){int a=y*(sides+1)+i,b=a+sides+1;tris[ti++]=a;tris[ti++]=b;tris[ti++]=a+1;tris[ti++]=a+1;tris[ti++]=b;tris[ti++]=b+1;}
        var mesh=new Mesh();mesh.name=name;mesh.vertices=v;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateBounds();
        var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=position;o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=material;return o.transform;
    }
    void Limb(string name,Vector3 a,Vector3 b,float radius,Material m,Transform parent)
    {
        var t=Shape(name,PrimitiveType.Capsule,(a+b)*.5f,new Vector3(radius*2,(b-a).magnitude*.5f+radius,radius*2),m,parent);t.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
    }
    void DetailedRider(float z,bool girl)
    {
        var r=new GameObject(girl?"小語 • 海灘台妹":"阿辰 • 南國台客").transform;r.SetParent(scooter,false);r.localPosition=new Vector3(0,0,z);
        var complexion=M(girl?"#dcb08f":"#bf8e6c");var hair=M(girl?"#50382d":"#262624");var cotton=M(girl?"#302b36":"#fff3dc");
        Loft("Fitted white sleeveless tank",new Vector3(0,1.16f,0),new float[,]{{0,.2f,.15f},{.11f,.19f,.14f},{.25f,.2f,.15f},{.41f,girl?.23f:.27f,.17f},{.51f,girl?.2f:.25f,.14f},{.55f,.075f,.08f}},cotton,r);
        Loft("Natural jaw and face",new Vector3(0,1.83f,.025f),new float[,]{{0,.075f,.08f},{.055f,.105f,.1f},{.14f,.145f,.125f},{.23f,.147f,.125f},{.31f,.13f,.11f},{.35f,.075f,.075f}},complexion,r);
        Limb("Neck",new Vector3(0,1.68f,0),new Vector3(0,1.91f,0),.067f,complexion,r);
        for(int s=-1;s<=1;s+=2){
            Shape("Ear",PrimitiveType.Sphere,new Vector3(s*.147f,2,.01f),new Vector3(.055f,.1f,.045f),complexion,r);
            Shape("Eye white",PrimitiveType.Sphere,new Vector3(s*.061f,2.035f,.142f),new Vector3(.061f,.027f,.015f),white,r);
            Shape("Brown iris",PrimitiveType.Sphere,new Vector3(s*.061f,2.035f,.151f),new Vector3(.022f,.024f,.009f),M("#3e3028"),r);
            Shape("Pupil",PrimitiveType.Sphere,new Vector3(s*.061f,2.035f,.156f),new Vector3(.011f,.016f,.005f),dark,r);
            Shape("Eye highlight",PrimitiveType.Sphere,new Vector3(s*.061f-.004f,2.041f,.16f),Vector3.one*.006f,white,r);
            Limb("Eyebrow",new Vector3(s*.035f,2.078f,.144f),new Vector3(s*.09f,2.073f,.135f),.008f,hair,r);
            Shape("Cheek",PrimitiveType.Sphere,new Vector3(s*.093f,1.988f,.108f),new Vector3(.09f,.062f,.05f),complexion,r);
            Shape("Grey helmet ear shell",PrimitiveType.Sphere,new Vector3(s*.161f,2.09f,-.035f),new Vector3(.055f,.16f,.2f),M("#82898b"),r);
        }
        Shape("Nose bridge",PrimitiveType.Sphere,new Vector3(0,2.01f,.149f),new Vector3(.032f,.071f,.043f),complexion,r);
        Shape("Nose tip",PrimitiveType.Sphere,new Vector3(0,1.986f,.169f),new Vector3(.045f,.032f,.035f),complexion,r);
        Shape("Upper lip",PrimitiveType.Sphere,new Vector3(0,1.948f,.13f),new Vector3(.066f,.012f,.018f),M(girl?"#ac6b66":"#926658"),r);
        Shape("Lower lip",PrimitiveType.Sphere,new Vector3(0,1.938f,.131f),new Vector3(.06f,.013f,.019f),M("#bb8274"),r);
        Loft("Open face grey helmet",new Vector3(0,2.1f,-.025f),new float[,]{{0,.176f,.165f},{.065f,.178f,.169f},{.12f,.15f,.145f},{.16f,.1f,.09f},{.18f,.003f,.003f}},M("#92999a"),r);
        if(girl){
            for(int i=0;i<8;i++)Limb("Layered chestnut hair",new Vector3(-.12f+i*.034f,2.12f,-.12f),new Vector3(-.12f+i*.034f,1.78f,-.17f),.024f,hair,r);
            Limb("High ponytail",new Vector3(.12f,2.16f,-.13f),new Vector3(.2f,1.79f,-.26f),.064f,hair,r);
            Shape("Hair tie",PrimitiveType.Sphere,new Vector3(.13f,2.1f,-.18f),Vector3.one*.08f,dark,r);
        }else for(int i=0;i<5;i++)Limb("Short fringe",new Vector3(-.11f+i*.052f,2.12f,.09f),new Vector3(-.1f+i*.049f,2.085f,.125f),.025f,hair,r);
        var shorts=M(girl?"#516b89":"#387c8b");
        for(int s=-1;s<=1;s+=2){
            var hip=new Vector3(s*.16f,1.14f,.035f);var knee=new Vector3(s*.32f,.91f,.32f);var ankle=new Vector3(s*.36f,.5f,.2f);
            Limb("Beach shorts leg",hip,knee,.14f,shorts,r);Limb("Calf",knee,ankle,.075f,complexion,r);
            Shape("Kneecap",PrimitiveType.Sphere,knee,new Vector3(.15f,.16f,.15f),complexion,r);
            Shape("Foot",PrimitiveType.Sphere,new Vector3(s*.36f,.47f,.28f),new Vector3(.15f,.085f,.29f),complexion,r);
            Box("Blue white slipper sole",new Vector3(s*.36f,.425f,.28f),new Vector3(.18f,.035f,.33f),blue,r);
            for(int toe=0;toe<4;toe++)Shape("Toe",PrimitiveType.Sphere,new Vector3(s*.36f-.05f+toe*.033f,.47f,.406f),new Vector3(.03f,.035f,.04f),complexion,r);
            Limb("Slipper Y strap",new Vector3(s*.36f-.07f,.5f,.24f),new Vector3(s*.36f,.51f,.37f),.014f,white,r);Limb("Slipper Y strap",new Vector3(s*.36f+.07f,.5f,.24f),new Vector3(s*.36f,.51f,.37f),.014f,white,r);
            var shoulder=new Vector3(s*(girl?.23f:.27f),1.66f,0);var elbow=new Vector3(s*.35f,1.38f,.24f);var hand=new Vector3(s*(girl?.28f:.49f),1.48f,girl?.43f:.6f);
            Limb("Upper arm",shoulder,elbow,girl?.057f:.07f,complexion,r);Limb("Forearm",elbow,hand,.053f,complexion,r);Shape("Elbow",PrimitiveType.Sphere,elbow,Vector3.one*.115f,complexion,r);
            Shape("Hand gripping",PrimitiveType.Sphere,hand,new Vector3(.11f,.09f,.13f),complexion,r);
            for(int finger=0;finger<4;finger++)Limb("Finger",hand+new Vector3(-.035f+finger*.023f,0,.025f),hand+new Vector3(-.035f+finger*.023f,-.04f,.07f),.01f,complexion,r);
            for(int i=0;i<3;i++){var stripe=Box("Beach shorts stripe",new Vector3(s*(.19f+i*.025f),1.11f,.195f+i*.017f),new Vector3(.018f,.19f,.018f),M(girl?"#e9b39a":"#a4bdc1"),r);stripe.localRotation=Quaternion.Euler(0,0,s*-25);}
            Shape("Silver ear stud",PrimitiveType.Sphere,new Vector3(s*.171f,1.99f,.033f),Vector3.one*.023f,white,r);
        }
        StyleRider(r,girl);
        Limb("Neck chain left",new Vector3(-.09f,1.72f,.1f),new Vector3(0,1.59f,.17f),.008f,M(girl?"#cfb890":"#e4ae39"),r);Limb("Neck chain right",new Vector3(.09f,1.72f,.1f),new Vector3(0,1.59f,.17f),.008f,M(girl?"#cfb890":"#e4ae39"),r);
        RealHumanFace(r,true,girl);
    }
}
