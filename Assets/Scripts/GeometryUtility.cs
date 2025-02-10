using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeometryUtility
{

    public static Vector2 CentreCercleCirconscrit(Triangle t1)
    {
        Vector2 a = t1.sommets[0].p;
        Vector2 b = t1.sommets[1].p;
        Vector2 c = t1.sommets[2].p;

        float delta = ((a.x * b.y) - (b.x * a.y)) - ((a.x * c.y) - (c.x * a.y)) + ((b.x * c.y) - (c.x * b.y));

        float x = (
            (a.x * a.x + a.y * a.y) * (b.y - c.y) +
            (b.x * b.x + b.y * b.y) * (c.y - a.y) +
            (c.x * c.x + c.y * c.y) * (a.y - b.y)
        ) / delta;

        float y = (
            (a.x * a.x + a.y * a.y) * (c.x - b.x) +
            (b.x * b.x + b.y * b.y) * (a.x - c.x) +
            (c.x * c.x + c.y * c.y) * (b.x - a.x)
        ) / delta;  
        
        return new Vector2(x, y);
    }
    
}
