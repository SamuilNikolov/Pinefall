using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;
using Pinefall;

public static partial class BuildReserve
{
    const string R="Assets/Resources/";
    static Font serif,sans;
    static Material wood,roof,metal,cream;
    public static void InspectTerrain()
    {
        var data=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/Generated/ReserveTerrain.asset");
        Vector2[] points={new Vector2(-20,-120),new Vector2(-500,-270),new Vector2(-330,500),new Vector2(420,450),new Vector2(500,-260)};
        foreach(var p in points)
        {
            int x=Mathf.Clamp(Mathf.RoundToInt((p.x/ReserveWorld.Size+.5f)*(data.alphamapWidth-1)),0,data.alphamapWidth-1),z=Mathf.Clamp(Mathf.RoundToInt((p.y/ReserveWorld.Size+.5f)*(data.alphamapHeight-1)),0,data.alphamapHeight-1);
            var a=data.GetAlphamaps(x,z,1,1);var weights=new List<float>();for(int i=0;i<data.alphamapLayers;i++)weights.Add(a[0,0,i]);Debug.Log("TERRAIN_SAMPLE "+p+" biome "+ReserveBiomes.At(p.x,p.y)+" weights "+string.Join(",",weights));
        }
    }
    [MenuItem("Pinefall/Build playable demo")]
    public static void Build()
    {
        Setup();
        BuildCurrent();
    }
    [MenuItem("Pinefall/Build current scene")]
    public static void BuildCurrent()
    {
        PrepareFieldcraft();PrepareRoadCoverage();PlayerSettings.bundleVersion="0.9.0";
        string requested=ReserveSession.Arg("-pinefallBuildPath");
        string path=string.IsNullOrEmpty(requested)?Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/Windows/Pinefall.exe")):Path.GetFullPath(requested);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Reserve.unity"},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        Debug.Log("PINEFALL_BUILD "+report.summary.result+" "+report.summary.totalSize);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed");
    }
    [MenuItem("Pinefall/Rebuild reserve scene")]
    public static void Setup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Directory.CreateDirectory(R+"Clips");Directory.CreateDirectory("Assets/Generated");Directory.CreateDirectory("Assets/Scenes");
        serif=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ReserveSerif.ttf");sans=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ReserveSans.ttf");
        CreatePrefabs();
        CreateExpeditionPrefabs();CreateRealAssets();PrepareAtlasTexture();CreateWeaponIcons();
        wood=MaterialAsset("Outpost timber",new Color(.24f,.16f,.105f));roof=MaterialAsset("Weathered roof",new Color(.13f,.21f,.21f));metal=MaterialAsset("Field iron",new Color(.13f,.16f,.16f));cream=MaterialAsset("Sign paint",new Color(.84f,.77f,.56f));
        var terrain=CreateTerrain();
        var root=new GameObject("Pinefall reserve");var world=root.AddComponent<ReserveWorld>();world.ground=terrain;
        world.pine=Nature("Pine needles",new Color(.20f,.31f,.23f),.025f);world.bark=Nature("Pine bark",new Color(.28f,.20f,.13f),0);world.stone=Nature("Lichen stone",new Color(.43f,.46f,.40f),0);
        var needle=NeedleTexture();Save(needle,"Assets/Generated/PineNeedles.asset");world.pine.SetTexture("_MainTex",needle);
        world.palm=Nature("Rainforest leaves",new Color(.19f,.39f,.20f),.035f);world.cactus=Nature("Cactus",new Color(.27f,.37f,.19f),0);
        world.bark.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/Bark.jpg"));
        world.stone.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/Rock.jpg"));
        var wear=MaterialAsset("Vehicle wear",Color.white);Save(new Material(wear),R+"VehicleWear.mat");
        Outpost();Settlements();RoadsideStops();Bridges();ExtraDocks();Backdrop();PrepareLivingResources();CreateCaves(terrain);CreateSettlementDetail();RegenerateRoadCoverage();
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.55f,.64f,.70f);RenderSettings.ambientEquatorColor=new Color(.48f,.5f,.43f);RenderSettings.ambientGroundColor=new Color(.29f,.30f,.23f);RenderSettings.ambientIntensity=1;
        var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.48f,.57f,.67f));sky.SetColor("_GroundColor",new Color(.48f,.47f,.37f));sky.SetFloat("_SunSize",.025f);sky.SetFloat("_AtmosphereThickness",.85f);sky.SetFloat("_Exposure",1.15f);Save(sky,"Assets/Generated/ReserveSky.mat");RenderSettings.skybox=sky;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.67f,.73f,.70f);RenderSettings.fogDensity=.00105f;
        var sun=new GameObject("Late afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.88f,.69f);sun.intensity=1.2f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.68f;sun.transform.rotation=Quaternion.Euler(29,-38,0);RenderSettings.sun=sun;
        AddWeather(sun);
        var cam=new GameObject("Reserve vista");cam.tag="MainCamera";cam.transform.position=ReserveWorld.OnGround(-13,-176,3.6f);var camera=cam.AddComponent<Camera>();camera.fieldOfView=63;camera.farClipPlane=2300;camera.nearClipPlane=.1f;camera.allowHDR=true;cam.AddComponent<AudioListener>();
        var vista=cam.AddComponent<VistaCamera>();vista.lookAt=ReserveWorld.OnGround(38,-45,2.5f);cam.transform.LookAt(vista.lookAt);
        var herd=new GameObject("Meadow preview herd");
        for(int i=0;i<4;i++)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(R+(i==0?"Stag":"Deer")+".prefab");var model=prefab.transform.Find("Body");
            var g=UnityEngine.Object.Instantiate(model.gameObject,herd.transform);g.name="Grazing "+i;g.transform.position=ReserveWorld.OnGround(35+i*7,-103+i*11);g.transform.rotation=Quaternion.Euler(0,190+i*22,0);g.AddComponent<PreviewAnimal>();
        }
        var app=new GameObject("Reserve application");var session=app.AddComponent<ReserveSession>();session.vistaCamera=camera;session.previewHerd=herd;
        var ui=app.AddComponent<ReserveUI>();ui.serif=serif;ui.sans=sans;
        var dust=new Material(Shader.Find("Particles/Standard Unlit"));dust.SetColor("_Color",new Color(.6f,.55f,.4f,.8f));Save(dust,R+"Dust.mat");
        PlayerSettings.companyName="Pinefall";PlayerSettings.productName="Pinefall Wildfront";PlayerSettings.bundleVersion="0.9.0";
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.allowFullscreenSwitch=true;
        var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");layers.GetArrayElementAtIndex(8).stringValue="Wildlife";layers.GetArrayElementAtIndex(9).stringValue="Rangers";layers.GetArrayElementAtIndex(10).stringValue="Viewmodel";tags.ApplyModifiedProperties();
        Physics.IgnoreLayerCollision(9,9,true);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Scenes/Reserve.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Reserve.unity",true)};
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        ExportHandbookData();Debug.Log("PINEFALL_SETUP_OK");
    }
    static void CreatePrefabs()
    {
        var match=new GameObject("Wildfront round");match.AddComponent<NetworkObject>();match.AddComponent<WildfrontMatch>();PrefabUtility.SaveAsPrefabAsset(match,R+"WildfrontMatch.prefab");UnityEngine.Object.DestroyImmediate(match);
        var loot=new GameObject("Harvest reward");loot.AddComponent<NetworkObject>();var pickup=loot.AddComponent<FieldPickup>();
        var col=loot.AddComponent<BoxCollider>();col.size=Vector3.one*.8f;col.isTrigger=true;
        var rewardMaterial=MaterialAsset("Reward case",new Color(.31f,.43f,.34f));var brass=MaterialAsset("Reward marker",new Color(.9f,.72f,.34f));
        Part("Supply case",PrimitiveType.Cube,Vector3.zero,new Vector3(.65f,.35f,.45f),rewardMaterial,loot.transform);
        pickup.marker=Part("Reward marker",PrimitiveType.Cube,new Vector3(0,.95f,0),new Vector3(.11f,.3f,.11f),brass,loot.transform).transform;
        PrefabUtility.SaveAsPrefabAsset(loot,R+"FieldReward.prefab");UnityEngine.Object.DestroyImmediate(loot);
        var weather=new GameObject("Shared weather");weather.AddComponent<NetworkObject>();weather.AddComponent<WeatherState>();PrefabUtility.SaveAsPrefabAsset(weather,R+"ReserveWeather.prefab");UnityEngine.Object.DestroyImmediate(weather);
        Animal("Deer","Assets/Art/Animals/Deer.gltf",false);Animal("Stag","Assets/Art/Animals/Stag.gltf",true);
        Animal("Wolf","Assets/Art/Animals/Wolf.gltf",false);Animal("Fox","Assets/Art/Animals/Fox.gltf",false);Animal("Ox","Assets/Art/Animals/Bull.gltf",false);
        var root=new GameObject("Ranger");root.layer=9;root.AddComponent<NetworkObject>();var nt=root.AddComponent<OwnerTransform>();nt.Interpolate=true;nt.SyncScaleX=nt.SyncScaleY=nt.SyncScaleZ=false;nt.SyncRotAngleX=false;nt.SyncRotAngleZ=false;
        var controller=root.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.3f;controller.center=Vector3.up*.9f;controller.stepOffset=.35f;controller.slopeLimit=48;controller.skinWidth=.04f;
        var hitbox=root.AddComponent<CapsuleCollider>();hitbox.height=1.8f;hitbox.radius=.33f;hitbox.center=Vector3.up*.9f;hitbox.isTrigger=true;
        var ranger=root.AddComponent<Ranger>();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Hunter.glb");
        if(!source)throw new Exception("Hunter import failed");
        var body=UnityEngine.Object.Instantiate(source,root.transform);body.name="Body";
        Normalize(body,1.8f);ranger.body=body.transform;
        var rig=body.AddComponent<VisualRig>();SetupClips(rig,"Assets/Art/Characters/Animations.glb","Ranger",new[]{"Idle_Loop","Walk_Loop","Sprint_Loop",""});
        SetLayer(body,9);PrefabUtility.SaveAsPrefabAsset(root,R+"Ranger.prefab");UnityEngine.Object.DestroyImmediate(root);
    }
    static void Animal(string name,string source,bool stag)
    {
        var root=new GameObject(name);root.layer=8;root.AddComponent<NetworkObject>();var nt=root.AddComponent<NetworkTransform>();nt.Interpolate=true;nt.SyncScaleX=nt.SyncScaleY=nt.SyncScaleZ=false;nt.SyncRotAngleX=false;nt.SyncRotAngleZ=false;
        var wildlife=root.AddComponent<Wildlife>();wildlife.species=stag?"Red stag":"White-tailed deer";
        if(name=="Horse")wildlife.species="Wild horse";if(name=="Alpaca")wildlife.species="Highland alpaca";
        wildlife.kind=name=="Fox"?1:name=="Wolf"?2:name=="Ox"?3:0;wildlife.maxHealth=name=="Ox"?180:name=="Fox"?60:100;
        if(name=="Wolf")wildlife.species="Timber wolf";if(name=="Fox")wildlife.species="Desert fox";if(name=="Ox")wildlife.species="Wild ox";
        var col=root.AddComponent<CapsuleCollider>();col.radius=name=="Fox"?.23f:name=="Wolf"?.33f:.42f;col.height=name=="Fox"?.65f:name=="Wolf"?.95f:1.6f;col.center=Vector3.up*col.height*.5f;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(source);if(!prefab)throw new Exception("Animal import failed: "+source);
        var body=UnityEngine.Object.Instantiate(prefab,root.transform);body.name="Body";Normalize(body,name=="Wolf"?.95f:name=="Fox"?.65f:name=="Ox"?1.7f:stag?2.1f:1.5f);SetLayer(body,8);
        var rig=body.AddComponent<VisualRig>();SetupClips(rig,source,name,new[]{"Eating","Walk","Gallop","Death",name=="Wolf"||name=="Fox"?"Attack":"Attack_Headbutt"});wildlife.rig=rig;
        PrefabUtility.SaveAsPrefabAsset(root,R+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
    }
    static void Normalize(GameObject body,float height)
    {
        var renderers=body.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("Model has no renderers: "+body.name);
        var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        float scale=height/Mathf.Max(.01f,bounds.size.y);body.transform.localScale*=scale;
        body.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
        Debug.Log("MODEL_BOUNDS "+body.name+" "+bounds+" scale "+scale);
    }
    static void SetupClips(VisualRig rig,string path,string prefix,string[] names)
    {
        var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
        Debug.Log("MODEL_CLIPS "+prefix+" "+string.Join(",",clips.Select(c=>c.name)));
        var slots=new AnimationClip[5];
        for(int i=0;i<names.Length;i++)
        {
            if(names[i]=="")continue;
            var clip=clips.FirstOrDefault(c=>c.name==names[i])??clips.FirstOrDefault(c=>c.name.EndsWith(names[i]));
            if(!clip)throw new Exception("Missing animation "+names[i]+" in "+path);
            var copy=UnityEngine.Object.Instantiate(clip);copy.name=prefix+"_"+names[i];copy.legacy=true;
            // glTF animation libraries can put the shared skeleton under another scene root.
            var transforms=rig.GetComponentsInChildren<Transform>(true);
            var bindings=AnimationUtility.GetCurveBindings(copy);
            foreach(var b in bindings)
            {
                if(rig.transform.Find(b.path))continue;
                string leaf=b.path.Split('/').Last();var bone=transforms.FirstOrDefault(t=>t.name==leaf);if(!bone)continue;
                string corrected=AnimationUtility.CalculateTransformPath(bone,rig.transform);
                if(corrected!=b.path){var curve=AnimationUtility.GetEditorCurve(copy,b);AnimationUtility.SetEditorCurve(copy,b,null);var dest=b;dest.path=corrected;AnimationUtility.SetEditorCurve(copy,dest,curve);}
            }
            Save(copy,R+"Clips/"+copy.name+".anim");slots[i]=copy;
        }
        rig.idle=slots[0];rig.walk=slots[1];rig.run=slots[2];rig.death=slots[3];rig.attack=slots[4];
    }
    static void SetLayer(GameObject root,int layer){foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;}
    static void AddWeather(Light sun)
    {
        var clouds=new Material(Shader.Find("Pinefall/Clouds"));var noise=new Texture2D(256,256,TextureFormat.RGB24,true){wrapMode=TextureWrapMode.Repeat};
        for(int y=0;y<256;y++)for(int x=0;x<256;x++)
        {
            float u=x/255f,v=y/255f;
            float a=Mathf.PerlinNoise(u*4+13,v*4+21),b=Mathf.PerlinNoise((u-1)*4+13,v*4+21),c=Mathf.PerlinNoise(u*4+13,(v-1)*4+21),d=Mathf.PerlinNoise((u-1)*4+13,(v-1)*4+21);
            float n=Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(c,d,u),v);noise.SetPixel(x,y,new Color(n,n,n));
        }
        noise.Apply(true);Save(noise,"Assets/Generated/CloudNoise.asset");clouds.SetTexture("_CloudTex",noise);Save(clouds,R+"Clouds.mat");
        var rain=new Material(Shader.Find("Pinefall/Rain"));Save(rain,R+"Rain.mat");var grade=new Material(Shader.Find("Hidden/Pinefall/Atmosphere"));Save(grade,R+"Atmosphere.mat");
        var system=new GameObject("Reserve atmosphere").AddComponent<ReserveWeather>();system.sun=sun;system.cloudMaterial=clouds;system.rainMaterial=rain;system.gradeMaterial=grade;
        var storm=new Material(Shader.Find("Pinefall/Storm"));Save(storm,R+"Storm.mat");new GameObject("Wildfront storm").AddComponent<StormView>().material=storm;
    }
    static Material Nature(string name,Color c,float wind){var m=new Material(Shader.Find("Pinefall/Nature")){name=name,color=c,enableInstancing=true};m.SetFloat("_Wind",wind);Save(m,"Assets/Generated/"+name+".mat");return m;}
    static Material MaterialAsset(string name,Color c)
    {
        var m=new Material(Shader.Find("Standard")){name=name,color=c};m.SetFloat("_Glossiness",.12f);
        string surface=name.Contains("timber")||name.Contains("cladding")?"WeatheredWood":name.Contains("roof")||name.Contains("Roof")||name=="Vehicle wear"?"WeatheredMetal":"";
        if(surface!=""){string path="Assets/Art/Terrain/"+surface;var normalImporter=AssetImporter.GetAtPath(path+"Normal.jpg") as TextureImporter;if(normalImporter){normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.SaveAndReimport();}m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path+".jpg");m.mainTextureScale=new Vector2(2,1);m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path+"Normal.jpg"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.5f);}
        Save(m,"Assets/Generated/"+name+".mat");return m;
    }
    static void Save(UnityEngine.Object obj,string path){AssetDatabase.CreateAsset(obj,path);}

    static Terrain CreateTerrain()
    {
        var data=new TerrainData{heightmapResolution=1025,size=new Vector3(ReserveWorld.Size,360,ReserveWorld.Size),alphamapResolution=512,baseMapResolution=1024};
        var h=new float[1025,1025];for(int z=0;z<1025;z++)for(int x=0;x<1025;x++)h[z,x]=ReserveWorld.Height((x/1024f-.5f)*ReserveWorld.Size,(z/1024f-.5f)*ReserveWorld.Size)/360;
        data.SetHeights(0,0,h);
        Color[] colors={new Color(.39f,.43f,.21f),new Color(.27f,.31f,.19f),new Color(.43f,.43f,.36f),new Color(.45f,.37f,.24f),new Color(.68f,.55f,.30f),new Color(.82f,.87f,.89f),new Color(.17f,.29f,.13f)};
        var layers=new TerrainLayer[7];
        for(int i=0;i<7;i++)
        {
            var tex=GroundTexture(colors[i],i);Save(tex,"Assets/Generated/Ground"+i+".asset");
            string real=i==0?"Meadow":i==1?"ForestFloor":"";
            Texture2D photograph=real==""?null:AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/"+real+".jpg");
            var layer=new TerrainLayer{diffuseTexture=photograph?photograph:tex,tileSize=new Vector2(photograph?8:i==2?9:5,photograph?8:i==2?9:5),metallic=0,smoothness=0};
            if(i==0)layer.diffuseRemapMax=new Vector4(.80f,1.12f,.72f,1);
            string normalPath=i==0?"Assets/Art/Terrain/MeadowNormal.jpg":i==1?"Assets/Art/Terrain/ForestNormal.jpg":"";
            if(normalPath!="")
            {
                var importer=AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if(importer){importer.textureType=TextureImporterType.NormalMap;importer.anisoLevel=4;importer.SaveAndReimport();layer.normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);layer.normalScale=.65f;}
            }
            Save(layer,"Assets/Generated/Layer"+i+".terrainlayer");layers[i]=layer;
        }
        data.terrainLayers=layers;
        var weights=new float[512,512,7];
        for(int z=0;z<512;z++)for(int x=0;x<512;x++)
        {
            float wx=(x/511f-.5f)*ReserveWorld.Size,wz=(z/511f-.5f)*ReserveWorld.Size;
            float f=Mathf.PerlinNoise(wx*.009f+40,wz*.009f+76);
            float path=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.8f,5.5f,Mathf.Abs(wx-ReserveWorld.PathX(wz))));if(wz<-190||wz>370)path=0;
            float slope=data.GetSteepness(x/511f,z/511f);float rock=Mathf.SmoothStep(0,1,Mathf.InverseLerp(19,38,slope))*.9f;
            float forest=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.7f,f))*.8f;
            weights[z,x,3]=path*.8f;weights[z,x,2]=rock*(1-weights[z,x,3]);weights[z,x,1]=forest*(1-weights[z,x,3]-weights[z,x,2]);weights[z,x,0]=1-weights[z,x,1]-weights[z,x,2]-weights[z,x,3];
            float valley=ReserveBiomes.Weight(wx,wz,0),desert=ReserveBiomes.Weight(wx,wz,1),ice=ReserveBiomes.Weight(wx,wz,2),mountain=ReserveBiomes.Weight(wx,wz,3),jungle=ReserveBiomes.Weight(wx,wz,4);
            for(int b=0;b<4;b++)weights[z,x,b]*=valley;
            float snowline=Mathf.InverseLerp(113,146,ReserveWorld.Height(wx,wz));
            weights[z,x,2]+=mountain*(1-snowline);weights[z,x,4]=desert;weights[z,x,5]=ice+mountain*snowline;weights[z,x,6]=jungle;
            float sum=0;for(int b=0;b<7;b++)sum+=weights[z,x,b];for(int b=0;b<7;b++)weights[z,x,b]/=Mathf.Max(.001f,sum);
        }
        data.SetAlphamaps(0,0,weights);
        var grass=GrassTexture();Save(grass,"Assets/Generated/Grass.asset");
        data.SetDetailResolution(1024,16);data.wavingGrassAmount=.15f;data.wavingGrassSpeed=.25f;data.wavingGrassStrength=.3f;data.wavingGrassTint=new Color(.65f,.70f,.39f);
        data.detailPrototypes=new[]{new DetailPrototype{prototypeTexture=grass,renderMode=DetailRenderMode.GrassBillboard,minWidth=.6f,maxWidth=1.1f,minHeight=.35f,maxHeight=.65f,healthyColor=new Color(.38f,.58f,.28f),dryColor=new Color(.59f,.65f,.34f),noiseSpread=.25f}};
        var detail=new int[1024,1024];
        for(int z=0;z<1024;z++)for(int x=0;x<1024;x++)
        {
            float wx=(x/1023f-.5f)*ReserveWorld.Size,wz=(z/1023f-.5f)*ReserveWorld.Size;
            int biome=ReserveBiomes.At(wx,wz);
            if((biome==0||biome==4||biome==5)&&!ReserveWorld.Clearing(wx,wz)&&data.GetSteepness(x/1023f,z/1023f)<24)detail[z,x]=Mathf.PerlinNoise(wx*.07f+21,wz*.07f+1)>.4f?5:2;
        }
        data.SetDetailLayer(0,0,0,detail);Save(data,"Assets/Generated/ReserveTerrain.asset");data=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/Generated/ReserveTerrain.asset");
        // Creating a TerrainData asset initializes its control textures. Paint after persistence.
        data.SetAlphamaps(0,0,weights);data.SyncTexture(TerrainData.AlphamapTextureName);EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
        Debug.Log("PINEFALL_GENERATED_SNOW_WEIGHT "+weights[422,146,5]);
        if(weights[422,146,5]<.2f)throw new Exception("Biome generation failed validation.");
        var g=Terrain.CreateTerrainGameObject(data);g.name="Pinefall terrain";g.transform.position=new Vector3(-ReserveWorld.Size/2,0,-ReserveWorld.Size/2);var terrain=g.GetComponent<Terrain>();terrain.heightmapPixelError=3;terrain.basemapDistance=2000;terrain.drawInstanced=false;terrain.detailObjectDistance=85;terrain.detailObjectDensity=.6f;
        var surface=new Material(Shader.Find("Pinefall/BiomeTerrain"));surface.SetTexture("_Meadow",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/Meadow.jpg"));surface.SetTexture("_Forest",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/ForestFloor.jpg"));surface.SetTexture("_MeadowNormal",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/MeadowNormal.jpg"));surface.SetTexture("_ForestNormal",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/ForestNormal.jpg"));foreach(string name in new[]{"Sand","Snow","Rock"}){surface.SetTexture("_"+name,AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/"+name+".jpg"));string path="Assets/Art/Terrain/"+name+"Normal.jpg";var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer){importer.textureType=TextureImporterType.NormalMap;importer.anisoLevel=4;importer.SaveAndReimport();}surface.SetTexture("_"+name+"Normal",AssetDatabase.LoadAssetAtPath<Texture2D>(path));}Save(surface,"Assets/Generated/BiomeTerrain.mat");terrain.materialTemplate=surface;
        return terrain;
    }
    static Texture2D GroundTexture(Color basis,int seed)
    {
        var t=new Texture2D(128,128,TextureFormat.RGB24,true){wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
        var rng=new System.Random(seed+61);
        for(int y=0;y<128;y++)for(int x=0;x<128;x++)
        {
            float n=Mathf.PerlinNoise(x*.13f+seed*9,y*.13f)*.22f+(float)rng.NextDouble()*.12f-.16f;
            if(seed==2)n+=(Mathf.Sin(x*.45f+Mathf.Sin(y*.3f))>.8f?-.07f:0);
            t.SetPixel(x,y,basis+new Color(n,n,n,0));
        }
        t.Apply(true);return t;
    }
    static Texture2D GrassTexture()
    {
        var t=new Texture2D(64,64,TextureFormat.RGBA32,true){wrapMode=TextureWrapMode.Clamp};var pixels=new Color[4096];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)
        {
            float a=0;for(int b=0;b<7;b++){float height=39+(b*17)%25;float center=8+b*8+Mathf.Pow(y/64f,2)*((b%2==0)?12:-12);float width=(1-y/height)*1.8f;if(y<height&&Mathf.Abs(x-center)<width)a=1;}
            pixels[x+y*64]=new Color(.78f,.83f,.56f,a);
        }
        t.SetPixels(pixels);t.Apply(true);return t;
    }
    static Texture2D NeedleTexture()
    {
        var t=new Texture2D(128,128,TextureFormat.RGBA32,true){wrapMode=TextureWrapMode.Clamp,anisoLevel=2};var pixels=new Color[128*128];
        Action<int,int,int,int,Color> line=(x0,y0,x1,y1,color)=>
        {
            int count=Mathf.Max(Mathf.Abs(x1-x0),Mathf.Abs(y1-y0));for(int k=0;k<=count;k++)
            {int x=Mathf.RoundToInt(Mathf.Lerp(x0,x1,k/(float)Mathf.Max(1,count))),y=Mathf.RoundToInt(Mathf.Lerp(y0,y1,k/(float)Mathf.Max(1,count)));if(x>0&&x<127&&y>0&&y<127){pixels[x+y*128]=color;if(x<126)pixels[x+1+y*128]=color;}}
        };
        line(64,0,64,125,new Color(.8f,.72f,.44f,1));
        for(int y=4;y<124;y+=4)
        {
            float width=(1-y/130f)*51+4;int rise=(int)(width*.62f);
            foreach(int sign in new[]{-1,1})
            {
                int endX=64+sign*(int)width,endY=Mathf.Min(125,y+rise);
                line(64,y,endX,endY,new Color(.83f,.96f,.73f,1));
                for(int q=2;q<width;q+=3)
                {
                    int px=64+sign*q,py=y+Mathf.RoundToInt(q*.62f);int len=Mathf.RoundToInt(8+(1-q/width)*8);
                    line(px,py,px+sign*len,py+9,new Color(.82f,.99f,.79f,1));
                    line(px,py,px-sign*3,py+len,new Color(.69f,.85f,.64f,1));
                }
            }
        }
        t.SetPixels(pixels);t.Apply(true);return t;
    }
    static GameObject Part(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,Transform parent=null,Vector3 angles=default(Vector3))
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;if(parent)g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.transform.localEulerAngles=angles;g.GetComponent<Renderer>().sharedMaterial=mat;g.isStatic=true;return g;
    }
    static void Outpost()
    {
        var camp=new GameObject("Ranger outpost").transform;camp.position=ReserveWorld.OnGround(-16,-166);
        Part("Deck",PrimitiveType.Cube,new Vector3(0,.25f,0),new Vector3(9,.5f,6),wood,camp);
        Part("Cabin back",PrimitiveType.Cube,new Vector3(0,2.1f,2.6f),new Vector3(8.5f,3.8f,.25f),wood,camp);
        Part("Cabin side",PrimitiveType.Cube,new Vector3(-4.15f,2.1f,0),new Vector3(.25f,3.8f,5.5f),wood,camp);
        Part("Cabin side",PrimitiveType.Cube,new Vector3(4.15f,2.1f,0),new Vector3(.25f,3.8f,5.5f),wood,camp);
        Part("Roof left",PrimitiveType.Cube,new Vector3(-2.25f,4.6f,0),new Vector3(4.9f,.22f,7.1f),roof,camp,new Vector3(0,0,18));
        Part("Roof right",PrimitiveType.Cube,new Vector3(2.25f,4.6f,0),new Vector3(4.9f,.22f,7.1f),roof,camp,new Vector3(0,0,-18));
        foreach(float x in new[]{-3.8f,3.8f})Part("Porch post",PrimitiveType.Cylinder,new Vector3(x,2.1f,-2.6f),new Vector3(.22f,2,.22f),wood,camp);
        Part("Workbench",PrimitiveType.Cube,new Vector3(0,1.15f,1.3f),new Vector3(3,.16f,1),wood,camp);
        foreach(float x in new[]{-1.2f,1.2f})Part("Bench leg",PrimitiveType.Cube,new Vector3(x,.65f,1.3f),new Vector3(.15f,1,.15f),metal,camp);
        var sign=Part("Welcome sign",PrimitiveType.Cube,new Vector3(0,3.7f,-2.82f),new Vector3(5,.7f,.12f),cream,camp);
        Label("Pinefall Preserve",new Vector3(-2.15f,3.92f,-2.895f),.12f,22,camp,new Color(.13f,.22f,.22f),Vector3.zero);
        var trail=new GameObject("Trail marker").transform;trail.position=ReserveWorld.OnGround(5,-149);
        Part("Trail post",PrimitiveType.Cube,new Vector3(0,.9f,0),new Vector3(.12f,1.8f,.12f),wood,trail);
        Part("Trail sign",PrimitiveType.Cube,new Vector3(0,1.75f,0),new Vector3(2.6f,.65f,.13f),wood,trail);
        Label("Meadow Mile",new Vector3(-1.1f,1.95f,-.08f),.09f,18,trail,new Color(.9f,.85f,.65f),Vector3.zero);
        for(int i=0;i<3;i++)
        {
            var p=ReserveWorld.OnGround(25+i*6,-116+i*9);
            Part("Range post",PrimitiveType.Cube,p+Vector3.up*.7f,new Vector3(.12f,1.4f,.12f),wood);
            var target=Part("Practice target",PrimitiveType.Cylinder,p+Vector3.up*1.55f,new Vector3(1.1f,.07f,1.1f),cream,null,new Vector3(90,0,0));target.AddComponent<RangeTarget>();
            Part("Target centre",PrimitiveType.Cylinder,p+new Vector3(0,1.55f,-.08f),new Vector3(.35f,.012f,.35f),metal,null,new Vector3(90,0,0));
        }
        // A low field fence frames the starting area without closing off the landscape.
        for(int i=0;i<8;i++)
        {
            var p=ReserveWorld.OnGround(-32+i*4,-188);Part("Fence post",PrimitiveType.Cube,p+Vector3.up*.65f,new Vector3(.15f,1.3f,.15f),wood);
            if(i<7)for(int r=0;r<2;r++)Part("Fence rail",PrimitiveType.Cube,p+new Vector3(2,.6f+r*.4f,0),new Vector3(4,.12f,.1f),wood);
        }
    }
    static void Label(string value,Vector3 pos,float scale,int size,Transform parent,Color color,Vector3 angles)
    {
        var g=new GameObject(value);g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localEulerAngles=angles;g.transform.localScale=Vector3.one*scale;
        var text=g.AddComponent<TextMesh>();text.text=value;text.font=sans;text.fontSize=64;text.characterSize=1;var fit=g.AddComponent<WorldSign>();fit.Width=scale<.1f?2.25f:scale<.13f?4.5f:6.2f;fit.Height=scale<.1f?.40f:.52f;text.color=color;
        g.GetComponent<Renderer>().sharedMaterial=sans.material;
    }
    static void Backdrop()
    {
        var material=MaterialAsset("Distant ridgelines",new Color(.37f,.45f,.43f));
        var v=new List<Vector3>();var t=new List<int>();
        for(int ring=0;ring<2;ring++)for(int i=0;i<=192;i++)
        {
            float a=i/192f*Mathf.PI*2;float r=ring==0?ReserveWorld.Size*.84f:ReserveWorld.Size*1.20f;float h=ring==0?42:180+Mathf.PerlinNoise(Mathf.Cos(a)*3+4,Mathf.Sin(a)*3+9)*220;
            v.Add(new Vector3(Mathf.Cos(a)*r,h,Mathf.Sin(a)*r));
        }
        for(int i=0;i<192;i++){t.AddRange(new[]{i,i+193,i+1,i+1,i+193,i+194});}
        var mesh=new Mesh{name="Mountain rim"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();Save(mesh,"Assets/Generated/MountainRim.asset");
        var go=new GameObject("Mountains beyond the reserve");go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
}
