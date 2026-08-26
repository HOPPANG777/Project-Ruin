//=============================================================================
// File:        MuseumBottleInteractable.cs
// Description: 씬 술병 상호작용과 액체 채움 Transform을 상태에 연결한다.
// History:     08/24/2026 + Created -- Codex
//=============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MuseumBottleInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("museum_bottles.csv와 정확히 일치하는 술병 ID입니다.")]
    [SerializeField] private string _bottleId;
    [Tooltip("아래쪽 Pivot을 기준으로 Y Scale이 증가하는 액체 표현 Transform입니다.")]
    [SerializeField] private Transform _liquidFill;
    [Tooltip("완성 시 액체 Transform의 Local Scale Y입니다.")]
    [Min(0f)]
    [SerializeField] private float _fullScaleY = 1f;

    private IMuseumBottleService _bottles;

    public string BottleId => _bottleId;

    private void OnEnable() => TryBind();
    private void OnDisable()
    {
        if (_bottles != null) _bottles.BottleChanged -= HandleBottleChanged;
        _bottles = null;
    }

    /// <summary>전역 술병 Presenter에 현재 술병 선택 UI 열기를 요청합니다.</summary>
    public void Interact()
    {
        if (!TryBind() || !GameRoot.TryGetInstance(out GameRoot root)
            || root.MuseumBottlePresenter == null)
        {
            Debug.LogWarning($"[MuseumBottleInteractable] 술병 UI가 준비되지 않았습니다. id={_bottleId}", this);
            return;
        }
        root.MuseumBottlePresenter.Open(_bottleId);
    }

    private bool TryBind()
    {
        if (_bottles != null) return true;
        if (!GameRoot.TryGetInstance(out GameRoot root) || root.MuseumBottles == null) return false;
        _bottles = root.MuseumBottles;
        _bottles.BottleChanged += HandleBottleChanged;
        RefreshFill();
        return true;
    }

    private void HandleBottleChanged(string bottleId)
    {
        if (bottleId == _bottleId) RefreshFill();
    }

    private void RefreshFill()
    {
        if (_liquidFill == null || _bottles == null
            || !_bottles.TryGetProgress(_bottleId, out MuseumBottleProgress progress)) return;
        Vector3 scale = _liquidFill.localScale;
        scale.y = _fullScaleY * progress.FillRatio;
        _liquidFill.localScale = scale;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_bottleId))
            Debug.LogError($"[MuseumBottleInteractable] {name}: Bottle ID가 비어 있습니다.", this);
        if (_liquidFill == null)
            Debug.LogWarning($"[MuseumBottleInteractable] {name}: 액체 채움 Transform이 없습니다.", this);
    }
#endif
}
