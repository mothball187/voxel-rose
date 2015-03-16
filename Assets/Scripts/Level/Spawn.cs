using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Spawn : MonoBehaviour {

	public Transform playerPrefab;
	public ArrayList playerScripts = new ArrayList();
	public ArrayList closePlayers = new ArrayList();
	public GameObject worldPrefab;
	public GameObject world;
	public enum PlayerColor { white = 0, pink, red, blue, green, yellow };
	int timer = 0;

	public Player GetPlayer(NetworkPlayer player){
		foreach(Player p in playerScripts){
			if(p.owner == player)
				return p;
		}

		return null;
	}

	public Player GetPlayer(string playerGuid){
		foreach(Player p in playerScripts){
			if(p.guid == playerGuid)
				return p;
		}

		return null;
	}

	void OnServerInitialized(){
		if (Network.isServer) {
			//world = Network.Instantiate(worldPrefab, Vector3.zero, Quaternion.identity, 0) as GameObject;
			world = Instantiate(worldPrefab, Vector3.zero, Quaternion.identity) as GameObject;
			world.GetComponent<World>().spawn = this;
			world.networkView.viewID = Network.AllocateViewID();
			//Instantiate(worldScriptsPrefab);
		}
	}

	void OnPlayerConnected(NetworkPlayer newPlayer) {
		//A player connected to me(the server)!
		Spawnplayer(newPlayer);
	}	

		
	void Spawnplayer(NetworkPlayer newPlayer){
		//Called on the server only
		
		int playerNumber = int.Parse(newPlayer+"");
		int color = Network.connections.GetLength (0) % 6;
		//Instantiate a new object for this player, remember; the server is therefore the owner.
		Transform myNewTrans = Network.Instantiate(playerPrefab, transform.position, transform.rotation, 0) as Transform;
		NetworkView nView = myNewTrans.networkView;
		/* this doesnt work for some reason
		Transform myNewTrans;
		myNewTrans = Instantiate(playerPrefab, transform.position, transform.rotation) as Transform as Transform;
		NetworkViewID viewID = Network.AllocateViewID();
		NetworkView nView;
		nView = myNewTrans.GetComponent<NetworkView>();
		nView.viewID = viewID;
		nView.group = 0;
		*/
		//Keep track of this new player so we can properly destroy it when required.
		myNewTrans.GetComponent<Player>().guid = newPlayer.guid;
		playerScripts.Add(myNewTrans.GetComponent<Player>());
		//Call an RPC on this new networkview, set the player who controls this player
		nView.RPC("SetPlayer", RPCMode.AllBuffered, newPlayer, world.networkView.viewID, color);//Set it on the owner
	}



	void OnPlayerDisconnected(NetworkPlayer player) {
		Debug.Log("Clean up after player " + player);
		Player p = GetPlayer (player);
		string str = "Player " + p.playerName + " disconnected from: " + player.ipAddress+":" + player.port;
		networkView.RPC("ApplyGlobalChatText", RPCMode.Others, "", str);
		foreach(Player script in playerScripts){
			if(player==script.owner){//We found the players object
				string pname = script.playerName;
				RemoveClosePlayer(script);
				script.networkView.RPC("SetClosePlayer", RPCMode.Others, 0);
				RemoveNameplate(pname);
				networkView.RPC("RemoveNameplate", RPCMode.Others, pname);
				Network.RemoveRPCs(script.gameObject.networkView.viewID);//remove the bufferd SetPlayer call
				Network.Destroy(script.gameObject);//Destroying the GO will destroy everything
				playerScripts.Remove(script);//Remove this player from the list
				break;
			}
		}
		
		//Remove the buffered RPC call for instantiate for this player.
		int playerNumber = int.Parse(player+"");
		Network.RemoveRPCs(Network.player, playerNumber);
		
		
		// The next destroys will not destroy anything since the players never
		// instantiated anything nor buffered RPCs
		Network.RemoveRPCs(player);
		Network.DestroyPlayerObjects(player);
	}

	void OnDisconnectedFromServer(NetworkDisconnection info) {
		Debug.Log("Resetting the scene the easy way.");
		Application.LoadLevel(Application.loadedLevel);	
	}

	void RemoveClosePlayer(Player player){
		for(int i=0; i < closePlayers.Count; i++) {
			string close = (string)closePlayers[i];
			if(close.Contains(player.owner.guid)){
				closePlayers.RemoveAt(i);
				i--;
			}
		}
	}

	[RPC]
	public void RemoveNameplate(string playerName){
		foreach (GUIText txt in GameObject.FindObjectsOfType(typeof(GUIText))) {
			if(txt.text == playerName){
				Destroy(txt.gameObject);
				break;
			}
		}
	}


	void Update(){
		if (timer == 10) {
			//look for close players, update them
			if (Network.isServer) {
				for(int i = 0; i < playerScripts.Count; i++){
					for(int j = i+1; j < playerScripts.Count; j++){
						Player p1 = (Player)playerScripts[i];
						Player p2 = (Player)playerScripts[j];
						if(!closePlayers.Contains("" + p1.owner.guid + p2.owner.guid)){
							if(Vector3.Distance(p1.transform.position, p2.transform.position) <= 256){
								closePlayers.Add("" + p1.owner.guid + p2.owner.guid);
								p2.networkView.RPC("SetClosePlayer", p1.owner, 1);
								p1.networkView.RPC("SetClosePlayer", p2.owner, 1);
							}
						}
						else{
							if(Vector3.Distance(p1.transform.position, p2.transform.position) > 256){
								closePlayers.Remove("" + p1.owner.guid + p2.owner.guid);
								p2.networkView.RPC("SetClosePlayer", p1.owner, 0);
								p1.networkView.RPC("SetClosePlayer", p2.owner, 0);
							}
						}
					}
				}
			}
			timer = 0;
		}
		timer++;
	}

}