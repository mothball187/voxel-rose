using UnityEngine;
using System.Collections;

public class Chat : MonoBehaviour {

	bool usingChat = false;	//Can be used to determine if we need to stop player movement since we're chatting
	//GUISkin skin;						//Skin
	bool showChat = false;			//Show/Hide the chat
	
	//Private vars used by the script
	private string inputField = "";
	private Vector2 scrollPosition;
	private int width = 500;
	private int height = 180;
	private string playerName;
	private float lastUnfocusTime =0;
	private Rect window;
	private Spawn spawn;
	
	//Server-only playerlist
	private ArrayList chatEntries = new ArrayList();

	class ChatEntry
	{
		public string name = "";
		public string text = "";	
	}
	
	void Awake(){
		spawn = gameObject.GetComponent<Spawn> ();
		//window = new Rect(Screen.width/2-width/2, Screen.height-height+5, width, height);
		window = new Rect(Screen.width-width, Screen.height-height+5, width, height);
		
		//We get the name from the masterserver example, if you entered your name there ;).
		playerName = PlayerPrefs.GetString("playername", "");
		if(playerName==""){
			playerName = "RandomName"+Random.Range(1,999);
		}	
		
	}
	
	
	//Client function
	void OnConnectedToServer() {
		ShowChatWindow();
	}
	
	//Server function
	void OnServerInitialized() {

	}
	
	
	//Server function
	void OnPlayerDisconnected(NetworkPlayer player) {

	}
	
	void OnDisconnectedFromServer(){
		CloseChatWindow();
	}
	
	//Server function
	void OnPlayerConnected(NetworkPlayer player) {
		Player p = spawn.GetPlayer (player);
		string str = "Player " + p.playerName + " connected from: " + player.ipAddress +":" + player.port;
		networkView.RPC("ApplyGlobalChatText", RPCMode.Others, "", str);
	}
	

	void CloseChatWindow ()
	{
		showChat = false;
		inputField = "";
		chatEntries = new ArrayList();
	}
	
	void ShowChatWindow ()
	{
		showChat = true;
		inputField = "";
		chatEntries = new ArrayList();
	}
	

	void OnGUI ()
	{
		if(!showChat){
			return;
		}
		
		//GUI.skin = skin;		
		
		if (Event.current.type == EventType.keyDown && Event.current.character == '\n' && inputField.Length <= 0)
		{
			if(lastUnfocusTime+0.25<Time.time){
				usingChat=true;
				GUI.FocusWindow(5);
				GUI.FocusControl("Chat input field");
			}
		}
		
		window = GUI.Window (5, window, GlobalChatWindow, "");
	}

	void GlobalChatWindow (int id) {
		
		GUILayout.BeginVertical();
		GUILayout.Space(10);
		GUILayout.EndVertical();
		
		// Begin a scroll view. All rects are calculated automatically - 
		// it will use up any available screen space and make sure contents flow correctly.
		// This is kept small with the last two parameters to force scrollbars to appear.
		scrollPosition = GUILayout.BeginScrollView (scrollPosition);
		
		foreach (ChatEntry entry in chatEntries)
		{
			GUILayout.BeginHorizontal();
			if(entry.name==""){//Game message
				GUILayout.Label (entry.text);
			}else{
				GUILayout.Label (entry.name+": "+entry.text);
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(3);
			
		}
		// End the scrollview we began above.
		GUILayout.EndScrollView ();
		
		if (Event.current.type == EventType.keyDown && Event.current.character == '\n' && inputField.Length > 0)
		{
			HitEnter(inputField);
		}
		GUI.SetNextControlName("Chat input field");
		inputField = GUILayout.TextField(inputField);
		GUI.SetNextControlName("");
		
		if(Input.GetKeyDown("mouse 0")){
			if(usingChat){
				usingChat=false;
				GUI.UnfocusWindow ();//Deselect chat
				lastUnfocusTime=Time.time;
			}
		}
	}
	
	void HitEnter(string msg){
		if (Network.isClient) {
			msg = msg.Replace ("\n", "");
			networkView.RPC ("SendGlobalChatText", RPCMode.Server, msg);
			inputField = ""; //Clear line
			GUI.UnfocusWindow ();//Deselect chat
			GUI.FocusControl("");
			lastUnfocusTime = Time.time;
			usingChat = false;
		}
	}
	
	
	[RPC]
	void SendGlobalChatText (string msg, NetworkMessageInfo info)
	{
		if (Network.isServer) {
			if(Network.connections.GetLength(0) > 0){
				string name = spawn.GetPlayer(info.sender.guid).playerName;
				networkView.RPC("ApplyGlobalChatText", RPCMode.Others, name, msg);	
			}	
		}
	}

	[RPC]
	void ApplyGlobalChatText (string name, string msg)
	{
		if (Network.isClient) {
			var entry = new ChatEntry();
			entry.name = name;
			entry.text = msg;
			
			chatEntries.Add(entry);
			
			//Remove old entries
			if (chatEntries.Count > 4){
				chatEntries.RemoveAt(0);
			}
			
			scrollPosition.y = 1000000;	
		}
	}


}

