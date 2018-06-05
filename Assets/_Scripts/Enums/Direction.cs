
public enum Direction {
	North, East, South, West
}

public static class Directions
{
    public static Direction getLeftDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.North:
                return Direction.West;
            case Direction.East:
                return Direction.North;
            case Direction.South:
                return Direction.East;
            case Direction.West:
                return Direction.South;
            default:
                throw new System.Exception( "Direction does not exist." );
        }
    }

    public static Direction getRightDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.North:
                return Direction.East;
            case Direction.East:
                return Direction.South;
            case Direction.South:
                return Direction.West;
            case Direction.West:
                return Direction.North;
            default:
                throw new System.Exception( "Direction does not exist." );
        }
    }

    public static Direction getOppositeDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.North:
                return Direction.South;
            case Direction.East:
                return Direction.West;
            case Direction.South:
                return Direction.North;
            case Direction.West:
                return Direction.East;
            default:
                throw new System.Exception( "Direction does not exist." );
        }
    }


    public static float getRotation(Direction direction)
    {
        switch (direction)
        {
            case Direction.North:
                return 0.0f;
            case Direction.East:
                return 90.0f;
            case Direction.South:
                return 180.0f;
            case Direction.West:
                return -90.0f;
            default:
                throw new System.Exception("Direction does not exist.");
        }
    }
}
