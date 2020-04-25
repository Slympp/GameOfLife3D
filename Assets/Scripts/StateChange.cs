using UnityEngine;

public struct StateChange {
    public Vector3Int Pos { get; }
    public CellState NewState { get; }
    
    public StateChange(Vector3Int pos, CellState newState) {
        Pos = pos;
        NewState = newState;
    }
}
