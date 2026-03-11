using UnityEngine;
public struct UnoptimisedStruct1
{
    public UnoptimizedStruct2 mainFriend;
    public Vector3 position;
    public float velocity;
    public float[] distancesFromObjectives;
    public UnoptimizedStruct2[] otherFriends;
    public double range;
    public int baseHP;
    public float size;
    public int nbAllies;
    public int currentHp;
    public byte colorAlpha;
    public bool isVisible;
    public bool isStanding;
    public bool canJump;
    
    
    public UnoptimisedStruct1(float velocity, bool canJump, int baseHP, int nbAllies, Vector3 position, int currentHp, float[] distancesFromObjectives, byte colorAlpha, double range, UnoptimizedStruct2 mainFriend, bool isVisible, UnoptimizedStruct2[] otherFriends, bool isStanding, float size)
    {
        this.velocity = velocity;
        this.canJump = canJump;
        this.baseHP = baseHP;
        this.nbAllies = nbAllies;
        this.position = position;
        this.currentHp = currentHp;
        this.distancesFromObjectives = distancesFromObjectives;
        this.colorAlpha = colorAlpha;
        this.range = range;
        this.mainFriend = mainFriend;
        this.isVisible = isVisible;
        this.otherFriends = otherFriends;
        this.isStanding = isStanding;
        this.size = size;
    }
}

public enum FriendState
{
    isFolowing,
    isSearching,
    isPatrolling,
    isGuarding,
    isJumping,
}

public struct UnoptimizedStruct2 
{
    public float height;
    public FriendState currentState;
    public float velocity;
    public int currentObjective;
    public double maxSight;
    public float acceleration;
    public Vector3 position;
    public float maxVelocity;
    public bool isAlive;
    public bool canJump;
    
    public UnoptimizedStruct2(bool isAlive, float height, FriendState currentState, float velocity, int currentObjective, double maxSight, bool canJump, float acceleration, Vector3 position, float maxVelocity)
    {
        this.isAlive = isAlive;
        this.height = height;
        this.currentState = currentState;
        this.velocity = velocity;
        this.currentObjective = currentObjective;
        this.maxSight = maxSight;
        this.canJump = canJump;
        this.acceleration = acceleration;
        this.position = position;
        this.maxVelocity = maxVelocity;
    }
}
