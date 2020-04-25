using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
// ReSharper disable InconsistentNaming

public class GameManager : MonoBehaviour {

    [Header("General Settings")]
    [SerializeField] private float offset = .1f;
    [SerializeField] private GameObject cellObject;

    [Header("SettingsHUD")]
    [SerializeField] private TMP_InputField boardXSizeInputField;
    [SerializeField] private TMP_InputField boardYSizeInputField;
    [SerializeField] private TMP_InputField boardZSizeInputField;
    [SerializeField] private TMP_Text cellCountText;
    [SerializeField] private Slider spawnProbabilitySlider;
    [SerializeField] private Button generateButton;
    
    [SerializeField] private TMP_InputField tickDurationInputField;
    [SerializeField] private TMP_InputField tickCountInputField;
    [SerializeField] private Button playButton;
    [SerializeField] private Button stopButton;

    [Header("ChartsHUD")] 
    [SerializeField] private Slider aliveDeadSlider;
    [SerializeField] private TMP_Text aliveDeadPercentageText;
    
    [SerializeField] private Slider survivorSlider;
    [SerializeField] private Slider reproductionSlider;
    [SerializeField] private Slider underPopulationSlider;
    [SerializeField] private Slider overPopulationSlider;
    [SerializeField] private Slider emptySlider;

    private Vector3Int _boardSize;
    private CellController[,,] _board;
    private Stats _stats;
    
    private Transform _transform;

    private bool _isPlaying;
    private bool _isGenerating;
    private int _cachedTickCount;
    
    private void Awake() => _transform = transform;

    void Start() {
        UpdateCellCountText();
        Generate();
    }
    
    #region GAME
    
    public void Generate() {
        if (_isPlaying || _isGenerating)
            return;

        _isGenerating = true;
        UpdateSettingsHUD();
        
        if (_board != null) {
            foreach (Transform t in _transform)
                Destroy(t.gameObject);
        }
        
        RefreshBoardSize();
        _board = new CellController[_boardSize.x, _boardSize.y, _boardSize.z];
        _stats = new Stats(_boardSize.x * _boardSize.y * _boardSize.z);
        
        for (var x = 0; x < _boardSize.x; x++) {
            for (var y = 0; y < _boardSize.y; y++) {
                for (var z = 0; z < _boardSize.z; z++) {
                    
                    var pos = new Vector3(
                        x + x * offset -(_boardSize.x + _boardSize.x * offset) / 2, 
                        y + y * offset -(_boardSize.y + _boardSize.y * offset) / 2, 
                        z + z * offset -(_boardSize.z + _boardSize.z * offset) / 2
                    );
                    _board[x, y, z] = Instantiate(cellObject, pos, Quaternion.identity, transform).GetComponent<CellController>();

                    if (Random.value < SpawnProbability) {
                        _board[x, y, z].ToggleState(CellState.Survivor);
                        _stats.AddSurvivor();
                    }
                }
            }
        }

        UpdateChartsHUDAliveDead();
        UpdateChartsHUDRatio();
        
        _isGenerating = false;
        UpdateSettingsHUD();
    }

    public void Play() {
        if (_isPlaying || _isGenerating)
            return;

        StartCoroutine(nameof(PlayTicks));
    }

    /*
     * Any live cell with fewer than 7 live neighbours dies, as if by underpopulation.
     * Any live cell with between 7 and 10 live neighbours lives on to the next generation.
     * Any live cell with more than 10 live neighbours dies, as if by overpopulation.
     * Any dead cell with between 6 and 11 live neighbours becomes a live cell, as if by reproduction.
     */
    private IEnumerator PlayTicks() {
        _isPlaying = true;
        UpdateSettingsHUD();
        
        _cachedTickCount = TickCount();
        var duration = TickDuration();
        
        for (int t = _cachedTickCount; t > 0; t--) {
            if (!_isPlaying)
                yield break;
            
            tickCountInputField.text = t.ToString();
            
            var changes = new List<StateChange>();
            _stats.Reset();
            
            for (var x = 0; x < _boardSize.x; x++) {
                for (var y = 0; y < _boardSize.y; y++) {
                    for (var z = 0; z < _boardSize.z; z++) {
                        
                        var pos = new Vector3Int(x, y, z);
                        var n = GetNeighboursCount(pos);
                        
                        var current = _board[x, y, z];

                        if (current.IsAlive() && n <= 6) {
                            changes.Add(new StateChange(pos, CellState.Underpopulation));
                            _stats.AddUnderPopulation();
                        } else if (current.IsAlive() && n >= 7 && n <= 10) {
                            changes.Add(new StateChange(pos, CellState.Survivor));
                            _stats.AddSurvivor();
                        } else if (current.IsAlive() && n > 10) {
                            changes.Add(new StateChange(pos, CellState.Overpopulation));
                            _stats.AddOverPopulation();
                        } else if (!current.IsAlive() && n >= 6 && n <= 11) {
                            changes.Add(new StateChange(pos, CellState.Reproduction));
                            _stats.AddReproduction();
                        } else if (!current.IsAlive() && !current.IsDefault)
                            changes.Add(new StateChange(pos, CellState.Default));
                    }
                }
            }

            foreach (var c in changes) {
                _board[c.Pos.x, c.Pos.y, c.Pos.z].ToggleState(c.NewState, duration / 2);
            }
            
            UpdateChartsHUDAliveDead(duration / 2);
            UpdateChartsHUDRatio(duration / 2);

            yield return new WaitForSeconds(duration);
        }

        tickCountInputField.text = _cachedTickCount.ToString();
        
        _isPlaying = false;
        UpdateSettingsHUD();
    }

    public void Stop() {
        if (_isPlaying) {
            tickCountInputField.text = _cachedTickCount.ToString();
            _isPlaying = false;
            UpdateSettingsHUD();
        }
    }

    private int GetNeighboursCount(Vector3Int pos) {
        var n = 0;
        
        for (var x = -1; x <= 1; x++)
            for (var y = -1; y <= 1; y++)
                for (var z = -1; z <= 1; z++) {

                    // Skip self and out of bounds
                    if (x == 0 && y == 0 && z == 0 
                        || x + pos.x < 0 || x + pos.x == _boardSize.x 
                        || y + pos.y < 0 || y + pos.y == _boardSize.y 
                        || z + pos.z < 0 || z + pos.z == _boardSize.z)
                        continue;
                    
                    if (_board[x + pos.x, y + pos.y, z + pos.z].IsAlive())
                        n++;
                }
        
        return n;
    }
    
    #endregion
    
    #region SettingsHUD

    public void UpdateCellCountText() {
        var count = XSize * YSize * ZSize;
        
        // Notify high count of cells -> performance hit
        cellCountText.color = count >= 5000 ? Color.red : Color.white;
        cellCountText.SetText($"{count} cells");
    }

    private void UpdateSettingsHUD() {
        tickDurationInputField.interactable = !_isPlaying;
        tickCountInputField.interactable = !_isPlaying;
        generateButton.interactable = !(_isGenerating || _isPlaying);
        
        playButton.interactable = !_isPlaying;
        stopButton.interactable = _isPlaying;
    }

    private int XSize => int.TryParse(boardXSizeInputField.text, out var v) ? v : 1;
    private int YSize => int.TryParse(boardYSizeInputField.text, out var v) ? v : 1;
    private int ZSize => int.TryParse(boardZSizeInputField.text, out var v) ? v : 1;
    private void RefreshBoardSize() => _boardSize = new Vector3Int(XSize, YSize, ZSize);
    
    private float SpawnProbability => spawnProbabilitySlider.value;

    private float TickDuration() {
        if (string.IsNullOrWhiteSpace(tickDurationInputField.text))
            tickDurationInputField.text = "0.5";
        return float.Parse(tickDurationInputField.text);
    }

    private int TickCount() {
        if (string.IsNullOrWhiteSpace(tickCountInputField.text))
            tickCountInputField.text = "20";
        return int.Parse(tickCountInputField.text);
    }
    
    #endregion
    
    #region ChartsHUD

    private void UpdateChartsHUDAliveDead(float fadeDuration = .5f) {
        var alive = (_stats.SurvivorCount + _stats.ReproductionCount) / _stats.Total;
        StartCoroutine(nameof(FadeSlider), new FadeSliderProperties(aliveDeadSlider, alive, fadeDuration));
        
        aliveDeadPercentageText.SetText($"{alive * 100:F1}% - {(1 - alive) * 100:F1}%");
    }

    private void UpdateChartsHUDRatio(float fadeDuration = .5f) {
        StartCoroutine(nameof(FadeSlider), new FadeSliderProperties(survivorSlider, _stats.SurvivorCount / _stats.Total, fadeDuration));
        StartCoroutine(nameof(FadeSlider), new FadeSliderProperties(reproductionSlider, _stats.ReproductionCount / _stats.Total, fadeDuration));
        StartCoroutine(nameof(FadeSlider), new FadeSliderProperties(underPopulationSlider, _stats.UnderPopulationCount / _stats.Total, fadeDuration));
        StartCoroutine(nameof(FadeSlider), new FadeSliderProperties(overPopulationSlider, _stats.OverPopulationCount / _stats.Total, fadeDuration));
        StartCoroutine(nameof(FadeSlider), new FadeSliderProperties(emptySlider, _stats.EmptyCount / _stats.Total, fadeDuration));
    }

    private IEnumerator FadeSlider(FadeSliderProperties p) {
        float elapsedTime = 0;
        float from = p.Slider.value;
        
        while (elapsedTime < p.Duration) {
            elapsedTime += Time.deltaTime;
            p.Slider.value = Mathf.Lerp(from, p.To, elapsedTime / p.Duration);
            
            yield return new WaitForEndOfFrame();
        }
        p.Slider.value = p.To;
    }

    private struct FadeSliderProperties {
        public readonly Slider Slider;
        public readonly float To;
        public readonly float Duration;
        
        public FadeSliderProperties(Slider slider, float to, float duration) {
            Slider = slider;
            To = to;
            Duration = duration;
        }
    }

    private class Stats {
        public readonly float Total;
        public int SurvivorCount { get; private set; }
        public int ReproductionCount { get; private set; }
        public int UnderPopulationCount { get; private set; }
        public int OverPopulationCount { get; private set; }

        public int EmptyCount => (int)Total - SurvivorCount - ReproductionCount - UnderPopulationCount - OverPopulationCount;

        public Stats(int total) {
            Total = total;
            Reset();
        }

        public void Reset() {
            SurvivorCount = 0;
            ReproductionCount = 0;
            UnderPopulationCount = 0;
            OverPopulationCount = 0;
        }

        public void AddSurvivor() => SurvivorCount++;
        public void AddReproduction() => ReproductionCount++;
        public void AddUnderPopulation() => UnderPopulationCount++;
        public void AddOverPopulation() => OverPopulationCount++;
    }
    
    #endregion
}
