
public class TilePosition {
	public TileMap map;
	public int x;
	public int y;

	public Tile GetTile () {
		return map.GetTileAt ( x , y );
	}

	public TilePosition Offset ( Direction direction, int distance = 1 ) {
		switch ( direction ) {
			case Direction.North:
				if ( y < TileMap.Size - 1 ) {
					return new TilePosition () { map = map, x = x, y = y + distance };
				} else {
					return null;
				}
			case Direction.East:
				if ( x < TileMap.Size - 1 ) {
					return new TilePosition () { map = map, x = x + distance, y = y };
				} else {
					return null;
				}
			case Direction.South:
				if ( y > 0 ) {
					return new TilePosition () { map = map, x = x, y = y - distance };
				} else {
					return null;
				}
			case Direction.West:
				if ( x > 0 ) {
					return new TilePosition () { map = map, x = x - distance, y = y };
				} else {
					return null;
				}
			default:
				return null;
		}
	}
}
