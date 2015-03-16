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

	public double m_InterpolationBackTime = 0.1;
	public double m_ExtrapolationLimit = 0.5;

	internal struct  State
	{
		internal double timestamp;
		internal Vector3 velocity;
		internal Vector3 angularVelocity;
	}
	
	// We store twenty states with "playback" information
	State[] m_BufferedState = new State[20];
	// Keep track of what slots are used
	int m_TimestampCount;


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

		if(Network.isClient){
			nameplate = Instantiate (nameplatePrefab, new Vector3(0.5f, 0.5f, 0), Quaternion.identity) as GameObject;
			nameplate.guiText.enabled = false;
			nameplate.guiText.text = name;
			nameplate.guiText.alignment = TextAlignment.Center;
			nameplate.guiText.anchor = TextAnchor.UpperCenter;
		}
		else {
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


			SendMovementInput(HInput, VInput, YInput, 0);
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
				if(go == null)
					break;
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
		/*
		else{
			// This is the target playback time of the rigid body
			double interpolationTime = Network.time - m_InterpolationBackTime;
			
			// Use interpolation if the target playback time is present in the buffer
			if (m_BufferedState[0].timestamp > interpolationTime)
			{
				// Go through buffer and find correct state to play back
				for (int i=0;i<m_TimestampCount;i++)
				{
					if (m_BufferedState[i].timestamp <= interpolationTime || i == m_TimestampCount-1)
					{
						// The state one slot newer (<100ms) than the best playback state
						State rhs = m_BufferedState[Mathf.Max(i-1, 0)];
						// The best playback state (closest to 100 ms old (default time))
						State lhs = m_BufferedState[i];
						
						// Use the time between the two slots to determine if interpolation is necessary
						double length = rhs.timestamp - lhs.timestamp;
						float t = 0.0F;
						// As the time difference gets closer to 100 ms t gets closer to 1 in 
						// which case rhs is only used
						// Example:
						// Time is 10.000, so sampleTime is 9.900 
						// lhs.time is 9.910 rhs.time is 9.980 length is 0.070
						// t is 9.900 - 9.910 / 0.070 = 0.14. So it uses 14% of rhs, 86% of lhs
						if (length > 0.0001){
							t = (float)((interpolationTime - lhs.timestamp) / length);
						}
						//	Debug.Log(t);
						// if t=0 => lhs is used directly
						transform.position = Vector3.Lerp(lhs.velocity, rhs.velocity, t);
						transform.rotation = Quaternion.Slerp(Quaternion.Euler(lhs.angularVelocity), Quaternion.Euler(rhs.angularVelocity), t);
						return;
					}
				}
			}
			// Use extrapolation
			else
			{
				State latest = m_BufferedState[0];
				
				float extrapolationLength = (float)(interpolationTime - latest.timestamp);
				// Don't extrapolation for more than 500 ms, you would need to do that carefully
				if (extrapolationLength < m_ExtrapolationLimit)
				{
					float axisLength = extrapolationLength * latest.angularVelocity.magnitude * Mathf.Rad2Deg;
					Quaternion angularRotation = Quaternion.AngleAxis(axisLength, latest.angularVelocity);
					
					transform.position = latest.velocity + latest.velocity * extrapolationLength;
					transform.rotation = angularRotation * Quaternion.Euler(latest.angularVelocity);
					//rigidbody.velocity = latest.velocity;
					//rigidbody.angularVelocity = latest.angularVelocity;
				}
			}
		}
		*/
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
			//if(Network.player==owner){
				Vector3 posReceive = Vector3.zero;
				Quaternion rotReceive = Quaternion.identity;
				stream.Serialize( ref posReceive); //"Decode" it and receive it
				stream.Serialize(ref rotReceive);
				//Debug.Log("position receive: "+posReceive.x+","+posReceive.y+","+posReceive.z);
				//We've just recieved the current servers position of this object in 'posReceive'.
				if(Network.player == owner)
					transform.position = Vector3.Lerp(transform.position, posReceive, Time.deltaTime * 5);
				else
					transform.position = posReceive;

				transform.localRotation = rotReceive;
			//}
			/*
			else{
				enabled = true;
				Vector3 velocity = Vector3.zero;
				Vector3 angularVelocity = Vector3.zero;
				stream.Serialize(ref velocity);
				stream.Serialize(ref angularVelocity);
				
				// Shift the buffer sideways, deleting state 20
				for (int i=m_BufferedState.Length-1;i>=1;i--)
				{
					m_BufferedState[i] = m_BufferedState[i-1];
				}
				
				// Record current state in slot 0
				State state = new State();
				state.timestamp = info.timestamp;
				state.velocity = velocity;
				state.angularVelocity = angularVelocity;
				m_BufferedState[0] = state;
				
				// Update used slot count, however never exceed the buffer size
				// Slots aren't actually freed so this just makes sure the buffer is
				// filled up and that uninitalized slots aren't used.
				m_TimestampCount = Mathf.Min(m_TimestampCount + 1, m_BufferedState.Length);
				
				// Check if states are in order, if it is inconsistent you could reshuffel or 
				// drop the out-of-order state. Nothing is done here

				for (int i=0;i<m_TimestampCount-1;i++)
				{
					if (m_BufferedState[i].timestamp < m_BufferedState[i+1].timestamp)
						Debug.Log("State inconsistent");
				}	

			}
		*/
		}
	}

}