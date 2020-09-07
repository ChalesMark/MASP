using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

static class Converter
{
    public static Position UnityVector3ToMASPPosition (Vector3 vector3)
    {
        return new Position(vector3.x, vector3.z);
    }

    public static Position UnityVector2ToMASPPosition(Vector2 vector2)
    {
        return new Position(vector2.x, vector2.y);
    }

    public static Vector3 MASPPositionToUnityVector3 (Position position)
    {
        return new Vector3(position.X,0,position.Y);
    }
    public static List<Vector2> ToVector2Path(List<Position> msapPath)
    {        
        List<Vector2> path = new List<Vector2>();
        foreach (var t in msapPath)
        {
            
            path.Add(new Vector2(t.X,t.Y));
        }

        return path;
    }

    public static Vector3 EightWayDigital(Vector3 vector3)
    {
        return new Vector3(
            (vector3.x != 0 ? (vector3.x > 0 ? 1 : -1) : 0),
            (vector3.y != 0 ? (vector3.y > 0 ? 1 : -1) : 0),
            (vector3.z != 0 ? (vector3.z > 0 ? 1 : -1) : 0)
            );
    }

    public static Vector3 ToVector3(this Vector2 vector)
    {
        return new Vector3(vector.x,0,vector.y);
    }

    public static Vector2 ToVector2(this Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }

    public static void PNGMapToTIAM(Texture2D texture2D)
    {
        List<string> text = new List<string>();

        Color floorColor = new Color(0, 0, 0);
        Color wallColor = new Color(0, 1, 0);
        Color playerColor = new Color(0, 0, 1);
        Color monsterColor = new Color(1, 0, 0);
        Color goalColor = new Color(1, 1, 0);

        Color graveFloorColor = new Color(1, 0, 1);
        Color clutterColor = new Color(0, 1, 1);

        for (int x = 0; x < texture2D.width; x++)
            for (int y = 0; y < texture2D.height; y++)
            {
                if (texture2D.GetPixel(x, y) == floorColor)
                    text.Add("f,"+x+" "+y+",0");
                else if (texture2D.GetPixel(x, y) == wallColor)
                    text.Add("w," + x + " " + y + ",0");
                else if (texture2D.GetPixel(x, y) == playerColor)
                    text.Add("p," + x + " " + y + ",0");
                else if (texture2D.GetPixel(x, y) == monsterColor)
                    text.Add("m," + x + " " + y + ",0");
                else if (texture2D.GetPixel(x, y) == goalColor)
                    text.Add("g," + x + " " + y + ",0");
                else if (texture2D.GetPixel(x, y) == graveFloorColor)
                    text.Add("gf," + x + " " + y + ",0");
                else if (texture2D.GetPixel(x, y) == clutterColor)
                    text.Add("c," + x + " " + y + ",0");
            }

        File.WriteAllLines(@"C:\Users\colli\Desktop\convertedMap.txt", text);
    }
}

