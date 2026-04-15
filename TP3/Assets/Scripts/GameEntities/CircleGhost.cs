using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CircleGhost : NetworkBehaviour
{
    [SerializeField]
    private MovingCircle m_MovingCircle;

    private GameState m_GameState;
    private const float m_Radius = 1f;

    private void Awake()
    {
        m_GameState = FindFirstObjectByType<GameState>();
    }

    private void Update()
    {
        //transform.position = m_MovingCircle.Position;
        if (IsServer || m_GameState == null)
        {
            transform.localPosition = (Vector3)m_MovingCircle.Position;
            return;
        }

        // Client : extrapoler la position du cercle vers l'avant de RTT/2
        // (l'état du serveur que nous avons reçu a été envoyé il y a RTT/2)
        Vector2 predictedPosition = m_MovingCircle.Position;
        Vector2 predictedVelocity = m_MovingCircle.Velocity;

        int localTick = NetworkUtility.GetLocalTick();
        int serverTick = m_MovingCircle.ServerTick;

        int ticksToPredict = Mathf.Max(0, localTick - serverTick);

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
