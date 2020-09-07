using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MemorySpot
{
    Vector3 spot;
    float memory;

    public MemorySpot(Vector3 spot, float memory)
    {
        this.memory = memory;
        this.spot = spot;
    }

    public bool IsForgotten()
    {
        return (memory <= 0.0f);
    }

    public Vector3 GetSpot()
    {
        return spot;
    }

    public void ModifyMemory(float modify)
    {
        memory -= modify;
    }
    public void SetMemory(float memory)
    {
        this.memory = memory;
    }
    public float GetMemory()
    {
        return memory;
    }
}

public class MonsterAI : MonoBehaviour
{
    public enum AIState { Goto,Chasing, Hunting, Looking, DoNothing }
    System.Random ran = new System.Random();

    public PathFinder pathFinder;   // The Pathfinder
    public GameObject target;       // The Target Gameobject
    Vector3 pathGoal;               // Where the pathfinding will goto
    Vector3 myLoggedLocation;       // This is used for pathfinding. Its to prevent rounding issues 
    List<Vector2> apath;            // The path for the monster will follow
    int apathCurrent = 0;           // The current spot that the monster is at in the path

    public GameObject TestTile;
    public GameObject TestPath;
    public GameObject TestPoint;
    
    List<MemorySpot> spotsChecked;  // This is used to figure out where the monster has checked
    float forgetRate = 1000;        // this is how long the spots checked stay checked
    float sizeOfFOV = 45;           // This is the width of the field of view, this will change during gameplay

    //List<GameObject> DebugPath = new List<GameObject>();
    //Dictionary<Vector3,GameObject> DebugTiles = new Dictionary<Vector3, GameObject>();
    //GameObject DebugPoint;

    PlayerController playerController;
    CharacterController characterController;
    public float moveSpeed;
    public AIState aiState;

    void BeginSearch()
    {
        Physics.IgnoreLayerCollision(8, 8, true);
        characterController = this.GetComponent<CharacterController>();
        aiState = AIState.Looking;
        //pathGoal = target.transform.position;
        myLoggedLocation = this.transform.position;

        List<MemorySpot> spotsNotChecked = spotsChecked.Where(x => x.IsForgotten()).ToList();
        
        if (spotsNotChecked.Count != 0)
            pathGoal = spotsNotChecked.ElementAt(ran.Next(0, spotsNotChecked.Count - 1)).GetSpot();
        else
            pathGoal = Converter.MASPPositionToUnityVector3(pathFinder.GetRandomWalkableSpace());
        CalculatePath();
    }

    // Update is called once per frame
    void Update()
    {
        MonsterBehaviour();
        /*
        if (Vector3.Distance(this.transform.position, target.transform.position) < 5)
        {
            Destroy(target.gameObject);
        }
        */
    }

    public void SetupMonster(PathFinder pathFinder,GameObject target)
    {
        this.pathFinder = pathFinder;
        this.target = target;
        this.playerController = target.GetComponent<PlayerController>();
        spotsChecked = new List<MemorySpot>();

        foreach (var p in pathFinder.GetMap())
        {
            Vector3 tempPos = Converter.MASPPositionToUnityVector3(p.Key)*4;
            //DebugTiles.Add(tempPos,Instantiate(TestTile, tempPos, Quaternion.identity,null));
            spotsChecked.Add(new MemorySpot(tempPos, 0));
        }
        BeginSearch();
    }

    void MonsterBehaviour()
    {
        Memory();
        if (
            WithinFieldOfView() && playerController.IsVisible() &&
            !Physics.Linecast(new Vector3(this.transform.position.x, 0.5f, this.transform.position.z), target.transform.position)            
            )
        {
            if (aiState != AIState.DoNothing)
            aiState = AIState.Chasing;
            sizeOfFOV = 360;

        }            
        else
        {
            if (aiState != AIState.DoNothing)
                if (aiState == AIState.Chasing)
                {
                    aiState = AIState.Hunting;
                    sizeOfFOV = 120;
                }
        }

        switch (aiState)
        {
            case AIState.Goto:
                GotoLocation();
                break;
            case AIState.Chasing:
                ChasingTarget();
                break;
            case AIState.Hunting:
                GotoLastSeenLocation();
                break;
            case AIState.Looking:
                LookForTarget();
                break;
        }

        
    }

    private bool WithinFieldOfView()
    {
        float toPlayer = Quaternion.FromToRotation(this.transform.forward, this.transform.position-target.transform.position).eulerAngles.y;
        return (toPlayer < 180 + sizeOfFOV && toPlayer > 180 - sizeOfFOV);
    }

    private void LookForTarget()
    {
        if (
            apathCurrent < apath.Count - 1 &&
            Physics.Linecast(new Vector3(this.transform.position.x, 0.5f, this.transform.position.z), pathGoal))
        {
            this.transform.LookAt(apath[apathCurrent + 1].ToVector3() * 4);
            characterController.Move(this.transform.forward * (moveSpeed) * Time.deltaTime);
            if (Vector3.Distance(this.transform.position, apath[apathCurrent + 1].ToVector3() * 4) < .2)
            {
                myLoggedLocation = apath[apathCurrent + 1].ToVector3() * 4;
                apathCurrent++;
            }

        }
        else
        {
            List<MemorySpot> spotsNotChecked = spotsChecked.Where(x => x.IsForgotten()).ToList();
            if (spotsNotChecked.Count != 0)
                pathGoal = spotsNotChecked.ElementAt(ran.Next(0, spotsNotChecked.Count - 1)).GetSpot();
            else
                pathGoal = Converter.MASPPositionToUnityVector3(pathFinder.GetRandomWalkableSpace());
            CalculatePath();
        }
    }

    private void GotoLastSeenLocation()
    {
        if (apathCurrent < apath.Count - 1)
        {
            this.transform.LookAt(apath[apathCurrent + 1].ToVector3() * 4);
            characterController.Move(this.transform.forward * (moveSpeed) * Time.deltaTime);
            if (Vector3.Distance(this.transform.position, apath[apathCurrent + 1].ToVector3() * 4) < .2)
            {
                myLoggedLocation = apath[apathCurrent + 1].ToVector3() * 4;
                apathCurrent++;
            }

        }
        else
        {
            List<MemorySpot> spotsNotChecked = spotsChecked.Where(x => x.IsForgotten()).ToList();
            pathGoal = spotsNotChecked.ElementAt(ran.Next(0, spotsNotChecked.Count - 1)).GetSpot();
            CalculatePath();
            apathCurrent = 0;
            aiState = AIState.Looking;
            sizeOfFOV = 45;
        }
    }

    private void ChasingTarget()
    {
        pathGoal = target.transform.position;
        this.transform.LookAt(apath[apathCurrent + 1].ToVector3() * 4);
        characterController.Move(this.transform.forward * (moveSpeed) * Time.deltaTime);
        if (Vector3.Distance(this.transform.position, apath[apathCurrent + 1].ToVector3() * 4) < .2)
        {            
            myLoggedLocation = apath[apathCurrent + 1].ToVector3() * 4;
            CalculatePath();
        }
    }

    private void Memory()
    {
        foreach (var s in spotsChecked)
        {
            if (!s.IsForgotten())
            {                
                s.ModifyMemory(100 * Time.deltaTime);    
            }
            else
            {               
                
                //if (DebugTiles[s.GetSpot()].activeSelf) DebugTiles[s.GetSpot()].SetActive(false);
                if (!Physics.Linecast(new Vector3(this.transform.position.x,0.5f, this.transform.position.z), s.GetSpot()))
                {
                    s.SetMemory(forgetRate);
                    //DebugTiles[s.GetSpot()].SetActive(true);
                }                    
            }
        }
    }

    void CalculatePath()
    {
        apathCurrent = 0;
        apath = Converter.ToVector2Path(pathFinder.FindPath(
            Converter.UnityVector3ToMASPPosition(myLoggedLocation / 4),
            Converter.UnityVector3ToMASPPosition(pathGoal / 4)));

        /*
        // Debug --------
        // Path
        foreach (var g in DebugPath)
            Destroy(g.gameObject);
        foreach (var p in apath)
            DebugPath.Add(Instantiate(TestPath, p.ToVector3()*4, Quaternion.identity, null));
        // Goal
        if (DebugPoint != null)
            DebugPoint.transform.position = pathGoal;
        else
            DebugPoint = Instantiate(TestPoint,pathGoal, Quaternion.identity, null);
        */
    }

    void GotoLocation()
    {        
        if (apathCurrent < apath.Count-1) {
            this.transform.LookAt(apath[apathCurrent + 1].ToVector3() * 4);
            characterController.Move(this.transform.forward * (moveSpeed) * Time.deltaTime);
            if (Vector3.Distance(this.transform.position, apath[apathCurrent + 1].ToVector3() * 4) < .2)
            {
                myLoggedLocation = apath[apathCurrent + 1].ToVector3() * 4;
                apathCurrent++;
            }
            
        }
        else
        {
            List<MemorySpot> spotsNotChecked = spotsChecked.Where(x => x.IsForgotten()).ToList();
            if (spotsNotChecked.Count != 0)
                pathGoal = spotsNotChecked.ElementAt(ran.Next(0, spotsNotChecked.Count - 1)).GetSpot();
            else
                pathGoal = Converter.MASPPositionToUnityVector3(pathFinder.GetRandomWalkableSpace());
            CalculatePath();
            aiState = AIState.Looking;
        }            
    }
}

