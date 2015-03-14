using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;


public class World : MonoBehaviour {

	public Dictionary<WorldPos, Chunk> chunks = new Dictionary<WorldPos, Chunk>();

	//2 level dictionary, upper level worldpos for chunks, lower level for blocks
	public Dictionary<WorldPos, Dictionary<WorldPos, int>> moddedBlocks = new Dictionary<WorldPos, Dictionary<WorldPos, int>>();

	public GameObject chunkPrefab;
	public string worldName = "world";
	public Spawn spawn;
	public Player owner;
	private string sentinelSTOP = "!@#$";
	private string modBlockBuf = "";

	// Use this for initialization
	void Start () {
		/*
		for (int x = -4; x < 4; x++)
		{
			for (int y = -1; y < 3; y++)
			{
				for (int z = -4; z < 4; z++)
				{
					CreateChunk(x * 16, y * 16, z * 16);
				}
			}
		}
		*/
	}
	
	// Update is called once per frame
	void Update () {
	

	}

	public void CreateChunk(int x, int y, int z)
	{
		WorldPos worldPos = new WorldPos(x, y , z);
		
		//Instantiate the chunk at the coordinates using the chunk prefab
		GameObject newChunkObject = Instantiate(
			chunkPrefab, new Vector3(x, y, z),
			Quaternion.Euler(Vector3.zero)
			) as GameObject;
		
		Chunk newChunk = newChunkObject.GetComponent<Chunk>();
		
		newChunk.pos = worldPos;
		newChunk.world = this;
		
		//Add it to the chunks dictionary with the position as the key
		chunks.Add(worldPos, newChunk);

		var terrainGen = new TerrainGen();
		newChunk = terrainGen.ChunkGen(newChunk);

		//for now, we will always save all blocks modified since fresh map creation. may want to change this in the future..
		newChunk.SetBlocksUnmodified();

		//handle modified blocks
		Dictionary<WorldPos, int> modBlocks;
		//does this chunk have modified blocks?
		if(moddedBlocks.TryGetValue(worldPos, out modBlocks) == true){
			List<WorldPos> keyList = new List<WorldPos>(modBlocks.Keys);
			foreach(WorldPos key in keyList)
			{
				SetBlock(key.x, key.y, key.z, modBlocks[key]);
				//networkView.RPC ("SetBlock", RPCMode.Others, modBlock.Key.x, modBlock.Key.y, modBlock.Key.z, modBlock.Value);
			}
		}

		//TODO: handle periodic serialization of modded blocks as server
		//Serialization.LoadChunk(newChunk);

	}

	[RPC]
	public void GetModBlocks(Vector3 chunkPos, NetworkMessageInfo info){
		//handle modified blocks
		Dictionary<WorldPos, int> modBlocks;
		//does this chunk have modified blocks?
		if(moddedBlocks.TryGetValue(new WorldPos((int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z), out modBlocks) == true){
			MemoryStream o = new MemoryStream();
			IFormatter formatter = new BinaryFormatter();
			formatter.Serialize(o, modBlocks);
			string buf = System.Convert.ToBase64String(o.GetBuffer());
			int size = buf.Length;
			int sent = 0;
			//max string size for RPC is 4k
			while(sent < size){
				int send = Mathf.Min(size - sent, 2048);
				string subbuf = buf.Substring(sent, send);
				networkView.RPC("SetModBlocks", info.sender, subbuf);
				sent += send;
			}
			networkView.RPC("SetModBlocks", info.sender, sentinelSTOP);
		}
	}

	[RPC]
	public void SetModBlocks(string moddedBlocksData){

		if (moddedBlocksData != sentinelSTOP) {
			modBlockBuf += moddedBlocksData;
		} 
		else {
			IFormatter formatter = new BinaryFormatter ();
			MemoryStream o = new MemoryStream (System.Convert.FromBase64String (modBlockBuf));
			Dictionary<WorldPos, int> modBlocks = (Dictionary<WorldPos,int>)formatter.Deserialize (o);
			modBlockBuf = "";
			foreach (KeyValuePair<WorldPos, int> modBlock in modBlocks) {
					SetBlock (modBlock.Key.x, modBlock.Key.y, modBlock.Key.z, modBlock.Value);
			}
		}
	}

	public void GetChunkPos(int x, int y, int z, out WorldPos pos){
		float multiple = Chunk.chunkSize;
		pos.x = Mathf.FloorToInt(x / multiple ) * Chunk.chunkSize;
		pos.y = Mathf.FloorToInt(y / multiple ) * Chunk.chunkSize;
		pos.z = Mathf.FloorToInt(z / multiple ) * Chunk.chunkSize;
	}

	public Chunk GetChunk(int x, int y, int z)
	{
		WorldPos pos = new WorldPos ();

		GetChunkPos (x, y, z, out pos);
		
		Chunk containerChunk = null;
		
		chunks.TryGetValue(pos, out containerChunk);
		
		return containerChunk;
	}
	
	public Block GetBlock(int x, int y, int z)
	{
		Chunk containerChunk = GetChunk(x, y, z);
		
		if (containerChunk != null)
		{
			Block block = containerChunk.GetBlock(
				x - containerChunk.pos.x,
				y - containerChunk.pos.y, 
				z - containerChunk.pos.z);
			
			return block;
		}
		else
		{
			return new BlockAir();
		}
		
	}
	
	public void AddToModifiedBlocks(int x, int y, int z, int block){
		WorldPos blockPos = new WorldPos(x,y,z);
		WorldPos chunkPos = new WorldPos();
		GetChunkPos (x, y, z, out chunkPos);
		Dictionary<WorldPos, int> modBlocks;
		if(moddedBlocks.TryGetValue(chunkPos, out modBlocks) == true){
			moddedBlocks[chunkPos][blockPos] = block;
		}
		else{
			modBlocks = new Dictionary<WorldPos, int>();
			modBlocks.Add(blockPos, block);
			moddedBlocks.Add(chunkPos, modBlocks);
		}
	}
	

	[RPC]
	public void SetBlock(int x, int y, int z, int block)
	{
		AddToModifiedBlocks (x, y, z, block);
		Chunk chunk = GetChunk(x, y, z);
		if (chunk != null)
		{
			chunk.SetBlock(x - chunk.pos.x, y - chunk.pos.y, z - chunk.pos.z, block);
			chunk.update = true;
			UpdateIfEqual(x - chunk.pos.x, 0, new WorldPos(x - 1, y, z));
			UpdateIfEqual(x - chunk.pos.x, Chunk.chunkSize - 1, new WorldPos(x + 1, y, z));
			UpdateIfEqual(y - chunk.pos.y, 0, new WorldPos(x, y - 1, z));
			UpdateIfEqual(y - chunk.pos.y, Chunk.chunkSize - 1, new WorldPos(x, y + 1, z));
			UpdateIfEqual(z - chunk.pos.z, 0, new WorldPos(x, y, z - 1));
			UpdateIfEqual(z - chunk.pos.z, Chunk.chunkSize - 1, new WorldPos(x, y, z + 1));
		}
		if (Network.isServer) {
			networkView.RPC("SetBlock", RPCMode.Others, x, y, z, block);
		}
	}


	public void DestroyChunk(int x, int y, int z)
	{
		Chunk chunk = null;
		if (chunks.TryGetValue(new WorldPos(x, y, z), out chunk))
		{
			//TODO: handle periodic serialization of modded blocks as server
			//Serialization.SaveChunk(chunk);
			Object.Destroy(chunk.gameObject);
			chunks.Remove(new WorldPos(x, y, z));
		}
	}
	

	void UpdateIfEqual(int value1, int value2, WorldPos pos)
	{
		if (value1 == value2)
		{
			Chunk chunk = GetChunk(pos.x, pos.y, pos.z);
			if (chunk != null)
				chunk.update = true;
		}
	}

	/*
	public void OnDestroy()
	{
		foreach (WorldPos pos in chunks.Keys) {
			DestroyChunk(pos.x, pos.y, pos.z);
		}
		chunks = new Dictionary<WorldPos, Chunk>();
	}
	*/
}
