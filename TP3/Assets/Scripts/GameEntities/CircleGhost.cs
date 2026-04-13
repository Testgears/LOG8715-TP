using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CircleGhost : NetworkBehaviour
{
    [SerializeField]
    private MovingCircle m_MovingCircle;

    private GameState m_GameState;

    private void Awake()
    {
        m_GameState = FindFirstObjectByType<GameState>();
    }

    private void Update()
    {
        //transform.position = m_MovingCircle.Position;
        if (IsServer)
        {
            transform.position = (Vector3)m_MovingCircle.Position;
            return;
        }

        // Client : extrapoler la position du cercle vers l'avant de RTT/2
        // (l'état du serveur que nous avons reçu a été envoyé il y a RTT/2)
        float latency = m_GameState != null ? m_GameState.CurrentRTT / 2f : 0f;
        Vector2 predicted = m_MovingCircle.Position + m_MovingCircle.Velocity * latency;
        transform.position = (Vector3)predicted;   
    }
}
