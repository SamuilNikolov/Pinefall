using System;
using UnityEngine;
namespace Pinefall
{
    public static class GameBindings
    {
        public static readonly string[] Actions={"Forward","Backward","Left","Right","Sprint","Crouch","Jump","Fire","Aim","Reload","Interact","Inventory","Map","Camera","Lobby","Roster","Food","Heal","Plate","Repair","Refuel","Vehicle upgrade","Brake","Talk","Weapon 1","Weapon 2","Weapon 3","Flashlight","Survey","Build","Pickaxe","Edit build","Build Wall","Build Floor","Build Stairs","Build Pyramid","Next spectator","Previous spectator"};
        static readonly KeyCode[] Defaults={KeyCode.W,KeyCode.S,KeyCode.A,KeyCode.D,KeyCode.LeftShift,KeyCode.LeftControl,KeyCode.Space,KeyCode.Mouse0,KeyCode.Mouse1,KeyCode.R,KeyCode.E,KeyCode.I,KeyCode.M,KeyCode.V,KeyCode.L,KeyCode.Tab,KeyCode.Alpha4,KeyCode.Alpha5,KeyCode.B,KeyCode.G,KeyCode.F,KeyCode.T,KeyCode.Space,KeyCode.N,KeyCode.Alpha1,KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.X,KeyCode.Q,KeyCode.Z,KeyCode.BackQuote,KeyCode.H,KeyCode.F1,KeyCode.F2,KeyCode.F3,KeyCode.F4,KeyCode.Mouse0,KeyCode.Mouse1};
        public static KeyCode Key(string action){int i=Array.IndexOf(Actions,action);return i<0?KeyCode.None:(KeyCode)PlayerPrefs.GetInt("bind."+action,(int)Defaults[i]);}
        public static string Label(string action){return Key(action).ToString().Replace("Alpha","").Replace("LeftShift","Shift").Replace("LeftControl","Ctrl").Replace("LeftAlt","Alt").Replace("Mouse0","LMB").Replace("Mouse1","RMB");}
        public static bool Held(string action){return Input.GetKey(Key(action));}public static bool Down(string action){return Input.GetKeyDown(Key(action));}
        public static float Horizontal{get{return (Held("Right")?1:0)-(Held("Left")?1:0);}}public static float Vertical{get{return (Held("Forward")?1:0)-(Held("Backward")?1:0);}}
        public static void Bind(string action,KeyCode key){if(key==KeyCode.None||key==KeyCode.Escape)return;PlayerPrefs.SetInt("bind."+action,(int)key);PlayerPrefs.Save();}
        public static void Reset(){foreach(var a in Actions)PlayerPrefs.DeleteKey("bind."+a);PlayerPrefs.Save();}
    }
}
