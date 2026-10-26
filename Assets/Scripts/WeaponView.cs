using UnityEngine;

namespace Pinefall
{
    public sealed class WeaponView : MonoBehaviour
    {
        public Ranger owner;
        Transform model;
        Material metal,wood,glass,skin,sleeve;
        float recoil,flashUntil;
        Light flash;
        int equipped=-1,attachmentStamp=-1;
        void Start()
        {
            metal=Mat("Blued steel",new Color(.12f,.15f,.16f),.7f,.42f);
            wood=Mat("Walnut",new Color(.29f,.13f,.065f),0,.24f);
            glass=Mat("Optic",new Color(.08f,.24f,.28f),.5f,.8f);
            skin=Mat("Hands",new Color(.59f,.4f,.26f),0,.15f);
            sleeve=Mat("Field jacket",new Color(.23f,.29f,.21f),0,.1f);
            Equip(owner?owner.Weapon.Value:0);
        }
        public static Material Mat(string name,Color color,float metallic=0,float smooth=.15f)
        {
            var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);return m;
        }
        public void Equip(int index)
        {
            if(!metal)return;
            if(model)Destroy(model.gameObject);equipped=index;if(index<0){model=null;return;}
            attachmentStamp=owner?owner.PackedMods(index):0;model=new GameObject("Held "+WeaponData.Get(index).name).transform;model.SetParent(transform,false);
            var root=new GameObject("Weapon assembly").transform;root.SetParent(model,false);
            // Z points along the barrel. The complete model is replaceable independently of weapon logic.
            Part(root,PrimitiveType.Cube,new Vector3(0,-.025f,-.12f),new Vector3(.075f,.105f,.36f),wood);
            Part(root,PrimitiveType.Cube,new Vector3(0,.02f,.11f),new Vector3(.075f,.07f,.32f),metal);
            Part(root,PrimitiveType.Cube,new Vector3(0,-.025f,.2f),new Vector3(.065f,.08f,.24f),index==1?metal:wood);
            Tube(root,new Vector3(0,.055f,.4f),.018f,index==2?.48f:.4f,metal);
            Part(root,PrimitiveType.Cube,new Vector3(0,-.1f,.005f),new Vector3(.05f,.14f,.065f),wood,new Vector3(-18,0,0));
            if(index==0||index==5)
            {
                Tube(root,new Vector3(0,.15f,.12f),.033f,.25f,metal);
                Tube(root,new Vector3(0,.15f,.255f),.04f,.025f,glass);
                Part(root,PrimitiveType.Cube,new Vector3(0,.105f,.1f),new Vector3(.025f,.08f,.16f),metal);
                Tube(root,new Vector3(.07f,.04f,.04f),.01f,.09f,metal,new Vector3(0,90,0));
                Part(root,PrimitiveType.Sphere,new Vector3(.105f,.04f,.04f),Vector3.one*.03f,metal);
            }
            if(index==1||index>=4)
            {
                Part(root,PrimitiveType.Cube,new Vector3(0,-.1f,.09f),new Vector3(.05f,.16f,.075f),metal,new Vector3(-8,0,0));
                Part(root,PrimitiveType.Cube,new Vector3(0,.12f,.12f),new Vector3(.07f,.09f,.04f),metal);
                Part(root,PrimitiveType.Cube,new Vector3(0,.12f,.095f),new Vector3(.045f,.055f,.007f),glass);
            }
            if(index==2)Tube(root,new Vector3(0,.014f,.35f),.02f,.35f,metal);
            if(index==3){root.localScale=new Vector3(.9f,.95f,.43f);Part(root,PrimitiveType.Cube,new Vector3(0,.09f,.2f),new Vector3(.082f,.09f,.48f),metal);}
            if(index==4)root.localScale=new Vector3(.95f,1,.74f);
            if(index==6){Part(root,PrimitiveType.Cube,new Vector3(0,-.17f,.15f),new Vector3(.18f,.22f,.18f),metal);for(int rib=0;rib<9;rib++)Part(root,PrimitiveType.Cube,new Vector3(0,.07f,.2f+rib*.027f),new Vector3(.085f,.025f,.012f),metal);}
            var detailed=Resources.Load<GameObject>(index==3||index==4?"RealPistol":"RealRifle");
            if(detailed)
            {
                Destroy(root.gameObject);root=Instantiate(detailed,model,false).transform;root.localPosition=new Vector3(0,.025f,index==3||index==4?.13f:.10f);
                if(index!=0&&index!=5)foreach(var child in root.GetComponentsInChildren<Transform>())if(child.name.Contains("_scope")||child.name.Contains("_wrap"))child.gameObject.SetActive(false);
                if(index==1)root.localScale=new Vector3(1,1,.82f);
                if(index==2){root.localScale=new Vector3(1.12f,1.10f,.90f);Tube(root,new Vector3(0,-.015f,.28f),.022f,.39f,metal);Part(root,PrimitiveType.Capsule,new Vector3(0,-.01f,.22f),new Vector3(.07f,.11f,.07f),wood,new Vector3(90,0,0));}
                if(index==4){root.localScale=new Vector3(1,1,1.5f);Part(root,PrimitiveType.Cube,new Vector3(0,-.10f,-.03f),new Vector3(.035f,.15f,.055f),metal);Tube(root,new Vector3(0,.04f,.20f),.023f,.20f,metal);Part(root,PrimitiveType.Cube,new Vector3(0,.025f,-.22f),new Vector3(.06f,.08f,.25f),metal);}
                if(index==6){Part(root,PrimitiveType.Cube,new Vector3(0,-.11f,-.08f),new Vector3(.16f,.16f,.13f),metal);foreach(float side in new[]{-1f,1f})Part(root,PrimitiveType.Capsule,new Vector3(side*.07f,-.08f,.35f),new Vector3(.015f,.14f,.015f),metal,new Vector3(0,0,side*24));}
            }
            if(owner){int optic=owner.Mod(index,0),barrel=owner.Mod(index,1),handling=owner.Mod(index,2);if(optic>0){foreach(var child in root.GetComponentsInChildren<Transform>())if(child.name.Contains("_scope"))child.gameObject.SetActive(false);if(optic==1){Part(model,PrimitiveType.Cube,new Vector3(0,.14f,.26f),new Vector3(.07f,.09f,.05f),metal);Part(model,PrimitiveType.Cube,new Vector3(0,.14f,.23f),new Vector3(.05f,.06f,.008f),glass);}else{Tube(model,new Vector3(0,.16f,.22f),optic==3?.045f:.034f,optic==3?.31f:.23f,metal);Tube(model,new Vector3(0,.16f,.38f),.04f,.015f,glass);}}if(barrel>0)Tube(model,new Vector3(0,.055f,.66f),barrel==6?.029f:.024f,.11f,metal);if(handling==8)Part(model,PrimitiveType.Capsule,new Vector3(0,-.09f,.29f),new Vector3(.04f,.09f,.04f),metal);}
            Part(model,PrimitiveType.Sphere,new Vector3(.035f,-.115f,.01f),new Vector3(.085f,.1f,.11f),skin);
            Part(model,PrimitiveType.Capsule,new Vector3(.13f,-.22f,-.08f),new Vector3(.11f,.2f,.11f),sleeve,new Vector3(0,0,40));
            Part(model,PrimitiveType.Sphere,new Vector3(-.035f,-.055f,.29f),new Vector3(.08f,.07f,.11f),skin);
            Part(model,PrimitiveType.Capsule,new Vector3(-.14f,-.17f,.15f),new Vector3(.1f,.19f,.1f),sleeve,new Vector3(0,0,-42));
            var f=new GameObject("Muzzle light");f.transform.SetParent(root,false);f.transform.localPosition=new Vector3(0,.06f,.65f);flash=f.AddComponent<Light>();flash.type=LightType.Point;flash.color=new Color(1,.65f,.25f);flash.range=6;flash.intensity=0;
        }
        public static GameObject Part(Transform root,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,Vector3 angles=default(Vector3))
        {
            var g=GameObject.CreatePrimitive(type);g.name=mat?mat.name:"Part";g.transform.SetParent(root,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.transform.localEulerAngles=angles;
            var collider=g.GetComponent<Collider>();if(collider)Destroy(collider);
            if(type==PrimitiveType.Cube)g.GetComponent<MeshFilter>().sharedMesh=SoftGeometry.BeveledCube;
            g.GetComponent<Renderer>().sharedMaterial=mat;g.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;g.layer=10;return g;
        }
        static void Tube(Transform root,Vector3 pos,float radius,float length,Material mat,Vector3 extra=default(Vector3))
        {Part(root,PrimitiveType.Cylinder,pos,new Vector3(radius*2,length/2,radius*2),mat,new Vector3(90,0,0)+extra);}
        public void Kick(){recoil=1;flashUntil=Time.time+.055f;}
        public void SetVisible(bool visible){if(model)model.gameObject.SetActive(visible);}
        void LateUpdate()
        {
            if(!owner)return;
            if(equipped!=owner.Weapon.Value||attachmentStamp!=owner.PackedMods(owner.Weapon.Value))Equip(owner.Weapon.Value);if(!model)return;
            bool scoped=owner.ScopedView;var camera=GetComponent<RangerCamera>();
            model.gameObject.SetActive(owner.Fighting&&(!owner.CurrentVehicle||owner.CurrentVehicle.Type.Value==4||owner.VehicleSeat.Value>0)&&!scoped&&!(camera&&camera.ExternalView));
            float bob=owner.Velocity.Value>.2f?Mathf.Sin(Time.time*owner.Velocity.Value*2.2f)*.007f:0;
            var basePos=owner.Aiming?new Vector3(.02f,-.14f,.32f):new Vector3(.28f,-.27f,.46f);
            if(owner.Reloading.Value)basePos+=new Vector3(.12f,-.22f,-.09f);
            model.localPosition=Vector3.Lerp(model.localPosition,basePos+new Vector3(0,bob,-recoil*.11f),Time.deltaTime*14);
            model.localRotation=Quaternion.Slerp(model.localRotation,Quaternion.Euler(-recoil*9,0,owner.Reloading.Value?25:0),Time.deltaTime*18);
            recoil=Mathf.MoveTowards(recoil,0,Time.deltaTime*(5));
            if(flash)flash.intensity=Time.time<flashUntil?2:0;
        }
        void OnDestroy(){foreach(var m in new[]{metal,wood,glass,skin,sleeve})if(m)Destroy(m);}
    }
}
