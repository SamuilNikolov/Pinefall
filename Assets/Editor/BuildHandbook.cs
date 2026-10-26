using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Pinefall;
public static partial class BuildReserve
{
    [Serializable]class HandbookData{public string version="0.7.1";public int roadLinks,serviceStops,boats,vehicles;public PlaceData[] locations;public GunData[] guns;public ModData[] mods;}
    [Serializable]class PlaceData{public string name,biome,description;public float x,z;public bool hamlet;}
    [Serializable]class GunData{public string name,ammo,description;public int magazine,damage,pellets;public float reload,range;}
    [Serializable]class ModData{public string name,slot,effect;}
    static void PrepareAtlasTexture(){const string path="Assets/Resources/DetailedAtlas.png";if(!File.Exists(path))return;RefineAtlasWater(path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)return;importer.maxTextureSize=4096;importer.mipmapEnabled=true;importer.sRGBTexture=true;importer.isReadable=false;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();}
    static void ExportHandbookData(){var data=new HandbookData{roadLinks=ExpeditionLayout.Roads.GetLength(0),serviceStops=ExpeditionLayout.MobilityStops().Length,boats=ExpeditionLayout.Regions.Count(r=>r.vehicle==4)+ExpeditionLayout.BoatStops().Length,vehicles=ExpeditionLayout.Regions.Length+ExpeditionLayout.MobilityStops().Length+ExpeditionLayout.BoatStops().Length,locations=ExpeditionLayout.Regions.Select(r=>new PlaceData{name=r.name,biome=ReserveBiomes.Label(r.center.x,r.center.y),description=r.description,x=r.center.x,z=r.center.y,hamlet=r.minor}).ToArray(),guns=WeaponData.All.Select(g=>new GunData{name=g.name,ammo=WeaponData.AmmoNames[g.ammoType],description=g.description,magazine=g.magazine,damage=g.playerDamage,pellets=g.pellets,reload=g.reload,range=g.range}).ToArray(),mods=Enumerable.Range(1,9).Select(i=>new ModData{name=WeaponMods.Names[i],slot=WeaponMods.SlotName(WeaponMods.Slot(i)),effect=WeaponMods.Effects[i]}).ToArray()};File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../handbook-data.json")),JsonUtility.ToJson(data,true));}
}
