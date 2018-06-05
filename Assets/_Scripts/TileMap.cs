using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent( typeof( NetworkIdentity ) )]
public class TileMap : NetworkBehaviour {
	public static readonly int Size = 12;

	public Dictionary<int, Tile> tiles = new Dictionary<int, Tile> ();

	public Tile GetTileAt ( int x , int y ) {
		if ( x >= 0 && x < Size && y >= 0 && y < Size ) {
			return tiles [ y * Size + x ];
		} else {
			return null;
		}
	}
}
