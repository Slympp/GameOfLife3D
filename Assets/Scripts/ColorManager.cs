using System.Collections.Generic;
using UnityEngine;

public class ColorManager : MonoBehaviour {
    public static ColorManager Instance { get; private set; }
    public static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");
    
    [ColorUsageAttribute(true,true)]
    [SerializeField] private Color defaultColor;
    
    [ColorUsageAttribute(true,true)]
    [SerializeField] private Color underPopulationColor;
    
    [ColorUsageAttribute(true,true)]
    [SerializeField] private Color overPopulationColor;
    
    [ColorUsageAttribute(true,true)]
    [SerializeField] private Color survivorColor;
    
    [ColorUsageAttribute(true,true)]
    [SerializeField] private Color reproductionColor;
    
    private Dictionary<CellState, Color> _colorDictionary;

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        _colorDictionary = new Dictionary<CellState, Color> {
                { CellState.Default, defaultColor },
                { CellState.Underpopulation, underPopulationColor },
                { CellState.Overpopulation, overPopulationColor },
                { CellState.Survivor, survivorColor },
                { CellState.Reproduction, reproductionColor }
        };
    }

    public Color GetColor(CellState s) => _colorDictionary[s];
}