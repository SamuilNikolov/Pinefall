using UnityEngine;
namespace Pinefall
{
    public static class ReserveBiomes
    {
        public static readonly string[] Names={"Lush valley","Dune basin","Ice desert","High Rockies","Rainforest","Reedwater marsh"};
        static readonly Vector2[] anchors={new Vector2(-20,-120),new Vector2(-530,-270),new Vector2(-330,540),new Vector2(430,470),new Vector2(530,-260),new Vector2(260,-610)};
        public static bool Autumn(float x,float z){return Vector2.Distance(new Vector2(x,z),new Vector2(1000,850))<720;}
        public static string Label(float x,float z){return Autumn(x,z)?"Amberwood floodplain":Names[At(x,z)];}
        static Vector2 Point(float x,float z){x/=ReserveWorld.LayoutScale;z/=ReserveWorld.LayoutScale;x+=Mathf.PerlinNoise(x*.004f+31,z*.004f+7)*65-32;z+=Mathf.PerlinNoise(x*.004f+61,z*.004f+4)*65-32;return new Vector2(x,z);}
        static float W(Vector2 p,int i){return Mathf.Exp(-(p-anchors[i]).sqrMagnitude/65000);}
        public static void Weights(float x,float z,out float a,out float b,out float c,out float d,out float e,out float f)
        {var p=Point(x,z);a=W(p,0);b=W(p,1);c=W(p,2);d=W(p,3);e=W(p,4);f=W(p,5);float sum=Mathf.Max(.000001f,a+b+c+d+e+f);a/=sum;b/=sum;c/=sum;d/=sum;e/=sum;f/=sum;}
        public static float Weight(float x,float z,int biome){float a,b,c,d,e,f;Weights(x,z,out a,out b,out c,out d,out e,out f);return biome==0?a:biome==1?b:biome==2?c:biome==3?d:biome==4?e:f;}
        public static int At(float x,float z){Vector2 p=Point(x,z);int best=0;float min=float.MaxValue;for(int i=0;i<anchors.Length;i++){float d=(p-anchors[i]).sqrMagnitude;if(d<min){best=i;min=d;}}return best;}
    }
}
