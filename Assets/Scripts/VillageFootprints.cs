using System.Collections.Generic;
using UnityEngine;
namespace Pinefall
{
    public static class VillageFootprints
    {
        static readonly Dictionary<District,Vector2[]> sites=new Dictionary<District,Vector2[]>();
        static bool Dry(Vector2 at){foreach(var corner in new[]{Vector2.zero,new Vector2(-5,-5),new Vector2(5,-5),new Vector2(-5,5),new Vector2(5,5)})if(ReserveWater.HasSurface(at.x+corner.x,at.y+corner.y))return false;return true;}
        public static Vector2 Offset(District district,int house)
        {
            Vector2[] points;if(!sites.TryGetValue(district,out points))
            {
                points=new Vector2[ExpeditionLayout.HouseCount(district)];for(int i=0;i<points.Length;i++)
                {
                    Vector2 desired=ExpeditionLayout.HouseOffsets[i],chosen=desired;
                    for(int attempt=0;attempt<100;attempt++)
                    {
                        if(attempt<4){float angle=attempt*Mathf.PI*.5f;chosen=new Vector2(desired.x*Mathf.Cos(angle)-desired.y*Mathf.Sin(angle),desired.x*Mathf.Sin(angle)+desired.y*Mathf.Cos(angle));}
                        else{float angle=attempt*2.39996f,radius=25+((attempt-4)/16)*14;chosen=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;}
                        bool free=Dry(district.center+chosen);for(int prior=0;prior<i;prior++)if((points[prior]-chosen).sqrMagnitude<13*13)free=false;if(free)break;
                    }
                    points[i]=chosen;
                }
                sites.Add(district,points);
            }
            return points[Mathf.Clamp(house,0,points.Length-1)];
        }
    }
}
