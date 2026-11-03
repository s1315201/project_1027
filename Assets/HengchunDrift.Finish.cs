using System.Collections.Generic;
using UnityEngine;

public partial class HengchunDrift
{
    readonly List<Walker> finishSupporters=new List<Walker>();
    Transform finishFestival;
    void BuildMarketPaving()
    {
        // Continuous pavements fill the exposed earth between road, carts and shopfronts.
        for(int d=0;d<=1000;d+=8){
            Vector3 p=CoursePosition(d);Quaternion q=Quaternion.Euler(0,CourseHeading(d),0);
            for(int side=-1;side<=1;side+=2){
                var paving=Box("Night market paved forecourt",p+q*new Vector3(side*8,.015f,0),new Vector3(6.6f,.06f,8.5f),M((d/8)%2==0?"#737b79":"#697471"));paving.rotation=q;
                var curb=Box("Continuous market kerb",p+q*new Vector3(side*4.72f,.07f,0),new Vector3(.18f,.14f,8.5f),M("#b6b2a1"));curb.rotation=q;
                if(d%24==0){
                    var lot=RoadsideLot("Shopfront illumination",p,q);
                    Box("Shopfront light strip",new Vector3(side*10.4f,3.6f,0),new Vector3(.07f,.06f,5),Glow("#ffe3a5"));
                    FinishLot(lot,p,q);
                }
            }
        }
        // Extend asphalt behind the start and past the finish so the street has no abrupt edge.
        foreach(float d in new[]{0f,1000f}){
            var q=Quaternion.Euler(0,CourseHeading(d),0);var p=CoursePosition(d);
            var extension=Box("Market road continuation",p+q*new Vector3(0,.025f,d==0?-14:16),new Vector3(RoadHalfWidth*2,.07f,32),asphalt);extension.rotation=q;
        }
    }
    void BuildFinishCelebration()
    {
        finishSupporters.Clear();
        finishFestival=new GameObject("恭喜通關 · 夜市終點嘉年華").transform;
        finishFestival.SetParent(courseRoot,false);finishFestival.localPosition=CoursePosition(1000);finishFestival.localRotation=Quaternion.Euler(0,CourseHeading(1000),0);
        var coral=M("#ed6450");var yellow=M("#ffd36e");
        // Rounded inflated columns and a high rounded crossbar leave the entire road open.
        for(int side=-1;side<=1;side+=2){
            Shape("Inflatable finish upright",PrimitiveType.Capsule,new Vector3(side*5.5f,2.8f,1),new Vector3(1.05f,2.8f,1.05f),coral,finishFestival);
            Shape("Inflatable arch elbow",PrimitiveType.Sphere,new Vector3(side*5.25f,5.1f,1),new Vector3(1.5f,1.5f,1.1f),coral,finishFestival);
            Shape("Inflatable base anchor",PrimitiveType.Sphere,new Vector3(side*5.5f,.25f,1),new Vector3(1.8f,.5f,1.7f),yellow,finishFestival);
            for(int stripe=0;stripe<3;stripe++)Shape("Inflatable column bands",PrimitiveType.Cylinder,new Vector3(side*5.5f,1+stripe*1.2f,1),new Vector3(1.075f,.11f,1.075f),yellow,finishFestival);
        }
        var top=Shape("Inflatable overhead beam",PrimitiveType.Capsule,new Vector3(0,5.4f,1),new Vector3(1.35f,5.2f,1.35f),coral,finishFestival);top.localRotation=Quaternion.Euler(0,0,90);
        Box("Finish message panel",new Vector3(0,5.35f,.3f),new Vector3(7.8f,1.1f,.06f),M("#493126"),finishFestival);
        environmentRoot=finishFestival;
        Label("恭喜通關",new Vector3(0,5.42f,.25f),.22f,new Color(1,.94f,.69f),Quaternion.identity);
        Label("墾丁大街 · 1 KM 完走",new Vector3(0,4.84f,.23f),.06f,Color.white,Quaternion.identity);
        Lamp(new Vector3(0,5,-1),new Color(1,.85f,.59f),12,15);
        for(int row=0;row<4;row++)for(int side=-1;side<=1;side+=2){
            // Spectators beyond the finish are outside the drivable road envelope.
            Vector3 home=new Vector3(side*(6.4f+(row%2)*.5f),0,4+row*3);
            Person(home,side<0?65:-65,410+row*2+(side>0?1:0),false);
            var w=walkers[walkers.Count-1];w.root.name="終點加油觀眾";w.gesture=0;finishSupporters.Add(w);
            var flag=new GameObject("Cheering flag").transform;flag.SetParent(w.rightArm,false);flag.localPosition=new Vector3(0,-.4f,.04f);
            Beam("Cheering flag stick",Vector3.zero,new Vector3(0,-.45f,0),.018f,white,flag);
            Box("Finish cheering pennant",new Vector3(.16f,-.32f,0),new Vector3(.3f,.22f,.025f),row%2==0?coral:yellow,flag);
            Shape("Finish balloon",PrimitiveType.Sphere,home+new Vector3(side*.4f,2.6f,0),new Vector3(.45f,.6f,.45f),row%2==0?yellow:teal,finishFestival);
            Beam("Balloon string",home+new Vector3(side*.4f,1.4f,0),home+new Vector3(side*.4f,2.35f,0),.007f,white,finishFestival);
            if(row%2==0){
                Box("Cheering board",home+new Vector3(-side*.3f,2.4f,-.2f),new Vector3(1.15f,.5f,.04f),yellow,finishFestival);
                Label(row==0?"加油！":"你最棒！",home+new Vector3(-side*.3f,2.4f,-.24f),.07f,new Color(.25f,.13f,.1f),Quaternion.identity);
            }
        }
        // Approach-side cheers follow the real curve, leaving room for the final drift.
        for(int d=973;d<=997;d+=6)for(int side=-1;side<=1;side+=2){
            var q=Quaternion.Euler(0,CourseHeading(d),0);
            var lot=RoadsideLot("Finish approach cheering",CoursePosition(d),q);
            Person(new Vector3(side*6.1f,0,0),side<0?80:-80,d+side+20,false);
            var w=walkers[walkers.Count-1];w.root.name="兩側加油觀眾";w.gesture=0;finishSupporters.Add(w);
            FinishLot(lot,CoursePosition(d),q);
        }
        environmentRoot=finishFestival;
        for(int i=0;i<16;i++){
            float x=-4.4f+i*.58f;
            var confetti=Box("Finish hanging streamer",new Vector3(x,4.2f+(i%3)*.14f,1),new Vector3(.07f,.32f,.025f),i%2==0?yellow:teal,finishFestival);
            confetti.localRotation=Quaternion.Euler(0,0,(i%2==0?1:-1)*25);
        }
        for(int i=-4;i<=4;i++)for(int row=0;row<2;row++)Box("Finish chequered stripe",new Vector3(i,.085f,row*.5f),new Vector3(1,.025f,.5f),(i+row)%2==0?white:dark,finishFestival);
        environmentRoot=courseRoot;
    }
    void AnimateFinishCelebration()
    {
        foreach(var w in finishSupporters){
            if(!w.root)continue;
            float wave=Mathf.Sin(clock*7+w.phase);
            w.leftArm.localRotation=Quaternion.Euler(-15,0,-145+wave*15);
            w.rightArm.localRotation=Quaternion.Euler(-12,0,145-wave*20);
            w.root.localPosition=w.origin+Vector3.up*Mathf.Max(0,Mathf.Sin(clock*4+w.phase))*.045f;
        }
    }
}
