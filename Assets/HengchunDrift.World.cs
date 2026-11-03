using UnityEngine;

public partial class HengchunDrift
{
    readonly RaycastHit[] sweepHits=new RaycastHit[32];
    readonly Collider[] overlapHits=new Collider[32];
    const float RiderRadius=.46f;
    float safeCrashSlide=2;
    public int ClearedRoadObstructions { get; private set; }

    static bool SolidStreetShape(string name)
    {
        return name=="Trash bag" || name=="Townhouse" || name.Contains("Vendor cart") || name=="Food cart" ||
            name.Contains("city gate pier") || name=="Coral wall" || name.Contains("city wall") ||
            name=="Gate pier" || name.Contains("lintel") || name=="Gatehouse" ||
            name=="Concrete power pole" || name=="Arcade column" || name=="Palm trunk" ||
            name=="Stainless worktop" || name=="Shop awning" || name=="Scooter shell" ||
            name=="Leg shield" || name=="Seawall" || name=="Roadside bollard" ||
            name=="Concrete boundary wall" || name=="Arched stone";
    }
    bool Blocking(Collider other)
    {
        return other && other.enabled && other.gameObject.activeInHierarchy &&
            !other.isTrigger && !other.transform.IsChildOf(scooter) &&
            (!rescueVan || !other.transform.IsChildOf(rescueVan));
    }
    // Continuous volume sweep, rather than a point teleport or a single ray.
    // The volume includes both riders and a longitudinal capsule for the wheels.
    bool MoveScooterSafely(Vector3 target)
    {
        Physics.SyncTransforms();
        Vector3 from=scooter.position,delta=target-from;
        Vector3 low=Vector3.up*.56f,high=Vector3.up*1.65f;
        float length=delta.magnitude,stop=length;
        if(length>.0001f){
            int count=Physics.CapsuleCastNonAlloc(from+low,from+high,RiderRadius,delta/length,sweepHits,length+.025f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(Blocking(sweepHits[i].collider))stop=Mathf.Min(stop,Mathf.Max(0,sweepHits[i].distance-.03f));
            if(count==sweepHits.Length)stop=0; // A saturated query must not permit tunnelling.
            Vector3 heading=Quaternion.Euler(0,rideHeading,0)*Vector3.forward;
            // Sweep the wheelbase too: the front wheel must stop before the wall,
            // rather than stopping only the middle of the motorcycle.
            for(int end=0;end<2;end++){
                Vector3 offset=heading*(end==0?-.62f:.75f);
                count=Physics.CapsuleCastNonAlloc(from+low+offset,from+high+offset,.34f,delta/length,sweepHits,length+.025f,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<count;i++)if(Blocking(sweepHits[i].collider))stop=Mathf.Min(stop,Mathf.Max(0,sweepHits[i].distance-.03f));
                if(count==sweepHits.Length)stop=0;
            }
            if(stop<length){scooter.position=from+delta.normalized*stop;return false;}
        }
        int overlaps=Physics.OverlapCapsuleNonAlloc(target+low,target+high,RiderRadius,overlapHits,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<overlaps;i++)if(Blocking(overlapHits[i]))return false;
        // The scooter is longer than it is wide; check its front and rear footprint too.
        Vector3 forward=Quaternion.Euler(0,rideHeading,0)*Vector3.forward;
        overlaps=Physics.OverlapCapsuleNonAlloc(target+Vector3.up*.65f-forward*.62f,target+Vector3.up*.65f+forward*.75f,.34f,overlapHits,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<overlaps;i++)if(Blocking(overlapHits[i]))return false;
        scooter.position=target;return true;
    }
    void SetSafeCrashSlide()
    {
        Physics.SyncTransforms();safeCrashSlide=2;
        Vector3 forward=crashRotation*Vector3.forward;
        // A broad falling-body envelope is conservative, including the front wheel.
        int n=Physics.CapsuleCastNonAlloc(crashPosition+Vector3.up*.55f,crashPosition+Vector3.up*1.5f,.9f,forward,sweepHits,2.05f,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<n;i++)if(Blocking(sweepHits[i].collider))safeCrashSlide=Mathf.Min(safeCrashSlide,Mathf.Max(0,sweepHits[i].distance-.08f));
        n=Physics.OverlapCapsuleNonAlloc(crashPosition+Vector3.up*.55f,crashPosition+Vector3.up*1.5f,.9f,overlapHits,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<n;i++)if(Blocking(overlapHits[i]))safeCrashSlide=0;
    }
    bool LotClear(Vector3 centre,Quaternion orientation,Vector2 halfExtents)
    {
        Quaternion inverse=Quaternion.Inverse(orientation);
        // Reserve the complete road width on every route branch, not just this block.
        for(int i=0;i<course.Count;i+=2){
            Vector3 p=inverse*(course[i]-centre);
            if(Mathf.Abs(p.x)<halfExtents.x+RoadHalfWidth+.8f && Mathf.Abs(p.z)<halfExtents.y+RoadHalfWidth+.8f)return false;
        }
        return true;
    }
    Transform RoadsideLot(string name,Vector3 position,Quaternion rotation)
    {
        var t=new GameObject("Roadside lot · "+name).transform;t.SetParent(courseRoot,false);
        environmentRoot=t; // Build in local space, then orient the complete group.
        return t;
    }
    void FinishLot(Transform t,Vector3 position,Quaternion rotation)
    { t.localPosition=position;t.localRotation=rotation;environmentRoot=courseRoot; }

    void BuildVariedStreetSections()
    {
        int seed=0;
        for(int d=20;d<courseLength;){
            int type=0; // Long lively sections alternate with residential and coastal sections.
            bool lively=type==0;int blockStep=lively?11:type==1?12:24;
            Vector3 p=CoursePosition(d);Quaternion q=Quaternion.Euler(0,CourseHeading(d),0);
            for(int side=-1;side<=1;side+=2){
                Vector3 building=p+q*new Vector3(side*13.8f,0,0);
                Quaternion facing=q*Quaternion.Euler(0,side>0?180:0,0);
                if((d-20)%22==0 && type!=2 && LotClear(building,facing,new Vector2(3.7f,5.2f))){
                    var lot=RoadsideLot(lively?"busy shopfront":"residential building",building,facing);
                    Shop(Vector3.zero,false,seed++);
                    Box("Recessed shop doorway",new Vector3(3.56f,1.25f,0),new Vector3(.05f,2.5f,3),dark);
                    if(lively){
                        Box("Shop fluorescent tube",new Vector3(3.9f,2.6f,0),new Vector3(.12f,.1f,3),Glow("#ffd7a1"));
                        Lamp(new Vector3(4.2f,2.5f,0),new Color(1,.77f,.48f),2.4f,7);
                        Box("Roof water tank",new Vector3(-.5f,10.4f,0),new Vector3(1.6f,1.5f,1.6f),M("#8b989a"));
                        Box("Air conditioner",new Vector3(3.65f,4.1f,2.3f),new Vector3(.6f,.65f,.95f),M("#b3b9af"));
                    }else{
                        for(int slat=0;slat<12;slat++)Box("Residential shutter",new Vector3(3.62f,.2f+slat*.2f,0),new Vector3(.08f,.15f,2.6f),M("#808a85"));
                    }
                    FinishLot(lot,building,facing);
                }
                if(lively&&d<790){
                    Vector3 vendor=p+q*new Vector3(side*7.3f,0,0);
                    Quaternion vendorFacing=q*Quaternion.Euler(0,side<0?90:-90,0);
                    if(LotClear(vendor,vendorFacing,new Vector2(3.5f,1.8f))){
                        var lot=RoadsideLot("food and games",vendor,vendorFacing);
                        DetailedStall(Vector3.zero,seed++);
                        FinishLot(lot,vendor,vendorFacing);
                    }
                }
                // Walkers stay on the pavements, with their path running parallel to the road.
                Vector3 pavement=p+q*new Vector3(side*5.8f,0,0);
                if(d<790&&LotClear(pavement,q,new Vector2(.1f,1.5f))){
                    var lot=RoadsideLot("pavement life",pavement,q);
                    int people=lively?4:type==1?1:0;
                    for(int i=0;i<people;i++)Person(new Vector3(side*.3f,0,i*2.2f),i%2==0?0:180,seed++,true);
                    if(lively)ParkedScooter(new Vector3(side*1.2f,0,-3),side*18,seed%3);
                    FinishLot(lot,pavement,q);
                }
                Vector3 pole=p+q*new Vector3(side*6.2f,0,blockStep*.4f);
                if(LotClear(pole,q,new Vector2(.18f,.18f))){
                    var lot=RoadsideLot("street lighting",pole,q);
                    Box("Concrete power pole",new Vector3(0,3.2f,0),new Vector3(.2f,6.4f,.2f),M("#8a8c82"));
                    Box("Street lamp",new Vector3(-side*.7f,6.1f,0),new Vector3(1.5f,.14f,.35f),Glow("#f4cc8e"));
                    if(lively){
                        for(int i=0;i<3;i++)Shape("Paper lantern",PrimitiveType.Sphere,new Vector3(-side*(1.7f+i*1.2f),4.8f,0),new Vector3(.45f,.58f,.45f),Glow("#d6533c"));
                        Beam("Lantern wire",new Vector3(0,5.2f,0),new Vector3(-side*4.2f,5.2f,0),.018f,dark);
                    }
                    FinishLot(lot,pole,q);
                }
                if(type==2){
                    Vector3 palm=p+q*new Vector3(side*10,0,0);
                    if(LotClear(palm,q,new Vector2(.4f,.4f))){var lot=RoadsideLot("coastal palm",palm,q);Palm(Vector3.zero);FinishLot(lot,palm,q);}
                }
            }
            // Gutter and curb strips are aligned to the actual road tangent.
            if(lively)for(int side=-1;side<=1;side+=2){var curb=Box("Red curb marking",p+q*new Vector3(side*4.5f,.07f,0),new Vector3(.2f,.05f,5),M("#b34b42"));curb.rotation=q;}
            d+=blockStep;
        }
        environmentRoot=courseRoot;
        // A horizon beyond the sparse coastal sections avoids an empty tan void.
        Box("Coastal water",new Vector3(0,-.2f,-2500),new Vector3(12000,.08f,4000),M("#355e6c"));
    }
    void ClearRoadObstructions()
    {
        Physics.SyncTransforms();ClearedRoadObstructions=0;
        for(int i=0;i<course.Count;i+=2){
            Vector3 p=course[i];int n=Physics.OverlapCapsuleNonAlloc(p+Vector3.up*.65f,p+Vector3.up*1.65f,.75f,overlapHits,~0,QueryTriggerInteraction.Ignore);
            for(int j=0;j<n;j++){
                var c=overlapHits[j];if(!Blocking(c)||!c.transform.IsChildOf(courseRoot))continue;
                Transform group=c.transform;
                while(group.parent && group.parent!=courseRoot)group=group.parent;
                group.gameObject.SetActive(false);ClearedRoadObstructions++;
            }
        }
        Physics.SyncTransforms();
    }
}



