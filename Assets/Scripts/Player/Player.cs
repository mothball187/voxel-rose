using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
	public NetworkPlayer owner;
	public string playerName;
	public string guid;
	public Spawn spawn;
	public GameObject world;
	public GameObject worldPrefab;
	public GameObject nameplate;
	public GameObject nameplatePrefab;
	private Camera mycam;

	
	//Last input value, we're saving this to save network messages/bandwidth.
	private float lastClientHInput=0;
	private float lastClientVInput=0;
	private float lastClientYInput=0;
	private float lastMouseX = 0;
	
	//The input values the server will execute on this object
	private float serverCurrentHInput=0;
	private float serverCurrentVInput=0;
	private float serverCurrentYInput=0;
	private float serverCurrentMouseX=0;

	void Awake(){
		// We are probably not the owner of this object: disable this script.
		// RPC's and OnSerializeNetworkView will STILL get trough!

		if (Network.isClient) {
			enabled = false;	 // disable this script (this enables Update());	
		} else {
			enabled = true;
		}

		Camera cam = gameObject.GetComponentInChildren<Camera>();
		cam.enabled = false;
		mycam = cam;
		//LoadChunks lc = gameObject.GetComponent<LoadChunks> ();
		//lc.enabled = false;
	}

	
	[RPC]
	void SetPlayer(NetworkPlayer player, NetworkViewID worldViewID, int color){
		owner = player;
		switch ((Spawn.PlayerColor)color) {
		case Spawn.PlayerColor.pink:
			transform.renderer.material.color = new Color(255, 0, 206);
			break;
		case Spawn.PlayerColor.red:
			transform.renderer.material.color = new Color(255, 0, 0);
			break;
		case Spawn.PlayerColor.green:
			transform.renderer.material.color = new Color(0, 255, 0);
			break;
		case Spawn.PlayerColor.blue:
			transform.renderer.material.color = new Color(0, 0, 255);
			break;
		case Spawn.PlayerColor.yellow:
			transform.renderer.material.color = new Color(255,255,0);
			break;
		default:
			break;
		}

		//NetworkView nview = gameObject.GetComponent<NetworkView> ();
		//if(nview.isMine){
		if(player==Network.player){
			//Hey thats us! We can control this player: enable this script (this enables Update());
			//world = GameObject.Find("World(Clone)");
			world = Instantiate(worldPrefab, Vector3.zero, Quaternion.identity) as GameObject;
			world.GetComponent<World> ().owner = this;
			world.networkView.viewID = worldViewID;
			enabled=true;
			Camera cam = gameObject.GetComponentInChildren<Camera>();
			cam.enabled = true;
			playerName = PlayerPrefs.GetString("playername");
			networkView.RPC("SetName", RPCMode.Server, playerName);
			//LoadChunks lc = gameObject.GetComponent<LoadChunks> ();
			//lc.enabled = true;
		}
	}

	[RPC]
	void SetName(string name, NetworkMessageInfo info)
	{
		playerName = name;
		nameplate = Instantiate (nameplatePrefab, new Vector3(0.5f, 0.5f, 0), Quaternion.identity) as GameObject;
		nameplate.guiText.enabled = false;
		nameplate.guiText.text = name;
		nameplate.guiText.alignment = TextAlignment.Center;
		nameplate.guiText.anchor = TextAnchor.UpperCenter;

		if (Network.isServer) {
			networkView.RPC("SetName", RPCMode.OthersBuffered, name);
		}
	}

	[RPC]
	public void SetClosePlayer(int add)
	{
		if (add == 1)
			gameObject.tag = "close";
		else{
			gameObject.tag = "";
			nameplate.guiText.enabled = false;
		}
	}

	
	void Update(){

		//Client code
		if(owner!=null && Network.player==owner){
			if (Input.GetKeyDown(KeyCode.Tab))
			{
				RaycastHit hit;

				//Debug.DrawRay (transform.position, gameObject.transform.FindChild("Main Camera").transform.forward * 100);
				if (Physics.Raycast(transform.position, gameObject.transform.FindChild("Camera").transform.forward,out hit, 100 ))
				{
					//Terrain.SetBlock(hit, (int)Terrain.BlockType.Air, int.Parse(Network.player+""));
					Chunk chunk = hit.collider.GetComponent<Chunk>();
					if (chunk != null){
					
						WorldPos pos = Terrain.GetBlockPos(hit, false);
						chunk.world.SetBlock(pos.x, pos.y, pos.z, (int)Terrain.BlockType.Air);
						chunk.world.networkView.RPC("SetBlock", RPCMode.Server, pos.x, pos.y, pos.z, (int)Terrain.BlockType.Air);
					}
				}
			}
			
			if (Input.GetKeyDown(KeyCode.Z))
			{
				
				RaycastHit hit;
				//Debug.DrawRay (transform.position, gameObject.transform.FindChild("Main Camera").transform.forward * 100);
				if (Physics.Raycast(transform.position, gameObject.transform.FindChild("Camera").transform.forward,out hit, 100 ))
				{
					//Terrain.SetBlock(hit, (int)Terrain.BlockType.Grass, int.Parse(Network.player+""), true);
					Chunk chunk = hit.collider.GetComponent<Chunk>();
					if (chunk != null){
						
						WorldPos pos = Terrain.GetBlockPos(hit, true);
						chunk.world.SetBlock(pos.x, pos.y, pos.z, (int)Terrain.BlockType.Grass);
						chunk.world.networkView.RPC("SetBlock", RPCMode.Server, pos.x, pos.y, pos.z, (int)Terrain.BlockType.Grass);
					}
				}
			}

			//Only the client that owns this object executes this code
			float HInput = Input.GetAxis("Horizontal");
			float VInput = Input.GetAxis("Vertical");
			float YInput = 0;
			if (Input.GetKey (KeyCode.Space)) {
				YInput = 1;
			}
			
			if (Input.GetKey (KeyCode.LeftControl)) {
				YInput = -1;
			}


			Vector2 rot = Vector2.zero;
			float mouseX = Input.GetAxis("Mouse X");
			float mouseY = Input.GetAxis("Mouse Y");

			rot= new Vector2(
				rot.x + mouseX * 3,
				rot.y + mouseY * 3);


			//Is our input different? Do we need to update the server?
			if(lastClientHInput!=HInput || lastClientVInput!=VInput || lastClientYInput!=YInput || lastMouseX!=mouseX){
				lastClientHInput = HInput;
				lastClientVInput = VInput;	
				lastClientYInput = YInput;
				lastMouseX = mouseX;
				networkView.RPC("SendMovementInput", RPCMode.Server, HInput, VInput, YInput, mouseX);
			}

			gameObject.transform.FindChild("Camera").transform.localRotation *= Quaternion.AngleAxis(rot.y, Vector3.left);

			//nameplate fun
			foreach(GameObject go in GameObject.FindGameObjectsWithTag("close")){
				Player player = go.GetComponent<Player>();
				if(player.renderer.isVisible){
					RaycastHit hit;
					//can i see this player?
					if(Physics.Raycast(transform.position, (player.transform.position - transform.position), out hit, 256))
					{
						if(hit.transform == player.transform)
						{
							// In Range and i can see you!
							player.nameplate.guiText.transform.position = mycam.WorldToViewportPoint(player.transform.position + Vector3.up * 1.5f);
							player.nameplate.guiText.enabled = true;
						}
						else
							player.nameplate.guiText.enabled = false;
					}
				}
				else
					player.nameplate.guiText.enabled = false;
			}

		}


		//Server movement code
		if(Network.isServer || Network.player==owner){
			//Actually move the player using his/her input

			Vector3 moveDirection = new Vector3(serverCurrentHInput, serverCurrentYInput, serverCurrentVInput);
			float speed = 15;
			transform.Translate(speed * moveDirection * Time.deltaTime);
			transform.localRotation *= Quaternion.AngleAxis(serverCurrentMouseX * 3, Vector3.up);
			//Debug.Log("moving player " + owner + " to " + transform.position.x + "," + transform.position.y + "," + transform.position.z);

		}
		
	}	
	
	[RPC]
	void SendMovementInput(float HInput, float VInput, float YInput, float mouseX){	
		//Debug.Log("receiving movement RPC");
		//Called on the server
		serverCurrentHInput = HInput;
		serverCurrentVInput = VInput;
		serverCurrentYInput = YInput;
		serverCurrentMouseX = mouseX;
		//Debug.Log("movement RPC received");
	}
	
	
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		if (stream.isWriting){
			//This is executed on the owner of the networkview
			//The owner sends it's position over the network
			
			Vector3 pos = transform.position;	
			Quaternion rot = transform.localRotation;
			//Debug.Log("position sent: "+pos.x+","+pos.y+","+pos.z);
			stream.Serialize(ref pos);//"Encode" it, and send it
			stream.Serialize(ref rot);
			
		}else{
			//Executed on all non-owners
			//This client receive a position and set the object to it
			
			Vector3 posReceive = Vector3.zero;
			Quaternion rotReceive = Quaternion.identity;
			stream.Serialize( ref posReceive); //"Decode" it and receive it
			stream.Serialize(ref rotReceive);
			//Debug.Log("position receive: "+posReceive.x+","+posReceive.y+","+posReceive.z);
			//We've just recieved the current servers position of this object in 'posReceive'.
			
			//transform.position = posReceive;		
			//To reduce laggy movement a bit you could comment the line above and use position lerping below instead:	
			transform.position = Vector3.Lerp(transform.position, posReceive, 0.9f); //"lerp" to the posReceive by 90%
			transform.localRotation = rotReceive;
			
		}
	}

}