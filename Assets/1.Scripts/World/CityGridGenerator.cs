using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Mirror;
using Unity.VisualScripting;
class RoadPiece : IEquatable<RoadPiece>
{
    public Vector3Int position;
    public CityGridGenerator.RoadType type;
    public int yRotation;
    public GameObject road;

    public bool Equals(RoadPiece other)
    {
        if (other == null) return false;
        return (position == other.position && type == other.type && yRotation == other.yRotation ||
            position == other.position && 
            type == CityGridGenerator.RoadType.Straight && 
            other.type == CityGridGenerator.RoadType.Straight
            && Mathf.Abs(yRotation - other.yRotation) == 180);
    }
}

public class CityGridGenerator : NetworkBehaviour
{
    public Transform cityParent;
    public NavMeshBuilder navMeshBuilder;
    public NetworkStartPosition startPos;

    public DungeonController dungeonControllerPrefab;
    public List<Transform> monsterFiledList;
    public List<DungeonController> dungeonControllers;

    public GameObject straight;
    public GameObject corner;
    public GameObject crossRoad;
    public GameObject tjunction;
    public GameObject[] residentialSmall;
    public GameObject[] residentialMedium;
    public GameObject[] residentialLarge;
    public GameObject[] commercialSmall;
    public GameObject[] commercialMedium;
    public GameObject[] commercialLarge;
    public GameObject[] industrialSmall;
    public GameObject[] industrialMedium;
    public GameObject[] industrialLarge;
    public GameObject[] fillers;
    public GameObject[] trees;
    public GameObject park;

    Vector3Int minDImensions = Vector3Int.zero;
    Vector3Int maxDImensions = Vector3Int.zero;

    public enum RoadType
    {
        Straight,
        Corner,
        Cross,
        TJunc
    };

    public enum PieceType
    {
        None,
        Road,
        House,
        Shack,
        Lawn,
        Industrial,
        Commercial,
        Park
    }

    public Dictionary<Vector3Int, PieceType> cityMap = new Dictionary<Vector3Int, PieceType>();

    List<RoadPiece> roadPieces = new List<RoadPiece>();

    public enum ZoneType { R, C, I };
    List<List<int>> zones = new List<List<int>>();

    int width = 100;
    int depth = 100;
    Vector3Int crawlerPos;
    Vector3 dir = new Vector3(0, 0, 1);
    Vector3 neutral = new Vector3(0, 0, 1);

    public int numberOfCrawls = 50;
    float progess = 0.1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        zones.Add(new List<int> { 0, 1 });//Residential
        zones.Add(new List<int> { 2, 3 });//commercial
        zones.Add(new List<int> { 4, 5 });//industrial
        
        //for (int i = 0; i <= 5; i++)
        //{
        //    Vector3Int mapKey = Vector3Int.RoundToInt(Vector3Int.RoundToInt(dir * -i));
        //    if (!cityMap.ContainsKey(mapKey))
        //    {
        //        cityMap.Add(mapKey, PieceType.Road);
        //    }
        //}

        UnityEditor.EditorUtility.DisplayProgressBar("GeneratingCity", "Drawing Roads", progess);

        StartCoroutine(Crawl());
    }

    void ReclaimMap()
    {
        int roadMask = 1 << 22;
        RaycastHit hitup;

        for (int i = 0; i < 5; i++)
        {
            Vector3Int mapKey = Vector3Int.RoundToInt(crawlerPos - Vector3Int.RoundToInt(dir * i));
            if (cityMap.ContainsKey(mapKey))
            {
                if(!HitRoad(mapKey))
                    cityMap.Remove(mapKey);
            }
        }
    }

    bool HitRoad(Vector3Int gridPos)
    {
        int roadMask = 1 << 22;
        RaycastHit hitup;

        return (Physics.Raycast(gridPos + new Vector3Int(0, -5, 0), Vector3.up, out hitup, 10, roadMask));
            
    }

    void CheckOutBounds()
    {
        if(crawlerPos.x > width || crawlerPos.x  < 0||
            crawlerPos.z > depth || crawlerPos.z < 0)
        {
            //ReclaimMap();
            crawlerPos.x = 0;
            crawlerPos.z = 0;
        }
    }

    IEnumerator Crawl()
    {
        int crawlCount = 0;
        while (crawlCount < numberOfCrawls)
        {
            crawlCount++;
            UnityEditor.EditorUtility.DisplayProgressBar("GeneratingCity", "Drawing Roads", progess += 0.1f);

            int randomTurn = UnityEngine.Random.Range(0, 3);
            float rot;
            GameObject go;
            RoadPiece newRoad;
            if (randomTurn == 0)
            {
                dir = Quaternion.Euler(0, -90, 0) * dir;
                rot = Vector3.SignedAngle(neutral, dir, this.transform.up) + 90;
                go = Instantiate(corner, crawlerPos, Quaternion.identity);
                go.transform.Rotate(0, rot, 0);
                go.transform.Translate(0, -0.1f, 0);
                go.transform.SetParent(cityParent);

                newRoad = new RoadPiece
                {
                    position = crawlerPos,
                    type = RoadType.Corner,
                    yRotation = (int)Mathf.Round(go.transform.rotation.eulerAngles.y / 90) * 90,
                    road = go
                };
            }
            else if (randomTurn == 1)
            {
                dir = Quaternion.Euler(0, 90, 0) * dir;
                rot = Vector3.SignedAngle(neutral, dir, this.transform.up) + 180;
                go = Instantiate(corner, crawlerPos, Quaternion.identity);
                go.transform.Rotate(0, rot, 0);
                go.transform.Translate(0, -0.1f, 0);
                go.transform.SetParent(cityParent);

                newRoad = new RoadPiece
                {
                    position = crawlerPos,
                    type = RoadType.Corner,
                    yRotation = (int)Mathf.Round(go.transform.rotation.eulerAngles.y / 90) * 90,
                    road = go
                };
            }
            else
            {
                rot = Vector3.SignedAngle(neutral, dir, this.transform.up);
                go = Instantiate(straight, crawlerPos, Quaternion.identity);
                go.transform.Rotate(0, rot, 0);
                go.transform.Translate(0, -0.1f, 0);
                go.transform.SetParent(cityParent);

                newRoad = new RoadPiece
                {
                    position = crawlerPos,
                    type = RoadType.Straight,
                    yRotation = (int)Mathf.Round(go.transform.rotation.eulerAngles.y / 90) * 90,
                    road = go
                };
            }

            AddNoDuplication(newRoad);

            yield return null;

            Vector3 straightPos = crawlerPos + Vector3Int.RoundToInt(dir * 10);
            rot = Vector3.SignedAngle(neutral, dir, this.transform.up);
            go = Instantiate(straight, straightPos, Quaternion.identity);
            go.transform.Rotate(0, rot, 0);
            go.transform.Translate(0, -0.1f, 0);
            go.transform.SetParent(cityParent);

            newRoad = new RoadPiece
            {
                position = Vector3Int.RoundToInt(straightPos),
                type = RoadType.Straight,
                yRotation = (int)Mathf.Round(go.transform.rotation.eulerAngles.y / 90) * 90,
                road = go
            };

            AddNoDuplication(newRoad);

            yield return null;

            //for (int i = 0; i <= 20; i++)
            //{
            //    Vector3Int mapKey = Vector3Int.RoundToInt(crawlerPos + Vector3Int.RoundToInt(dir * i));
            //    if (!cityMap.ContainsKey(mapKey))
            //    {
            //        cityMap.Add(mapKey, PieceType.Road);
            //    }
            //}

            crawlerPos += Vector3Int.RoundToInt(dir * 20);
            //if (crawlerPos.x > width || crawlerPos.x < 0 || crawlerPos.z > depth || crawlerPos.z < 0)
            {
                //crawler.transform.position -= crawlerPos;
            }

            //crawler.transform.position = crawlerPos;

            if (minDImensions.x > crawlerPos.x) minDImensions.x = crawlerPos.x;
            if (minDImensions.z > crawlerPos.z) minDImensions.z = crawlerPos.z;
            if (maxDImensions.x < crawlerPos.x) maxDImensions.x = crawlerPos.x;
            if (maxDImensions.z < crawlerPos.z) maxDImensions.z = crawlerPos.z;

            CheckOutBounds();

            yield return null;
        }


        UnityEditor.EditorUtility.DisplayProgressBar("GeneratingCity", "Mapping Roads", progess += 0.2f);
        Invoke("FindRoads", 0.1f);
        UnityEditor.EditorUtility.DisplayProgressBar("GeneratingCity", "Fixing Roads", progess += 0.2f);
        Invoke("FixRoads", 0.2f);
        UnityEditor.EditorUtility.DisplayProgressBar("GeneratingCity", "Building Houses", progess += 0.2f);
        //Invoke("BuildHouses", 0.3f);
    }

    void AddNoDuplication(RoadPiece newPiece)
    {
        bool found = false;
        foreach (RoadPiece piece in roadPieces)
        {
            if (piece.Equals(newPiece))
            {
                found = true;
                break;
            }
        }
        if (!found)
        {
            roadPieces.Add(newPiece);
        }
        else
        {
            DestroyImmediate(newPiece.road);
        }
    }

    void FixRoads()
    {
        Lookup<Vector3Int, RoadPiece> lookup = (Lookup<Vector3Int, RoadPiece>)roadPieces.ToLookup(rp => rp.position, rp => rp);

        foreach (IGrouping<Vector3Int, RoadPiece> group in lookup)
        {
            if (group.Count() > 1)
            {
                bool hasCorner0 = false;
                bool hasCorner90 = false;
                bool hasCorner180 = false;
                bool hasCorner270 = false;

                bool hasStraight0 = false;
                bool hasStraight90 = false;
                bool hasStraight180 = false;
                bool hasStraight270 = false;

                foreach (RoadPiece r in group)
                {
                    if (r.yRotation == 0 && r.type == RoadType.Corner) hasCorner0 = true;
                    if (r.yRotation == 90 && r.type == RoadType.Corner) hasCorner90 = true;
                    if (r.yRotation == 180 && r.type == RoadType.Corner) hasCorner180 = true;
                    if (r.yRotation == 270 && r.type == RoadType.Corner) hasCorner270 = true;

                    if (r.yRotation == 0 && r.type == RoadType.Straight) hasStraight0 = true;
                    if (r.yRotation == 90 && r.type == RoadType.Straight) hasStraight90 = true;
                    if (r.yRotation == 180 && r.type == RoadType.Straight) hasStraight180 = true;
                    if (r.yRotation == 270 && r.type == RoadType.Straight) hasStraight270 = true;

                    DestroyImmediate(r.road);
                }

                if (hasStraight0 && hasStraight90 ||
                   hasStraight90 && hasStraight180 ||
                   hasStraight180 && hasStraight270 ||
                   hasStraight270 && hasStraight0 ||
                   hasCorner0 && hasCorner180 ||
                   hasCorner90 && hasCorner270
                   ||
                   hasCorner90 && (hasStraight90 || hasStraight270) && hasCorner0 ||
                   hasCorner0 && (hasStraight0 || hasStraight180) && hasCorner270)
                {
                    GameObject go = Instantiate(crossRoad, group.Key, Quaternion.identity);
                    go.transform.Translate(0, -0.1f, 0);
                    cityMap[group.Key] = PieceType.Road;

                    go.transform.SetParent(cityParent);
                }
                else if (
                        hasCorner0 && hasCorner90 ||
                        hasCorner0 && hasStraight0 ||
                        hasCorner0 && hasStraight180 ||
                        hasCorner90 && hasStraight0 ||
                        hasCorner90 && hasStraight180
                        )
                {
                    GameObject go = Instantiate(tjunction, group.Key, Quaternion.identity);
                    go.transform.Translate(0, -0.1f, 0);
                    cityMap[group.Key] = PieceType.Road;

                    go.transform.SetParent(cityParent);
                }
                else if (
                        hasCorner0 && hasCorner270 ||
                        hasCorner0 && hasStraight90 ||
                        hasCorner0 && hasStraight270 ||
                        hasCorner270 && hasStraight90 ||
                        hasCorner270 && hasStraight270
                        )
                {
                    GameObject go = Instantiate(tjunction, group.Key, Quaternion.identity);
                    go.transform.Rotate(0, -90, 0);
                    go.transform.Translate(0, -0.1f, 0);
                    cityMap[group.Key] = PieceType.Road;

                    go.transform.SetParent(cityParent);
                }
                else if (
                    hasCorner90 && hasCorner180 ||
                    hasCorner90 && hasStraight90 ||
                    hasCorner90 && hasStraight270 ||
                    hasCorner180 && hasStraight90 ||
                    hasCorner180 && hasStraight270
                    )
                {
                    GameObject go = Instantiate(tjunction, group.Key, Quaternion.identity);
                    go.transform.Rotate(0, 90, 0);
                    go.transform.Translate(0, -0.1f, 0);
                    cityMap[group.Key] = PieceType.Road;

                    go.transform.SetParent(cityParent);
                }
                else if (
                hasCorner180 && hasCorner270 ||
                hasCorner180 && hasStraight0 ||
                hasCorner180 && hasStraight180 ||
                hasCorner270 && hasStraight0 ||
                hasCorner270 && hasStraight180
                )
                {
                    GameObject go = Instantiate(tjunction, group.Key, Quaternion.identity);
                    go.transform.Rotate(0, 180, 0);
                    go.transform.Translate(0, -0.1f, 0);
                    cityMap[group.Key] = PieceType.Road;

                    go.transform.SetParent(cityParent);
                }
            }
        }

        BuildHouses();
    }

    bool IsVornoiType(int x, int z, ZoneType type)
    {
        foreach(int t in zones[(int)type])
        {
            if (MeshUtils.voronoiMap[x + Mathf.Abs(minDImensions.x) + 10, 
                                    z + Mathf.Abs(minDImensions.z) + 10] == t)
            {
                return true;
            }
        }

        return false;
    }

    bool OutSizeMap(Vector3Int gridPos)
    {
        return (gridPos.x > maxDImensions.x || gridPos.x < minDImensions.x ||
            gridPos.z > maxDImensions.z || gridPos.z < minDImensions.z);
    }

    void FindRoads()
    {
        for (int z = minDImensions.z - 10; z < maxDImensions.z + 10; z++)
        {
            for (int x = minDImensions.x - 10; x < maxDImensions.x + 10; x++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);

                if(HitRoad(pos) && !cityMap.ContainsKey(pos))
                {
                    cityMap.Add(pos, PieceType.Road);
                }
            }
        }
    }

    void BuildHouses()
    {
        MeshUtils.GenerateVoronoi(6, maxDImensions.x + Mathf.Abs(minDImensions.x) + 20,
                                     maxDImensions.z + Mathf.Abs(minDImensions.z) + 20);

        for (int z = minDImensions.z - 10; z < maxDImensions.z + 10; z++)
        {
            for (int x = minDImensions.x - 10; x < maxDImensions.x + 10; x++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);

                GameObject go = null;
                PieceType pt = PieceType.None;
                int rValue;
                float density = MeshUtils.fBM(x * 0.005f, z * 0.005f, 3);

                if (UnityEngine.Random.Range(0, 100) < 1)
                {
                    go = Instantiate(park, pos, Quaternion.identity);
                    pt = PieceType.Park;

                    go.transform.SetParent(cityParent);
                }
                else
                { 
                    if (IsVornoiType(x, z, ZoneType.R))
                    {
                        if (density < 0.5f)
                        {
                            rValue = UnityEngine.Random.Range(0, residentialSmall.Length);
                            go = Instantiate(residentialSmall[rValue], pos, Quaternion.identity);

                            go.transform.SetParent(cityParent);
                        }
                        else if (density < 0.6f)
                        {
                            rValue = UnityEngine.Random.Range(0, residentialMedium.Length);
                            go = Instantiate(residentialMedium[rValue], pos, Quaternion.identity);

                            go.transform.SetParent(cityParent);
                        }
                        else
                        {
                            rValue = UnityEngine.Random.Range(0, residentialLarge.Length);
                            go = Instantiate(residentialLarge[rValue], pos, Quaternion.identity);

                            go.transform.SetParent(cityParent);
                        }
                        pt = PieceType.House;
                    }

                    if (IsVornoiType(x, z, ZoneType.C))
                    {
                        if (density < 0.464)
                        {
                            rValue = UnityEngine.Random.Range(0, commercialSmall.Length);
                            go = Instantiate(commercialSmall[rValue], pos, Quaternion.identity);

                            go.transform.SetParent(cityParent);
                        }
                        else if (density < 0.62f)
                        {
                            rValue = UnityEngine.Random.Range(0, commercialMedium.Length);
                            go = Instantiate(commercialMedium[rValue], pos, Quaternion.identity);

                            go.transform.SetParent(cityParent);
                        }
                        else
                        {
                            rValue = UnityEngine.Random.Range(0, commercialLarge.Length);
                            go = Instantiate(commercialLarge[rValue], pos, Quaternion.identity);

                            go.transform.SetParent(cityParent);
                        }
                        pt = PieceType.Commercial;
                    }

                    if (IsVornoiType(x, z, ZoneType.I))
                    {
                        if (density < 0.3)
                        {
                            rValue = UnityEngine.Random.Range(0, industrialSmall.Length);
                            go = Instantiate(industrialSmall[rValue], pos, Quaternion.identity);

                            go.transform.SetParent(cityParent);
                        }
                        else if (density < 0.6)
                        {
                            rValue = UnityEngine.Random.Range(0, industrialMedium.Length);
                            go = Instantiate(industrialMedium[rValue], pos, Quaternion.identity);

                            go.transform.SetParent(cityParent);
                        }
                        else
                        {
                            rValue = UnityEngine.Random.Range(0, industrialLarge.Length);
                            go = Instantiate(industrialLarge[rValue], pos, Quaternion.identity);

                            go.transform.SetParent(cityParent);
                        }
                        pt = PieceType.Industrial;
                    }
                }

                if (go == null) continue;

                BoxCollider box = go.GetComponent<BoxCollider>();

                if(box == null)
                {
                    continue;
                }

                bool found = false;

                for (int j = (int)(-box.size.z / 2.0f); j < box.size.z / 2.0f; j++)
                {
                    for (int i = (int)(-box.size.x / 2.0f); i < box.size.x / 2.0f; i++)
                    {
                        Vector3Int mapKey = Vector3Int.RoundToInt(go.transform.position + new Vector3Int(i, 0, j));
                        if (cityMap.ContainsKey(mapKey) || OutSizeMap(mapKey))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found == true) break;
                }

                if (found == true)
                {
                    DestroyImmediate(go);
                    go = null;
                    continue;
                }

                RaycastHit hitUp = new RaycastHit();
                RaycastHit hitforward = new RaycastHit();
                RaycastHit hitback = new RaycastHit();
                RaycastHit hitleft = new RaycastHit();
                RaycastHit hitright = new RaycastHit();

                int roadMask = 1 << 22;

                if (!Physics.Raycast(pos, go.transform.forward, out hitforward, box.size.z + 1, roadMask) &&
                    !Physics.Raycast(pos, -go.transform.forward, out hitback, box.size.z + 1, roadMask) &&
                    !Physics.Raycast(pos, go.transform.right, out hitright, box.size.x+ 1, roadMask) &&
                    !Physics.Raycast(pos, -go.transform.right, out hitleft, box.size.x + 1, roadMask))
                {
                    DestroyImmediate(go);
                    go = null;
                }

                if(HitRoad(pos))
                {
                    DestroyImmediate(go);
                    go = null;
                }

                if (go != null)
                {
                    if (hitforward.normal != Vector3.zero)
                    {
                        go.transform.LookAt(hitforward.point + hitforward.normal);
                    }
                    else if (hitback.normal != Vector3.zero)
                    {
                        go.transform.LookAt(hitback.point + hitback.normal);
                    }
                    else if (hitright.normal != Vector3.zero)
                    {
                        go.transform.LookAt(hitright.point + hitright.normal);
                    }
                    else if (hitleft.normal != Vector3.zero)
                    {
                        go.transform.LookAt(hitleft.point + hitleft.normal);
                    }

                    if(UnityEngine.Random.Range(0, 2) == 1)
                    {
                        if(!Physics.Raycast(go.transform.position - go.transform.forward, Vector3.up, out hitUp, 3))
                        {
                            go.transform.Translate(0, 0, 1);
                        }
                    }

                    for(int j = (int)(-box.size.z/2.0f); j < box.size.z/2.0f; j++)
                    {
                        for(int i = (int)(-box.size.x/2.0f); i < box.size.x/2.0f; i++)
                        {
                            Vector3Int mapKey = Vector3Int.RoundToInt(go.transform.position + new Vector3Int(i, 0, j));
                            if (!cityMap.ContainsKey(mapKey))
                            {
                                cityMap.Add(mapKey, pt);
                            }
                        }
                    }
                }
            }
        }

        monsterFiledList.Clear();
        AddFillers(0, ZoneType.R);
        AddFillers(1, ZoneType.C);
        AddFillers(2, ZoneType.I);
        CleanUpDeadEnd();
        UnityEditor.EditorUtility.ClearProgressBar();

    }

    void CleanUpDeadEnd()
    {
        GameObject[] ends = GameObject.FindGameObjectsWithTag("DeadEnd");
        Dictionary<Vector3Int, GameObject> matches = new Dictionary<Vector3Int, GameObject>();

        foreach(GameObject e in ends)
        {
            Vector3Int endPos = Vector3Int.RoundToInt(e.transform.position);
            
            if(!matches.ContainsKey(endPos))
            {
                matches.Add(endPos, e);

            }
            else
            {
                DestroyImmediate(matches[endPos]);
                DestroyImmediate(e);
            }
        }

        cityParent.transform.localScale = Vector3.one * 18;

        navMeshBuilder.NavMeshbuilding();

        startPos.transform.position = monsterFiledList[0].position;

        SetDungeonController();

        SetPlayersPos();
    }

    void AddFillers(int modelID, ZoneType type)
    {
        List<Mesh> meshes = new List<Mesh>();
        List<Vector3> mPosition = new List<Vector3>();
        GameObject go = null;
        Material mat = null;
        PieceType thisPiece = PieceType.None;

        for (int z = minDImensions.z - 10; z < maxDImensions.z + 10; z++)
        {
            for (int x = minDImensions.x - 10; x < maxDImensions.x + 10; x++)
            {
                Vector3Int mapkey = new Vector3Int(x, 0, z);
                if (!cityMap.ContainsKey(mapkey) && IsVornoiType(x, z, type))
                {
                    if (type == ZoneType.R) thisPiece = PieceType.Lawn;
                    else if (type == ZoneType.C) thisPiece = PieceType.Commercial;
                    else if (type == ZoneType.I) thisPiece = PieceType.Industrial;

                    cityMap.Add(mapkey, thisPiece);

                    Vector3 treePos = mapkey + new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f),0,
                        UnityEngine.Random.Range(-0.8f, 0.8f));

                    if(!HitRoad(Vector3Int.RoundToInt(treePos)) && !OutSizeMap(Vector3Int.RoundToInt(treePos)))
                    {
                        float placedTree1 = MeshUtils.fBM(x * 0.006f, z * 0.006f, 5);
                        float placedTree2 = MeshUtils.fBM(x * 0.005f, z * 0.005f, 2);
                        float placedTree3 = MeshUtils.fBM(x * 0.007f, z * 0.007f, 6);
                        float placedTree4 = MeshUtils.fBM(x * 0.004f, z * 0.004f, 3);
                        if (thisPiece == PieceType.Lawn)
                        {
                            if (placedTree1 < 0.4f && UnityEngine.Random.Range(0, 10) < 1)
                            {
                                go = Instantiate(trees[0], treePos, Quaternion.identity);
                                go.transform.localScale *= 1 + Mathf.PerlinNoise(x * 0.005f, z * 0.005f) * 2.0f;

                                go.transform.SetParent(cityParent);
                            }
                            else if (placedTree2 < 0.5f && UnityEngine.Random.Range(0, 10) < 3)
                            {
                                go = Instantiate(trees[1], treePos, Quaternion.identity);
                                go.transform.localScale *= 1 + Mathf.PerlinNoise(x * 0.004f, z * 0.004f) * 3.0f;

                                go.transform.SetParent(cityParent);
                            }
                            else if (placedTree3 < 0.3f && UnityEngine.Random.Range(0, 10) < 5)
                            {
                                go = Instantiate(trees[2], treePos, Quaternion.identity);
                                go.transform.localScale *= 1 + Mathf.PerlinNoise(x * 0.002f, z * 0.002f);

                                go.transform.SetParent(cityParent);
                            }
                            else if (placedTree4 < 0.6f && UnityEngine.Random.Range(0, 10) < 1)
                            {
                                go = Instantiate(trees[3], treePos, Quaternion.identity);
                                go.transform.localScale *= 1 + Mathf.PerlinNoise(x * 0.006f, z * 0.006f) * 4.0f;

                                go.transform.SetParent(cityParent);
                            }
                        }
                    }

                    if(!HitRoad(mapkey))
                    {
                        go = Instantiate(fillers[modelID], mapkey, Quaternion.identity);
                        mat = go.GetComponent<MeshRenderer>().material;
                        MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();

                        go.transform.SetParent(cityParent);

                        foreach (MeshFilter mf in meshFilters)
                        {
                            meshes.Add(mf.sharedMesh);
                            mPosition.Add(mf.transform.position);
                        }

                        DestroyImmediate(go);
                    }
                }
            }
        }

        if (meshes.Count > 0)
        {
            GameObject combineMesh = new GameObject("CombinedMesh");
            List<List<Mesh>> allMeshed = MeshTools.Split(meshes, 1000);
            List<List<Vector3>> allPositions = MeshTools.Split(mPosition, 1000);

            combineMesh.transform.SetParent(cityParent);

            for (int i = 0; i < allMeshed.Count; i++)
            {
                GameObject subMesh = new GameObject("SubMesh_" + i);
                monsterFiledList.Add(subMesh.transform);

                subMesh.transform.parent = combineMesh.transform;
                MeshRenderer mr = subMesh.AddComponent<MeshRenderer>();
                mr.material = mat;
                MeshFilter mf = subMesh.AddComponent<MeshFilter>();
                mf.mesh = MeshTools.MergeMeshes(allMeshed[i], allPositions[i]);
            }
        }
    }

    [ClientRpc]
    public void SetPlayersPos()
    {
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
            {
                conn.identity.transform.position = startPos.transform.position;
            }
        }
    }

    [Command(requiresAuthority = false)]
    void SetDungeonController()
    {
        bool haveBoss = false;

        for (int i = 0; i < monsterFiledList.Count; i++)
        {
            monsterFiledList[i].AddComponent<BoxCollider>();
            if (monsterFiledList[i].GetComponent<BoxCollider>() != null)
            {
                DungeonController conObj = Instantiate(dungeonControllerPrefab, monsterFiledList[i].position, Quaternion.identity);
                DungeonController con = conObj.GetComponent<DungeonController>();

                BoxCollider dstBox = con.GetComponent<BoxCollider>();
                if (dstBox != null)
                {
                    dstBox.center = Vector3.zero;
                    dstBox.size = Vector3.one * 10;
                }

                if (i == 0)
                {
                    con.roomType = RoomType.Start;
                }

                if (haveBoss == false && monsterFiledList[i] == monsterFiledList.Last())
                {
                    con.SetRoomBoss();
                    haveBoss = true;
                }

                GameObject sp = new GameObject("SpawnPoint");

                sp.transform.SetParent(con.transform);
                sp.transform.localPosition = Vector3.zero;
                con.spawnPoints.Add(sp.transform);

                //라운드용 이벤트 추가
                con.OnEndCombat += RoundManager.Instance.ClearRoom;

                NetworkServer.Spawn(conObj.gameObject);
                dungeonControllers.Add(con);

                Debug.Log($"던전 컨트롤러 추가 + {i}");

            }
        }
    }
}
