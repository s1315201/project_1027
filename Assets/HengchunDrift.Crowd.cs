using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class HengchunDrift
{
    class CrowdWalker { public Walker actor; public Vector3 home; public float phase,sign; }
    readonly List<CrowdWalker> roadCrowd=new List<CrowdWalker>();
    public int NightMarketCrowdCount => roadCrowd.Count;
    bool LeanRequested => distance<800&&(touchLean||Held(Key.LeftShift)||Held(Key.RightShift));

    void AdvanceOptionalBends()
    {
        while(bendIndex<bends.Count){
            var bend=bends[bendIndex];
            if(distance<=bend.end+2)break;
            if(bend.hit&&!bend.released)break; // An opted-in lean still has to be completed.
            if(!bend.hit){bend.released=true;driftPoints+=40;}
            bendIndex++;
        }
    }
    void DrawCentreSpeedometer(float h,Color ivory,Color mint)
    {
        Panel(new Rect(624,h-100,192,83),new Color(.025f,.06f,.065f,.78f));
        Text((speed*3.6f).ToString("00"),644,h-101,150,65,48,ivory,FontStyle.Bold,TextAnchor.MiddleCenter);
        Text("km/h",644,h-43,150,20,13,mint,FontStyle.Normal,TextAnchor.MiddleCenter);
        Panel(new Rect(640,h-19,160,3),new Color(.19f,.25f,.25f,.8f));
        Panel(new Rect(640,h-19,160*Mathf.Clamp01(speed/RaceSpeed),3),mint);
    }
    void UpdateExtraTouchControls(float h,float scale)
    {
        var brake=new Rect(349,h-72,95,45);brakeTouch=false;
        if(Pointer.current!=null&&Pointer.current.press.isPressed){var p=Pointer.current.position.ReadValue();brakeTouch=brake.Contains(new Vector2(p.x/scale,(Screen.height-p.y)/scale));}
        if(Touchscreen.current!=null)foreach(var touch in Touchscreen.current.touches)if(touch.press.isPressed){var p=touch.position.ReadValue();brakeTouch|=brake.Contains(new Vector2(p.x/scale,(Screen.height-p.y)/scale));}
    }
    void DrawExtraControls(float h,Color ivory)
    {
        Panel(new Rect(349,h-72,95,45),new Color(brakeTouch?.55f:.12f,.13f,.12f,.8f));
        Text("S ↓",349,h-72,95,45,19,ivory,FontStyle.Normal,TextAnchor.MiddleCenter);
        if(distance>=800)Text("漂移",456,h-72,110,45,19,ivory,FontStyle.Normal,TextAnchor.MiddleCenter);
        else if(Button(touchLean?"壓彎 ON":"一般",new Rect(456,h-72,110,45)))touchLean=!touchLean;
    }
    void BuildNightMarketCrowd()
    {
        roadCrowd.Clear();int seed=160;
        // Crowds occupy the straights; turning spaces remain readable and rideable.
        for(int section=0;section<=bends.Count;section++){
            float start=section==0?18:bends[section-1].end+10;
            float end=section==bends.Count?courseLength-12:bends[section].start-16;
            int row=0;
            for(float d=start;d+6<end&&d<790;d+=11,row++){
                Vector3 p=CoursePosition(d);Quaternion heading=Quaternion.Euler(0,CourseHeading(d),0);
                var group=new GameObject("夜市人潮 · 逛攤與穿行").transform;group.SetParent(courseRoot,false);
                environmentRoot=group;float gapSign=(row+section)%2==0?1:-1;
                // Each cluster leaves a real gap on one side. Alternating gaps reward
                // slowing down and weaving, rather than requiring a blind sprint.
                float[] x={-gapSign*2.5f,-gapSign*1.6f,-gapSign*.9f,0,gapSign*3.8f,-gapSign*3.8f,-gapSign*2.1f,-gapSign*3.2f};
                for(int i=0;i<x.Length;i++){
                    Vector3 home=new Vector3(x[i],0,i*.95f);
                    Person(home,(i%2==0?0:180),seed++,true);var actor=walkers[walkers.Count-1];
                    actor.root.name=i==0?"結伴逛街遊客":"夜市路面行人";actor.travel=0;
                    var hit=actor.root.gameObject.AddComponent<CapsuleCollider>();hit.height=1.72f;hit.center=Vector3.up*.86f;hit.radius=.22f;
                    roadCrowd.Add(new CrowdWalker{actor=actor,home=home,phase=seed*.73f,sign=i%2==0?1:-1});
                }
                group.localPosition=p;group.localRotation=heading;environmentRoot=courseRoot;
            }
        }
    }
    void AnimateNightMarketCrowd(float dt)
    {
        foreach(var person in roadCrowd){
            var w=person.actor;if(!w.root)continue;
            float t=clock*.65f+person.phase;
            bool pause=(clock+person.phase)%13>9;
            float bob=pause?0:Mathf.Abs(Mathf.Sin(t*4))*.012f;
            // Small bounded paths cannot close the reserved 1.8 m passing gaps.
            w.root.localPosition=person.home+new Vector3(Mathf.Sin(t*.5f)*.14f,bob,Mathf.Sin(t*.35f)*.85f);
            w.root.localRotation=Quaternion.Euler(0,person.sign>0?0:180,0);
            float swing=pause?0:Mathf.Sin(t*4)*19;
            w.leftLeg.localRotation=Quaternion.Euler(swing,0,0);w.rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
            if(!pause){w.leftArm.localRotation=Quaternion.Euler(-swing*.65f,0,-7);w.rightArm.localRotation=Quaternion.Euler(swing*.65f,0,7);}
        }
    }
}

