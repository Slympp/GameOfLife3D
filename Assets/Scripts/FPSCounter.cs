using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class FPSCounter : MonoBehaviour {
    
    private float _deltaTime;
    private TMP_Text _text;

    private void Awake() => TryGetComponent(out _text);

    private void Update() {
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        _text.SetText($"{_deltaTime * 1000.0f:0.0} ms ({1.0f / _deltaTime:0.} fps)");
    }
}
