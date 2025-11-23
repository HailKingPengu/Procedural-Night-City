using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Node
{
    public int id;
    public Vector2 location;
    public List<int> connections;

    public Vector2 direction;
    public bool active;

    public Node()
    {

    }

    public Node(int id, Vector2 location, int connection, Vector2 direction)
    {
        this.id = id;
        this.location = location;
        connections = new List<int>();
        this.connections.Add(connection);

        //direction = new Vector2(Random.Range(0,1), Random.Range(0,1));
        this.direction = direction;
        active = true;
    }

    //public Node(int id, Vector2 location, int[] connections, Vector2 direction, float influence)
    //{
    //    this.id = id;
    //    this.location = location;
    //    this.connections = connections.ToList();

    //    this.direction = new Vector2(Random.Range(0, 1), Random.Range(0, 1));
    //    priority = false;
    //}

    public void Deactivate()
    {
        active = false;
    }

    public void EditConnections(int newId)
    {
        if (!connections.Contains(newId))
        {
            connections.Add(newId);
        }
    }

    public void EditConnections()
    {
        connections.RemoveAt(connections.Count - 1);
    }
}

public class RoadGen : MonoBehaviour
{
    [SerializeField]
    int updateSteps;

    [SerializeField]
    int maxNodes;

    [SerializeField]
    float stepSize;

    [SerializeField]
    float mutationChance;

    [SerializeField]
    bool step;

    [SerializeField, ReadOnly(true)]
    int numNodes;
    [SerializeField, ReadOnly(true)]
    int inactiveNodes;

    [SerializeField]
    bool debug;

    int lastNumNodes;
    int iterationsInactive;
    bool generationInactive = false;

    List<Node> nodes;

    List<bool> activeNodes;

    [SerializeField]
    bool regenerate;

    [Header("Road Generation Settings")]
    [SerializeField]
    float roadCurve;

    [SerializeField]
    float cornerCurve;

    [SerializeField]
    float maxCitySize;

    [Header("Road Mesh Settings")]
    [SerializeField]
    float roadWidth;

    [SerializeField]
    Mesh mesh;
    [SerializeField]
    MeshFilter meshFilter;
    [SerializeField]
    MeshRenderer meshRenderer;

    List<string> labels;
    List<Vector2> labelSpots;

    List<Vector2> nodeLocations;

    [Header("Building Spawn Settings")]

    [SerializeField]
    GameObject tempVisualiser;

    [SerializeField]
    int buildingsVisualised;

    [SerializeField]
    float buildingsMaxHeight = 11;

    [SerializeField]
    float buildingsMinHeight = 6;

    [SerializeField]
    float heightFalloffSpeed = 50;


    void Start()
    {
        nodes = new List<Node>();
        activeNodes = new List<bool>();

        Node newNode = new Node(0, new Vector2(0, 0), 0, Random.insideUnitCircle);
        nodes.Add(newNode);
        activeNodes.Add(true);


        labels = new List<string>();
        labelSpots = new List<Vector2>();

        nodeLocations = new List<Vector2>();
    }

    void Update()
    {

        int iterations = 0;

        while (iterations < updateSteps)
        {

            if (nodes.Count < maxNodes)
            {

                //step = false;

                for (int i = nodes.Count - 1; i > -1; i--)
                {

                    if (activeNodes[i] == true)
                    {
                        Node node = nodes[i];
                        if (node.connections.Count == 1 && Vector2.Distance(Vector2.zero, node.location) < maxCitySize)
                        {
                            Node newNode = new Node();
                            if (roadCurve > 0)
                            {
                                newNode = new Node(nodes.Count, node.location + node.direction.normalized * stepSize, node.id, (node.direction + (Random.insideUnitCircle * roadCurve)).normalized * stepSize);
                            }
                            else
                            {
                                newNode = new Node(nodes.Count, node.location + node.direction.normalized * stepSize, node.id, node.direction * stepSize);
                            }

                            nodes[i].EditConnections(newNode.id);
                            nodes.Add(newNode);
                            activeNodes.Add(true);

                            //less likely to mutate the farther from center of city
                            //disallowed to mutate if last node junction
                            if (Random.Range(0f, 100f) < mutationChance - Vector2.Distance(Vector2.zero, node.location) / 30 && nodes[nodes[node.connections[0]].connections[0]].connections.Count <= 2)
                            {

                                Vector2 direction;

                                if (Random.Range(0, 2) == 0)
                                {
                                    direction = new Vector2(-node.direction.y, node.direction.x);
                                }
                                else
                                {
                                    direction = new Vector2(node.direction.y, -node.direction.x);
                                }

                                Node newNode2 = new Node(nodes.Count, node.location + direction * stepSize, node.id, (direction + Random.insideUnitCircle * cornerCurve).normalized * stepSize);
                                nodes[i].EditConnections(newNode2.id);
                                nodes.Add(newNode2);
                                activeNodes.Add(true);

                                float[] closeData = GetClosestNodeID(newNode);
                                if (closeData[1] < stepSize * 0.8f && closeData[0] != node.id && closeData[0] != newNode2.id && closeData[0] < newNode.id - nodes.Count / 80 /*30*/)
                                {
                                    if (nodes[(int)closeData[0]].connections.Count < 3)
                                    {
                                        nodes[newNode.connections[0]].EditConnections();
                                        nodes[newNode.connections[0]].EditConnections((int)closeData[0]);
                                        nodes[(int)closeData[0]].EditConnections(newNode.connections[0]);

                                        if (debug) Debug.DrawLine(nodes[newNode.connections[0]].location, nodes[(int)closeData[0]].location, UnityEngine.Color.red, 1000);
                                    }
                                    nodes.RemoveAt(nodes.Count - 1);
                                }

                                float[] closeData2 = GetClosestNodeID(newNode2);
                                if (closeData2[1] < stepSize * 0.8f && closeData2[0] != node.id && closeData2[0] != newNode.id && closeData[0] < newNode.id - 5)
                                {
                                    if (nodes[(int)closeData2[0]].connections.Count < 3)
                                    {
                                        nodes[newNode2.connections[0]].EditConnections();
                                        nodes[newNode2.connections[0]].EditConnections((int)closeData[0]);
                                        nodes[(int)closeData[0]].EditConnections(newNode2.connections[0]);

                                        if (debug) Debug.DrawLine(nodes[newNode2.connections[0]].location, nodes[(int)closeData[0]].location, UnityEngine.Color.red, 1000);

                                    }
                                    nodes.RemoveAt(nodes.Count - 1);
                                }

                                if (debug) Debug.DrawLine(node.location, newNode.location, UnityEngine.Color.white, 1000);
                                if (debug) Debug.DrawLine(node.location, newNode2.location, UnityEngine.Color.white, 1000);

                            }
                            else
                            {
                                float[] closeData = GetClosestNodeID(newNode);
                                if (closeData[1] < stepSize * 0.8f && closeData[0] != node.id && closeData[0] < newNode.id - 1)
                                {
                                    //newNode.location = nodes[(int)closeData[0]].location + new Vector2(0.1f, 0);
                                    //newNode.EditConnections((int)closeData[0]);
                                    //nodes[(int)closeData[0]].EditConnections(newNode.id);

                                    if (nodes[(int)closeData[0]].connections.Count < 3)
                                    {
                                        //nodes[newNode.connections[0]].location = (new Vector2(nodes[(int)closeData[0]].direction.y, -nodes[(int)closeData[0]].location.x) * Vector2.Dot(nodes[(int)closeData[0]].location, nodes[newNode.connections[0]].location)).normalized * roadWidth * 2;
                                        nodes[newNode.connections[0]].EditConnections();
                                        nodes[newNode.connections[0]].EditConnections((int)closeData[0]);
                                        nodes[(int)closeData[0]].EditConnections(newNode.connections[0]);

                                        if (debug) Debug.DrawLine(nodes[newNode.connections[0]].location, nodes[(int)closeData[0]].location, UnityEngine.Color.red, 1000);
                                    }
                                    else
                                    {
                                        nodes[newNode.connections[0]].EditConnections();
                                    }
                                    nodes.RemoveAt(nodes.Count - 1);
                                }
                                else
                                {
                                    if (debug) Debug.DrawLine(node.location, newNode.location, UnityEngine.Color.white, 1000);
                                }

                            }
                        }
                        else
                        {
                            activeNodes[i] = false;

                            inactiveNodes++;
                        }
                    }
                }

                numNodes = nodes.Count;

            }

            if(numNodes == lastNumNodes)
            {
                iterationsInactive++;
            }
            else
            {
                iterationsInactive = 0;
            }
            lastNumNodes = numNodes;

            if(iterationsInactive >= 5)
            {
                generationInactive = true;
            }

            iterations++;

        }

        if (regenerate || Input.GetKeyDown(KeyCode.R))
        {
            regenerate = false;
            ReGenerate();
        }

        if (inactiveNodes >= nodes.Count - 1 || numNodes >= maxNodes || generationInactive)
        {
            if (mesh == null || mesh.vertices.Length == 0)
            {

                //mesh generation sometimes breaks, leaving the city without a street mesh.
                //since node generation is fast enough, just regenerate the layout and try again

                try
                {
                    GenerateMesh();
                }
                catch
                {
                    ReGenerate();
                }
            }
        }

        if (inactiveNodes >= nodes.Count - 1 || numNodes >= maxNodes || generationInactive)
        {
            if(buildingsVisualised < nodes.Count && mesh != null)
            {
                GenerateBuildingNodes();
            }
            
        }
    }

    private void OnDrawGizmos()
    {
        if (debug)
        {
            //if (inactiveNodes == nodes.Count)
            {
                for (int i = 0; i < nodes.Count; i++)
                {

                    //Handles.Label(nodes[i].location, nodes[i].connections.Count.ToString());

                    //if (nodes[i].connections.Count > 2)
                    //{
                    //    Gizmos.DrawSphere(nodes[i].location, 1);
                    //}
                }
            }

            for (int i = 0; i < labels.Count; i++)
            {
                //Handles.Label(labelSpots[i], labels[i]);
            }
        }
    }


    float[] GetClosestNodeID(Node from)
    {
        int closestI = -1;
        float shortestDistance = 10000;

        for (int i = 0; i < nodes.Count; i++)
        {

            float currentDistance = Vector2.Distance(nodes[i].location, from.location);

            if (currentDistance < shortestDistance)
            {
                if (i != from.id)
                {
                    closestI = i;
                    shortestDistance = currentDistance;
                }
            }
        }

        return new float[] { closestI, shortestDistance };
    }


    void GenerateMesh()
    {

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].connections.Count == 0)
            {
                //Debug.Log("bruh");
            }
        }

        int uvOffset = 0;

        mesh = new Mesh();



        List<Vector2> verts = new List<Vector2>();
        List<Vector2> uvs = new List<Vector2>();

        //mostly empty, will hold connection points to reference for connecting roads
        Vector2[][] junctionConnectors = new Vector2[nodes.Count][];
        int[][] connectedverts = new int[nodes.Count][];
        int[] uvOffsets = new int[nodes.Count];

        List<int> tris = new List<int>();
        List<int> tris2 = new List<int>();

        int vertCount = 0;

        for (int i = 0; i < nodes.Count; i++)
        {

            if (nodes[i].connections.Count == 1)
            {

                if (uvOffsets[nodes[i].connections[0]] == 0)
                {
                    uvOffset = 1;
                    uvOffsets[i] = 1;
                }
                else
                {
                    uvOffset = 0;
                    uvOffsets[i] = 0;
                }

                connectedverts[i] = (new int[2]);

                Vector2 roadDir = nodes[i].location - nodes[nodes[i].connections[0]].location;
                verts.Add(nodes[i].location + new Vector2(roadDir.y, -roadDir.x).normalized * roadWidth);
                uvs.Add(new Vector2(0, uvOffset));
                connectedverts[i][0] = vertCount;
                vertCount++;

                verts.Add(nodes[i].location + new Vector2(roadDir.y, -roadDir.x).normalized * -roadWidth);
                uvs.Add(new Vector2(1, uvOffset));
                connectedverts[i][1] = vertCount;
                vertCount++;
            }
            if (nodes[i].connections.Count == 2)
            {

                if (uvOffsets[nodes[i].connections[0]] == 0)
                {
                    uvOffset = 1;
                    uvOffsets[i] = 1;
                }
                else
                {
                    uvOffset = 0;
                    uvOffsets[i] = 0;
                }

                connectedverts[i] = (new int[2]);

                //Debug.Log(i);

                //Debug.Log(connectedverts.Count);

                //Debug.Log(connectedverts[i].Length);


                Vector2 roadDir = nodes[nodes[i].connections[1]].location - nodes[nodes[i].connections[0]].location;
                verts.Add(nodes[i].location + new Vector2(roadDir.y, -roadDir.x).normalized * roadWidth);
                uvs.Add(new Vector2(0, uvOffset));
                connectedverts[i][0] = vertCount;
                vertCount++;

                //labels.Add(0.ToString());
                //labelSpots.Add(verts[verts.Count - 1]);

                verts.Add(nodes[i].location + new Vector2(roadDir.y, -roadDir.x).normalized * -roadWidth);
                connectedverts[i][1] = vertCount;
                uvs.Add(new Vector2(1, uvOffset));
                vertCount++;

                //labels.Add(1.ToString());
                //labelSpots.Add(verts[verts.Count - 1]);


                if (uvOffset == 0) { uvOffset = 1; } else { uvOffset = 0; }

                for (int j = 0; j < nodes[i].connections.Count; j++)
                {
                    if (connectedverts[nodes[i].connections[j]] != null)
                    {

                        if (connectedverts[i].Length == 2)
                        {

                            if (connectedverts[nodes[i].connections[j]].Length == 2)
                            {
                                tris.Add(connectedverts[nodes[i].connections[j]][0]);
                                tris.Add(connectedverts[nodes[i].id][0]);
                                tris.Add(connectedverts[nodes[i].connections[j]][1]);
                                tris.Add(connectedverts[nodes[i].id][0]);
                                tris.Add(connectedverts[nodes[i].id][1]);
                                tris.Add(connectedverts[nodes[i].connections[j]][1]);
                            }
                            else if (connectedverts[nodes[i].connections[j]].Length == 1)
                            {
                                tris.Add(connectedverts[nodes[i].id][0]);
                                tris.Add(connectedverts[nodes[i].id][1]);
                                tris.Add(connectedverts[nodes[i].connections[j]][0]);
                            }
                            else if (connectedverts[nodes[i].connections[j]].Length > 2)
                            {

                                int closestPointI0 = 0;
                                float closestDist0 = 1000;
                                int closestPointI1 = 0;
                                float closestDist1 = 1000;

                                for (int k = 0; k < 4; k++)
                                {
                                    if (Vector2.Distance(verts[connectedverts[nodes[i].id][0]], verts[connectedverts[nodes[i].connections[j]][k]]) < closestDist0)
                                    {
                                        closestDist0 = Vector2.Distance(verts[connectedverts[nodes[i].id][0]], verts[connectedverts[nodes[i].connections[j]][k]]);
                                        closestPointI0 = connectedverts[nodes[i].connections[j]][k];
                                    }
                                }

                                for (int k = 0; k < 4; k++)
                                {
                                    if (Vector2.Distance(verts[connectedverts[nodes[i].id][1]], verts[connectedverts[nodes[i].connections[j]][k]]) < closestDist1 && connectedverts[nodes[i].connections[j]][k] != closestPointI0)
                                    {
                                        closestDist1 = Vector2.Distance(verts[connectedverts[nodes[i].id][1]], verts[connectedverts[nodes[i].connections[j]][k]]);
                                        closestPointI1 = connectedverts[nodes[i].connections[j]][k];
                                    }
                                }

                                //
                                //tris.Add(connectedverts[nodes[i].id][1]);
                                //tris.Add(closestPointI0);
                                //tris.Add(connectedverts[nodes[i].id][0]);
                                //tris.Add(closestPointI1);
                                //tris.Add(closestPointI0);
                                //tris.Add(connectedverts[nodes[i].id][1]);

                                verts[connectedverts[nodes[i].id][0]] = verts[closestPointI0];
                                verts[connectedverts[nodes[i].id][1]] = verts[closestPointI1];

                            }
                        }
                    }
                }
            }
            //crossing
            else if (nodes[i].connections.Count == 3 || nodes[i].connections.Count == 4)
            {
                connectedverts[i] = new int[4];
                junctionConnectors[i] = new Vector2[4];

                Vector2 roadDir = nodes[nodes[i].connections[1]].location - nodes[nodes[i].connections[0]].location;
                Vector2 roadDirNorm = roadDir.normalized * roadWidth;
                float diagonalLength = Mathf.Pow((roadWidth * roadWidth * 2), 0.5f);
                Vector3 diagonalPosition3 = Quaternion.Euler(0, 0, 45f) * (roadDir.normalized * diagonalLength);

                //quad
                verts.Add(nodes[i].location + new Vector2(diagonalPosition3.x, diagonalPosition3.y));
                junctionConnectors[i][3] = roadDirNorm + nodes[i].location;
                connectedverts[i][0] = vertCount; vertCount++;

                //labels.Add(0.ToString()); labelSpots.Add(verts[verts.Count - 1]);

                verts.Add(nodes[i].location + new Vector2(-diagonalPosition3.y, diagonalPosition3.x));
                junctionConnectors[i][0] = new Vector2(-roadDirNorm.y, roadDirNorm.x) + nodes[i].location;
                connectedverts[i][1] = vertCount; vertCount++;

                //labels.Add(1.ToString()); labelSpots.Add(verts[verts.Count - 1]);

                verts.Add(nodes[i].location + new Vector2(-diagonalPosition3.x, -diagonalPosition3.y));
                junctionConnectors[i][1] = new Vector2(-roadDirNorm.x, -roadDirNorm.y) + nodes[i].location;
                connectedverts[i][2] = vertCount; vertCount++;

                //labels.Add(2.ToString()); labelSpots.Add(verts[verts.Count - 1]);

                verts.Add(nodes[i].location + new Vector2(diagonalPosition3.y, -diagonalPosition3.x));
                junctionConnectors[i][2] = new Vector2(roadDirNorm.y, -roadDirNorm.x) + nodes[i].location;
                connectedverts[i][3] = vertCount; vertCount++;

                //labels.Add(3.ToString()); labelSpots.Add(verts[verts.Count - 1]);


                //labels.Add(0.ToString());
                //labelSpots.Add(junctionConnectors[i][0]);
                //labels.Add(1.ToString());
                //labelSpots.Add(junctionConnectors[i][1]);
                //labels.Add(2.ToString());
                //labelSpots.Add(junctionConnectors[i][2]);
                //labels.Add(3.ToString());
                //labelSpots.Add(junctionConnectors[i][3]);

                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));
                tris2.Add(connectedverts[i][0]); tris2.Add(connectedverts[i][1]); tris2.Add(connectedverts[i][2]);
                tris2.Add(connectedverts[i][0]); tris2.Add(connectedverts[i][2]); tris2.Add(connectedverts[i][3]);

                for (int j = 0; j < nodes[i].connections.Count; j++)
                {
                    if (connectedverts[nodes[i].connections[j]] != null)
                    {

                        //if connected to a generated road

                        if (connectedverts[nodes[i].connections[j]].Length == 2)
                        {
                            //tris.Add(connectedverts[nodes[i].id][0]);
                            //tris.Add(connectedverts[nodes[i].connections[j]][1]);
                            //tris.Add(connectedverts[nodes[i].connections[j]][0]);

                            int closestPointI0 = 0;
                            float closestDist0 = 1000;
                            int closestPointI1 = 0;
                            float closestDist1 = 1000;

                            for (int k = 0; k < 4; k++)
                            {
                                if (Vector2.Distance(verts[connectedverts[nodes[i].id][k]], verts[connectedverts[nodes[i].connections[j]][0]]) < closestDist0)
                                {
                                    closestDist0 = Vector2.Distance(verts[connectedverts[nodes[i].id][k]], verts[connectedverts[nodes[i].connections[j]][0]]);
                                    closestPointI0 = connectedverts[nodes[i].id][k];
                                }
                            }

                            for (int k = 0; k < 4; k++)
                            {
                                if (Vector2.Distance(verts[connectedverts[nodes[i].id][k]], verts[connectedverts[nodes[i].connections[j]][1]]) < closestDist1)
                                {
                                    closestDist1 = Vector2.Distance(verts[connectedverts[nodes[i].id][k]], verts[connectedverts[nodes[i].connections[j]][1]]);
                                    closestPointI1 = connectedverts[nodes[i].id][k];
                                }
                            }

                            verts[connectedverts[nodes[i].connections[j]][0]] = verts[closestPointI0];
                            verts[connectedverts[nodes[i].connections[j]][1]] = verts[closestPointI1];

                        }

                        //if connected to a junction
                        if (connectedverts[nodes[i].connections[j]].Length > 2)
                        {

                            //find the closest combination of verts
                            //its not entirely reliable, sadly

                            int closestPointI = 0;
                            int closestPointTheirsI = 0;
                            float closestDist = 1000;


                            for (int k = 0; k < 4; k++)
                            {
                                for (int l = 0; l < 4; l++)
                                {
                                    if (Vector2.Distance(junctionConnectors[i][k], junctionConnectors[nodes[i].connections[j]][l]) < closestDist)
                                    {
                                        closestDist = Vector2.Distance(junctionConnectors[i][k], junctionConnectors[nodes[i].connections[j]][l]);
                                        closestPointI = k;
                                        closestPointTheirsI = l;

                                    }
                                }
                            }

                            //because the junctions are always constructed in a clockwise fashion, we don't have to resort to any madness to construct the triangles.
                            //we do make a new set of verts because we can't fix the UV if we want both the road and the junction to show up correctly.

                            int corner1 = connectedverts[i][closestPointI];
                            int corner2 = 0;

                            if (closestPointI == 3) corner2 = connectedverts[i][0];
                            else corner2 = connectedverts[i][closestPointI + 1];

                            int corner3 = connectedverts[nodes[i].connections[j]][closestPointTheirsI];
                            int corner4 = 0;

                            if (closestPointTheirsI == 3) corner4 = connectedverts[nodes[i].connections[j]][0];
                            else corner4 = connectedverts[nodes[i].connections[j]][closestPointTheirsI + 1];

                            //quad
                            verts.Add(verts[corner1]);
                            verts.Add(verts[corner2]);
                            verts.Add(verts[corner3]);
                            verts.Add(verts[corner4]);

                            labels.Add(0.ToString());
                            labelSpots.Add(verts[corner1]);
                            labels.Add(1.ToString());
                            labelSpots.Add(verts[corner2]);
                            labels.Add(2.ToString());
                            labelSpots.Add(verts[corner3]);
                            labels.Add(3.ToString());
                            labelSpots.Add(verts[corner4]);

                            vertCount += 4;

                            uvs.Add(new Vector2(0, 1));
                            uvs.Add(new Vector2(1, 1));
                            uvs.Add(new Vector2(1, 0));
                            uvs.Add(new Vector2(0, 0));

                            //Debug.Log(tris.Count);

                            tris.Add(vertCount - 4); tris.Add(vertCount - 2); tris.Add(vertCount - 3);
                            tris.Add(vertCount - 4); tris.Add(vertCount - 1); tris.Add(vertCount - 2);

                            //Debug.Log(tris.Count);

                        }
                    }
                }
            }

        }

        Vector3[] verts3 = new Vector3[verts.Count];
        for (int i = 0; i < verts.Count; i++)
        {
            verts3[i] = verts[i];

            //Debug.Log(verts[i]);
            //Debug.Log(verts3[i]);
        }

        mesh.indexFormat = IndexFormat.UInt32;

        mesh.subMeshCount = 2;

        mesh.vertices = verts3.ToArray();
        mesh.SetTriangles(tris, 0);
        mesh.SetTriangles(tris2, 1);
        mesh.uv = uvs.ToArray();


        //mesh.UploadMeshData(false);

        //foreach(int i in mesh.triangles)
        //{
        //    Debug.Log(i);
        //}

        meshFilter.sharedMesh = mesh;

        transform.Rotate(Vector3.right, -90);
        //transform.localScale = Vector3.one * 50;

    }

    void GenerateBuildingNodes()
    {

        int thisStepSize = buildingsVisualised + updateSteps;

        for (int i = buildingsVisualised; i < thisStepSize && i < nodes.Count; i++)
        {
            if (nodes[i].connections.Count > 1)
            {

                Quaternion nodeAngle = Quaternion.Euler(0, Quaternion.FromToRotation(Vector2.up, new Vector2(nodes[i].direction.y, -nodes[i].direction.x)).eulerAngles.z, 0);

                if (TrySize(3, nodes[i], false))
                {
                    spawnBuilding(i, nodeAngle, 2.5f, 2f);
                }
                else if (TrySize(2f, nodes[i], false))
                {
                    spawnBuilding(i, nodeAngle, 1.75f, 1.5f);
                }
                else if (TrySize(1f, nodes[i], false))
                {
                    spawnBuilding(i, nodeAngle, 0.75f, 1f);
                }



                if (TrySize(3, nodes[i], true))
                {
                    spawnBuilding(i, nodeAngle, 2.5f/*2.25f*/, -2f);
                }
                else if (TrySize(2f, nodes[i], true))
                {
                    spawnBuilding(i, nodeAngle, 1.75f, -1.5f);
                }
                else if (TrySize(1f, nodes[i], true))
                {
                    spawnBuilding(i, nodeAngle, 0.75f, -1f);
                }




                //if (shortestRoadDistance > roadWidth * 2.3 && shortestBuildingDistance > stepSize * 0.9f)
                //{
                //    GameObject temp1 = Instantiate(tempVisualiser, nodes[i].location + new Vector2(nodes[i].direction.y, -nodes[i].direction.x).normalized * roadWidth * 2.5f, Quaternion.identity);
                //    nodeLocations.Add(location);
                //    temp1.transform.RotateAround(Vector3.zero, Vector3.right, -90);
                //    temp1.transform.rotation = nodeAngle;
                //    temp1.transform.localScale = new Vector3(1.8f, Random.Range(20, 30), 3);
                //}


                //location = nodes[i].location - new Vector2(nodes[i].direction.y, -nodes[i].direction.x).normalized * roadWidth * 2.5f;

                //shortestRoadDistance = 10000;

                //for (int j = 0; j < nodes.Count; j++)
                //{
                //    float currentDistance = Vector2.Distance(nodes[j].location, location);
                //    if (currentDistance < shortestRoadDistance) shortestRoadDistance = currentDistance;
                //}

                //for (int j = 0; j < nodeLocations.Count; j++)
                //{
                //    float currentDistance = Vector2.Distance(nodeLocations[j], location);
                //    if (currentDistance < shortestBuildingDistance) shortestBuildingDistance = currentDistance;
                //}

                //if (shortestRoadDistance > roadWidth * 2.3 && shortestBuildingDistance > stepSize * 0.9f)
                //{
                //    GameObject temp2 = Instantiate(tempVisualiser, nodes[i].location - new Vector2(nodes[i].direction.y, -nodes[i].direction.x).normalized * roadWidth * 2.5f, Quaternion.identity);
                //    nodeLocations.Add(location);
                //    temp2.transform.RotateAround(Vector3.zero, Vector3.right, -90);
                //    temp2.transform.rotation = nodeAngle;
                //    temp2.transform.Rotate(Vector3.up, 180);
                //    temp2.transform.localScale = new Vector3(1.8f, Random.Range(20, 30), 3);
                //    //nodes[i].location + new Vector2(nodes[i].direction.y, -nodes[i].direction.x);
                //}
            }

            buildingsVisualised++;

        }
    }

    bool TrySize(float length, Node node, bool flipped)
    {

        Vector2 location;
        Vector2 roadLocation;

        roadWidth = 1;

        if (flipped)
        {
            location = node.location - new Vector2(node.direction.y, -node.direction.x).normalized * roadWidth * length;
            roadLocation = node.location - new Vector2(node.direction.y, -node.direction.x).normalized * roadWidth * length;
        }
        else
        {
            location = node.location + new Vector2(node.direction.y, -node.direction.x).normalized * roadWidth * length;
            roadLocation = node.location + new Vector2(node.direction.y, -node.direction.x).normalized * roadWidth * length;
        }

        float shortestRoadDistance = 10000;
        float shortestRoadDistance2 = 10000;
        float shortestBuildingDistance = 10000;

        //making sure they don't overlap any other roads
        for (int j = 0; j < nodes.Count; j++)
        {
            float currentDistance = Vector2.Distance(nodes[j].location, location);
            if (currentDistance < shortestRoadDistance) shortestRoadDistance = currentDistance;

            ////second check to make sure small buildings dont squeeze through the gaps at junctions
            //currentDistance = Vector2.Distance(nodes[j].location, roadLocation);
            //if (currentDistance < shortestRoadDistance2) shortestRoadDistance2 = currentDistance;
        }

        //and that they dont overlap TOO much with the other buildings
        for (int j = 0; j < nodeLocations.Count; j++)
        {
            float currentDistance = Vector2.Distance(nodeLocations[j], location);
            if (currentDistance < shortestBuildingDistance) shortestBuildingDistance = currentDistance;
        }

        if (shortestRoadDistance > roadWidth * length * 0.95f && shortestBuildingDistance > stepSize * 0.9f && shortestRoadDistance2 > roadWidth * 2.3f)
        {
            return true;
        }
        return false;

    }

    void spawnBuilding(int nodeIndex, Quaternion nodeAngle, float size, float roadDistance)
    {
        Vector2 location = nodes[nodeIndex].location + new Vector2(nodes[nodeIndex].direction.y, -nodes[nodeIndex].direction.x).normalized * roadWidth * roadDistance;

        GameObject temp1 = Instantiate(tempVisualiser, location, Quaternion.identity);
        BuildingGenerator buildingGen1 = temp1.GetComponent<BuildingGenerator>();

        float distCenter = Vector2.Distance(location, Vector2.zero);
        float minHeight = buildingsMinHeight + ((-buildingsMinHeight * 0.9f * distCenter) / (heightFalloffSpeed / 2 + distCenter));
        float maxHeight = buildingsMaxHeight + ((-buildingsMaxHeight * 0.8f * distCenter) / (heightFalloffSpeed * 1.5f + distCenter));

        bool billboards = false;

        if(Random.Range(minHeight, maxHeight) > 3)
        {
            billboards = true;
        }

        buildingGen1.Generate(stepSize * 0.9f, size, billboards, minHeight, maxHeight, Random.Range(2, 5));
        nodeLocations.Add(location);
        temp1.transform.RotateAround(Vector3.zero, Vector3.right, -90);
        temp1.transform.rotation = nodeAngle;

        temp1.transform.SetParent(transform, true);
        //temp1.transform.localScale = new Vector3(stepSize * 0.9f, Random.Range(1, 2), size);
    }

    void ReGenerate()
    {
        nodes.Clear();
        activeNodes.Clear();
        mesh.Clear();
        mesh = null;

        numNodes = 0;
        inactiveNodes = 0;

        labels.Clear();
        labelSpots.Clear();
        nodeLocations.Clear();

        foreach(Transform child in transform)
        {
            Destroy(child.gameObject);
        }


        //to kickstart generation

        Node newNode = new Node(0, new Vector2(0, 0), 0, Random.insideUnitCircle);
        nodes.Add(newNode);
        activeNodes.Add(true);

    }
}
