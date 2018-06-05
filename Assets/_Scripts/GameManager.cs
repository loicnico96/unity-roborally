using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

[RequireComponent(typeof(NetworkIdentity))]
public class GameManager : NetworkBehaviour {
	public int maxPlayers = 6;
    
	public GameObject mapPrefab;
	public GameObject mapFolder;
    
	void Start () {
		Player.OnPlayerConnection += OnPlayerConnection;
		Player.OnPlayerDisconnection += OnPlayerDisconnection;
	}

	void OnPlayerConnection ( Player player ) {
		if ( isServer ) {
			RpcMessagePush ( player.playerName + " has joined the room." );
			_buttonStartGame.SetActive ( !_running && Player.players.Count >= 1 && Player.players.Count <= maxPlayers );
		}
	}

	void OnPlayerDisconnection ( Player player ) {
        if ( isServer ) {
			RpcMessagePush ( player.playerName + " has left the room." );
			_buttonStartGame.SetActive ( !_running && Player.players.Count >= 1 && Player.players.Count <= maxPlayers );
		}
	}



	/*** Maps ***/




	/*** Game Engine ***/
	[SyncVar] private bool _running = false;

	[Command]
	public void CmdStartGame () {
		// RpcStartGame ?
		StartCoroutine ( SrvStartGame() );
	}

	[ClientRpc]
	void RpcEnablePlayer (NetworkInstanceId playerId) {
		Player player = Player.players [ playerId ];
		player.GetComponent<Rigidbody> ().useGravity = true;
		player.GetComponent<MeshRenderer> ().enabled = true;
	}

	[Server]
	IEnumerator SrvStartGame () {
		GameObject map = (GameObject) Instantiate ( mapPrefab , mapFolder.transform );
		map.transform.localPosition = Vector3.zero;
		map.transform.localRotation = Quaternion.identity;
		NetworkServer.Spawn ( map );
		foreach ( Player player in Player.players.Values ) {
			RpcEnablePlayer ( player.playerId );
		}

		// Initializing some variables
		_running = true;

		// Taking turns until game ends
		while ( !SrvCheckVictory () ) {
			yield return StartCoroutine ( SrvTakeTurn () );
		}

		_running = false;
	}

	[Server]
	bool SrvCheckVictory () {
		return false;
	}

	public int _currentCommand;

	[Server]
	IEnumerator SrvTakeTurn () {
		RpcMessageClear ();
		RpcMessagePush ( "Program your robot!" );

		// Giving commands to all players
		foreach ( Player player in Player.players.Values ) {
			player.RpcTurnChooseCommand ( new Command[] {
				Command.CommandMove1,
				Command.CommandMove2,
				Command.CommandMove2,
				Command.CommandMove3,
				Command.CommandBack,
				Command.CommandTurnLeft,
				Command.CommandTurnLeft,
				Command.CommandTurnRight,
				Command.CommandTurnRight
			} , 5 /* should be command count */ );
		}
		
		// Waiting for all players to make their program
		while ( !IsAllPlayersReady () ) {
			yield return null;
		}

		// Executing the program
		RpcMessageClear ();
		_currentCommand = 0;
		while ( _currentCommand < 5 /* should be command count */ ) {
			RpcMessagePush ( string.Format ( "Executing command {0}" , _currentCommand + 1 ) );
			foreach ( Player player in Player.players.Values ) {
				player.RpcTurnExecuteCommand ( _currentCommand );
			}

			// Executing the command
			foreach ( Player player in Player.players.Values ) {
				Command command = player.GetSelectedCommand ( _currentCommand );
				if ( command != Command.None ) {
					yield return StartCoroutine ( SrvExecuteCommandForPlayer ( player , command ) );
				}
			}

			// Preparing for next command
			_currentCommand++;
		}
		
		// Ending the turn
		foreach ( Player player in Player.players.Values ) {
			player.RpcTurnExecuteCommand ( _currentCommand );
		}
	}

    [Server]
    IEnumerator SrvExecuteCommandForPlayer(Player player, Command command)
    {
        float duration = 1.0f;
        float distance = 1.0f;
        switch (command)
        {
            case Command.CommandBack:
                yield return StartCoroutine( SrvMovePlayer( player, Directions.getOppositeDirection( player.direction ), 1, duration, true, false ) );
                break;
            case Command.CommandMove1:
                yield return StartCoroutine( SrvMovePlayer( player, player.direction, 1, duration, true, false ) );
                break;
            case Command.CommandMove2:
                yield return StartCoroutine( SrvMovePlayer( player, player.direction, 2, duration, true, false ) );
                break;
            case Command.CommandMove3:
                yield return StartCoroutine( SrvMovePlayer( player, player.direction, 3, duration, true, false ) );
                break;
            case Command.CommandTurnLeft:
                yield return StartCoroutine( SrvRotatePlayer( player, Directions.getLeftDirection( player.direction ), duration ) );
                break;
            case Command.CommandTurnRight:
                yield return StartCoroutine( SrvRotatePlayer( player, Directions.getRightDirection( player.direction ), duration ) );
                break;
            case Command.CommandTurnAround:
                yield return StartCoroutine( SrvRotatePlayer( player, Directions.getOppositeDirection( player.direction ), duration ) );
                break;
        }
    }


	[Server]
	IEnumerator SrvMovePlayer ( Player player, Direction direction, int distance, float duration, bool movedByCommand, bool movedByConveyor ) {
		for (int i = 0 ; i < distance ; i++) {


			// Check for walls
			// Check for other robots
			// Check for holes
			player.RpcMove ( player.tileMap.netId, direction , duration / distance );
			yield return new WaitForSeconds ( duration / distance );
		}
	}

	[Server]
	IEnumerator SrvRotatePlayer ( Player player, Direction direction, float duration ) {
		player.RpcRotate ( direction , duration );
		yield return new WaitForSeconds ( duration );
	}
	
	bool IsAllPlayersReady () {
		foreach ( Player player in Player.players.Values ) {
			if ( !player.IsPlayerReady () ) {
				return false;
			}
		}
		return true;
	}





	/*** User Interface ***/

	[SerializeField] private GameObject _buttonStartGame;
	[SerializeField] private Text _messageWindow;

	public void OnClickStartGame () {
		_buttonStartGame.SetActive ( false );
		CmdStartGame ();
	}

	[ClientRpc]
	void RpcMessagePush ( string text ) {
		_messageWindow.text += text;
		_messageWindow.text += "\n";
	}

	[ClientRpc]
	void RpcMessageClear () {
		_messageWindow.text = "";
	}
}
