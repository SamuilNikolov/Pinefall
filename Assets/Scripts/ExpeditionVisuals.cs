using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public sealed class LootVisual:MonoBehaviour
    {
        Material material,trim;Transform beacon,modelRoot;
        void Start()
        {
            var loot=GetComponent<WorldLoot>();Color color=loot.Kind.Value==12?new Color(.52f,.63f,1):loot.Kind.Value==0?WeaponData.RarityColor(loot.Rarity.Value):loot.Kind.Value==5?new Color(.85f,.65f,.31f):new Color(.51f,.76f,.67f);
            material=WeaponView.Mat("Dropped equipment",color*.7f);trim=WeaponView.Mat("Equipment marker",color);
            var root=new GameObject("Pickup").transform;modelRoot=root;root.SetParent(transform,false);
            if(loot.Kind.Value==12){WeaponView.Part(root,PrimitiveType.Cube,Vector3.zero,new Vector3(.28f,.38f,.16f),material);WeaponView.Part(root,PrimitiveType.Cylinder,new Vector3(-.07f,.34f,0),new Vector3(.025f,.24f,.025f),trim);WeaponView.Part(root,PrimitiveType.Cube,new Vector3(0,.05f,-.09f),new Vector3(.19f,.13f,.025f),trim);}
            else if(loot.Kind.Value==0)
            {WeaponView.Part(root,PrimitiveType.Cube,Vector3.zero,new Vector3(.13f,.15f,.7f),material);WeaponView.Part(root,PrimitiveType.Cylinder,new Vector3(0,.04f,.4f),new Vector3(.035f,.22f,.035f),trim,new Vector3(90,0,0));WeaponView.Part(root,PrimitiveType.Cube,new Vector3(0,-.05f,-.34f),new Vector3(.14f,.17f,.3f),material);}
            else {WeaponView.Part(root,loot.Kind.Value==5?PrimitiveType.Sphere:PrimitiveType.Cube,Vector3.zero,loot.Kind.Value==4?new Vector3(.28f,.42f,.22f):new Vector3(.32f,.22f,.26f),material);WeaponView.Part(root,PrimitiveType.Cube,new Vector3(0,.12f,0),new Vector3(.12f,.025f,.25f),trim);}
            beacon=WeaponView.Part(root,PrimitiveType.Sphere,new Vector3(0,.45f,0),Vector3.one*.07f,trim).transform;
            foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=0;
        }
        void OnEnable(){if(modelRoot)modelRoot.gameObject.SetActive(true);}
        void OnDisable(){if(modelRoot)modelRoot.gameObject.SetActive(false);}
        void Update(){if(beacon)beacon.localPosition=Vector3.up*(.45f+Mathf.Sin(Time.time*2.5f)*.06f);}
        void OnDestroy(){if(material)Destroy(material);if(trim)Destroy(trim);}
    }
    public sealed class VehicleVisual:MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();readonly List<Transform>wheels=new List<Transform>();RoadVehicle car;AudioSource engine;Transform modelRoot;
        Material Mat(string n,Color c,float metal=0){var template=n=="Faded vehicle paint"?Resources.Load<Material>("VehicleWear"):null;var m=template?new Material(template):WeaponView.Mat(n,c,metal);m.color=c;m.SetFloat("_Metallic",metal);materials.Add(m);return m;}
        void Start()
        {
            car=GetComponent<RoadVehicle>();var root=new GameObject("Vehicle body").transform;modelRoot=root;root.SetParent(transform,false);if(car.Type.Value==5){gameObject.AddComponent<MotorcycleVisual>().Build(root);MakeEngine();return;}if(car.Type.Value==4){gameObject.AddComponent<BoatVisual>().Build(root);var collider=GetComponent<BoxCollider>();collider.size=new Vector3(2,1.1f,4.6f);collider.center=new Vector3(0,.1f,0);MakeEngine();return;}
            var paint=Mat("Faded vehicle paint",new[]{new Color(.37f,.42f,.32f),new Color(.62f,.43f,.22f),new Color(.36f,.46f,.49f),new Color(.58f,.55f,.44f)}[car.Type.Value],.3f);
            var iron=Mat("Weathered chassis",new Color(.15f,.16f,.15f),.5f);var rubber=Mat("Rubber tyres",new Color(.06f,.065f,.06f));var window=Mat("Dusty glass",new Color(.19f,.29f,.31f),.5f);var lamp=Mat("Headlamps",new Color(.81f,.78f,.59f));
            window.color=new Color(.19f,.29f,.31f,.22f);window.SetFloat("_Mode",3);window.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);window.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);window.SetInt("_ZWrite",0);window.EnableKeyword("_ALPHABLEND_ON");window.renderQueue=3000;
            System.Action<string,Vector3,Vector3,Material> box=(n,p,s,m)=>{var g=WeaponView.Part(root,PrimitiveType.Cube,p,s,m);g.name=n;g.layer=0;g.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;};
            bool buggy=car.Type.Value==1,van=car.Type.Value==3,pickup=car.Type.Value==2;
            box("Chassis",new Vector3(0,0,0),new Vector3(1.65f,.38f,3.7f),iron);
            box("Bonnet",new Vector3(0,.38f,1.05f),new Vector3(1.7f,.45f,1.25f),paint);
            box("Cabin floor",new Vector3(0,.22f,-.25f),new Vector3(1.6f,.14f,van?2.6f:1.7f),iron);
            foreach(float seatX in new[]{-.43f,.43f}){box("Seat cushion",new Vector3(seatX,.38f,.02f),new Vector3(.53f,.16f,.58f),rubber);box("Seat back",new Vector3(seatX,.69f,-.27f),new Vector3(.52f,.61f,.14f),rubber);}box("Dashboard",new Vector3(0,.80f,.62f),new Vector3(1.53f,.21f,.26f),iron);
            if(!buggy){box("Cab roof",new Vector3(0,1.35f,pickup?.05f:-.2f),new Vector3(1.75f,.13f,pickup?1.3f:van?2.65f:1.9f),paint);box("Windscreen",new Vector3(0,1.02f,.72f),new Vector3(1.48f,.55f,.055f),window);box("Rear glass",new Vector3(0,1.02f,pickup?-.59f:van?-1.5f:-1.1f),new Vector3(1.48f,.52f,.05f),window);}
            foreach(int side in new[]{-1,1})
            {
                if(!buggy)box("Door",new Vector3(side*.84f,.56f,.04f),new Vector3(.075f,.42f,1.12f),paint);else box("Side rail",new Vector3(side*.84f,.48f,.04f),new Vector3(.065f,.075f,1.28f),iron);
                if(!buggy)box("Side glazing",new Vector3(side*.85f,1.02f,0),new Vector3(.04f,.54f,1.1f),window);
                foreach(float z in new[]{-.72f,.68f})box("Cab pillar",new Vector3(side*.8f,1.02f,z),new Vector3(.07f,.65f,.075f),iron);
                box("Door handle",new Vector3(side*.9f,.75f,-.34f),new Vector3(.05f,.04f,.22f),iron);
                box("Mirror",new Vector3(side*1.02f,1.05f,.54f),new Vector3(.22f,.15f,.16f),iron);
                box("Headlamp",new Vector3(side*.57f,.35f,1.72f),new Vector3(.31f,.19f,.06f),lamp);
                foreach(float z in new[]{-1.15f,1.15f})
                {var wheel=new GameObject("Tyre");wheel.transform.SetParent(root,false);wheel.transform.localPosition=new Vector3(side*.91f,-.25f,z);wheel.transform.localRotation=Quaternion.Euler(0,0,90);wheel.AddComponent<MeshFilter>().sharedMesh=SoftGeometry.Tyre;wheel.AddComponent<MeshRenderer>().sharedMaterial=rubber;wheels.Add(wheel.transform);var hub=WeaponView.Part(wheel.transform,PrimitiveType.Cylinder,Vector3.up*(-side*.09f),new Vector3(.4f,.025f,.4f),iron);hub.layer=0;}
            }
            if(van){foreach(int side in new[]{-1,1})box("Van cargo side",new Vector3(side*.82f,.85f,-.97f),new Vector3(.1f,1.1f,1.25f),paint);}
            if(buggy){box("Rear engine",new Vector3(0,.30f,-1.33f),new Vector3(1.2f,.55f,.75f),iron);}
            if(pickup){box("Pickup bed",new Vector3(0,.37f,-1.3f),new Vector3(1.68f,.38f,1.05f),paint);box("Cargo floor",new Vector3(0,.57f,-1.3f),new Vector3(1.43f,.03f,.9f),iron);}
            box("Front bumper",new Vector3(0,.04f,1.93f),new Vector3(1.82f,.19f,.16f),iron);box("Rear bumper",new Vector3(0,.04f,-1.94f),new Vector3(1.82f,.19f,.16f),iron);
            for(int i=0;i<6;i++)box("Radiator grille",new Vector3(-.4f+i*.16f,.31f,1.77f),new Vector3(.055f,.18f,.03f),iron);
            MakeEngine();
        }
        void OnEnable(){if(modelRoot)modelRoot.gameObject.SetActive(true);if(engine)engine.UnPause();var bike=GetComponent<MotorcycleVisual>();if(bike)bike.enabled=true;}
        void OnDisable(){if(modelRoot)modelRoot.gameObject.SetActive(false);if(engine)engine.Pause();var bike=GetComponent<MotorcycleVisual>();if(bike)bike.enabled=false;}
        void MakeEngine()
        {
            int rate=12000;float[] samples=new float[rate];for(int i=0;i<rate;i++){float t=i/(float)rate;samples[i]=(Mathf.Sin(t*2*Mathf.PI*48)+Mathf.Sin(t*2*Mathf.PI*96)*.3f)*.14f;}
            var clip=AudioClip.Create("Low engine idle",rate,1,rate,false);clip.SetData(samples,0);engine=gameObject.AddComponent<AudioSource>();engine.clip=clip;engine.loop=true;engine.spatialBlend=1;engine.minDistance=3;engine.maxDistance=65;engine.rolloffMode=AudioRolloffMode.Linear;engine.dopplerLevel=0;engine.Play();
        }

        void Update(){if(!car)return;foreach(var wheel in wheels)wheel.Rotate(Vector3.up,car.Speed.Value*Time.deltaTime*145,Space.Self);if(engine){engine.volume=car.Condition.Value>0&&car.Fuel.Value>0&&car.Seat0.Value!=RoadVehicle.Empty?.27f:0;engine.pitch=.7f+Mathf.Abs(car.Speed.Value)*.045f;}}
        void OnDestroy(){foreach(var m in materials)if(m)Destroy(m);if(engine&&engine.clip)Destroy(engine.clip);}
    }
}
