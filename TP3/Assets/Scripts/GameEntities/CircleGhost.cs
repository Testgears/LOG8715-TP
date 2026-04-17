using Unity.Netcode;
using UnityEngine;

public class CircleGhost : NetworkBehaviour
{
    [SerializeField]
    private MovingCircle m_MovingCircle;

    private GameState m_GameState;
    private bool m_WasStunned = false;
    private Vector2 m_FrozenPosition;
    private int m_FrozenServerTick = -1;
    private int m_FrozenLocalTick;
    private int m_StunEndLocalTick = -1;

    private void Awake()
    {
        m_GameState = FindFirstObjectByType<GameState>();
    }

    private void Update()
    {
        if (IsServer || m_GameState == null)
        {
            transform.localPosition = (Vector3)m_MovingCircle.Position;
            return;
        }

        int localTick = NetworkUtility.GetLocalTick();
        int serverTick = m_MovingCircle.ServerTick;

        bool isStunned = m_GameState.IsStunned;

        if (!m_WasStunned && isStunned)
        {
            m_FrozenPosition = (Vector2)transform.localPosition;
            m_FrozenServerTick = serverTick;
            m_FrozenLocalTick = localTick;
        }

        if (m_WasStunned && !isStunned)
            m_StunEndLocalTick = localTick;

        m_WasStunned = isStunned;

        if (isStunned)
        {
            transform.localPosition = (Vector3)m_FrozenPosition;
            return;
        }

        Vector2 predictedPosition = m_MovingCircle.Position;
        Vector2 predictedVelocity = m_MovingCircle.Velocity;

        int maxTicks = (int)(NetworkUtility.GetLocalTickRate() * 2);
        int ticksToPredict;

        if (m_StunEndLocalTick >= 0)
        {
            int preStunTicks = m_FrozenLocalTick - m_FrozenServerTick;
            ticksToPredict = Mathf.Clamp(localTick - m_StunEndLocalTick, 0, preStunTicks);
        }
        else
        {
            ticksToPredict = Mathf.Clamp(localTick - serverTick, 0, maxTicks);
        }

        for (int i = 0; i < ticksToPredict; i++)
        {
            SimulateCircleTick(ref predictedPosition, ref predictedVelocity);
        }

        transform.localPosition = (Vector3)predictedPosition;
    }

    private void SimulateCircleTick(ref Vector2 position, ref Vector2 velocity)
    {
        position += velocity * Time.fixedDeltaTime;

        var size = m_GameState.GameSize;
        float radius = m_MovingCircle.Radius;

        if (position.x - radius < -size.x)
        {
            position = new Vector2(-size.x + radius, position.y);
            velocity *= new Vector2(-1, 1);
        }
        else if (position.x + radius > size.x)
        {
            position = new Vector2(size.x - radius, position.y);
            velocity *= new Vector2(-1, 1);
        }

        if (position.y + radius > size.y)
        {
            position = new Vector2(position.x, size.y - radius);
            velocity *= new Vector2(1, -1);
        }
        else if (position.y - radius < -size.y)
        {
            position = new Vector2(position.x, -size.y + radius);
            velocity *= new Vector2(1, -1);
        }
    }
}
