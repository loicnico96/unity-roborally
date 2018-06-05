using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

[RequireComponent( typeof( NetworkIdentity ) )]
public class Player : NetworkBehaviour
{
    public static Dictionary<NetworkInstanceId, Player> players = new Dictionary<NetworkInstanceId, Player>();
    public static Player localPlayer = null;

    public delegate void PlayerEvent(Player player);
    public static event PlayerEvent OnPlayerConnection;
    public static event PlayerEvent OnPlayerDisconnection;

    private NetworkInstanceId _playerId;
    public NetworkInstanceId playerId { get { return _playerId; } }

    [SerializeField] private string _playerName;
    public string playerName { get { return _playerName; } }

    void Start()
    {
        _playerId = this.netId;
        players.Add( _playerId, this );
        if (string.IsNullOrEmpty( _playerName ))
        {
            _playerName = "Player" + _playerId;
        }
        if (isLocalPlayer)
        {
            localPlayer = this;
        }
        if (OnPlayerConnection != null)
        {
            OnPlayerConnection( this );
        }
    }

    void OnDestroy()
    {
        players.Remove( _playerId );
        if (isLocalPlayer)
        {
            localPlayer = null;
        }
        if (OnPlayerDisconnection != null)
        {
            OnPlayerDisconnection( this );
        }
    }

    [ClientCallback]
    void Update()
    {
        UpdateCheckPosition();
        if (isLocalPlayer)
        {
            UpdateCheckInput();
        }
    }




    /*****************************
	 *      User Interface       *
	 *****************************/
    [SerializeField] private Text _commandTextAvailable;
    [SerializeField] private Text _commandTextSelected;

    void UpdateCommandSelectionInterface()
    {
        // Updating available commands
        if (_commandReady && _commandCount > 0)
        {
            if (_commandCurrent < 0)
            {
                _commandTextAvailable.text = string.Format( "<b>Waiting for other players...</b>" );
            } else
            {
                _commandTextAvailable.text = "";
            }
        } else
        {
            _commandTextAvailable.text = string.Format( "<b>Available commands</b>\n" );
            for (int i = 0; i < _availableCommands.Count; i++)
            {
                if (_selectedCommands.Contains( i ))
                {
                    _commandTextAvailable.text += string.Format( "<color=#404040>[{0}] {1}</color>\n", i + 1, _availableCommands[ i ] );
                } else
                {
                    _commandTextAvailable.text += string.Format( "[{0}] {1}\n", i + 1, _availableCommands[ i ] );
                }
            }
        }

        // Updating selected commands
        _commandTextSelected.text = string.Format( "<b>Selected commands</b>\n" );
        for (int j = 0; j < _selectedCommands.Count; j++)
        {
            int i = _selectedCommands[ j ];
            if (_commandCurrent == j)
            {
                _commandTextSelected.text += string.Format( "<b>>>></b> {1} [{0}]\n", i + 1, _availableCommands[ i ] );
            } else if (_commandCurrent > j)
            {
                _commandTextSelected.text += string.Format( "<color=#404040>{1} [{0}]</color>\n", i + 1, _availableCommands[ i ] );
            } else
            {
                _commandTextSelected.text += string.Format( "{1} [{0}]\n", i + 1, _availableCommands[ i ] );
            }
        }
        if (_commandCurrent < 0 && !_commandReady && _selectedCommands.Count > 0)
        {
            _commandTextSelected.text += string.Format( "Press [Back] to cancel the last command.\n" );
            if (_selectedCommands.Count == _commandCount)
            {
                _commandTextSelected.text += string.Format( "Press [Return] to validate the program.\n" );
            }
        }
    }

    void UpdateCheckInput()
    {
        // Numbers or Numeric Pad = Select a command
        if (_selectedCommands.Count < _commandCount)
        {
            for (int i = 0; i < _availableCommands.Count; i++)
            {
                string key = ( i + 1 ).ToString();
                if (( Input.GetKeyDown( key ) || Input.GetKeyDown( "[" + key + "]" ) ) && !_selectedCommands.Contains( i ))
                {
                    _selectedCommands.Add( i );
                    UpdateCommandSelectionInterface();
                }
            }
        }

        // Cancel = Remove the last command (if not validated)
        if (Input.GetKeyDown( "backspace" ) && _commandCurrent < 0 && _selectedCommands.Count > 0)
        {
            _selectedCommands.RemoveAt( _selectedCommands.Count - 1 );
            UpdateCommandSelectionInterface();
        }

        // Submit = Validate all commands
        if (Input.GetKeyDown( "return" ) && _commandCurrent < 0 && !_commandReady && _selectedCommands.Count == _commandCount)
        {
            CmdTurnValidateCommand( _selectedCommands.ToArray() );
            UpdateCommandSelectionInterface();
        }
    }




    /*****************************
	 *         Commands          *
	 *****************************/
    private int _commandCount = 0;
    private int _commandCurrent = -1;
    private bool _commandReady = false;
    private List<Command> _availableCommands = new List<Command>();
    private List<int> _selectedCommands = new List<int>();

    [ClientRpc]
    public void RpcTurnChooseCommand(Command[] commands, int count)
    {
        _commandCount = count;
        _commandCurrent = -1;
        _commandReady = ( count == 0 );
        _availableCommands.Clear();
        _availableCommands.AddRange( commands );
        _selectedCommands.Clear();
        if (isLocalPlayer)
        {
            UpdateCommandSelectionInterface();
        }
    }

    [ClientRpc]
    public void RpcTurnExecuteCommand(int index)
    {
        _commandCurrent = index;
        _commandReady = false;
        if (isLocalPlayer)
        {
            UpdateCommandSelectionInterface();
        }
    }

    [Command]
    public void CmdTurnValidateCommand(int[] selectedCommands)
    {
        _selectedCommands.Clear();
        _selectedCommands.AddRange( selectedCommands );
        RpcTurnValidateCommand();
    }

    [ClientRpc]
    public void RpcTurnValidateCommand()
    {
        _commandReady = true;
        if (isLocalPlayer)
        {
            UpdateCommandSelectionInterface();
        }
    }

    public Command GetSelectedCommand(int index)
    {
        if (index < _selectedCommands.Count)
        {
            return _availableCommands[ _selectedCommands[ index ] ];
        } else
        {
            return Command.None;
        }
    }

    public bool IsPlayerReady()
    {
        return _commandReady;
    }



    /*****************************
	 *         Movement          *
	 *****************************/
    // 3D coordinates
    public Vector3 _moveDestination;
    public float _moveSpeed;
    public Quaternion _rotateDestination;
    public float _rotateSpeed;
    // Tile coordinates
    public TileMap _positionTileMap;
    public TileMap tileMap { get { return _positionTileMap; } }
    public int _positionTileX;
    public int _positionTileY;
    public Direction _direction;
    public Direction direction { get { return _direction; } }

    [ClientRpc]
    public void RpcSetPosition(NetworkInstanceId mapId, int x, int y)
    {
        _positionTileMap = ClientScene.FindLocalObject( mapId ).GetComponent<TileMap>();
        _positionTileX = x;
        _positionTileY = y;
        Tile tile = _positionTileMap.GetTileAt( x, y );
        if (tile != null)
        {
            transform.position = tile.transform.position;
            _moveDestination = tile.transform.position;
            _moveSpeed = 0.0f;
        }
    }

    [ClientRpc]
    public void RpcMove(NetworkInstanceId mapId, Direction direction, float duration)
    {
        _positionTileMap = ClientScene.FindLocalObject( mapId ).GetComponent<TileMap>();

        switch (direction)
        {
            case Direction.North:
                _positionTileY++;
                break;
            case Direction.East:
                _positionTileX++;
                break;
            case Direction.South:
                _positionTileY--;
                break;
            case Direction.West:
                _positionTileX--;
                break;
            default:
                throw new System.Exception( "Direction does not exist." );
        }

        Tile tile = _positionTileMap.GetTileAt( _positionTileX, _positionTileY );
        if (tile != null)
        {
            _moveDestination = tile.transform.position;
            _moveSpeed = ( _moveDestination - transform.position ).magnitude / duration;
        }
    }

    [ClientRpc]
    public void RpcSetDirection(Direction direction)
    {
        transform.rotation = Quaternion.Euler( 0.0f, Directions.getRotation( direction ), 0.0f );
        _rotateDestination = transform.rotation;
        _rotateSpeed = 0.0f;
    }

    [ClientRpc]
    public void RpcRotate(Direction direction, float duration)
    {
        _rotateDestination = Quaternion.Euler( 0.0f, Directions.getRotation( direction ), 0.0f );
        _rotateSpeed = Quaternion.Angle( _rotateDestination, transform.rotation ) / duration;
    }

    void UpdateCheckPosition()
    {
        transform.position = Vector3.MoveTowards( transform.position, _moveDestination, _moveSpeed * Time.deltaTime );
        transform.rotation = Quaternion.RotateTowards( transform.rotation, _rotateDestination, _rotateSpeed * Time.deltaTime );
    }
}