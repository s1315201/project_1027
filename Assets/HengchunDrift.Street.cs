using System.Collections.Generic;
using UnityEngine;

public partial class HengchunDrift
{
    class Walker { public Transform root, leftLeg, rightLeg, leftArm, rightArm; public Vector3 origin; public float phase, travel, pace, walkClock; public int gesture; public Transform head, cameraProp; }
    readonly List<Walker> walkers = new List<Walker>();
    readonly Dictionary<string, Material> streetMaterials = new Dictionary<string, Material>();
    Material M(string hex) { if(!streetMaterials.TryGetValue(hex,out var m)){m=Mat(hex,hex);streetMaterials.Add(hex,m);}return m; }
    Material Glow(string hex) { var m=M(hex);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",m.color*2);return m; }
    void Lamp(Vector3 p, Color color, float power=4, float range=10) { var l=new GameObject("Warm market bulb").AddComponent<Light>();l.transform.SetParent(environmentRoot,false);l.transform.localPosition=p;l.type=LightType.Point;l.color=color;l.intensity=power;l.range=range; }
    void BuildStreetLife()
    {
        // Physical surface variation, made locally rather than repeating flat colour.
        Texture2D roadTex=new Texture2D(128,128);var pixels=new Color[128*128];
        for(int i=0;i<pixels.Length;i++){float n=Random.Range(.78f,1.15f);pixels[i]=new Color(n,n,n);}
        roadTex.SetPixels(pixels);roadTex.Apply();roadTex.wrapMode=TextureWrapMode.Repeat;asphalt.mainTexture=roadTex;asphalt.mainTextureScale=new Vector2(7,25);
        var stoneTex=new Texture2D(128,128);
        for(int y=0;y<128;y++)for(int x=0;x<128;x++){bool mortar=y%16<2||(x+(y/16%2)*16)%32<2;float n=mortar?.46f:Random.Range(.76f,1.15f);pixels[y*128+x]=new Color(n,n*.97f,n*.94f);}
        stoneTex.SetPixels(pixels);stoneTex.Apply();brick.mainTexture=stoneTex;brick.mainTextureScale=new Vector2(2,2);
        // Voussoirs describe the recognisable rounded opening of the south gate.
        for(int i=0;i<21;i++){float a=(i+.5f)/21*Mathf.PI;var t=Box("Arch wedge stone",new Vector3(Mathf.Cos(a)*4.48f,2.25f+Mathf.Sin(a)*2.25f,61.88f),new Vector3(.72f,.55f,.18f),cream);t.rotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90);}
        for(int s=-1;s<=1;s+=2)for(int z=0;z<4;z++){var e=Box("Upturned roof corner",new Vector3(s*(4.1f+z*.35f),8.15f+z*z*.06f,64),new Vector3(.65f,.17f,4.1f-z*.35f),M("#785047"));e.rotation=Quaternion.Euler(0,0,s*z*10);}
        for(int x=-3;x<=3;x+=2)Box("Gatehouse timber pillar",new Vector3(x,7.3f,62.2f),new Vector3(.18f,1.9f,.2f),M("#6b3228"));
        Lamp(new Vector3(0,5,59),new Color(1,.61f,.27f),8,18);
        // Curb, painted pedestrian crossings, utility wires and parked scooters.
        for(int x=-135;x<-20;x+=6){Box("Red curb paint",new Vector3(x,.12f,103),new Vector3(3,.17f,.25f),M("#b54e43"));Box("Market sidewalk",new Vector3(x,.08f,104),new Vector3(6,.2f,1.5f),M("#89857c"));}
        for(int i=0;i<7;i++)Box("Zebra crossing",new Vector3(-18+i*.8f,.085f,97),new Vector3(.45f,.02f,9),white);
        for(int x=-28;x>-144;x-=11) {
            // Previous generic carts are replaced by distinctive narrow vendor carts.
            DetailedStall(new Vector3(x,0,88.7f),(-x-28)/11);
            if(x%2==0)ParkedScooter(new Vector3(x+3,.12f,103),-30+x%20,(-x)%3);
            Box("Concrete power pole",new Vector3(x+4,4.5f,108),new Vector3(.22f,9,.22f),M("#8c8a83"));
            Beam("Overhead utility wire",new Vector3(x+4,8.5f,108),new Vector3(x-7,8.5f,108),.025f,dark);
            Lamp(new Vector3(x,4,95),new Color(1,.68f,.36f),5,12);
            Box("Roof water tank",new Vector3(x,10.2f,109),new Vector3(2,1.8f,2),M("#8d989b"));
            Box("Air conditioner",new Vector3(x+3,4.4f,103.3f),new Vector3(1,.65f,.6f),M("#a8aea9"));
            for(int k=0;k<3;k++)Box("Condenser grille",new Vector3(x+2.7f+k*.3f,4.4f,102.98f),new Vector3(.04f,.47f,.02f),dark);
        }
        // A few stalls and residents along the approach keep the first-person opening alive.
        for(int z=9;z<57;z+=15){var p=new Vector3(-7.7f,0,z);Person(p,90,z,false);ParkedScooter(new Vector3(-5,.05f,z+4),18,z%3);}
        for(int i=0;i<34;i++){
            float x=-23-(i%17)*6.5f;float z=i<17?92:101.8f;
            Person(new Vector3(x,0,z),i%2==0?90:-90,i,true);
        }
        // Remove obsolete identical counters (their replacement has unique stock and games).
        foreach(var t in FindObjectsByType<Transform>())if(t.name=="Food cart"||t.name=="Market produce"||t.name=="Canvas stripe"||t.name=="Canopy post"||t.name=="炭烤魷魚"||t.name=="現打果汁")Destroy(t.gameObject);
    }
    void DetailedStall(Vector3 p,int index)
    {
        var previousEnvironment=environmentRoot;
        var realisticStall=new GameObject("Working night market stall "+index).transform;
        realisticStall.SetParent(environmentRoot,false);environmentRoot=realisticStall;
        int kind=index%10;string[] names={"炭烤大魷魚","酥炸鹽酥雞","南國現打果汁","手工冰淇淋","套圈圈","射氣球","滷味・關東煮","大阪章魚燒","炭烤玉米","現炸臭豆腐"};
        string[] colors={"#b44835","#d39c32","#4f9c65","#ce8198","#477eac","#9a5985","#b16d35","#be6942","#86a955","#9176a0"};var paint=M(colors[kind]);
        Box("Vendor cart · "+names[kind],p+new Vector3(0,.8f,0),new Vector3(6,1.6f,2),paint);
        Box("Stainless worktop",p+new Vector3(0,1.64f,0),new Vector3(6.2f,.12f,2.2f),M("#aeb6b4"));
        for(int s=-1;s<=1;s+=2)Beam("Cart frame",p+new Vector3(s*2.9f,.1f,-.6f),p+new Vector3(s*2.9f,3.7f,-.6f),.075f,dark);
        for(int i=0;i<10;i++){var t=Box("Striped canvas",p+new Vector3(-2.7f+i*.6f,3.6f,.2f),new Vector3(.59f,.12f,2.9f),i%2==0?paint:white);t.rotation=Quaternion.Euler(-7,0,0);}
        Box("Illuminated stall sign",p+new Vector3(0,2.96f,1.18f),new Vector3(5.8f,.75f,.12f),M("#24434b"));
        Label(names[kind],p+new Vector3(0,2.96f,1.26f),.16f,new Color(1,.94f,.69f),Quaternion.Euler(0,180,0));
        for(int i=-1;i<=1;i++){Shape("Warm hanging bulb",PrimitiveType.Sphere,p+new Vector3(i*2,3.2f,.7f),Vector3.one*.16f,Glow("#ffc071"));}
        DecorateStall(p,index,kind);
        DressRealStall(p,index,kind);
        Person(p+new Vector3(.7f,0,-1.25f),0,index+40,false);
        DressVendor(walkers[walkers.Count-1],index);
        if(kind==0){
            Box("Charcoal grill",p+new Vector3(-.8f,1.82f,0),new Vector3(2.9f,.23f,1.4f),dark);
            for(int i=0;i<10;i++)Box("Grill bars",p+new Vector3(-2+i*.27f,1.95f,0),new Vector3(.025f,.035f,1.35f),M("#a8a4a0"));
            for(int i=0;i<4;i++){float x=-1.8f+i*.65f;Shape("Grilled squid body",PrimitiveType.Capsule,p+new Vector3(x,2,.1f),new Vector3(.28f,.12f,.6f),M("#ca8768")).rotation=Quaternion.Euler(90,0,0);for(int k=0;k<4;k++)Beam("Squid tentacle",p+new Vector3(x-.1f+k*.06f,2,-.2f),p+new Vector3(x-.16f+k*.1f,2,-.6f),.035f,M("#c77354"));}
        }else if(kind==1){
            for(int i=0;i<3;i++){Box("Frying basket",p+new Vector3(-1.8f+i*1.25f,1.84f,0),new Vector3(1.1f,.18f,1.2f),M("#686f6c"));for(int j=0;j<12;j++)Shape("Fried chicken pieces",PrimitiveType.Sphere,p+new Vector3(-2.2f+i*1.25f+Random.value*.8f,2,Random.Range(-.4f,.4f)),Vector3.one*.18f,M("#d39a49"));}
        }else if(kind==2){
            for(int i=0;i<3;i++){Box("Juice blender base",p+new Vector3(-1.7f+i*1.4f,1.85f,0),new Vector3(.6f,.3f,.55f),dark);Shape("Blender jug",PrimitiveType.Cylinder,p+new Vector3(-1.7f+i*1.4f,2.18f,0),new Vector3(.45f,.2f,.45f),M(i==0?"#e76d56":i==1?"#f4bd48":"#98ba62"));}
            for(int i=0;i<7;i++)Shape("Fresh fruit",PrimitiveType.Sphere,p+new Vector3(-2+i*.65f,1.92f,.7f),Vector3.one*.32f,M(i%2==0?"#d8ad43":"#76a15a"));
        }else if(kind==3){
            Box("Ice cream freezer",p+new Vector3(-.8f,1.91f,0),new Vector3(3.8f,.45f,1.5f),white);
            for(int i=0;i<5;i++)Box("Ice cream tub",p+new Vector3(-2+i*.65f,2.16f,0),new Vector3(.55f,.07f,1.2f),M(i%2==0?"#f3b7a4":"#b7c98e"));
        }else if(kind==4){
            Box("Ring toss display",p+new Vector3(0,2,-.35f),new Vector3(5.2f,.15f,1.1f),M("#dfb36e"));
            for(int row=0;row<2;row++)for(int i=0;i<8;i++){var m=M(i%2==0?"#80afca":"#dc8f76");Shape("Ring toss prize bottle",PrimitiveType.Cylinder,p+new Vector3(-2.2f+i*.6f,2.23f,-.6f+row*.6f),new Vector3(.22f,.2f,.22f),m);Shape("Bottle neck",PrimitiveType.Cylinder,p+new Vector3(-2.2f+i*.6f,2.48f,-.6f+row*.6f),new Vector3(.1f,.1f,.1f),m);}
        }else if(kind==5){
            Box("Balloon game backboard",p+new Vector3(0,2.6f,-.8f),new Vector3(5.3f,1.65f,.12f),dark);
            for(int row=0;row<3;row++)for(int i=0;i<8;i++)Shape("Target balloon",PrimitiveType.Sphere,p+new Vector3(-2.2f+i*.63f,2.05f+row*.47f,-.65f),new Vector3(.3f,.39f,.25f),M(new[]{"#e86568","#73b9d0","#edc44d","#9cc078"}[(i+row)%4]));
            Box("Toy rifle",p+new Vector3(-1.1f,1.89f,.3f),new Vector3(1.5f,.12f,.14f),dark);
        }else if(kind==6){
            for(int i=0;i<4;i++){Shape("Broth pot",PrimitiveType.Cylinder,p+new Vector3(-1.9f+i*1.2f,1.94f,0),new Vector3(.9f,.2f,.9f),M("#7c898b"));Shape("Broth surface",PrimitiveType.Cylinder,p+new Vector3(-1.9f+i*1.2f,2.15f,0),new Vector3(.77f,.015f,.77f),M("#88542f"));for(int k=0;k<4;k++)Beam("Oden skewer",p+new Vector3(-2.1f+i*1.2f+k*.12f,2,-.1f),p+new Vector3(-2.1f+i*1.2f+k*.12f,2.5f,-.1f),.025f,cream);}
        }
        if(kind==7){
            Box("Takoyaki iron plate",p+new Vector3(-.6f,1.83f,0),new Vector3(3.8f,.15f,1.45f),dark);
            for(int row=0;row<3;row++)for(int ball=0;ball<8;ball++){
                Shape("Takoyaki ball",PrimitiveType.Sphere,p+new Vector3(-2.1f+ball*.42f,1.95f,-.44f+row*.42f),Vector3.one*.24f,M("#c69053"));
                Box("Takoyaki sauce",p+new Vector3(-2.1f+ball*.42f,2.07f,-.44f+row*.42f),new Vector3(.15f,.014f,.065f),M("#654330"));
            }
        }else if(kind==8){
            Box("Corn charcoal grill",p+new Vector3(-.7f,1.87f,0),new Vector3(3.6f,.2f,1.4f),dark);
            for(int cob=0;cob<6;cob++){
                var corn=Shape("Grilled corn cob",PrimitiveType.Capsule,p+new Vector3(-2+cob*.49f,2.04f,0),new Vector3(.24f,.42f,.24f),M("#e8c34d"));corn.localRotation=Quaternion.Euler(90,0,0);
                Beam("Corn skewer",p+new Vector3(-2+cob*.49f,2.04f,.25f),p+new Vector3(-2+cob*.49f,2.04f,.7f),.018f,cream);
            }
        }else if(kind==9){
            Box("Tofu frying pan",p+new Vector3(-1,1.87f,0),new Vector3(3.1f,.18f,1.3f),M("#626a67"));
            for(int i=0;i<12;i++)Box("Golden fried tofu",p+new Vector3(-2.1f+(i%4)*.65f,2.02f,-.4f+(i/4)*.4f),new Vector3(.42f,.18f,.28f),M("#ce9d53"));
            Shape("Pickled cabbage bowl",PrimitiveType.Cylinder,p+new Vector3(1.55f,1.93f,.2f),new Vector3(.7f,.16f,.7f),white);
            for(int i=0;i<5;i++)Shape("Pickled cabbage",PrimitiveType.Sphere,p+new Vector3(1.35f+i*.09f,2.12f,.2f),new Vector3(.18f,.07f,.22f),M("#b4be72"));
        }
        if(index%2==0){Box("Red plastic stool",p+new Vector3(3.5f,.43f,.2f),new Vector3(.5f,.15f,.5f),M("#c34b3e"));for(int i=-1;i<=1;i+=2)Box("Stool leg",p+new Vector3(3.5f+i*.2f,.2f,.2f),new Vector3(.07f,.4f,.45f),M("#c34b3e"));}
        FinishRealStall(realisticStall);environmentRoot=previousEnvironment;
    }
    void Person(Vector3 p,float yaw,int variant,bool walking)
    {
        var r=new GameObject(walking?"Night market visitor "+variant:"Vendor / local "+variant).transform;r.SetParent(environmentRoot,false);r.localPosition=p;r.localRotation=Quaternion.Euler(0,yaw,0);
        bool girl=variant%3==0;var complexion=M(variant%2==0?"#d2a380":"#b88362");var shirt=M(new[]{"#e7d9c1","#242e36","#8b9d89","#a25445","#c2a477","#657e9e"}[variant%6]);var pants=M(new[]{"#2f4054","#bba88a","#393e3c","#a16c67"}[variant%4]);
        Loft(girl?"Summer blouse":"Loose T shirt",new Vector3(0,.94f,0),new float[,]{{0,.19f,.135f},{.18f,.2f,.14f},{.4f,.24f,.15f},{.49f,.2f,.12f},{.53f,.075f,.08f}},shirt,r);
        Box("Shorts",new Vector3(0,.85f,0),new Vector3(.4f,.24f,.29f),pants,r);
        Loft("Head",new Vector3(0,1.52f,0),new float[,]{{0,.065f,.075f},{.05f,.1f,.1f},{.16f,.14f,.13f},{.26f,.14f,.12f},{.34f,.1f,.09f},{.36f,.02f,.02f}},complexion,r);
        Limb("Visitor neck",new Vector3(0,1.42f,0),new Vector3(0,1.57f,0),.065f,complexion,r);
        Shape("Visitor nose",PrimitiveType.Sphere,new Vector3(0,1.69f,.14f),new Vector3(.035f,.045f,.04f),complexion,r);
        Shape("Hair",PrimitiveType.Sphere,new Vector3(0,1.83f,-.025f),new Vector3(.3f,.18f,.29f),M("#342b29"),r);
        if(girl)Shape("Long hair",PrimitiveType.Capsule,new Vector3(0,1.57f,-.13f),new Vector3(.28f,.45f,.13f),M("#342b29"),r);
        for(int side=-1;side<=1;side+=2){Shape("Eye",PrimitiveType.Sphere,new Vector3(side*.065f,1.72f,.135f),Vector3.one*.026f,dark,r);}
        if(variant%5==0){Shape("Baseball cap",PrimitiveType.Sphere,new Vector3(0,1.9f,0),new Vector3(.34f,.16f,.34f),pants,r);Box("Cap brim",new Vector3(0,1.87f,.18f),new Vector3(.27f,.035f,.2f),pants,r);}
        if(variant%4==0){Box("Crossbody bag",new Vector3(.19f,1.03f,.16f),new Vector3(.22f,.28f,.09f),dark,r);Beam("Bag strap",new Vector3(-.2f,1.42f,.16f),new Vector3(.2f,1.05f,.16f),.035f,dark,r);}
        var w=new Walker{root=r,origin=p,phase=variant*.9f,pace=.5f+variant%4*.12f,travel=walking?2.4f:0};
        for(int side=-1;side<=1;side+=2){
            var leg=new GameObject("Leg pivot").transform;leg.SetParent(r,false);leg.localPosition=new Vector3(side*.12f,.85f,0);
            Shape("Bare calf",PrimitiveType.Capsule,new Vector3(0,-.36f,0),new Vector3(.12f,.59f,.13f),complexion,leg);Box("Sandals",new Vector3(0,-.77f,.06f),new Vector3(.16f,.06f,.28f),variant%2==0?blue:dark,leg);
            var arm=new GameObject("Arm pivot").transform;arm.SetParent(r,false);arm.localPosition=new Vector3(side*.25f,1.4f,0);Shape("Arm",PrimitiveType.Capsule,new Vector3(0,-.27f,0),new Vector3(.1f,.55f,.11f),complexion,arm);
            if(side<0){w.leftLeg=leg;w.leftArm=arm;}else{w.rightLeg=leg;w.rightArm=arm;}
        }
        if(variant%4==1)Shape("Drink cup",PrimitiveType.Cylinder,new Vector3(.26f,.9f,.09f),new Vector3(.1f,.09f,.1f),white,r);
        StyleVisitor(r,girl,variant); SetupVisitorGesture(w,variant); GiveVisitorSnack(w,variant); walkers.Add(w);
    }
    void AnimateStreetLife(float dt) { AnimateVisitors(dt); }
    void ParkedScooter(Vector3 p,float yaw,int variant)
    {
        var r=new GameObject("Parked Taiwanese commuter scooter").transform;r.SetParent(environmentRoot,false);r.localPosition=p;r.localRotation=Quaternion.Euler(0,yaw,8);var body=M(variant==0?"#d0c5ad":variant==1?"#6c8285":"#4d474d");
        Box("Scooter shell",new Vector3(0,.65f,0),new Vector3(.55f,.55f,1.5f),body,r);Box("Seat",new Vector3(0,.99f,-.25f),new Vector3(.53f,.14f,.9f),dark,r);Box("Leg shield",new Vector3(0,1,.5f),new Vector3(.55f,.7f,.18f),body,r);
        for(int z=-1;z<=1;z+=2){var t=Shape("Tyre",PrimitiveType.Cylinder,new Vector3(0,.3f,z*.65f),new Vector3(.55f,.12f,.55f),dark,r);t.localRotation=Quaternion.Euler(0,0,90);}
        Box("Bars",new Vector3(0,1.4f,.55f),new Vector3(.9f,.06f,.09f),dark,r);
    }
    void CustomizeScooter()
    {
        var body=M("#343d47");body.SetFloat("_Smoothness",.65f);
        foreach(var r in scooter.GetComponentsInChildren<Renderer>())if(r.name=="Seafoam body"||r.name=="Front fairing")r.sharedMaterial=body;
        Box("Taiwan registration plate",new Vector3(0,.65f,-.91f),new Vector3(.35f,.19f,.025f),white,scooter);
        var plate=new GameObject("Plate letters").AddComponent<TextMesh>();plate.transform.SetParent(scooter,false);plate.transform.localPosition=new Vector3(0,.65f,-.932f);plate.transform.localRotation=Quaternion.identity;plate.text="KDR-0927";plate.font=font;plate.fontSize=40;plate.characterSize=.012f;plate.anchor=TextAnchor.MiddleCenter;plate.color=Color.black;plate.GetComponent<Renderer>().sharedMaterial=WorldFontMaterial(font);
        var pipe=Shape("Titanium aftermarket exhaust",PrimitiveType.Cylinder,new Vector3(.43f,.42f,-.55f),new Vector3(.16f,.43f,.16f),M("#858d9b"),scooter);pipe.localRotation=Quaternion.Euler(90,0,0);
        Box("Exhaust tip",new Vector3(.43f,.42f,-1),new Vector3(.15f,.15f,.07f),dark,scooter);
        Box("Rear red lamp",new Vector3(0,.87f,-.86f),new Vector3(.4f,.13f,.06f),Glow("#ef4f3f"),scooter);
        for(int side=-1;side<=1;side+=2){Shape("Amber turn signal",PrimitiveType.Sphere,new Vector3(side*.29f,1.3f,.9f),new Vector3(.12f,.08f,.06f),Glow("#fca64e"),scooter);Beam("Red rear shock",new Vector3(side*.28f,.36f,-.72f),new Vector3(side*.28f,.9f,-.48f),.07f,M("#c84c42"),scooter);}
        // Small neck chain and ear studs keep the streetwear cues specific without caricature.
        
        Shape("Ear stud",PrimitiveType.Sphere,new Vector3(.18f,2.03f,.03f),Vector3.one*.035f,white,scooter);
        var beam=new GameObject("Scooter low beam").AddComponent<Light>();beam.transform.SetParent(scooter,false);beam.transform.localPosition=new Vector3(0,1.3f,1);beam.type=LightType.Spot;beam.color=new Color(1,.9f,.68f);beam.intensity=8;beam.range=25;beam.spotAngle=65;beam.transform.localRotation=Quaternion.Euler(15,0,0);
    }
    void AddFacadeDetails()
    {
        var concrete=new Texture2D(64,64);var c=new Color[4096];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float v=.7f+Mathf.PerlinNoise(x*.17f,y*.17f)*.28f+Random.value*.07f;c[y*64+x]=new Color(v,v,v);}
        concrete.SetPixels(c);concrete.Apply();
        foreach(var r in FindObjectsByType<MeshRenderer>())if(r.name=="Townhouse"){r.sharedMaterial.mainTexture=concrete;r.sharedMaterial.mainTextureScale=new Vector2(3,3);}
        for(int x=-28,index=0;x>-145;x-=11,index++){
            // Recessed ground-floor shopfronts, varied roller shutters and balcony rails.
            Box("Recessed shop opening",new Vector3(x,1.45f,103.43f),new Vector3(8.8f,2.9f,.1f),M("#28343b"));
            for(int s=-1;s<=1;s+=2){Box("Arcade column",new Vector3(x+s*4.5f,1.6f,102.7f),new Vector3(.28f,3.2f,.35f),M("#acada1"));Box("Window trim",new Vector3(x+s*2.7f,5,103.36f),new Vector3(1.7f,2,.06f),M("#ced0c2"));Box("Window glass",new Vector3(x+s*2.7f,5,103.3f),new Vector3(1.43f,1.74f,.06f),M("#41555e"));}
            Box("Shop sign frame",new Vector3(x,3.9f,103.32f),new Vector3(9,.9f,.18f),M(index%2==0?"#e4d0a4":"#bdcfc3"));
            for(int i=0;i<18;i++)Box("Balcony railing bar",new Vector3(x-4.1f+i*.48f,4.3f,102.6f),new Vector3(.035f,.8f,.035f),M("#737d7c"));
            Box("Balcony top rail",new Vector3(x,4.7f,102.6f),new Vector3(8.6f,.055f,.055f),M("#737d7c"));
            if(index%3==0){for(int i=0;i<13;i++)Box("Closed shutter slat",new Vector3(x-2, .22f+i*.19f,103.26f),new Vector3(3.5f,.16f,.07f),M("#777d79"));}
            else {Lamp(new Vector3(x,2,103),new Color(1,.82f,.58f),2.5f,5);Box("Interior shelf",new Vector3(x,1.2f,103.1f),new Vector3(5,.13f,.5f),cream);for(int i=0;i<7;i++)Box("Shop merchandise",new Vector3(x-2+i*.7f,1.5f,103.1f),new Vector3(.4f,.5f,.35f),M(i%2==0?"#c9a871":"#8baa9e"));}
            for(int s=-1;s<=1;s+=2){var wheel=Shape("Cart wheel",PrimitiveType.Cylinder,new Vector3(x+s*2.3f,.28f,89.3f),new Vector3(.45f,.07f,.45f),dark);wheel.rotation=Quaternion.Euler(90,0,0);}
            // Menu boards belong to their stalls, rather than floating generic headings.
            Box("Small price board",new Vector3(x+2.3f,1.2f,90),new Vector3(.85f,.75f,.06f),M("#263d36"));
            Label(index%2==0?"現 做\n50 元":"人氣\n招牌",new Vector3(x+2.3f,1.2f,90.05f),.19f,Color.white,Quaternion.Euler(0,180,0));
        }
        // Small visible spoke hubs and contrasting side panels improve the scooter silhouette.
        foreach(var t in FindObjectsByType<Transform>())if(t.name=="Summer cloud")Destroy(t.gameObject);
    }
}



