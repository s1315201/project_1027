using UnityEngine;

public partial class HengchunDrift
{
    readonly System.Collections.Generic.Dictionary<Material,Material> comicMaterials=new System.Collections.Generic.Dictionary<Material,Material>();
    void InkRing(string name, Vector3 centre, float radius, Material colour, Transform parent)
    {
        for(int i=0;i<16;i++){
            float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;
            Limb(name,centre+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius,
                centre+new Vector3(Mathf.Cos(b),Mathf.Sin(b),0)*radius,.005f,colour,parent);
        }
    }
    void ApplyComicMaterials(Transform root)
    {
        ApplyNaturalMaterials(root);
    }

    void StyleRider(Transform root,bool girl)
    {
        foreach(var shape in root.GetComponentsInChildren<Transform>()){
            if(shape.name=="Cheek")shape.localScale*=.55f;
            if(shape.name=="Upper lip"||shape.name=="Lower lip")shape.localScale*=.72f;
            if(shape.name=="Eye white")shape.localScale=new Vector3(.052f,.023f,.012f);
            if(shape.name=="Brown iris")shape.localScale=new Vector3(.018f,.021f,.007f);
        }
        for(int side=-1;side<=1;side+=2){
            Limb("Cotton tank shoulder seam",new Vector3(side*.17f,1.66f,.12f),new Vector3(side*.21f,1.6f,.16f),.004f,M(girl?"#65586b":"#d8d1c2"),root);
            Limb("Shorts stitched side seam",new Vector3(side*.24f,1.13f,.12f),new Vector3(side*.34f,.97f,.28f),.004f,M("#9ca39c"),root);
        }
        root.name=girl?"小語 YU · 圓框眼鏡與馬尾":"阿辰 CHEN · 白背心與金鍊";
        for(int side=-1;side<=1;side+=2){
            Limb("Helmet chin strap",new Vector3(side*.15f,2.06f,0),new Vector3(0,1.84f,.07f),.009f,dark,root);
            if(girl){
                InkRing("Round glasses",new Vector3(side*.068f,2.04f,.17f),.054f,dark,root);
                InkRing("Gold hoop earrings",new Vector3(side*.169f,1.97f,.03f),.027f,M("#e4ae39"),root);
                Limb("Denim shorts hem",new Vector3(side*.19f,1.06f,.19f),new Vector3(side*.29f,.97f,.34f),.017f,white,root);
            }
        }
        if(girl){
            Limb("Glasses bridge",new Vector3(-.014f,2.04f,.17f),new Vector3(.014f,2.04f,.17f),.006f,dark,root);
            Box("Short top hem",new Vector3(0,1.22f,.155f),new Vector3(.37f,.035f,.035f),M("#746578"),root);
            Beam("Crossbody strap",new Vector3(-.19f,1.67f,.16f),new Vector3(.2f,1.25f,.18f),.025f,dark,root);
            Box("Small shoulder bag",new Vector3(.26f,1.17f,0),new Vector3(.16f,.22f,.12f),M("#bc9b78"),root);
        }else{
            Box("Gold pendant",new Vector3(0,1.55f,.18f),new Vector3(.045f,.07f,.018f),M("#e4ae39"),root);
            for(int i=0;i<7;i++){
                float x=-.12f+i*.04f;
                Limb("Tousled black fringe",new Vector3(x,2.13f,.09f),new Vector3(x+.025f,2.06f+(i%3)*.012f,.15f),.021f,M("#292828"),root);
            }
        }
    }
    void StyleVisitor(Transform root,bool girl,int variant)
    {
        // Share the riders' ink, warm skin palette and restrained two-tone shading.
        for(int side=-1;side<=1;side+=2){
            Shape("Comic eye white",PrimitiveType.Sphere,new Vector3(side*.06f,1.735f,.137f),new Vector3(.048f,.024f,.012f),white,root);
            Shape("Comic brown iris",PrimitiveType.Sphere,new Vector3(side*.06f,1.735f,.147f),new Vector3(.019f,.022f,.008f),M("#45352d"),root);
            Limb("Ink eyebrow",new Vector3(side*.035f,1.78f,.133f),new Vector3(side*.09f,1.78f,.12f),.007f,dark,root);
            if(variant%4==0)InkRing("Visitor round glasses",new Vector3(side*.062f,1.735f,.16f),.047f,dark,root);
        }
        Limb("Small comic smile",new Vector3(-.025f,1.63f,.13f),new Vector3(.025f,1.63f,.13f),.005f,M("#995f55"),root);
        if(girl)Limb("Visitor ponytail",new Vector3(.08f,1.85f,-.12f),new Vector3(.13f,1.49f,-.21f),.06f,M("#50382d"),root);
        RealHumanFace(root,false,girl);
    }
}
