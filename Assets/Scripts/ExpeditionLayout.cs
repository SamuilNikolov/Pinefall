using UnityEngine;
namespace Pinefall
{
    public sealed class District
    {
        public string name,description;public Vector2 center;public int biome,quality,vehicle;public bool minor;
        public District(string n,float x,float z,int b,int q,int v,string d,bool small=false){minor=small;name=n;center=new Vector2(x,z);biome=b;quality=q;vehicle=v;description=d;}
    }
    public static class ExpeditionLayout
    {
        public static readonly District[] Regions={
            new District("Ranger Refuge",-65,-185,0,1,0,"Medical supplies and modest weapons. Central access, with several teams likely to rotate past."),
            new District("Sunwash Sands",-510,-330,1,2,1,"Long desert sightlines. Rooms hide ammunition and occasional rare rifles; an old buggy sits outside."),
            new District("Windpump Works",-470,25,1,1,2,"Fuel, tools, and a service pickup. A useful departure point when the storm moves east."),
            new District("Frostwatch Flats",-335,490,2,3,3,"High-quality rifle odds and exposed approaches. The utility van needs repairs before it will run."),
            new District("Quarry Quarters",415,420,3,3,2,"Weapon cases and repair salvage among work sheds. Strong equipment odds, difficult terrain."),
            new District("Ridgeline Roost",150,400,3,2,0,"A visible landmark with precision weapons and limited vehicle fuel. Plan a route off the ridge."),
            new District("Fernwood Falls",520,-285,4,2,3,"Close cover, medicine, and fast-firing weapons. The abandoned van may be worth restoring."),
            new District("Cedar Crossing",285,15,0,1,1,"Reliable ammunition and tool crates. Good woodland cover, with roads into the valley and rainforest."),
            new District("Reedwater Reach",140,-560,5,2,4,"A marsh landing with medical supplies, fishing cabins, and a motor launch."),
            new District("Lakeside Landing",-125,120,0,2,4,"Shoreline supplies, wooded approaches, and a launch on the lake."),
            new District("Saltwind Strip",-660,-120,1,3,1,"An exposed desert strip with valuable equipment and fast transport."),
            new District("Northpass Nursery",-100,260,0,1,2,"Flat farm clearings and dependable supplies on the northern approach."),
            new District("Willow Warren",-290,-100,0,1,0,"A quiet woodland hamlet with food, spare parts and a sealed field cache.",true),
            new District("Prairie Patch",-140,-380,0,1,2,"Farm buildings and a survey cache beside the southern meadow road.",true),
            new District("Thistle Thicket",-100,-30,0,1,0,"A forest stop with medical supplies and short trails into the valley.",true),
            new District("Birch Borough",70,170,0,2,1,"A logging hamlet overlooking the river. Search its sealed cache.",true),
            new District("Copper Canyon",-660,-430,1,2,1,"Desert workshops with attachments, ammunition and a salvage cache.",true),
            new District("Dusty Dwellings",-530,-610,1,1,2,"A remote desert village with food, tools and rough road access.",true),
            new District("Cactus Court",-680,200,1,2,0,"An exposed roadside trading stop. Long sightlines reward careful approaches.",true),
            new District("Glacier Grove",-520,640,2,2,3,"A snowbound relay hamlet with medical stock and rifle supplies.",true),
            new District("Polar Post",-580,390,2,1,2,"Cold-weather cabins and a protected supply cache on the western road.",true),
            new District("Shiver Station",-100,650,2,2,3,"Abandoned utility housing near the ice desert boundary.",true),
            new District("Boulder Borough",620,620,3,2,2,"Rocky work cabins and a cache near high mountain passes.",true),
            new District("Granite Gulch",650,300,3,2,0,"A small quarry settlement with precision attachments and repair materials.",true),
            new District("Summit Shacks",270,650,3,3,1,"A remote mountain refuge with valuable salvage and exposed ridges.",true),
            new District("Canopy Crossing",680,-180,4,2,3,"Dense jungle housing and close approaches to a field cache.",true),
            new District("Mossy Market",540,-510,4,2,0,"An overgrown market camp with food and fast-firing weapon odds.",true),
            new District("Tangle Town",340,-350,4,1,2,"Forest huts and a supply stop between jungle and wetlands.",true),
            new District("Heron Haven",430,-660,5,2,0,"Marsh-edge cabins, reeds and a sealed medical cache.",true),
            new District("Sedge Settlement",40,-650,5,1,2,"A southern wetland village with hunting supplies and road access.",true),
            new District("Amberwood Acres",200,170,0,2,5,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Maple Mill",300,240,0,2,1,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Oak Orchard",50,380,0,2,2,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Juniper Junction",-330,110,0,2,5,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Aspen Annex",-260,270,0,2,0,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Clover Commons",-300,-460,0,2,1,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Hayfield Hollow",-360,-240,0,2,5,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Brookside Bend",140,-200,0,2,3,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Canyon Camp",-680,-670,1,2,0,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Dune Depot",-650,20,1,2,5,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Sandy Springs",-380,-660,1,2,2,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Frozen Ford",-680,660,2,2,3,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Snowdrift Shelters",-560,230,2,2,5,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Tundra Terrace",80,600,2,2,1,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Pinnacle Point",650,60,3,2,2,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Craggy Crossing",400,140,3,2,5,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Bamboo Borough",650,-650,4,2,0,"An abandoned village with stocked houses, transport and a sealed supply cache.",true),
            new District("Mangrove Market",360,-80,5,2,1,"An abandoned village with stocked houses, transport and a sealed supply cache.",true)
        };
        static ExpeditionLayout()
        {
            for(int i=0;i<Regions.Length;i++){var region=Regions[i];region.center*=ReserveWorld.LayoutScale;if(region.minor){Vector2 origin=region.center;for(int attempt=0;attempt<160;attempt++){float radius=attempt==0?0:30+attempt/12*25,angle=attempt*2.39996f;var candidate=origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;candidate.x=Mathf.Clamp(candidate.x,-ReserveWorld.Edge+160,ReserveWorld.Edge-160);candidate.y=Mathf.Clamp(candidate.y,-ReserveWorld.Edge+160,ReserveWorld.Edge-160);bool blocked=false;for(int k=0;k<12;k++){float a=k/12f*Mathf.PI*2;if(ReserveWater.HasSurface(candidate.x+Mathf.Cos(a)*125,candidate.y+Mathf.Sin(a)*125)){blocked=true;break;}}for(int k=0;k<i;k++)if((Regions[k].center-candidate).sqrMagnitude<210*210)blocked=true;if(blocked)continue;region.center=candidate;break;}region.biome=ReserveBiomes.At(region.center.x,region.center.y);}}
            Regions[30].minor=false;Regions[41].minor=false;Roads=BuildRoads();ServiceStops=ComputeMobilityStops();
        }
        public static readonly int[,] Roads;
        static int[,] BuildRoads()
        {
            var links=new System.Collections.Generic.List<Vector2Int>{new Vector2Int(0,1),new Vector2Int(1,2),new Vector2Int(2,3),new Vector2Int(3,5),new Vector2Int(5,4),new Vector2Int(4,7),new Vector2Int(7,6),new Vector2Int(6,0),new Vector2Int(0,7),new Vector2Int(0,2),new Vector2Int(0,5),new Vector2Int(0,9),new Vector2Int(9,11),new Vector2Int(8,6),new Vector2Int(0,8),new Vector2Int(1,10),new Vector2Int(10,2)};
            for(int i=12;i<Regions.Length;i++){int nearest=0,second=1;float firstDistance=float.MaxValue,secondDistance=float.MaxValue;for(int j=0;j<i;j++){float d=(Regions[i].center-Regions[j].center).sqrMagnitude;if(d<firstDistance){second=nearest;secondDistance=firstDistance;nearest=j;firstDistance=d;}else if(d<secondDistance){second=j;secondDistance=d;}}links.Add(new Vector2Int(i,nearest));if(i%2==0)links.Add(new Vector2Int(i,second));}var result=new int[links.Count,2];for(int i=0;i<links.Count;i++){result[i,0]=links[i].x;result[i,1]=links[i].y;}return result;
        }
        public static readonly Vector2[] ServiceStops;public static Vector2[] MobilityStops(){return ServiceStops;}
        static Vector2[] ComputeMobilityStops(){var stops=new System.Collections.Generic.List<Vector2>();for(int road=0;road<Roads.GetLength(0);road+=2){Vector2 a=Regions[Roads[road,0]].center,c=Regions[Roads[road,1]].center,side=new Vector2(-(c-a).y,(c-a).x).normalized;var p=Vector2.Lerp(a,c,.5f)+side*10;if(!ReserveWater.HasSurface(p.x,p.y)&&!ReserveWater.HasSurface(p.x+side.x*8,p.y+side.y*8))stops.Add(p);}return stops.ToArray();}
        public static Vector3[] BoatStops(){var stops=new System.Collections.Generic.List<Vector3>();foreach(var lake in ReserveWater.Lakes)stops.Add(new Vector3(lake.center.x+lake.radius.x*.6f,lake.level+.23f,lake.center.y));foreach(float z in new[]{-400f,-1100f,-1800f})stops.Add(new Vector3(ReserveWater.RiverX(z),ReserveWater.Level(ReserveWater.RiverX(z),z)+.23f,z));return stops.ToArray();}
        public static readonly Vector2[] HouseOffsets={new Vector2(-14,3),new Vector2(14,8),new Vector2(-14,40),new Vector2(-38,24),new Vector2(38,30),new Vector2(14,58),new Vector2(-62,-5),new Vector2(62,3),new Vector2(-62,50),new Vector2(62,55),new Vector2(-30,75),new Vector2(30,80)};
        public static int HouseCount(District district){return district.minor?4:12;}
        public static Vector2 VehiclePoint(int region){return Regions[region].center+new Vector2(17,-18);}
        public static Vector3 Spawn(int region,int member)
        {var p=Regions[Mathf.Clamp(region,0,Regions.Length-1)].center+new Vector2((member%2)*3-1.5f,-29-(member/2)*3);return ReserveWorld.OnGround(p.x,p.y,.4f);}
        public static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b,out float t)
        {var ab=b-a;t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(.01f,ab.sqrMagnitude));return Vector2.Distance(p,a+ab*t);}
        public static float RoadDistance(float x,float z)
        {float best=9999;for(int i=0;i<Roads.GetLength(0);i++){float t;best=Mathf.Min(best,SegmentDistance(new Vector2(x,z),Regions[Roads[i,0]].center,Regions[Roads[i,1]].center,out t));}return best;}
        public static int Nearest(Vector3 p)
        {int best=0;float distance=float.MaxValue;for(int i=0;i<Regions.Length;i++){float d=(new Vector2(p.x,p.z)-Regions[i].center).sqrMagnitude;if(d<distance){best=i;distance=d;}}return best;}
        public static bool IsClearing(float x,float z)
        {foreach(var stop in ServiceStops)if(Vector2.Distance(new Vector2(x,z),stop)<14)return true;foreach(var p in Regions)if(Vector2.Distance(new Vector2(x,z),p.center)<(p.minor?70:110))return true;return RoadDistance(x,z)<5;}
    }
    public sealed class StartingKit
    {
        public string name,description;public int cost,primary,rarity,spareMags,materials,medkits,fuel,armor;
        public static readonly StartingKit[] All={
            new StartingKit{name="Bare essentials",description="Service pistol, one spare magazine. Save almost everything for selection priority.",cost=0,primary=3,spareMags=1},
            new StartingKit{name="Light scout",description="Kestrel SMG and sidearm. Light ammunition; you will need to scavenge early.",cost=3,primary=4,spareMags=1},
            new StartingKit{name="Trail ranger",description="Trail Scout carbine, sidearm, two spare magazines, and one medkit.",cost=5,primary=1,spareMags=2,medkits=1},
            new StartingKit{name="Brush hunter",description="Brushmaster shotgun, sidearm, two spare magazines, and one medkit.",cost=5,primary=2,spareMags=2,medkits=1},
            new StartingKit{name="Field mechanic",description="Carbine, sidearm, four repair materials, and one fuel can. Less spare ammunition.",cost=6,primary=1,spareMags=1,materials=4,fuel=1},
            new StartingKit{name="Ridgeline marksman",description="Scoped rifle, sidearm, three spare magazines, and one medkit.",cost=7,primary=0,spareMags=3,medkits=1},
            new StartingKit{name="Prepared expedition",description="Uncommon Longwatch rifle, sidearm, three spare magazines, two medkits, tools, fuel, and a light vest.",cost=9,primary=5,rarity=1,spareMags=3,materials=2,medkits=2,fuel=1,armor=1},
            new StartingKit{name="Armored hunter",description="Shotgun, sidearm, two spare magazines, a medkit, and a field vest. Less priority for choosing a district.",cost=8,primary=2,spareMags=2,medkits=1,armor=2}
        };
    }
}
