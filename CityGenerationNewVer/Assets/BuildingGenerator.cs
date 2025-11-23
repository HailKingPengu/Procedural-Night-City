using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public struct SerializableMeshArray
{
    public Mesh[] meshes;
}

public class BuildingGenerator : MonoBehaviour
{

    [SerializeField]
    MeshRenderer meshR;
    [SerializeField]
    MeshFilter meshF;

    [SerializeField]
    Mesh[] possibleMeshes;
    [SerializeField]
    SerializableMeshArray[] possibleRoofMeshes;
    [SerializeField]
    Material[] possibleMaterials;

    Mesh mesh;
    Mesh roofMesh;

    Vector3[] mainShape;

    [SerializeField]
    int sideTowers;

    public float plotWidth;
    public float plotLength;

    float height;
    float width;
    float length;

    float originalWidth;
    float originalLength;
    float originalHeight;

    bool billboards = true;

    [SerializeField]
    float minHeight;
    [SerializeField]
    float maxHeight;

    [SerializeField]
    float textureScale;

    [SerializeField]
    float billboardOffset;

    [SerializeField]
    bool generate = false;

    // Start is called before the first frame update
    public void Generate(float plotWidth, float plotLength, bool billboard, float minheight, float maxheight, int sideTowers)
    {

        this.plotWidth = plotWidth;
        this.plotLength = plotLength;
        billboards = billboard;
        this.minHeight = minheight;
        this.maxHeight = maxheight;
        this.sideTowers = sideTowers;

        height = Random.Range(minHeight, maxHeight);

        int meshType = Random.Range(0, 3);

        mesh = possibleMeshes[meshType];
        roofMesh = possibleRoofMeshes[meshType].meshes[Random.Range(0, possibleRoofMeshes[meshType].meshes.Length)];


        List<Vector3> roofMeshVerts = new List<Vector3>();
        List<Vector2> roofMeshUVs = roofMesh.uv.ToList();
        List<int> roofMeshTris = new List<int>();
        List<Vector3> roofMeshNormals = roofMesh.normals.ToList();

        List<Vector3> tempRoofVerts = roofMesh.vertices.ToList();
        List<int> tempRoofTris = roofMesh.triangles.ToList();


        List<Vector3> meshVerts = new List<Vector3>();
        List<Vector2> meshUVs = new List<Vector2>();
        List<int> meshTris = mesh.triangles.ToList();
        List<Vector3> meshNormals = mesh.normals.ToList();

        List<Vector3> tempVerts = mesh.vertices.ToList();

        length = Random.Range(0.5f, 0.8f) * plotLength;
        width = Random.Range(0.5f, 0.8f) * plotWidth;
        originalLength = length;
        originalWidth = width;
        originalHeight = height;

        for (int i = 0; i < tempVerts.Count; i++)
        {
            //for some reason Vector3.Scale stopped working so switched to this
            //tempVerts[i].Scale(new Vector3(1, height, depth));
            tempVerts[i] = new Vector3(tempVerts[i].x * width, tempVerts[i].y * height, tempVerts[i].z * length);
        }

        for (int i = 0; i < tempRoofVerts.Count; i++)
        {
            tempRoofVerts[i] = new Vector3(tempRoofVerts[i].x * width, tempRoofVerts[i].y + height, tempRoofVerts[i].z * length);
        }

        roofMeshVerts.AddRange(tempRoofVerts);



        List<Vector2> tempUVs = mesh.uv.ToList();

        for (int i = 0; i < tempUVs.Count; i++)
        {
            tempUVs[i] = new Vector2(tempUVs[i].x * textureScale, tempUVs[i].y * textureScale * height);
        }

        meshVerts = new List<Vector3>();
        meshUVs = new List<Vector2>();

        meshVerts.AddRange(tempVerts);
        meshUVs.AddRange(tempUVs);

        for (int i = 0; i < sideTowers; i++)
        {
            tempVerts = mesh.vertices.ToList();

            float multiplier = (0.5f * i + 1.2f);

            height = Random.Range(minHeight / multiplier, maxHeight / multiplier);

            length = Random.Range(0.6f / multiplier, 0.8f / multiplier);

            width = Random.Range(0.8f / multiplier, 1.2f / multiplier);

            Vector3 offset = new Vector3(Random.Range(-0.5f * originalWidth, 0.5f * originalWidth), 0, Random.Range(-0.5f * originalLength, 0.5f * originalLength));

            for (int j = 0; j < tempVerts.Count; j++)
            {
                tempVerts[j] = new Vector3(tempVerts[j].x * width, tempVerts[j].y * height, tempVerts[j].z * length);
                //if (mirrored)
                //{
                //    tempVerts[j] = new Vector3(Mathf.Max(tempVerts[j].x, 0), tempVerts[j].y, tempVerts[j].z);
                //}
                tempVerts[j] += offset;
            }

            tempUVs = mesh.uv.ToList();

            for (int j = 0; j < tempUVs.Count; j++)
            {
                tempUVs[j] = new Vector2(tempUVs[j].x * textureScale * length * width, tempUVs[j].y * textureScale * height);
            }

            for(int j = 0; j < mesh.triangles.Length; j++)
            {
                meshTris.Add(mesh.triangles[j] + meshVerts.Count);
            }

            meshVerts.AddRange(tempVerts);
            meshUVs.AddRange(tempUVs);
            meshTris.AddRange(mesh.triangles.ToList());
            meshNormals.AddRange(mesh.normals.ToList());


            roofMesh = possibleRoofMeshes[meshType].meshes[Random.Range(0, possibleRoofMeshes[meshType].meshes.Length)];
            tempRoofVerts = roofMesh.vertices.ToList();

            for (int j = 0; j < tempRoofVerts.Count; j++)
            {
                tempRoofVerts[j] = new Vector3(tempRoofVerts[j].x * width, tempRoofVerts[j].y + height, tempRoofVerts[j].z * length);
                tempRoofVerts[j] += offset;
            }

            for (int j = 0; j < roofMesh.triangles.Length; j++)
            {
                tempRoofTris.Add(roofMesh.triangles[j] + roofMeshVerts.Count);
            }

            roofMeshVerts.AddRange(tempRoofVerts);
            roofMeshUVs.AddRange(roofMesh.uv.ToList());
            roofMeshNormals.AddRange(roofMesh.normals.ToList());


            //Debug.Log(tempVerts.Count);
            //Debug.Log(tempNormals.Count);

            //if (mirrored)
            //{
            //    for (int j = 0; j < tempVerts.Count; j++)
            //    {
            //        tempVerts[j] = new Vector3(-tempVerts[j].x, tempVerts[j].y, tempVerts[j].z);
            //    }

            //    for (int j = 0; j < mesh.triangles.Length; j += 3)
            //    {
            //        //flipping the triangles in the mirrored mesh

            //        meshTris.Add(mesh.triangles[j] + meshVerts.Count);
            //        meshTris.Add(mesh.triangles[j+2] + meshVerts.Count);
            //        meshTris.Add(mesh.triangles[j+1] + meshVerts.Count);
            //    }

            //    meshVerts.AddRange(tempVerts);
            //    meshUVs.AddRange(tempUVs);
            //    meshTris.AddRange(mesh.triangles.ToList());
            //    meshNormals.AddRange(mesh.normals.ToList());

            //}

        }

        //adding an underlay for the building
        tempRoofTris.Add(roofMeshVerts.Count); tempRoofTris.Add(roofMeshVerts.Count + 1); tempRoofTris.Add(roofMeshVerts.Count + 2);
        tempRoofTris.Add(roofMeshVerts.Count); tempRoofTris.Add(roofMeshVerts.Count + 2); tempRoofTris.Add(roofMeshVerts.Count + 3);

        roofMeshVerts.Add(new Vector3(plotWidth / 2, 0, plotLength / 2));
        roofMeshVerts.Add(new Vector3(plotWidth / 2, 0, -plotLength / 2));
        roofMeshVerts.Add(new Vector3(-plotWidth / 2, 0, -plotLength / 2));
        roofMeshVerts.Add(new Vector3(-plotWidth / 2, 0, plotLength / 2));

        roofMeshUVs.Add(new Vector2(plotWidth / 2, plotLength / 2));
        roofMeshUVs.Add(new Vector2(plotWidth / 2, -plotLength / 2));
        roofMeshUVs.Add(new Vector2(-plotWidth / 2, -plotLength / 2));
        roofMeshUVs.Add(new Vector2(-plotWidth / 2, plotLength / 2));

        roofMeshNormals.Add(Vector3.up); roofMeshNormals.Add(Vector3.up);
        roofMeshNormals.Add(Vector3.up); roofMeshNormals.Add(Vector3.up);


        //adding roof data to the mesh

        for (int j = 0; j < tempRoofTris.Count; j++)
        {
            roofMeshTris.Add(tempRoofTris[j] + meshVerts.Count);
        }

        meshVerts.AddRange(roofMeshVerts);
        meshUVs.AddRange(roofMeshUVs);
        meshNormals.AddRange(roofMeshNormals);



        //adding billboards

        List<int> boardtris = new List<int>();

        for (int i = 0; i < 3; i++)
        {
            int indexOffset = Random.Range(0, possibleMeshes[meshType].vertices.Length / 4);

            //Debug.Log(indexOffset);

            int vertOffset = indexOffset * 4;
            int trisOffset = indexOffset * 6;

            boardtris.Add(meshVerts.Count + 0); boardtris.Add(meshVerts.Count + 1); boardtris.Add(meshVerts.Count + 2);
            boardtris.Add(meshVerts.Count + 0); boardtris.Add(meshVerts.Count + 2); boardtris.Add(meshVerts.Count + 3);

            //Debug.Log(meshVerts.Count + roofMeshVerts.Count + 0);

            Vector3 billboardPoint = meshNormals[vertOffset + 0] * billboardOffset;

            float highestPoint = 0;

            for (int j = 0; j < 4; j++)
            {
                highestPoint = Mathf.Max(meshVerts[vertOffset + i].y, highestPoint);
            }

            float billboardHeight = Random.Range(highestPoint * 0.3f, highestPoint * 0.8f);

            meshVerts.Add(new Vector3(meshVerts[vertOffset + 0].x, Mathf.Max(meshVerts[vertOffset + 0].y, billboardHeight), meshVerts[vertOffset + 0].z) + billboardPoint);
            meshVerts.Add(new Vector3(meshVerts[vertOffset + 1].x, Mathf.Max(meshVerts[vertOffset + 1].y, billboardHeight), meshVerts[vertOffset + 1].z) + billboardPoint);
            meshVerts.Add(new Vector3(meshVerts[vertOffset + 2].x, Mathf.Max(meshVerts[vertOffset + 2].y, billboardHeight), meshVerts[vertOffset + 2].z) + billboardPoint);
            meshVerts.Add(new Vector3(meshVerts[vertOffset + 3].x, Mathf.Max(meshVerts[vertOffset + 3].y, billboardHeight), meshVerts[vertOffset + 3].z) + billboardPoint);

            //meshUVs.Add(meshUVs[vertOffset + 0].normalized); meshUVs.Add(meshUVs[vertOffset + 1].normalized); meshUVs.Add(meshUVs[vertOffset + 2].normalized); meshUVs.Add(meshUVs[vertOffset + 3].normalized);

            Vector2 uvOffset = new Vector2(Random.Range(0, 2000), Random.Range(0, 2000));

            //height should be originalHeight for correct application of the shader... but the messed up version with height looks better.
            meshUVs.Add(new Vector2(uvOffset.x, uvOffset.y + height - billboardHeight)); meshUVs.Add(new Vector2(uvOffset.x + 1, uvOffset.y + height - billboardHeight)); meshUVs.Add(new Vector2(uvOffset.x + 1, uvOffset.y)); meshUVs.Add(new Vector2(uvOffset.x, uvOffset.y));

            meshNormals.Add(meshNormals[vertOffset + 0]); meshNormals.Add(meshNormals[vertOffset + 0]); meshNormals.Add(meshNormals[vertOffset + 0]); meshNormals.Add(meshNormals[vertOffset + 0]);
        }


        mesh = new Mesh();

        mesh.subMeshCount = 3;
        mesh.vertices = meshVerts.ToArray();
        mesh.uv = meshUVs.ToArray();
        mesh.SetTriangles(meshTris, 0);
        mesh.SetTriangles(roofMeshTris, 1);
        mesh.SetTriangles(boardtris, 2);
        mesh.normals = meshNormals.ToArray();

        meshF.mesh = mesh;

        Material[] mats = meshR.sharedMaterials;
        mats[0] = possibleMaterials[Random.Range(0, possibleMaterials.Length)];
        meshR.sharedMaterials = mats;

        //Debug.Log(mesh.vertices.Length);

    }

    // Update is called once per frame
    void Update()
    {
        //uncomment for BuildingTester scene functionality
        if (generate)
        {
            generate = false;
            Generate(plotWidth, plotLength, billboards, minHeight, maxHeight, sideTowers);
        }
    }
}
