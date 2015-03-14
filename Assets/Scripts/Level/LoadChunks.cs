using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class LoadChunks : MonoBehaviour {
	
	public World world;
	List<WorldPos> updateList = new List<WorldPos>();
	List<WorldPos> buildList = new List<WorldPos>();
	int timer = 0;

	static  WorldPos[] chunkPositions= {   new WorldPos( 0, 0,  0), new WorldPos(-1, 0,  0), new WorldPos( 0, 0, -1), new WorldPos( 0, 0,  1), new WorldPos( 1, 0,  0),
		new WorldPos(-1, 0, -1), new WorldPos(-1, 0,  1), new WorldPos( 1, 0, -1), new WorldPos( 1, 0,  1), new WorldPos(-2, 0,  0),
		new WorldPos( 0, 0, -2), new WorldPos( 0, 0,  2), new WorldPos( 2, 0,  0), new WorldPos(-2, 0, -1), new WorldPos(-2, 0,  1),
		new WorldPos(-1, 0, -2), new WorldPos(-1, 0,  2), new WorldPos( 1, 0, -2), new WorldPos( 1, 0,  2), new WorldPos( 2, 0, -1),
		new WorldPos( 2, 0,  1), new WorldPos(-2, 0, -2), new WorldPos(-2, 0,  2), new WorldPos( 2, 0, -2), new WorldPos( 2, 0,  2),
		new WorldPos(-3, 0,  0), new WorldPos( 0, 0, -3), new WorldPos( 0, 0,  3), new WorldPos( 3, 0,  0), new WorldPos(-3, 0, -1),
		new WorldPos(-3, 0,  1), new WorldPos(-1, 0, -3), new WorldPos(-1, 0,  3), new WorldPos( 1, 0, -3), new WorldPos( 1, 0,  3),
		new WorldPos( 3, 0, -1), new WorldPos( 3, 0,  1), new WorldPos(-3, 0, -2), new WorldPos(-3, 0,  2), new WorldPos(-2, 0, -3),
		new WorldPos(-2, 0,  3), new WorldPos( 2, 0, -3), new WorldPos( 2, 0,  3), new WorldPos( 3, 0, -2), new WorldPos( 3, 0,  2),
		new WorldPos(-4, 0,  0), new WorldPos( 0, 0, -4), new WorldPos( 0, 0,  4), new WorldPos( 4, 0,  0), new WorldPos(-4, 0, -1),
		new WorldPos(-4, 0,  1), new WorldPos(-1, 0, -4), new WorldPos(-1, 0,  4), new WorldPos( 1, 0, -4), new WorldPos( 1, 0,  4),
		new WorldPos( 4, 0, -1), new WorldPos( 4, 0,  1), new WorldPos(-3, 0, -3), new WorldPos(-3, 0,  3), new WorldPos( 3, 0, -3),
		new WorldPos( 3, 0,  3), new WorldPos(-4, 0, -2), new WorldPos(-4, 0,  2), new WorldPos(-2, 0, -4), new WorldPos(-2, 0,  4),
		new WorldPos( 2, 0, -4), new WorldPos( 2, 0,  4), new WorldPos( 4, 0, -2), new WorldPos( 4, 0,  2), new WorldPos(-5, 0,  0),
		new WorldPos(-4, 0, -3), new WorldPos(-4, 0,  3), new WorldPos(-3, 0, -4), new WorldPos(-3, 0,  4), new WorldPos( 0, 0, -5),
		new WorldPos( 0, 0,  5), new WorldPos( 3, 0, -4), new WorldPos( 3, 0,  4), new WorldPos( 4, 0, -3), new WorldPos( 4, 0,  3),
		new WorldPos( 5, 0,  0), new WorldPos(-5, 0, -1), new WorldPos(-5, 0,  1), new WorldPos(-1, 0, -5), new WorldPos(-1, 0,  5),
		new WorldPos( 1, 0, -5), new WorldPos( 1, 0,  5), new WorldPos( 5, 0, -1), new WorldPos( 5, 0,  1), new WorldPos(-5, 0, -2),
		new WorldPos(-5, 0,  2), new WorldPos(-2, 0, -5), new WorldPos(-2, 0,  5), new WorldPos( 2, 0, -5), new WorldPos( 2, 0,  5),
		new WorldPos( 5, 0, -2), new WorldPos( 5, 0,  2), new WorldPos(-4, 0, -4), new WorldPos(-4, 0,  4), new WorldPos( 4, 0, -4),
		new WorldPos( 4, 0,  4), new WorldPos(-5, 0, -3), new WorldPos(-5, 0,  3), new WorldPos(-3, 0, -5), new WorldPos(-3, 0,  5),
		new WorldPos( 3, 0, -5), new WorldPos( 3, 0,  5), new WorldPos( 5, 0, -3), new WorldPos( 5, 0,  3), new WorldPos(-6, 0,  0),
		new WorldPos( 0, 0, -6), new WorldPos( 0, 0,  6), new WorldPos( 6, 0,  0), new WorldPos(-6, 0, -1), new WorldPos(-6, 0,  1),
		new WorldPos(-1, 0, -6), new WorldPos(-1, 0,  6), new WorldPos( 1, 0, -6), new WorldPos( 1, 0,  6), new WorldPos( 6, 0, -1),
		new WorldPos( 6, 0,  1), new WorldPos(-6, 0, -2), new WorldPos(-6, 0,  2), new WorldPos(-2, 0, -6), new WorldPos(-2, 0,  6),
		new WorldPos( 2, 0, -6), new WorldPos( 2, 0,  6), new WorldPos( 6, 0, -2), new WorldPos( 6, 0,  2), new WorldPos(-5, 0, -4),
		new WorldPos(-5, 0,  4), new WorldPos(-4, 0, -5), new WorldPos(-4, 0,  5), new WorldPos( 4, 0, -5), new WorldPos( 4, 0,  5),
		new WorldPos( 5, 0, -4), new WorldPos( 5, 0,  4), new WorldPos(-6, 0, -3), new WorldPos(-6, 0,  3), new WorldPos(-3, 0, -6),
		new WorldPos(-3, 0,  6), new WorldPos( 3, 0, -6), new WorldPos( 3, 0,  6), new WorldPos( 6, 0, -3), new WorldPos( 6, 0,  3),
		new WorldPos(-7, 0,  0), new WorldPos( 0, 0, -7), new WorldPos( 0, 0,  7), new WorldPos( 7, 0,  0), new WorldPos(-7, 0, -1),
		new WorldPos(-7, 0,  1), new WorldPos(-5, 0, -5), new WorldPos(-5, 0,  5), new WorldPos(-1, 0, -7), new WorldPos(-1, 0,  7),
		new WorldPos( 1, 0, -7), new WorldPos( 1, 0,  7), new WorldPos( 5, 0, -5), new WorldPos( 5, 0,  5), new WorldPos( 7, 0, -1),
		new WorldPos( 7, 0,  1), new WorldPos(-6, 0, -4), new WorldPos(-6, 0,  4), new WorldPos(-4, 0, -6), new WorldPos(-4, 0,  6),
		new WorldPos( 4, 0, -6), new WorldPos( 4, 0,  6), new WorldPos( 6, 0, -4), new WorldPos( 6, 0,  4), new WorldPos(-7, 0, -2),
		new WorldPos(-7, 0,  2), new WorldPos(-2, 0, -7), new WorldPos(-2, 0,  7), new WorldPos( 2, 0, -7), new WorldPos( 2, 0,  7),
		new WorldPos( 7, 0, -2), new WorldPos( 7, 0,  2), new WorldPos(-7, 0, -3), new WorldPos(-7, 0,  3), new WorldPos(-3, 0, -7),
		new WorldPos(-3, 0,  7), new WorldPos( 3, 0, -7), new WorldPos( 3, 0,  7), new WorldPos( 7, 0, -3), new WorldPos( 7, 0,  3),
		new WorldPos(-6, 0, -5), new WorldPos(-6, 0,  5), new WorldPos(-5, 0, -6), new WorldPos(-5, 0,  6), new WorldPos( 5, 0, -6),
		new WorldPos( 5, 0,  6), new WorldPos( 6, 0, -5), new WorldPos( 6, 0,  5) };

	void Start(){
		world.chunks = new Dictionary<WorldPos, Chunk>();
	}
	
	// Update is called once per frame
	void Update () {
		DeleteChunks();
		FindChunksToLoad();
		LoadAndRenderChunks();
	}

	void FindChunksToLoad()
	{
		//If there aren't already chunks to generate
		if (buildList.Count == 0) {
			if (Network.isServer) {
				foreach (Player playa in world.spawn.playerScripts) {
					WorldPos playerPos = new WorldPos (
Mathf.FloorToInt (playa.transform.position.x / Chunk.chunkSize) * Chunk.chunkSize,
Mathf.FloorToInt (playa.transform.position.y / Chunk.chunkSize) * Chunk.chunkSize,
Mathf.FloorToInt (playa.transform.position.z / Chunk.chunkSize) * Chunk.chunkSize
					);


					//Cycle through the array of positions
					for (int i = 0; i < chunkPositions.Length; i++) {
						//translate the player position and array position into chunk position
						WorldPos newChunkPos = new WorldPos (
chunkPositions [i].x * Chunk.chunkSize + playerPos.x,
0, 
chunkPositions [i].z * Chunk.chunkSize + playerPos.z
						);

						//Get the chunk in the defined position
						Chunk newChunk = world.GetChunk (
newChunkPos.x, 0, newChunkPos.z);

						//If the chunk already exists and it's already
						//rendered or in queue to be rendered continue
						if (newChunk != null
								&& (newChunk.rendered || updateList.Contains (newChunkPos) || buildList.Contains (newChunkPos)))
								continue;

						//load a column of chunks in this position
						for (int y = -4; y < 4; y++) {
								buildList.Add (new WorldPos (newChunkPos.x, y * Chunk.chunkSize, newChunkPos.z));
						}
					}
				}
			}
			else if(world.owner.owner == Network.player){
				WorldPos playerPos = new WorldPos (
					Mathf.FloorToInt (world.owner.transform.position.x / Chunk.chunkSize) * Chunk.chunkSize,
					Mathf.FloorToInt (world.owner.transform.position.y / Chunk.chunkSize) * Chunk.chunkSize,
					Mathf.FloorToInt (world.owner.transform.position.z / Chunk.chunkSize) * Chunk.chunkSize
					);
				
				
				//Cycle through the array of positions
				for (int i = 0; i < chunkPositions.Length; i++) {
					//translate the player position and array position into chunk position
					WorldPos newChunkPos = new WorldPos (
						chunkPositions [i].x * Chunk.chunkSize + playerPos.x,
						0, 
						chunkPositions [i].z * Chunk.chunkSize + playerPos.z
						);
					
					//Get the chunk in the defined position
					Chunk newChunk = world.GetChunk (
						newChunkPos.x, newChunkPos.y, newChunkPos.z);
					
					//If the chunk already exists and it's already
					//rendered or in queue to be rendered continue
					if (newChunk != null
					    && (updateList.Contains (newChunkPos) || buildList.Contains (newChunkPos)))
						continue;

					//64, -16, -160
					//load a column of chunks in this position
					for (int y = -4; y < 4; y++) {
						WorldPos newColumnChunkPos = new WorldPos(newChunkPos.x, y * Chunk.chunkSize, newChunkPos.z);
						newChunk = world.GetChunk(newColumnChunkPos.x, newColumnChunkPos.y, newColumnChunkPos.z);
						if(newChunk != null && newChunk.rendered == true)
							continue;
						buildList.Add (newColumnChunkPos);
						//this looks like a good time to send request for modified blocks
						world.networkView.RPC("GetModBlocks", RPCMode.Server, new Vector3(newChunkPos.x, y * Chunk.chunkSize, newChunkPos.z));
					}
				}
			}
		}
	}

	void BuildChunk(WorldPos pos)
	{
		for (int y = pos.y - Chunk.chunkSize; y <= pos.y + Chunk.chunkSize; y += Chunk.chunkSize)
		{
			if (y > 64 || y < -64)
				continue;
			
			for (int x = pos.x - Chunk.chunkSize; x <= pos.x + Chunk.chunkSize; x += Chunk.chunkSize)
			{
				for (int z = pos.z - Chunk.chunkSize; z <= pos.z + Chunk.chunkSize; z += Chunk.chunkSize)
				{
					if (world.GetChunk(x, y, z) == null)
					{
						world.CreateChunk(x, y, z);
					}
				}
			}
		}

		updateList.Add(pos);
	}

	void LoadAndRenderChunks()
	{
		for (int i = 0; i < 4; i++)
		{
			if (buildList.Count != 0)
			{
				BuildChunk(buildList[0]);
				buildList.RemoveAt(0);
			}
		}

		for (int i = 0; i < updateList.Count; i++)
		{
			Chunk chunk = world.GetChunk(updateList[0].x, updateList[0].y, updateList[0].z);
			if (chunk != null)
				chunk.update = true;
			updateList.RemoveAt(0);
		}

	}

	void DeleteChunks()
	{
		bool delete;
		if (timer == 10) {
			var chunksToDelete = new List<WorldPos> ();

			if(Network.isServer){
				foreach (var chunk in world.chunks) {
					delete = true;
					foreach (Player playa in world.spawn.playerScripts) {
						float distance = Vector3.Distance (
						new Vector3 (chunk.Key.x, 0, chunk.Key.z),
						new Vector3 (playa.transform.position.x, 0, playa.transform.position.z));

						if (distance <= 256)
							delete = false;
					}

					if(delete)
						chunksToDelete.Add (new WorldPos(chunk.Key.x, 0, chunk.Key.z));

				}
			}
			else if(world.owner.owner == Network.player){
				foreach (var chunk in world.chunks) {
					float distance = Vector3.Distance (
						new Vector3 (chunk.Key.x, 0, chunk.Key.z),
						new Vector3 (world.owner.transform.position.x, 0, world.owner.transform.position.z));
					
					if (distance > 256)
						chunksToDelete.Add (new WorldPos(chunk.Key.x, 0, chunk.Key.z));
					
				}
			}

			//delete entire column of chunks
			foreach (var chunk in chunksToDelete){

				for (int y = -4; y < 4; y++) {

					world.DestroyChunk (chunk.x, y * Chunk.chunkSize, chunk.z);
				
				}

			}

			timer = 0;	
		}	
		timer++;
	}

}