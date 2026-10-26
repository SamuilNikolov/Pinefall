using UnityEngine;
namespace Pinefall
{
    public enum WeaponClass { Pistol, Shotgun, SMG, AssaultRifle, Sniper, MarksmanRifle, SupportGun, Launcher }
    public enum WeaponFireMode { SemiAutomatic, PumpAction, BoltAction, Automatic, SingleShot }
    public sealed class WeaponData
    {
        public string name,cartridge,description;public int magazine,pellets,damage,playerDamage,ammoType;public WeaponClass category;public WeaponFireMode mode;public bool automatic{get{return mode==WeaponFireMode.Automatic;}}public float interval,reload,spread,range,zoom;
        public static readonly string[] Rarities={"Common","Uncommon","Rare","Epic","Legendary"};
        public static readonly Color[] RarityColors={new Color(.71f,.76f,.77f),new Color(.42f,.76f,.54f),new Color(.38f,.64f,.94f),new Color(.73f,.48f,.90f),new Color(.98f,.71f,.28f)};
        public static readonly string[] AmmoNames={".308",".223","12-gauge","9 mm","Rockets"};
        public static readonly WeaponData[] All={
            new WeaponData{name="Ridgeline",category=WeaponClass.Sniper,mode=WeaponFireMode.BoltAction,cartridge=".308 bolt-action",description="Accurate long-range rifle. Slow follow-up shots; powerful against large wildlife.",magazine=5,pellets=1,damage=110,playerDamage=80,ammoType=0,interval=1.15f,reload=2.5f,spread=.001f,range=650,zoom=24},
            new WeaponData{name="Trail Scout",category=WeaponClass.AssaultRifle,mode=WeaponFireMode.Automatic,cartridge=".223 assault rifle",description="Controlled automatic rifle for medium-range engagements.",magazine=15,pellets=1,damage=48,playerDamage=27,ammoType=1,interval=.14f,reload=1.9f,spread=.003f,range=350,zoom=48},
            new WeaponData{name="Brushmaster",category=WeaponClass.Shotgun,mode=WeaponFireMode.PumpAction,cartridge="12-gauge pump",description="Eight-pellet shotgun. Strong in close cover; limited reach.",magazine=6,pellets=8,damage=19,playerDamage=12,ammoType=2,interval=.9f,reload=2.8f,spread=.035f,range=85,zoom=57},
            new WeaponData{name="Service Nine",category=WeaponClass.Pistol,mode=WeaponFireMode.SemiAutomatic,cartridge="9 mm sidearm",description="Compact, dependable pistol. A modest start that leaves more budget for spawn priority.",magazine=12,pellets=1,damage=30,playerDamage=23,ammoType=3,interval=.3f,reload=1.4f,spread=.007f,range=110,zoom=58},
            new WeaponData{name="Kestrel",category=WeaponClass.SMG,mode=WeaponFireMode.Automatic,cartridge="9 mm SMG",description="Fast automatic fire for interiors and jungle cover. Shares ammunition with the pistol.",magazine=28,pellets=1,damage=25,playerDamage=18,ammoType=3,interval=.095f,reload=1.8f,spread=.012f,range=125,zoom=53},
            new WeaponData{name="Longwatch",category=WeaponClass.MarksmanRifle,mode=WeaponFireMode.SemiAutomatic,cartridge=".308 marksman",description="Scoped semi-automatic rifle. Flexible at range, but heavy on scarce ammunition.",magazine=10,pellets=1,damage=73,playerDamage=52,ammoType=0,interval=.45f,reload=2.4f,spread=.0025f,range=550,zoom=32},
            new WeaponData{name="Sawmill",category=WeaponClass.SupportGun,mode=WeaponFireMode.Automatic,cartridge=".223 support gun",description="Heavy automatic weapon. A large magazine trades quick handling for sustained fire.",magazine=40,pellets=1,damage=40,playerDamage=27,ammoType=1,interval=.16f,reload=3.3f,spread=.011f,range=330,zoom=49},
            new WeaponData{name="Stormbreak RPG",category=WeaponClass.Launcher,mode=WeaponFireMode.SingleShot,cartridge="Rocket launcher",description="A traveling explosive rocket. One shot, slow reload, blast falloff and cover protection. Carries dedicated rockets.",magazine=1,pellets=1,damage=220,playerDamage=110,ammoType=4,interval=1.1f,reload=3.8f,spread=0,range=280,zoom=62}
        };
        public string ClassLabel{get{return category==WeaponClass.AssaultRifle?"Assault rifle":category==WeaponClass.MarksmanRifle?"Marksman rifle":category==WeaponClass.SupportGun?"Support gun":category.ToString();}}
        public string ModeLabel{get{return mode==WeaponFireMode.BoltAction?"Bolt-action":mode==WeaponFireMode.PumpAction?"Pump-action":mode==WeaponFireMode.SemiAutomatic?"Semi-auto":mode==WeaponFireMode.SingleShot?"Single shot":"Automatic";}}
        public float RangeMultiplier(float distance)
        {
            float near=category==WeaponClass.Shotgun?12:category==WeaponClass.SMG?28:category==WeaponClass.Pistol?25:category==WeaponClass.Sniper?170:category==WeaponClass.MarksmanRifle?110:70;
            float far=category==WeaponClass.Shotgun?65:category==WeaponClass.SMG?110:category==WeaponClass.Pistol?95:category==WeaponClass.Sniper?650:category==WeaponClass.MarksmanRifle?550:300;
            float floor=category==WeaponClass.Shotgun?.22f:category==WeaponClass.SMG?.42f:category==WeaponClass.Pistol?.55f:category==WeaponClass.Sniper?.85f:.65f;
            return Mathf.Lerp(1,floor,Mathf.InverseLerp(near,far,distance));
        }
        static readonly WeaponData Unarmed=new WeaponData{name="Empty hands",cartridge="Find a weapon or pick up a dropped gun",magazine=0,ammoType=3,zoom=64,interval=1};
        public static WeaponData Get(int index){return index<0?Unarmed:All[Mathf.Clamp(index,0,All.Length-1)];}
        public static Color RarityColor(int index){return RarityColors[Mathf.Clamp(index,0,4)];}
        public static float RarityPower(int index){return 1+.065f*Mathf.Clamp(index,0,4);}
    }
}
