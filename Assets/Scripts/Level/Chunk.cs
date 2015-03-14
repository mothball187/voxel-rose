//Chunk.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class Chunk : MonoBehaviour {

	public static int chunkSize = 16;
	public Block[ , , ] blocks = new Block[chunkSize, chunkSize, chunkSize];
	public bool update = false;
	public World world;
	public WorldPos pos;
	public bool rendered = false;
	public bool renderTex = true;
	public bool includeCol = true;

	MeshFilter filter;
	MeshCollider coll;

	//Use this for initialization
	void Start () {
		
		filter = gameObject.GetComponent<MeshFilter>();
		coll = gameObject.GetComponent<MeshCollider>();
	}
	
	//Update is called once per frame
	void Update () {
		if (update)
		{
			update = false;
			UpdateChunk();
		}
	}
	
	public Block GetBlock(int x, int y, int z)
	{
		if(InRange(x) && InRange(y) && InRange(z))
			return blocks[x, y, z];
		return world.GetBlock(pos.x + x, pos.y + y, pos.z + z);
	}

	public static bool InRange(int index)
	{
		if (index < 0 || index >= chunkSize)
			return false;
		
		return true;
	}

	public void SetBlock(int x, int y, int z, int block)
	{
		Block setblock;
		switch (block) {
			case (int)Terrain.BlockType.Grass:
				setblock = new BlockGrass();
				break;
			case (int)Terrain.BlockType.Rock:
				setblock = new Block();
				break;
			default:
				setblock = new BlockAir();
				break;
		}
		if (InRange(x) && InRange(y) && InRange(z))
		{
			blocks[x, y, z] = setblock;
		}
		/*
		else
		{
			world.SetBlock(pos.x + x, pos.y + y, pos.z + z, block);
		}
		*/
	}
	
	// Updates the chunk based on its contents
	void UpdateChunk()
	{
		rendered = true;
		MeshData meshData = new MeshData();
		
		for (int x = 0; x < chunkSize; x++)
		{
			for (int y = 0; y < chunkSize; y++)
			{
				for (int z = 0; z < chunkSize; z++)
				{
					meshData = blocks[x, y, z].Blockdata(this, x, y, z, meshData);
				}
			}
		}
		
		RenderMesh(meshData);
	}
	
	// Sends the calculated mesh information
	// to the mesh and collision components
	void RenderMesh(MeshData meshData)
	{
		if (renderTex) {
			filter.mesh.Clear();
			filter.mesh.vertices = meshData.vertices.ToArray ();
			filter.mesh.triangles = meshData.triangles.ToArray ();
			filter.mesh.uv = meshData.uv.ToArray ();
			filter.mesh.RecalculateNormals ();
		}

		coll.sharedMesh = null;

		if (includeCol) {
			Mesh mesh = new Mesh ();
			mesh.vertices = meshData.colVertices.ToArray ();
			mesh.triangles = meshData.colTriangles.ToArray ();
			mesh.RecalculateNormals ();
			coll.sharedMesh = mesh;
		}
	}

	public void SetBlocksUnmodified()
	{
		foreach (Block block in blocks)
		{
			block.changed = false;
		}
	}

}
