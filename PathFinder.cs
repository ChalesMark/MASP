using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

/*
 * MASP: Mark's A Star Pathfinding (I know, its a dump name) 
 * Mark Colling
 *
 * This is meant to act as a standard pathfinding class to be used in multiple projects
*/

#region non Pathfinder stuff
// MASPVector2
// I'm using a custom vector2 so that it can be used in non unity projects
public class Position
{
    public float X, Y;

    public Position(float X, float Y)
    {
        this.X = X;
        this.Y = Y;
    }
    
    public static Position Zero()
    {
        return new Position(0, 0);
    }

    // Checks if both positions are the same
    public bool Equals (Position other)
    {
        return (this.X == other.X && this.Y == other.Y ? true : false);
    }

    // returns the distance between two positions
    public float Distance (Position other) {
        return Math.Abs(this.X - other.X) + Math.Abs(this.Y - other.Y);
    }

    // Returns a list of neighboor points
    public List<Tuple<Position,bool>> GetNeighboors()
    {
        List<Tuple<Position, bool>> temp = new List<Tuple<Position, bool>>();
        temp.Add(new Tuple<Position, bool>(new Position(X, Y+1),false));
        temp.Add(new Tuple<Position, bool>(new Position(X+1, Y), false));
        temp.Add(new Tuple<Position, bool>(new Position(X, Y-1), false));
        temp.Add(new Tuple<Position, bool>(new Position(X-1, Y), false));

        temp.Add(new Tuple<Position, bool>(new Position(X-1, Y-1), true));
        temp.Add(new Tuple<Position, bool>(new Position(X+1, Y+1), true));
        temp.Add(new Tuple<Position, bool>(new Position(X-1, Y+1), true));
        temp.Add(new Tuple<Position, bool>(new Position(X+1, Y-1), true));


        return temp;
    }

    public List<Position> GetNeighboorsNoCorners()
    {
        List<Position> temp = new List<Position>();
        temp.Add(new Position(X + 1, Y));
        temp.Add(new Position(X, Y + 1));
        temp.Add(new Position(X - 1, Y));
        temp.Add(new Position(X, Y - 1));

        return temp;
    }

    public override string ToString()
    {
        return X + " " + Y;
    }
}

// NeighboorData
// This class stores points and if it is a diagonal point
public class NeighboorData
{
    public Position position;
    public bool isDiagonal;

    public NeighboorData(Position position,bool isDiagonal)
    {
        this.position = position;
        this.isDiagonal = isDiagonal;
    }
}

// NodeData
// This stores a points parent and its score
public class NodeData
{
    public Position parent;
    public int score;

    public NodeData(Position parent, int score)
    {
        this.parent = parent;
        this.score = score;
    }
}

#endregion

// This is the actual pathfinding class
public class PathFinder
{
    private Dictionary<Position,int> map;

    Random ran;

    // Constructor
    // The only parameter is the map in which the pathfinding will be done
    public PathFinder(Dictionary<Position,int> map)
    {
        ran = new Random();
        this.map = new Dictionary<Position, int>(new PositionEqualityComparer());

        foreach (var m in map)
            if (m.Value != -1)
                this.map.Add(m.Key,m.Value);
    }

    public bool IsSpaceWalkable (Position position)
    {
        return map.ContainsKey(position);
    }

    public Dictionary<Position, int> GetMap()
    {
        return map;
    }

    public Position GetRandomWalkableSpace()
    {
        return map.ElementAt(ran.Next(0, map.Count - 1)).Key;
    }

    public int GetScore(Position current, Position goal, int parentCost, bool isDiagonal)
    {
        return parentCost - (int)current.Distance(goal) - (isDiagonal?1:0);
    }

    public List<Position> FindPath(Position start, Position goal)
    {
        Position startPosition = new Position((float)Math.Round(start.X), (float)Math.Round(start.Y));
        Position goalPosition = new Position((float)Math.Round(goal.X), (float)Math.Round(goal.Y));

        Dictionary<Position, NodeData> searching = new Dictionary<Position, NodeData>(new PositionEqualityComparer());
        Dictionary<Position, NodeData> alreadySearched = new Dictionary<Position, NodeData>(new PositionEqualityComparer());

        List<Position> path = new List<Position>();

        searching.Add(startPosition,new NodeData(null, 0));
        KeyValuePair<Position,NodeData> topSearch;
        int safety = 0;
        do
        {
            topSearch = FindLargest(searching);            
            searching.Remove(topSearch.Key);

            // Get node neighboors
            foreach (var neighboor in topSearch.Key.GetNeighboors())
            {
                // Check if neighboor is goal
                if (neighboor.Equals(goalPosition))
                {
                    alreadySearched.Add(neighboor.Item1,new NodeData(topSearch.Key,0));
                    break;
                }
                // Check if the map contains the neighboor
                else if (map.ContainsKey(neighboor.Item1))
                {
                    int score = GetScore(neighboor.Item1, goalPosition, topSearch.Value.score,neighboor.Item2);

                    if (!alreadySearched.ContainsKey(neighboor.Item1))
                    {
                        if (!searching.ContainsKey(neighboor.Item1))
                            searching.Add(neighboor.Item1, new NodeData(topSearch.Key, score));
                    }
                    else
                    {
                        if (alreadySearched[neighboor.Item1].score < score)
                        {
                            searching.Add(neighboor.Item1, new NodeData(topSearch.Key, score));
                            alreadySearched.Remove(neighboor.Item1);
                        }
                    }                   
                }
            }

            if (!alreadySearched.ContainsKey(topSearch.Key))
                alreadySearched.Add(topSearch.Key, topSearch.Value);

            safety++;

        } while (!alreadySearched.ContainsKey(goalPosition) && searching.Count > 0 && safety < 2000);
        
        path.Add(goalPosition);
        Position currentTile = goalPosition;
        Position parent = null;
        do
        {
            try
            {
                parent = alreadySearched[currentTile].parent;
                if (parent != null)
                {
                    path.Add(parent);
                    currentTile = parent;
                }
            }
            catch(Exception e)
            {
                UnityEngine.Debug.LogError(currentTile);
            }
        } while (parent != null);

        

        path.Reverse();
 
        return path;
    }

    private bool doesMapContains(Position neighboor)
    {
        foreach (var n in map)
            if (n.Key.Equals(neighboor))
            {
                return true;
            }
        
        return false;
    }    

    private KeyValuePair<Position, NodeData> FindLargest(Dictionary<Position, NodeData> searching)
    {
        KeyValuePair<Position,NodeData> largest = searching.ElementAt(0);

        foreach(var n in searching)
        {
            if (n.Value.score > largest.Value.score)
                largest = n;
        }
        return largest;
    }
}

class PositionEqualityComparer : IEqualityComparer<Position>
{
    public bool Equals(Position i1, Position i2)
    {
        if (i2 == null && i1 == null)
            return true;
        else if (i1 == null || i2 == null)
            return false;
        if (i1.X == i2.X && i1.Y == i2.Y)
            return true;
        else
            return false;
    }

    public int GetHashCode(Position ip)
    {
        int hCode = (int)ip.X ^ (int)ip.Y;
        return hCode.GetHashCode();
    }
}