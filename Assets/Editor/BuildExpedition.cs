using System;
using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Pinefall;
public static partial class BuildReserve
{
    static void CreateExpeditionPrefabs()
    {
        var drop=new GameObject("Supply drop");drop.AddComponent<NetworkObject>();drop.AddComponent<SupplyDrop>();var dropCollider=drop.AddComponent<BoxCollider>();dropCollider.size=Vector3.one*1.5f;dropCollider.isTrigger=true;PrefabUtility.SaveAsPrefabAsset(drop,R+"SupplyDrop.prefab");UnityEngine.Object.DestroyImmediate(drop);
        var draft=new GameObject("Open spawn draft");draft.AddComponent<NetworkObject>();draft.AddComponent<SpawnDraft>();PrefabUtility.SaveAsPrefabAsset(draft,R+"SpawnDraft.prefab");UnityEngine.Object.DestroyImmediate(draft);
        var loot=new GameObject("World equipment");loot.AddComponent<NetworkObject>();loot.AddComponent<WorldLoot>();var c=loot.AddComponent<BoxCollider>();c.size=new Vector3(.5f,.5f,.8f);c.isTrigger=true;PrefabUtility.SaveAsPrefabAsset(loot,R+"WorldLoot.prefab");UnityEngine.Object.DestroyImmediate(loot);
        var car=new GameObject("Abandoned vehicle");car.AddComponent<NetworkObject>();var nt=car.AddComponent<NetworkTransform>();nt.SyncScaleX=nt.SyncScaleY=nt.SyncScaleZ=false;nt.Interpolate=true;car.AddComponent<RoadVehicle>();var box=car.AddComponent<BoxCollider>();box.center=new Vector3(0,.45f,0);box.size=new Vector3(1.85f,1.55f,3.9f);PrefabUtility.SaveAsPrefabAsset(car,R+"RoadVehicle.prefab");UnityEngine.Object.DestroyImmediate(car);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/RangerAlternate.glb");var body=UnityEngine.Object.Instantiate(source);body.name="Alternate ranger";Normalize(body,1.8f);SetLayer(body,9);var rig=body.AddComponent<VisualRig>();SetupClips(rig,"Assets/Art/Characters/Animations.glb","Alternate",new[]{"Idle_Loop","Walk_Loop","Sprint_Loop",""});PrefabUtility.SaveAsPrefabAsset(body,R+"AlternateAvatar.prefab");UnityEngine.Object.DestroyImmediate(body);
        CreateShark();
        Animal("Horse","Assets/Art/Animals/Horse.gltf",false);Animal("Alpaca","Assets/Art/Animals/Alpaca.gltf",false);
        Variant("Antelope","Deer","Pronghorn",1.15f,new Color(.83f,.70f,.44f));Variant("Elk","Stag","Mountain elk",1.1f,new Color(.62f,.49f,.33f));Variant("ArcticFox","Fox","Arctic fox",1,new Color(.94f,.96f,.97f));
        SmallAnimal("Hare",0,new Color(.53f,.44f,.30f));SmallAnimal("SnowHare",0,new Color(.88f,.93f,.95f));SmallAnimal("Boar",1,new Color(.30f,.24f,.19f));SmallAnimal("JungleBoar",1,new Color(.22f,.20f,.15f));SmallAnimal("Ibex",2,new Color(.53f,.47f,.35f));
    }
    static void Variant(string name,string basis,string species,float scale,Color tint)
    {
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(R+basis+".prefab"));go.name=name;go.GetComponent<Wildlife>().species=species;
        go.transform.Find("Body").localScale*=scale;
        foreach(var r in go.GetComponentsInChildren<Renderer>()){var mat=new Material(r.sharedMaterial);mat.color=tint;Save(mat,"Assets/Generated/"+name+r.name+".mat");r.sharedMaterial=mat;}
        PrefabUtility.SaveAsPrefabAsset(go,R+name+".prefab");UnityEngine.Object.DestroyImmediate(go);
    }
    static void SmallAnimal(string name,int type,Color color)
    {
        var root=new GameObject(name);root.layer=8;root.AddComponent<NetworkObject>();var nt=root.AddComponent<NetworkTransform>();nt.SyncScaleX=nt.SyncScaleY=nt.SyncScaleZ=false;nt.Interpolate=true;
        var animal=root.AddComponent<Wildlife>();animal.species=name=="SnowHare"?"Snowshoe hare":name=="JungleBoar"?"Forest boar":name=="Ibex"?"Alpine ibex":name;animal.kind=type==1?3:0;animal.maxHealth=type==0?30:type==1?115:90;
        var col=root.AddComponent<CapsuleCollider>();col.height=type==0?.55f:1.05f;col.radius=type==0?.22f:.4f;col.center=Vector3.up*col.height*.5f;
        var body=new GameObject("Body").transform;body.SetParent(root.transform,false);var moving=new GameObject("Animated body").transform;moving.SetParent(body,false);
        Material fur=MaterialAsset(name+" fur",color),dark=MaterialAsset(name+" eyes",new Color(.055f,.045f,.035f)),horn=MaterialAsset(name+" horn",new Color(.73f,.69f,.55f));
        var model=body.gameObject.AddComponent<NativeFauna>();model.torso=moving;model.hare=type==0;model.legs=new Transform[4];
        Part("Torso",PrimitiveType.Sphere,new Vector3(0,type==0?.28f:.57f,0),type==0?new Vector3(.34f,.35f,.57f):new Vector3(.61f,.6f,.95f),fur,moving);
        Part("Head",PrimitiveType.Sphere,new Vector3(0,type==0?.43f:.75f,type==0?.25f:.53f),type==0?new Vector3(.25f,.25f,.24f):new Vector3(.38f,.38f,.43f),fur,moving);
        for(int side=-1;side<=1;side+=2)
        {
            Part("Ear",PrimitiveType.Capsule,new Vector3(side*(type==0?.075f:.18f),type==0?.65f:.94f,type==0?.22f:.48f),new Vector3(.065f,type==0?.18f:.07f,.075f),fur,moving,new Vector3(0,0,side*12));
            Part("Eye",PrimitiveType.Sphere,new Vector3(side*(type==0?.11f:.18f),type==0?.45f:.79f,type==0?.29f:.63f),Vector3.one*(type==0?.035f:.045f),dark,moving);
            for(int row=0;row<2;row++){var leg=new GameObject("Leg").transform;leg.SetParent(moving,false);leg.localPosition=new Vector3(side*(type==0?.12f:.22f),type==0?.18f:.44f,row==0?.28f:-.29f);Part("Lower leg",PrimitiveType.Capsule,new Vector3(0,type==0?-.05f:-.16f,0),new Vector3(type==0?.09f:.13f,type==0?.08f:.22f,type==0?.12f:.13f),fur,leg);model.legs[row*2+(side==1?1:0)]=leg;}
            if(type==1)Part("Tusk",PrimitiveType.Capsule,new Vector3(side*.16f,.65f,.75f),new Vector3(.045f,.1f,.045f),horn,moving,new Vector3(-25,0,0));
            if(type==2)for(int i=0;i<4;i++)Part("Curved horn",PrimitiveType.Capsule,new Vector3(side*.13f,.99f+i*.10f,.48f-i*.05f),new Vector3(.06f,.09f,.06f),horn,moving,new Vector3(-i*14,0,side*8));
        }
        Part("Tail",PrimitiveType.Sphere,new Vector3(0,type==0?.3f:.63f,type==0?-.28f:-.5f),Vector3.one*(type==0?.13f:.12f),fur,moving);SetLayer(body.gameObject,8);
        foreach(var collider in body.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
        foreach(var transform in body.GetComponentsInChildren<Transform>())transform.gameObject.isStatic=false;
        PrefabUtility.SaveAsPrefabAsset(root,R+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
    }
    static void Settlements()
    {
        for(int r=0;r<ExpeditionLayout.Regions.Length;r++)
        {
            var district=ExpeditionLayout.Regions[r];var site=new GameObject(district.name).transform;site.position=ReserveWorld.OnGround(district.center.x,district.center.y);
            var plaster=MaterialAsset(district.name+" cladding",district.biome==1?new Color(.63f,.48f,.32f):district.biome==2?new Color(.51f,.60f,.62f):district.biome==4?new Color(.31f,.35f,.23f):new Color(.4f,.36f,.28f));
            plaster.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/WeatheredWood.jpg");plaster.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/WeatheredWoodNormal.jpg"));plaster.EnableKeyword("_NORMALMAP");
            for(int house=0;house<ExpeditionLayout.HouseCount(district);house++)
            {
                var offset=VillageFootprints.Offset(district,house);var h=new GameObject("Abandoned cabin").transform;h.SetParent(site,false);h.localPosition=new Vector3(offset.x,ReserveWorld.Height(district.center.x+offset.x,district.center.y+offset.y)-site.position.y,offset.y);
                Part("Foundation",PrimitiveType.Cube,new Vector3(0,.1f,0),new Vector3(8,.2f,7),stoneMaterial(),h);
                Part("Rear wall",PrimitiveType.Cube,new Vector3(0,1.7f,3.4f),new Vector3(8,3.2f,.22f),plaster,h);
                foreach(float side in new[]{-3.9f,3.9f}){Part("Side wall lower",PrimitiveType.Cube,new Vector3(side,.65f,0),new Vector3(.22f,1.1f,7),plaster,h);Part("Side wall upper",PrimitiveType.Cube,new Vector3(side,2.9f,0),new Vector3(.22f,.75f,7),plaster,h);foreach(float z in new[]{-2.4f,0,2.4f})Part("Window post",PrimitiveType.Cube,new Vector3(side,1.8f,z),new Vector3(.23f,1.6f,.55f),wood,h);}
                foreach(float side in new[]{-2.65f,2.65f})Part("Door surround",PrimitiveType.Cube,new Vector3(side,1.7f,-3.4f),new Vector3(2.7f,3.2f,.22f),plaster,h);
                Part("Door lintel",PrimitiveType.Cube,new Vector3(0,2.9f,-3.4f),new Vector3(2.6f,.8f,.22f),wood,h);
                if(house<2){Part("Roof left",PrimitiveType.Cube,new Vector3(-2.1f,3.92f,0),new Vector3(4.65f,.18f,8),roof,h,new Vector3(0,0,18));Part("Roof right",PrimitiveType.Cube,new Vector3(2.1f,3.92f,0),new Vector3(4.65f,.18f,8),roof,h,new Vector3(0,0,-18));}
                else Part("Shed roof",PrimitiveType.Cube,new Vector3(0,3.6f,0),new Vector3(8.7f,.19f,8),roof,h,new Vector3(0,0,5));
                Part("Workbench",PrimitiveType.Cube,new Vector3(0,.9f,2.65f),new Vector3(3,.14f,.9f),wood,h);
                foreach(float x in new[]{-2.7f,2.7f}){Part("Storage crate",PrimitiveType.Cube,new Vector3(x,.4f,2.35f),new Vector3(.85f,.8f,.8f),wood,h);Part("Crate band",PrimitiveType.Cube,new Vector3(x,.81f,2.35f),new Vector3(.9f,.025f,.1f),metal,h);}
                for(int plank=0;plank<4;plank++)Part("Roof rib",PrimitiveType.Cube,new Vector3(-3+plank*2,3.73f,0),new Vector3(.06f,.045f,8),metal,h);
            }
            VillageDetails(site,district);Part("Site sign",PrimitiveType.Cube,new Vector3(-14,2,-30),new Vector3(7,1,.15f),wood,site);
            Label(district.name,new Vector3(-17.1f,2.2f,-30.1f),.14f,24,site,new Color(.89f,.84f,.68f),Vector3.zero);
            for(int i=0;i<9;i++)Part("Roadside barrier",PrimitiveType.Cube,new Vector3(-28+i*7,.6f,36),new Vector3(3,1.2f,.45f),wood,site);
        }
    }
    static Material stoneMaterial(){return AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/Lichen stone.mat");}
}
