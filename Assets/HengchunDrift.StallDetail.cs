using UnityEngine;

public partial class HengchunDrift
{
    Material steamMaterial;
    void DressVendor(Walker vendor,int index)
    {
        vendor.gesture=0;var apron=RealSurface(M(index%2==0?"#344d58":"#78564a"),"fabric");
        Box("Vendor work apron",new Vector3(0,1.1f,.16f),new Vector3(.36f,.56f,.025f),apron,vendor.root);
        Box("Apron pocket",new Vector3(0,1.02f,.18f),new Vector3(.2f,.14f,.016f),apron,vendor.root);
        for(int side=-1;side<=1;side+=2)Beam("Apron shoulder strap",new Vector3(side*.12f,1.38f,.14f),new Vector3(side*.12f,1.52f,.06f),.025f,apron,vendor.root);
        Shape("Vendor working cap",PrimitiveType.Sphere,new Vector3(0,1.89f,0),new Vector3(.3f,.09f,.29f),apron,vendor.root);
        Shape("Food service glove",PrimitiveType.Sphere,new Vector3(0,-.49f,.02f),new Vector3(.11f,.1f,.075f),M("#dbd9c8"),vendor.rightArm);
    }
    void DressRealStall(Vector3 p,int index,int kind)
    {
        var steel=RealSurface(M("#aeb4ae"),"metal");var rubber=M("#282b29");
        for(int side=-1;side<=1;side+=2){
            Beam("Stainless cart leg",p+new Vector3(side*2.7f,.2f,-.65f),p+new Vector3(side*2.7f,1.63f,-.65f),.065f,steel);
            for(int front=-1;front<=1;front+=2){
                var wheel=Shape("Cart caster wheel",PrimitiveType.Cylinder,p+new Vector3(side*2.6f,.17f,front*.66f),new Vector3(.27f,.06f,.27f),rubber);wheel.localRotation=Quaternion.Euler(0,0,90);
            }
            Box("Folded canopy valance",p+new Vector3(side*2.8f,3.38f,1.45f),new Vector3(.18f,.3f,.03f),M(index%2==0?"#b75e48":"#4a807a"));
        }
        for(int panel=0;panel<6;panel++){
            Box("Brushed steel cart panel",p+new Vector3(-2.5f+panel, .9f,1.015f),new Vector3(.96f,1.25f,.035f),steel);
            for(int rivet=-1;rivet<=1;rivet+=2)Shape("Counter rivet",PrimitiveType.Sphere,p+new Vector3(-2.5f+panel+rivet*.4f,1.45f,1.045f),Vector3.one*.035f,dark);
        }
        Box("Wooden counter cutting board",p+new Vector3(.7f,1.75f,.58f),new Vector3(1.1f,.045f,.52f),RealSurface(M("#aa835b"),"matte"));
        Box("Folded vendor towel",p+new Vector3(1.6f,1.74f,.58f),new Vector3(.34f,.025f,.28f),RealSurface(white,"fabric"));
        for(int i=0;i<3;i++){
            Shape("Condiment bottle",PrimitiveType.Cylinder,p+new Vector3(2.05f+i*.21f,1.9f,.42f),new Vector3(.13f,.16f,.13f),M(i==0?"#ad4932":i==1?"#d3b449":"#6a4930"));
            Shape("Condiment bottle cap",PrimitiveType.Cylinder,p+new Vector3(2.05f+i*.21f,2.08f,.42f),new Vector3(.08f,.03f,.08f),white);
        }
        Shape("Utensil holder",PrimitiveType.Cylinder,p+new Vector3(1.8f,1.87f,-.45f),new Vector3(.21f,.15f,.21f),steel);
        for(int i=0;i<4;i++)Beam("Cooking utensils",p+new Vector3(1.74f+i*.045f,1.85f,-.45f),p+new Vector3(1.7f+i*.07f,2.25f,-.45f),.018f,steel);
        Box("Stacked paper takeaway boxes",p+new Vector3(2.1f,1.87f,-.4f),new Vector3(.45f,.35f,.3f),M("#e2d6bb"));
        for(int i=0;i<4;i++)Box("Takeaway box folds",p+new Vector3(2.1f,1.75f+i*.08f,-.235f),new Vector3(.45f,.009f,.01f),M("#9d927c"));
        var cylinder=Shape("Cooking gas bottle",PrimitiveType.Capsule,p+new Vector3(-2.7f,.48f,-.75f),new Vector3(.4f,.48f,.4f),M("#be5b47"));
        Beam("Gas hose",p+new Vector3(-2.7f,.8f,-.75f),p+new Vector3(-1.4f,1.65f,-.5f),.028f,rubber);
        if(index%2==0){
            Person(p+new Vector3(-1.1f,0,2.1f),180,index+720,false);
            var customer=walkers[walkers.Count-1];customer.root.name="排隊買小吃的客人";customer.gesture=0;
        }
        if((kind==0||kind==1||kind==6||kind==7||kind==8||kind==9)&&index%2==0)AddCookingSteam(p+new Vector3(-.7f,2.05f,0));
    }
    void FinishRealStall(Transform group)
    {
        foreach(var r in group.GetComponentsInChildren<MeshRenderer>()){
            if(r.GetComponent<TextMesh>())continue;
            if(r.name=="Striped canvas"){
                r.transform.localScale=new Vector3(r.transform.localScale.x,.025f,r.transform.localScale.z);
                r.sharedMaterial=RealSurface(r.sharedMaterial,"fabric");
            }
            if(r.name=="Stainless worktop"||r.name.Contains("grill")||r.name.Contains("Frying basket"))r.sharedMaterial=RealSurface(r.sharedMaterial,"metal");
        }
        group.localScale=new Vector3(1,.78f,1);
        foreach(var w in walkers)if(w.root&&w.root.parent==group)w.root.localScale=new Vector3(1,1/.78f,1);
    }
    void AddCookingSteam(Vector3 position)
    {
        if(!steamMaterial){
            steamMaterial=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            var tex=new Texture2D(32,32);var pixels=new Color[1024];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){float a=Mathf.Clamp01(1-Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f);pixels[y*32+x]=new Color(1,1,1,a*a);}
            tex.SetPixels(pixels);tex.Apply();steamMaterial.SetTexture("_BaseMap",tex);steamMaterial.SetColor("_BaseColor",new Color(1,.96f,.88f,.17f));
            steamMaterial.SetFloat("_Surface",1);steamMaterial.SetFloat("_SrcBlend",5);steamMaterial.SetFloat("_DstBlend",10);steamMaterial.SetFloat("_ZWrite",0);steamMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");steamMaterial.renderQueue=3000;
        }
        var o=new GameObject("Hot food cooking steam");o.transform.SetParent(environmentRoot,false);o.transform.localPosition=position;o.transform.localRotation=Quaternion.Euler(-90,0,0);
        var ps=o.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.startLifetime=2;main.startSpeed=.28f;main.startSize=new ParticleSystem.MinMaxCurve(.18f,.38f);main.startColor=new Color(1,.95f,.85f,.2f);main.maxParticles=12;main.simulationSpace=ParticleSystemSimulationSpace.Local;
        var emission=ps.emission;emission.rateOverTime=2;
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.12f;
        var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.5f),new Keyframe(1,1.8f)));
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=steamMaterial;ps.Play();
    }
}
