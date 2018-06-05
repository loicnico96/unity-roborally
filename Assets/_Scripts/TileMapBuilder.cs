using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(TileMap))]
public class TileMapBuilder : MonoBehaviour {
	public bool resetMap = false;
	public bool buildMap = false;
	public Tile [] tilePrefabs = new Tile [ 0 ];
	public TileEnum [] tiles = new TileEnum [ TileMap.Size * TileMap.Size ];

	void Update () {
		// Reset and build the map
		if ( resetMap ) {
			ResetTileMap ();
			BuildTileMap ();
			resetMap = false;
		}
		// Build the map
		if ( buildMap ) {
			BuildTileMap ();
			buildMap = false;
		}
	}

	void ResetTileMap () {
		for ( int i = 0 ; i < TileMap.Size * TileMap.Size ; i++ ) {
			tiles [ i ] = TileEnum.TileFloor;
		}
	}

	void BuildTileMap () {
		// Delete all children
		foreach ( Transform t in transform ) {
			if ( t != transform ) {
				GameObject.DestroyImmediate ( t.gameObject );
			}
		}

		// Rebuild the map from scratch
		TileMap map = GetComponent<TileMap> ();
		map.tiles.Clear ();
		for ( int i = 0 ; i < TileMap.Size * TileMap.Size ; i++ ) {
			float xCoordF = ( i % TileMap.Size ) - ( TileMap.Size - 1 ) / 2.0f;
			float yCoordF = ( i / TileMap.Size ) - ( TileMap.Size - 1 ) / 2.0f;
			if ( tiles [ i ] != TileEnum.TileHole ) {
				foreach ( Tile tilePrefab in tilePrefabs ) {
					if ( tilePrefab.tileIndex == tiles [ i ] ) {
						GameObject tile = (GameObject) Instantiate ( tilePrefab.gameObject , transform );
						tile.transform.localPosition = new Vector3 ( xCoordF , yCoordF , 0.0f );
						tile.transform.localRotation = Quaternion.identity;
						map.tiles.Add ( i , tile.GetComponent<Tile> () );
						continue;
					}
				}
			}
		}
	}
}
