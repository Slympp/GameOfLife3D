using System.Collections;
using UnityEngine;

public class CellController : MonoBehaviour {
    
    private CellState _state = CellState.Default;

    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;

    void Awake() {
        TryGetComponent(out _renderer);
        _propertyBlock = new MaterialPropertyBlock();
    }

    public void ToggleState(CellState newState, float duration = 0f) {
        StartCoroutine(nameof(FadeColor), new FadeColorProperties(
            duration, 
            ColorManager.Instance.GetColor(_state),
            ColorManager.Instance.GetColor(newState)
        ));
        _state = newState;
    }

    private IEnumerator FadeColor(FadeColorProperties p) {
        float elapsedTime = 0;
        
        while (elapsedTime < p.Duration) {
            elapsedTime += Time.deltaTime;
            UpdateColor(Color.Lerp(p.StartColor, p.TargetColor, elapsedTime / p.Duration));
            
            yield return new WaitForEndOfFrame();
        }
        UpdateColor(p.TargetColor);
    }

    private void UpdateColor(Color c) {
        _propertyBlock.SetColor(ColorManager.ColorProperty, c);
        _renderer.SetPropertyBlock(_propertyBlock);
    }
    
    public bool IsAlive() => _state == CellState.Survivor || _state == CellState.Reproduction;
    public bool IsDefault => _state == CellState.Default;
    
    private struct FadeColorProperties {
        public float Duration { get; }
        public Color StartColor { get; }
        public Color TargetColor { get; }
        
        public FadeColorProperties(float duration, Color startColor, Color targetColor) {
            Duration = duration;
            StartColor = startColor;
            TargetColor = targetColor;
        }
    }
}
