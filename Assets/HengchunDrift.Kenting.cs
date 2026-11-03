using UnityEngine;

public partial class HengchunDrift
{
    const float RoadHalfWidth=4.6f; // 9.2 m asphalt; occupied edges leave a narrow shared street.
    Texture2D kentingOpening;
    float finaleDriftPoints,driftYaw;
    void UpdateFinaleDrift(float dt,float curvature)
    {
        if(distance<800){driftYaw=0;return;}
        float steer=SteeringInput;
        float target=steer*slip*25;
        driftYaw=Mathf.Lerp(driftYaw,target,1-Mathf.Exp(-dt*5));
        if(speed>3&&Mathf.Abs(steer)>.1f){
            float gain=dt*speed*slip*2;
            finaleDriftPoints+=gain;driftPoints+=gain;
        }
    }

    void DrawKentingOpening(float height)
    {
        if(!kentingOpening)kentingOpening=Resources.Load<Texture2D>("KentingOpening");
        if(kentingOpening)GUI.DrawTexture(new Rect(0,0,1440,height),kentingOpening,ScaleMode.ScaleAndCrop);
        Panel(new Rect(0,0,650,height),new Color(.015f,.035f,.06f,.73f));
        Text("A/D 轉向 · 最後 200m 左右鍵漂移 · 長按 0.45 秒留胎痕",48,height*.25f+163,550,24,17,new Color(1,.85f,.55f));
    }
    void BuildKentingEntrance()
    {
        for(int side=-1;side<=1;side+=2){
            var root=new GameObject("墾丁大街觀光入口柱").transform;root.SetParent(courseRoot,false);
            root.localPosition=new Vector3(side*5.8f,0,9);
            Box("Tourism pillar footing",new Vector3(0,.15f,0),new Vector3(1.3f,.3f,1.3f),brick,root);
            var pillar=Box("Kenting tourism stone pillar",new Vector3(0,2.3f,0),new Vector3(.85f,4.3f,.85f),M("#d7c6a1"),root);
            pillar.gameObject.AddComponent<BoxCollider>();
            Box("Illuminated tourism plaque",new Vector3(0,2.5f,-.44f),new Vector3(.66f,3.2f,.04f),Glow("#294854"),root);
            environmentRoot=root;
            Label("墾\n丁\n大\n街",new Vector3(0,2.6f,-.48f),.19f,new Color(1,.89f,.59f),Quaternion.identity);
            Label("KENTING",new Vector3(0,.65f,-.48f),.055f,Color.white,Quaternion.identity);
            Palm(new Vector3(side*1.8f,0,2));
            Lamp(new Vector3(0,4.4f,-.5f),new Color(1,.79f,.43f),6,9);
        }
        environmentRoot=courseRoot;
        for(int d=800;d<1000;d+=12){
            var q=Quaternion.Euler(0,CourseHeading(d),0);var p=CoursePosition(d);
            for(int side=-1;side<=1;side+=2){
                var mark=Box("Drift zone luminous curb",p+q*new Vector3(side*4.5f,.12f,0),new Vector3(.16f,.14f,4),Glow(side<0?"#e1aa46":"#56a4bc"));mark.rotation=q;
            }
        }
        var entry=new GameObject("Drift sector entry").transform;entry.SetParent(courseRoot,false);entry.localPosition=CoursePosition(800);entry.localRotation=Quaternion.Euler(0,CourseHeading(800),0);
        Box("Drift sector banner",new Vector3(0,5,0),new Vector3(8,.85f,.1f),Glow("#39495b"),entry);
        environmentRoot=entry;Label("最後 200 m · 漂移連彎",new Vector3(0,5,-.08f),.19f,new Color(1,.86f,.43f),Quaternion.identity);environmentRoot=courseRoot;
    }
    void DecorateStall(Vector3 p,int index,int kind)
    {
        string[] menus={"炭火現烤\n大魷魚 150","九層塔鹽酥雞\n現點現炸","西瓜汁・木瓜牛奶\n冰涼現打","手工冰淇淋\n雙球 80","套圈圈\n夜市限定獎品","射氣球\n一起挑戰！","滷味・關東煮\n熱湯免費","章魚燒\n柴魚美乃滋","炭烤玉米\n獨家醬汁","臭豆腐\n台式泡菜"};
        string[] accents={"#ef9c47","#e6cb5d","#73c49c","#df9eba","#79b5db","#ca98d5","#e9ad71","#df9b72","#c8d66e","#c2a0db"};
        Box("Glowing menu lightbox",p+new Vector3(-2.05f,2.1f,1.25f),new Vector3(1.35f,.65f,.08f),Glow(accents[kind]));
        Label(menus[kind],p+new Vector3(-2.05f,2.1f,1.31f),.04f,new Color(.13f,.13f,.12f),Quaternion.Euler(0,180,0));
        for(int side=-1;side<=1;side+=2){
            Beam("Stall LED edge",p+new Vector3(side*2.9f,2.6f,1.28f),p+new Vector3(side*2.9f,3.35f,1.28f),.035f,Glow(accents[kind]));
        }
        // Different silhouettes: tall flag, compact cart, open umbrella and hanging menu.
        if(index%4==0){
            Beam("Promotional flag pole",p+new Vector3(3.3f,0,0),p+new Vector3(3.3f,4.4f,0),.035f,dark);
            Box("Vertical vendor flag",p+new Vector3(3.65f,3.5f,0),new Vector3(.65f,1.7f,.035f),M(accents[kind]));
            Label("人\n氣\n必\n吃",p+new Vector3(3.65f,3.5f,.03f),.085f,Color.black,Quaternion.Euler(0,180,0));
        }else if(index%4==1){
            Beam("Parasol pole",p+new Vector3(-3.4f,0,-.3f),p+new Vector3(-3.4f,3.2f,-.3f),.055f,dark);
            var canopy=Shape("Round market parasol",PrimitiveType.Sphere,p+new Vector3(-3.4f,3.1f,-.3f),new Vector3(2.1f,.35f,2.1f),M(accents[kind]));
            for(int i=0;i<3;i++)Box("Produce crate",p+new Vector3(-2.2f+i*.75f,.25f,1.4f),new Vector3(.64f,.5f,.45f),M("#a78254"));
        }else if(index%4==2){
            Box("Hanging menu strip",p+new Vector3(.6f,2.2f,1.25f),new Vector3(2.5f,.46f,.035f),white);
            Label("現 做 · 排 隊 美 食",p+new Vector3(.6f,2.2f,1.3f),.07f,new Color(.55f,.17f,.12f),Quaternion.Euler(0,180,0));
        }
        if(index%2==0)Lamp(p+new Vector3(0,3.3f,1.4f),new Color(1,.86f,.68f),8,9);
        Box("Bright sign rim",p+new Vector3(0,3.4f,1.27f),new Vector3(5.8f,.045f,.035f),Glow(accents[kind]));
    }
}
