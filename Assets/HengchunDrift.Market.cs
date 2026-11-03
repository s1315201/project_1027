using UnityEngine;

public partial class HengchunDrift
{
    float naturalBank;
    void GiveVisitorSnack(Walker walker,int variant)
    {
        if(variant%5==4)return;
        var hand=new GameObject("Handheld night market snack").transform;
        hand.SetParent(walker.rightArm,false);hand.localPosition=new Vector3(0,-.49f,.08f);
        // Props follow the hand through walking, waving and photo gestures.
        switch(variant%4){
        case 0:
            Shape("Bubble tea cup",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.12f,.1f,.12f),M("#c99c6c"),hand);
            Shape("Sealed cup lid",PrimitiveType.Cylinder,new Vector3(0,.105f,0),new Vector3(.14f,.012f,.14f),white,hand);
            Beam("Bubble tea straw",new Vector3(.02f,.1f,0),new Vector3(.02f,.26f,0),.014f,dark,hand);
            for(int i=0;i<5;i++)Shape("Tapioca pearls",PrimitiveType.Sphere,new Vector3((i%3-1)*.033f,-.06f+(i/3)*.035f,.055f),Vector3.one*.026f,dark,hand);
            break;
        case 1:
            Box("Fried chicken paper bag",Vector3.zero,new Vector3(.17f,.22f,.1f),M("#c9a77b"),hand);
            for(int i=0;i<4;i++)Shape("Crispy chicken bite",PrimitiveType.Sphere,new Vector3((i%2)*.065f-.03f,.12f,(i/2)*.055f-.025f),Vector3.one*.075f,M("#bb7939"),hand);
            break;
        case 2:
            Beam("Bamboo snack skewer",new Vector3(0,-.08f,0),new Vector3(0,.28f,0),.012f,M("#c8a578"),hand);
            for(int i=0;i<3;i++)Shape("Grilled sausage skewer",PrimitiveType.Capsule,new Vector3(0,.04f+i*.075f,0),new Vector3(.075f,.065f,.075f),M("#a54f35"),hand);
            break;
        default:
            Loft("Ice cream cone",new Vector3(0,-.08f,0),new float[,]{{0,.01f,.01f},{.17f,.065f,.065f}},M("#c6995e"),hand);
            Shape("Ice cream scoop",PrimitiveType.Sphere,new Vector3(0,.13f,0),Vector3.one*.16f,M("#f1c5b1"),hand);
            break;
        }
    }
    void BuildMarketObstacles()
    {
        for(int d=28,index=0;d<790;d+=27,index++){
            var root=new GameObject("Roadside advertising and used food packaging").transform;
            root.SetParent(courseRoot,false);root.localPosition=CoursePosition(d);root.localRotation=Quaternion.Euler(0,CourseHeading(d),0);
            float side=index%2==0?-1:1;
            // Solid advertising stays near the curb; the central riding corridor is open.
            if(index%2==0){
                var board=Box("Vendor pavement advertisement",new Vector3(side*3.75f,.83f,0),new Vector3(.85f,1.5f,.16f),M("#343e37"),root);
                board.gameObject.AddComponent<BoxCollider>();
                for(int foot=-1;foot<=1;foot+=2)Limb("A frame sign legs",new Vector3(side*3.75f,.95f,0),new Vector3(side*3.75f,.04f,foot*.48f),.035f,M("#b19368"),root);
                environmentRoot=root;
                Label(index%4==0?"現炸鹽酥雞\n大份 80 元":"炭烤魷魚\n買二送一",new Vector3(side*3.75f,.98f,-.09f),.03f,new Color(1,.86f,.55f),Quaternion.identity);
                environmentRoot=courseRoot;
            }
            for(int item=0;item<4;item++){
                var litter=new GameObject("Discarded food packaging").transform;litter.SetParent(root,false);
                litter.localPosition=new Vector3(side*(2.9f+item*.25f),.04f,3+item*.65f);
                litter.localRotation=Quaternion.Euler(0,index*37+item*53,0);
                switch((index+item)%4){
                case 0:
                    var cup=Shape("Empty tipped drink cup",PrimitiveType.Cylinder,new Vector3(0,.08f,0),new Vector3(.16f,.13f,.16f),M("#cbb696"),litter);cup.localRotation=Quaternion.Euler(0,0,80);
                    Shape("Discarded cup lid",PrimitiveType.Cylinder,new Vector3(.19f,.015f,.05f),new Vector3(.17f,.01f,.17f),white,litter);
                    Beam("Used straw",new Vector3(-.15f,.02f,.12f),new Vector3(.12f,.02f,.18f),.012f,M("#aa5548"),litter);break;
                case 1:
                    Box("Empty takeaway tray",new Vector3(0,.03f,0),new Vector3(.36f,.035f,.25f),white,litter);
                    for(int edge=-1;edge<=1;edge+=2)Box("Paper tray rim",new Vector3(edge*.17f,.055f,0),new Vector3(.025f,.06f,.25f),white,litter);
                    Box("Grease stain",new Vector3(.03f,.05f,0),new Vector3(.13f,.005f,.1f),M("#a78959"),litter);break;
                case 2:
                    var paper=Box("Crumpled fried food wrapper",new Vector3(0,.04f,0),new Vector3(.25f,.06f,.19f),M("#bda17b"),litter);paper.localRotation=Quaternion.Euler(14,0,17);
                    Shape("Used napkin",PrimitiveType.Sphere,new Vector3(.17f,.03f,0),new Vector3(.13f,.06f,.1f),white,litter);break;
                default:
                    for(int stick=0;stick<2;stick++)Beam("Discarded bamboo skewer",new Vector3(-.18f,.02f,stick*.07f),new Vector3(.16f,.02f,stick*.04f+.04f),.009f,M("#bc9663"),litter);
                    Shape("Food scraps",PrimitiveType.Sphere,new Vector3(0,.025f,0),new Vector3(.1f,.04f,.07f),M("#975b38"),litter);break;
                }
                // A low trigger-free volume participates in the existing swept wheel checks.
                var hit=litter.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,.08f,0);hit.size=new Vector3(.38f,.16f,.3f);
            }
        }
        environmentRoot=courseRoot;
    }
}
