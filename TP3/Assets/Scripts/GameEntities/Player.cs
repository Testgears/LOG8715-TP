using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField]
    private float m_Velocity;

    [SerializeField]
    private float m_Size = 1;

    private GameState m_GameState;

    private bool m_HasPendingReconciliation = false;
    private int m_PendingServerTick = -1;

    private GameState GameState
    {
        get
        {
            if (m_GameState == null)
                m_GameState = FindFirstObjectByType<GameState>();
            return m_GameState;
        }
    }

    private NetworkVariable<Vector2> m_Position = new NetworkVariable<Vector2>();

    private NetworkVariable<int> m_ServerConfirmedTick = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Vector2 Position => m_Position.Value;

    public Vector2 m_PredictedPosition;
    public Vector2 PredictedPosition => IsOwner ? m_PredictedPosition : m_Position.Value;

    private struct InputRecord
    {
        public int Tick;
        public Vector2 Input;
    }
    private List<InputRecord> m_InputHistory = new List<InputRecord>();

    private Queue<(Vector2 input, int tick)> m_InputQueue = new Queue<(Vector2, int)>();

    private int m_LastReconciledTick = -1;
    private int m_LastSentTick = -1;

    private void Awake()
    {
        m_GameState = FindFirstObjectByType<GameState>();
    }

    public override void OnNetworkSpawn()
    {
        m_PredictedPosition = m_Position.Value;

        if (IsOwner)
            m_ServerConfirmedTick.OnValueChanged += OnServerTickConfirmed;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            m_ServerConfirmedTick.OnValueChanged -= OnServerTickConfirmed;
    }

    private void OnServerTickConfirmed(int oldTick, int newTick)
    {
        if (newTick <= m_LastReconciledTick) return;

        m_PendingServerTick = newTick;
        m_HasPendingReconciliation = true;
    }

    private void FixedUpdate()
    {
        if (GameState == null || GameState.IsStunned)
            return;

        if (IsServer)
            UpdatePositionServer();

        if (IsClient && IsOwner)
        {
            ReconcileIfNeeded();
            UpdateInputClient();
        }
    }

    private void ReconcileIfNeeded()
    {
        if (!m_HasPendingReconciliation) return;
        if (m_PendingServerTick <= m_LastReconciledTick) return;

        m_LastReconciledTick = m_PendingServerTick;
        m_HasPendingReconciliation = false;

        m_InputHistory.RemoveAll(r => r.Tick <= m_LastReconciledTick);

        Vector2 recomputed = m_Position.Value;
        foreach (var record in m_InputHistory)
            recomputed = SimulateMove(recomputed, record.Input);

        m_PredictedPosition = recomputed;
    }

    private void UpdatePositionServer()
    {
        if (m_InputQueue.Count > 0)
        {
            var (input, tick) = m_InputQueue.Dequeue();
            m_Position.Value = SimulateMove(m_Position.Value, input);
            m_ServerConfirmedTick.Value = tick;
        }
    }

    private Vector2 SimulateMove(Vector2 pos, Vector2 input)
    {
        pos += input * m_Velocity * Time.fixedDeltaTime;

        if (GameState == null) return pos;
        var size = GameState.GameSize;

        if (pos.x - m_Size < -size.x) pos = new Vector2(-size.x + m_Size, pos.y);
        else if (pos.x + m_Size > size.x) pos = new Vector2(size.x - m_Size, pos.y);
        if (pos.y + m_Size > size.y) pos = new Vector2(pos.x, size.y - m_Size);
        else if (pos.y - m_Size < -size.y) pos = new Vector2(pos.x, -size.y + m_Size);

        return pos;
    }

    private void UpdateInputClient()
    {
        int tick = NetworkUtility.GetLocalTick();
        if (tick == m_LastSentTick) return;
        m_LastSentTick = tick;

        Vector2 inputDirection = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) inputDirection += Vector2.up;
        if (Input.GetKey(KeyCode.A)) inputDirection += Vector2.left;
        if (Input.GetKey(KeyCode.S)) inputDirection += Vector2.down;
        if (Input.GetKey(KeyCode.D)) inputDirection += Vector2.right;
        inputDirection = inputDirection.normalized;

        m_InputHistory.Add(new InputRecord { Tick = tick, Input = inputDirection });
        m_PredictedPosition = SimulateMove(m_PredictedPosition, inputDirection);
        SendInputServerRpc(inputDirection, tick);
    }

    [ServerRpc]
    private void SendInputServerRpc(Vector2 input, int tick)
    {
        m_InputQueue.Enqueue((input, tick));
    }
}
