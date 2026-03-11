using UnityEngine;

[CreateAssetMenu(fileName = "Ex3Config", menuName = "Config/Ex3", order = 1)]
public class Ex3Config : ScriptableObject
{
    [SerializeField] public int plantCount = 200;
    [SerializeField] public int preyCount = 200;
    [SerializeField] public int predatorCount = 200;
    [SerializeField] public int gridSize = 600;

    public static readonly float PreySpeed = 1;
    public static readonly float PredatorSpeed = 0.5f;
    public static readonly float TouchingDistance = 0.5f;
}

