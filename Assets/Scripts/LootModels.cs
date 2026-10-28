using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pinefall
{
    // Shared source geometry/materials: rarity is a marker, never a paint job on a gun.
    public static class LootModels
    {
        static readonly Dictionary<int, GameObject> templates = new Dictionary<int, GameObject>();
        static Material steel, rubber, canvas, red, brass, ivory, olive, glass;
        static void Prepare()
        {
            if (steel) return;
            steel=WeaponView.Mat("Equipment / phosphate steel",new Color(.16f,.18f,.19f),.72f,.42f);
            rubber=WeaponView.Mat("Equipment / woven charcoal",new Color(.07f,.085f,.09f),0,.16f);
            canvas=WeaponView.Mat("Equipment / field canvas",new Color(.40f,.37f,.28f),0,.20f);
            red=WeaponView.Mat("Equipment / medical red",new Color(.48f,.055f,.045f),0,.27f);
            brass=WeaponView.Mat("Equipment / cartridge brass",new Color(.57f,.38f,.15f),.78f,.53f);
            ivory=WeaponView.Mat("Equipment / label",new Color(.79f,.81f,.75f),0,.20f);
            olive=WeaponView.Mat("Equipment / olive enamel",new Color(.22f,.28f,.20f),.25f,.35f);
            glass=WeaponView.Mat("Equipment / coated optic",new Color(.08f,.22f,.25f),.65f,.82f);
            foreach(var m in new[]{steel,rubber,canvas,red,brass,ivory,olive,glass})m.enableInstancing=true;
        }
        public static GameObject Create(Transform parent,int kind,int item)
        {
            Prepare();int key=kind*32+Mathf.Clamp(item,0,20);
            if(!templates.TryGetValue(key,out var source)||!source)
            {
                source=new GameObject("Loot source "+key);
                Object.DontDestroyOnLoad(source);Build(source.transform,kind,item);Combine(source.transform);source.SetActive(false);
                foreach(var child in source.GetComponentsInChildren<Transform>(true))child.gameObject.layer=0;
                templates[key]=source;
            }
            var result=Object.Instantiate(source,parent,false);result.name="Recognizable equipment";result.SetActive(true);
            return result;
        }
        static GameObject Box(Transform root,Vector3 position,Vector3 size,Material material,Vector3 angles=default)
        { return WeaponView.Part(root,PrimitiveType.Cube,position,size,material,angles); }
        static void Tube(Transform root,Vector3 position,float radius,float length,Material material,Vector3 angles=default)
        { WeaponView.Part(root,PrimitiveType.Cylinder,position,new Vector3(radius*2,length*.5f,radius*2),material,angles); }
        static void Strap(Transform root,float x,float width,float depth,float height)
        { Box(root,new Vector3(x,height,0),new Vector3(.035f,.018f,depth+.018f),rubber); }
        static void Build(Transform root,int kind,int item)
        {
            if(kind==0)
            {
                var gun=new GameObject(WeaponData.Get(item).ClassLabel).transform;gun.SetParent(root,false);
                if(item==7)LauncherModel.Create(gun);
                else
                {
                    var prefab=Resources.Load<GameObject>(item==3||item==4?"RealPistol":"RealRifle");
                    if(prefab)
                    {
                        var model=Object.Instantiate(prefab,gun,false).transform;
                        if(item!=0&&item!=5)foreach(var child in model.GetComponentsInChildren<Transform>(true))if(child.name.Contains("_scope")||child.name.Contains("_wrap"))child.gameObject.SetActive(false);
                        if(item==1)model.localScale=new Vector3(1,1,.82f);
                        if(item==2){model.localScale=new Vector3(1.08f,1.1f,.9f);Tube(gun,new Vector3(0,-.03f,.25f),.027f,.36f,steel,new Vector3(90,0,0));}
                        if(item==4){model.localScale=new Vector3(1,1,1.5f);Box(gun,new Vector3(0,-.09f,0),new Vector3(.04f,.20f,.075f),steel);Box(gun,new Vector3(0,.01f,-.25f),new Vector3(.07f,.08f,.24f),rubber);}
                        if(item==6)Box(gun,new Vector3(0,-.13f,.04f),new Vector3(.17f,.19f,.16f),steel);
                    }
                }
                gun.localScale=Vector3.one*(item==3?1.65f:item==4?1.4f:1.2f);
                gun.localRotation=Quaternion.Euler(0,70,48);gun.localPosition=Vector3.up*.25f;
            }
            else if(kind==1)
            {
                Box(root,Vector3.up*.06f,new Vector3(.26f,.12f,.18f),olive);
                Box(root,new Vector3(0,.123f,0),new Vector3(.17f,.008f,.13f),ivory);
                for(int i=0;i<4;i++){float x=-.09f+i*.06f;Tube(root,new Vector3(x,.18f,0),item==2?.022f:.012f,item==2?.075f:.11f,item==2?red:brass);WeaponView.Part(root,PrimitiveType.Capsule,new Vector3(x,.24f,0),new Vector3(.020f,.025f,.020f),brass);}
            }
            else if(kind==2||kind==5||kind==9)
            {
                Box(root,Vector3.up*.09f,new Vector3(.38f,.18f,.24f),kind==2?red:canvas);
                Strap(root,-.12f,.38f,.24f,.19f);Strap(root,.12f,.38f,.24f,.19f);
                Box(root,new Vector3(0,.195f,0),new Vector3(.14f,.012f,.12f),kind==2?ivory:olive);
                if(kind==2){Box(root,new Vector3(0,.204f,0),new Vector3(.025f,.008f,.08f),red);Box(root,new Vector3(0,.204f,0),new Vector3(.08f,.008f,.025f),red);}
            }
            else if(kind==4)
            {
                Box(root,Vector3.up*.22f,new Vector3(.29f,.43f,.17f),olive);
                Box(root,new Vector3(0,.47f,0),new Vector3(.24f,.035f,.09f),steel);
                foreach(float x in new[]{-.10f,.10f})Box(root,new Vector3(x,.435f,0),new Vector3(.028f,.065f,.09f),steel);
                Tube(root,new Vector3(-.09f,.46f,.055f),.035f,.045f,steel);
                foreach(float side in new[]{-1f,1f})Box(root,new Vector3(side*.065f,.23f,.09f),new Vector3(.02f,.27f,.015f),steel,new Vector3(0,0,side*28));
            }
            else if(kind==6||kind==7)
            {
                Box(root,new Vector3(0,.055f,0),new Vector3(.38f,.10f,.46f),kind==6?canvas:steel);
                Box(root,new Vector3(0,.12f,0),new Vector3(.28f,.04f,.34f),rubber);
                for(int i=0;i<3;i++)Box(root,new Vector3(-.12f+i*.12f,.145f,.07f),new Vector3(.09f,.025f,.17f),olive);
                if(kind==6)foreach(float x in new[]{-.14f,.14f})Box(root,new Vector3(x,.07f,-.28f),new Vector3(.07f,.06f,.15f),canvas);
            }
            else if(kind==8)
            {
                if(item<=3){Tube(root,Vector3.up*.08f,.04f,item==1?.10f:.27f,steel,new Vector3(0,0,90));Tube(root,new Vector3(.14f,.08f,0),.038f,.008f,glass,new Vector3(0,0,90));Box(root,Vector3.up*.025f,new Vector3(.15f,.04f,.065f),rubber);}
                else if(item<=6)Tube(root,Vector3.up*.04f,.033f,.24f,steel,new Vector3(0,0,90));
                else{Box(root,Vector3.up*.08f,new Vector3(.08f,.16f,.055f),rubber,new Vector3(0,0,18));Box(root,new Vector3(.02f,.15f,0),new Vector3(.16f,.035f,.075f),steel);}
            }
            else if(kind==12||kind==15)
            {
                Box(root,Vector3.up*.12f,new Vector3(.23f,.24f,.16f),rubber);Box(root,new Vector3(0,.17f,-.09f),new Vector3(.16f,.09f,.02f),glass);Tube(root,new Vector3(.075f,.38f,0),.009f,.35f,steel);
            }
            else if(kind==13)
            {
                for(int i=0;i<3;i++)Box(root,new Vector3((i-1)*.1f,.06f,0),new Vector3(.08f,.10f,.42f),item==0?canvas:item==1?ivory:steel);
                Box(root,Vector3.up*.12f,new Vector3(.34f,.02f,.04f),rubber);
            }
            else
            {
                bool cache=kind==11;Box(root,Vector3.up*(cache?.19f:.08f),cache?new Vector3(.62f,.38f,.42f):new Vector3(.30f,.16f,.24f),olive);
                Box(root,Vector3.up*(cache?.39f:.17f),cache?new Vector3(.65f,.055f,.45f):new Vector3(.32f,.025f,.26f),steel);
                foreach(float x in new[]{-.22f,.22f})if(cache)Box(root,new Vector3(x,.20f,-.22f),new Vector3(.07f,.11f,.025f),brass);
            }
        }
        static void Combine(Transform root)
        {
            // Merge the many tiny authored parts once per item type, not per drop.
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!filter.sharedMesh||!filter.sharedMesh.isReadable||renderer.sharedMaterials.Length!=1)continue;
                var material=renderer.sharedMaterial;if(!material)continue;
                if(!groups.TryGetValue(material,out var group)){group=new List<CombineInstance>();groups.Add(material,group);}
                group.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});renderer.enabled=false;
            }
            foreach(var pair in groups)
            {
                var mesh=new Mesh{name="Shared loot / "+pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray());
                var output=new GameObject("Equipment surface");output.transform.SetParent(root,false);output.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=output.AddComponent<MeshRenderer>();renderer.sharedMaterial=pair.Key;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
    }
}
