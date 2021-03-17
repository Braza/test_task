using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NodeRect
{
    public Rect Rect;
}

[Serializable]
public class NodeEdge
{
    public NodeRect Node1;
    public NodeRect Node2;

    public Vector2 P1;
    public Vector2 P2;
}

[Serializable]
public class Frustrum
{
    public Vector2 lowStart;
    public Vector2 highStart;

    public Vector2 lowEnd;
    public Vector2 highEnd;
}

public class TestTask : MonoBehaviour
{
    [SerializeField]
    List<NodeRect> rooms = new List<NodeRect>();

    [SerializeField]
    List<NodeEdge> doors = new List<NodeEdge>();

    [SerializeField]
    Vector2 start, end;

    [SerializeField]
    [Range(0.01f,0.16f)]
    float radius;

    [SerializeField]
    NodeEdge[,] graph;

    [SerializeField]
    List<Vector2> path = new List<Vector2>();

    List<Tuple<Frustrum,NodeRect>> pathZones = new List<Tuple<Frustrum, NodeRect>>();

    public List<Vector2> CreatePath(List<NodeRect> rp)
    {
        path.Clear();
        pathZones.Clear();
        Vector2 lowStart = start;
        Vector2 highStart = start;

        path.Add(start);
        for (int i = 0; i < rp.Count - 1; i++)
        {
            var nextDoor = graph[rooms.IndexOf(rp[i]), rooms.IndexOf(rp[i + 1])];
            
            if (pathZones.Count > 0)
            {
                var extendedFrustrum = FrustrumHitsDoor(pathZones[pathZones.Count-1].Item1, nextDoor);
                if (extendedFrustrum == null)
                {
                    pathZones.Add(new Tuple<Frustrum, NodeRect>(null, rp[i]));
                    if (i < rp.Count - 2)
                    {
                        var nextNextDoor = graph[rooms.IndexOf(rp[i + 1]), rooms.IndexOf(rp[i + 2])];
                        var virtualFrustrum = GetFrustrum(rp[i].Rect.center, rp[i].Rect.center, nextDoor);
                        var frustrum = GetFrustrum(virtualFrustrum.highEnd, virtualFrustrum.lowEnd, nextNextDoor);
                        if (frustrum != null)
                        {
                            float highAngle = Vector2.SignedAngle(Vector2.up, nextDoor.P1 - frustrum.highEnd);
                            float lowAngle = Vector2.SignedAngle(Vector2.up, nextDoor.P2 - frustrum.lowEnd);

                            if (highAngle < lowAngle)
                            {
                                lowStart = frustrum.highEnd;
                                highStart = frustrum.lowEnd;
                            }
                            else
                            {
                                lowStart = frustrum.lowEnd;
                                highStart = frustrum.highEnd;
                            }
                            pathZones.Add(new Tuple<Frustrum, NodeRect>(frustrum, rp[i + 1]));
                        }
                        
                    }
                    else
                    {
                        //we're at a pre-target room with no previous track. Let's reach the goal and connect with last frustrum.
                        pathZones.Add(new Tuple<Frustrum, NodeRect>(GetFrustrum(end, end, nextDoor),rp[i]));
                    }
                    
                }
                else
                {
                    //we're at a pre-target room with previous track. Does the target lie in the frustrum maybe?
                    //TODO
                }
            }
            else
            {
                pathZones.Add(new Tuple<Frustrum, NodeRect>(GetFrustrum(highStart, lowStart, nextDoor), rp[i]));
            }
        }

        ConnectUnlinkedFrustrums();

        path.Add(end);
        return path;
    }

    private void ConnectUnlinkedFrustrums()
    {
        bool found1, found2, found3, found4, found5, found6, found7, found8, found9, found10, found11, found12;
        for (int i = 0; i < pathZones.Count - 2; )
        {
            var f1 = pathZones[i].Item1;
            var f2 = pathZones[i+2].Item1; //TODO: handle more irregular case
            var gap = pathZones[i + 1].Item2;
            Rect borderedRect = Rect.MinMaxRect(gap.Rect.xMin + radius, gap.Rect.yMin + radius, gap.Rect.xMax - radius, gap.Rect.yMax - radius);


            var pt1 = GetIntersectionPointCoordinates(f1.highStart, f1.highEnd, f2.highStart, f2.highEnd, out found1);
            var pt2 = GetIntersectionPointCoordinates(f1.lowStart, f1.lowEnd, f2.lowStart, f2.lowEnd, out found2);
            var pt3 = GetIntersectionPointCoordinates(f1.highStart, f1.highEnd, f2.lowStart, f2.lowEnd, out found3);
            var pt4 = GetIntersectionPointCoordinates(f1.lowStart, f1.lowEnd, f2.highStart, f2.highEnd, out found4);
            if (found1 && borderedRect.Contains(pt1))
            { 
                path.Add(pt1); 
            }
            else if (found2 && borderedRect.Contains(pt2))
            {
                path.Add(pt2);
            }
            else if (found3 && borderedRect.Contains(pt3))
            {
                path.Add(pt3);
            }
            else if (found4 && borderedRect.Contains(pt4))
            {
                path.Add(pt4);
            }
            else
            {
                //Brute adding connecting points
                var pt5 = GetIntersectionPointCoordinates(f1.highStart, f1.highEnd, gap.Rect.center, gap.Rect.center + Vector2.right, out found5);
                var pt6 = GetIntersectionPointCoordinates(f1.highStart, f1.highEnd, gap.Rect.center, gap.Rect.center + Vector2.up, out found6);
                var pt9 = GetIntersectionPointCoordinates(f1.lowStart, f1.lowEnd, gap.Rect.center, gap.Rect.center + Vector2.right, out found9);
                var pt10 = GetIntersectionPointCoordinates(f1.lowStart, f1.lowEnd, gap.Rect.center, gap.Rect.center + Vector2.up, out found10);
                if (found5 && borderedRect.Contains(pt5))
                {
                    path.Add(pt5);
                }
                else if (found6 && borderedRect.Contains(pt6))
                {
                    path.Add(pt6);
                }
                else if (found9 && borderedRect.Contains(pt9))
                {
                    path.Add(pt9);
                }
                else if (found10 && borderedRect.Contains(pt10))
                {
                    path.Add(pt10);
                }
                var pt7 = GetIntersectionPointCoordinates(f2.highStart, f2.highEnd, gap.Rect.center, gap.Rect.center + Vector2.right, out found7);
                var pt8 = GetIntersectionPointCoordinates(f2.highStart, f2.highEnd, gap.Rect.center, gap.Rect.center + Vector2.up, out found8);
                var pt11 = GetIntersectionPointCoordinates(f2.lowStart, f2.lowEnd, gap.Rect.center, gap.Rect.center + Vector2.right, out found11);
                var pt12 = GetIntersectionPointCoordinates(f2.lowStart, f2.lowEnd, gap.Rect.center, gap.Rect.center + Vector2.up, out found12);
                if (found7 && borderedRect.Contains(pt7))
                {
                    path.Add(pt7);
                }
                else if (found8 && borderedRect.Contains(pt8))
                {
                    path.Add(pt8);
                }
                else if (found11 && borderedRect.Contains(pt11))
                {
                    path.Add(pt11);
                }
                else if (found12 && borderedRect.Contains(pt12))
                {
                    path.Add(pt12);
                }
            }
            i += 2; //TODO: handle more irregular case
        }
    }

    Frustrum GetFrustrum(Vector2 highStart, Vector2 lowStart, NodeEdge nextDoor)
    {
        Vector2 lowEnd;
        Vector2 highEnd;
        var requiredIndent = DoorWidthRequiredToPassFromPoint(lowStart, nextDoor); //it's ok to calculate just for 1 point with simplified function..for now

        if (requiredIndent > (nextDoor.P1 - nextDoor.P2).magnitude/2)
        {
            return null;
        }

        if (nextDoor.P1.x == nextDoor.P2.x)
        {
            lowEnd = nextDoor.P1.y > nextDoor.P2.y ? nextDoor.P2 + Vector2.up * requiredIndent : nextDoor.P1 + Vector2.up * requiredIndent;
            highEnd = nextDoor.P1.y > nextDoor.P2.y ? nextDoor.P1 - Vector2.up * requiredIndent : nextDoor.P2 - Vector2.up * requiredIndent;
            return new Frustrum { highEnd = highEnd, lowEnd = lowEnd, highStart = highStart, lowStart = lowStart };
        }
        else if (nextDoor.P1.y == nextDoor.P2.y)
        {
            lowEnd = nextDoor.P1.x > nextDoor.P2.x ? nextDoor.P2 + Vector2.right * requiredIndent : nextDoor.P1 + Vector2.right * requiredIndent;
            highEnd = nextDoor.P1.x > nextDoor.P2.x ? nextDoor.P1 - Vector2.right * requiredIndent : nextDoor.P2 - Vector2.right * requiredIndent;
            return new Frustrum { highEnd = highEnd, lowEnd = lowEnd, highStart = highStart, lowStart = lowStart };
        }
        else
        {
            Debug.LogError("Inconsistent door: " + nextDoor);
            return null;
        }
    }

    private Frustrum FrustrumHitsDoor(Frustrum frustrum, NodeEdge nextDoor)
    {
        if (frustrum == null)
        {
            return null;
        }
        float highAngle = Vector2.SignedAngle(Vector2.up, frustrum.highEnd - frustrum.highStart);
        float lowAngle = Vector2.SignedAngle(Vector2.up, frustrum.lowEnd - frustrum.lowStart);

        Vector2 lowEnd;
        Vector2 highEnd;

        var nextFrustrum = GetFrustrum(frustrum.highStart, frustrum.lowStart, nextDoor);

        if (nextFrustrum == null)
        {
            return null;
        }

        if (Vector2.SignedAngle(Vector2.up, nextFrustrum.lowEnd - frustrum.lowEnd) > Vector2.SignedAngle(Vector2.up, nextFrustrum.highEnd - frustrum.highEnd))
        {
            lowEnd = nextFrustrum.highEnd;
            highEnd = nextFrustrum.lowEnd;
        }
        else
        {
            highEnd = nextFrustrum.highEnd;
            lowEnd = nextFrustrum.lowEnd;
        }

        float highAngleNext = Vector2.SignedAngle(Vector2.up, highEnd - frustrum.highStart);
        float lowAngleNext = Vector2.SignedAngle(Vector2.up, lowEnd - frustrum.lowStart); ;

        bool hits = (highAngleNext >= lowAngle && highAngleNext <= highAngle) || (lowAngleNext >= lowAngle && lowAngleNext <= highAngle) || (highAngleNext >= highAngle && lowAngleNext <= lowAngle);

        Vector2 lowEndNew = lowEnd;
        Vector2 highEndNew = highEnd;

        if (highAngleNext < lowAngle || lowAngleNext > highAngle)
        {
            return null;
        }

        if (highAngleNext >= lowAngle && highAngleNext <= highAngle)
        {
            frustrum.highEnd = GetFrustrum(frustrum.highStart, frustrum.lowStart, nextDoor).highEnd; 
        } 
        else
        {
            frustrum.highEnd = highEnd; 
        }
        if (lowAngleNext >= lowAngle && lowAngleNext <= highAngle)
        {
            frustrum.lowEnd = GetFrustrum(frustrum.highStart, frustrum.lowStart, nextDoor).lowEnd;
        }
        else 
        {
            frustrum.lowEnd = lowEnd;
        }


        return frustrum;
    }

    public Vector2 GetIntersectionPointCoordinates(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2, out bool found)
    {
        float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);

        if (tmp == 0)
        {
            // No solution!
            found = false;
            return Vector2.zero;
        }

        float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;

        found = true;

        return new Vector2(
            B1.x + (B2.x - B1.x) * mu,
            B1.y + (B2.y - B1.y) * mu
        );
    }

    private void Start()
    {
        graph = new NodeEdge[rooms.Count, rooms.Count];
        
        LinkDoorsToRooms();
        InitGraph();
        var roomsPath = FindRoomsPath();
        CreatePath(roomsPath);
    }


    private void InitGraph()
    {
        foreach(var door in doors)
        {
            int idx1, idx2;
            idx1 = rooms.IndexOf(door.Node1);
            idx2 = rooms.IndexOf(door.Node2);
            graph[idx1, idx2] = graph[idx2, idx1] = door;
        }

    }

    private List<NodeRect> FindRoomsPath()
    {
        List<NodeRect> roomsPath = new List<NodeRect>();

        //Find start room
        NodeRect startRoom = FindRoomFromPoint(start);
        NodeRect endRoom = FindRoomFromPoint(end);

        if (start != null && end != null)
        {
            //Assume only 1 direct path exists, no DFS or DFS is used
            var currentRoom = startRoom;
            
            while (currentRoom != endRoom)
            {
                roomsPath.Add(currentRoom);
                currentRoom = GetNextRoom(currentRoom, roomsPath);
                if (currentRoom == null)
                {
                    return null;
                }
            }
        }
        roomsPath.Add(endRoom);
        return roomsPath;
    }

    float DoorWidthRequiredToPassFromPoint(Vector2 v, NodeEdge door)
    {
        float ratio = 1;
        if (door.P1.y == door.P2.y)
        {
            ratio = Mathf.Tan(Mathf.Deg2Rad * Vector2.Angle(Vector2.up, (door.P1 + door.P2) / 2 - v));
        }
        else if (door.P1.x == door.P2.x)
        {
            ratio = Mathf.Tan(Mathf.Deg2Rad * Vector2.Angle(Vector2.right, (door.P1 + door.P2) / 2 - v));
        }
        return radius* Mathf.Abs(ratio) + radius;
    }

    private NodeRect GetNextRoom(NodeRect currentRoom, List<NodeRect> roomsPath)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            int j = rooms.IndexOf(currentRoom);
            if ((graph[i,j] != null) 
                && ((roomsPath.Count <= 1) // 1st step
                || rooms[i] != roomsPath[roomsPath.Count - 2])) //not going backwards
            {
                return rooms[i];
            }
        }
        return null;
    }

    private NodeRect FindRoomFromPoint(Vector2 v)
    {
        foreach (var room in rooms)
        {
            var r = room.Rect;
            if (v.x >= r.x && 
                v.x <= r.x + r.size.x && 
                v.y >= r.y && 
                v.y <= r.y + r.size.y)
            {
                return room;
            }
        }
        return null;
    }

    private void LinkDoorsToRooms()
    {
        //TODO: optimize
        foreach (var door in doors)
        {
            bool entrance = false, exit = false;
            foreach (var room in rooms)
            {
                if (LineBelongsToRect(room.Rect, door.P1, door.P2))
                {
                    if (!entrance)
                    {
                        door.Node1 = room;
                        entrance = true;
                    }
                    else if (!exit)
                    {
                        door.Node2 = room;
                        exit = true;
                        break;
                    }
                }
            }
        }
    }

    private bool LineBelongsToRect(Rect r, Vector2 p1, Vector2 p2)
    {
        if (p1.x == p2.x)
        {
            if ((r.x == p1.x || r.x + r.size.x == p1.x) && 
                (r.y <= p1.y && r.y <= p2.y && r.y + r.size.y >= p1.y && r.y + r.size.y >= p2.y))
            {
                return true;
            }
        }
        else if (p1.y == p2.y)
        {
            if ((r.y == p1.y || r.y + r.size.y == p1.y) &&
                (r.x <= p1.x && r.x <= p2.x && r.x + r.size.x >= p1.x && r.x + r.size.x >= p2.x))
            {
                return true;
            }
        }
        else
        {
            Debug.LogError("Door in inconsistent: " + p1 + ", " + p2);
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        graph = new NodeEdge[rooms.Count, rooms.Count];

        LinkDoorsToRooms();
        InitGraph();
        var roomsPath = FindRoomsPath();
        CreatePath(roomsPath);

        Gizmos.color = Color.blue;
        foreach (var room in rooms)
        {
            DrawRect(room.Rect);
        }

        //Gizmos.color = Color.green;
        foreach (var door in doors)
        {
            /*var r = DoorWidthRequiredToPassFromPoint(start, door);
            Gizmos.DrawWireSphere((door.P1 + door.P2) / 2, r);*/
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(door.P1, door.P2);
            //Gizmos.color = Color.green;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(start, radius);
        Gizmos.DrawSphere(end, radius);

        /*Gizmos.color = Color.white;
        for (int i = 0; i < pathZones.Count; i++)
        {
            if (i % 2 == 0)
            {
                Gizmos.DrawLine(pathZones[i].Item1.lowStart, pathZones[i].Item1.lowEnd);
                Gizmos.DrawLine(pathZones[i].Item1.highStart, pathZones[i].Item1.highEnd);
            }
        }*/

        Gizmos.color = Color.red;
        for (int i = 0; i < path.Count - 1; i++)
        {
            Gizmos.DrawLine(path[i], path[i + 1]);
        }
    }

    private void DrawRect(Rect r)
    {
        Gizmos.DrawLine(new Vector3(r.x, r.y), new Vector3(r.x, r.y + r.size.y));
        Gizmos.DrawLine(new Vector3(r.x , r.y + r.size.y), new Vector3(r.x + r.size.x, r.y + r.size.y));
        Gizmos.DrawLine(new Vector3(r.x + r.size.x, r.y + r.size.y), new Vector3(r.x + r.size.x, r.y));
        Gizmos.DrawLine(new Vector3(r.x + r.size.x, r.y), new Vector3(r.x, r.y));
    }
}
