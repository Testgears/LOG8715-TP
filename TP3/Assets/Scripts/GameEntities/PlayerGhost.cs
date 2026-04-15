using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerGhost : NetworkBehaviour
{
    [SerializeField] 
    private Player m_Player;
    [SerializeField] 
    private SpriteRenderer m_SpriteRenderer;

    public override void OnNetworkSpawn()
    {
        // L'entite qui appartient au client est recoloriee en rouge
        if (IsOwner)
        {
            m_SpriteRenderer.color = Color.red;
        }
    }

    private void Update()
    {
        // Le propriétaire voit sa position prévue (sans lag)
        // Les autres voient la position répliquée par le serveur
        if (IsOwner)
        {
            transform.localPosition = (Vector3)m_Player.PredictedPosition;
        }
        else
        {
            transform.localPosition = (Vector3)m_Player.Position;
        }
            
    }
}
