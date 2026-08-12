//=============================================================================
// File:        PlayerHeartView.cs
//
// Description: 피격 시 플레이어의 현재 하트를 잠시 표시합니다.
//
// History:     08/08/2026 + Created -- Codex
//              08/09/2026 + 피격 시 일시 표시와 소진 하트 표현 추가 -- Codex
//
//=============================================================================

using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerHeartView : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private UnitPlayer _player;
    [Header("Hearts")]
    [Tooltip("피격 시 잠시 표시되는 하트 묶음입니다.")]
    [SerializeField] private GameObject _heartRoot;
    [SerializeField] private GameObject[] _fullHeartObjects;
    [SerializeField] private GameObject[] _emptyHeartObjects;
    [Min(0.1f)]
    [SerializeField] private float _visibleDuration = 1.5f;

    private bool _isBound;
    private float _hideAtTime;

    private void OnEnable()
    {
        if (!TryValidateSetup())
        {
            return;
        }
        _player.HealthChanged += HandleHealthChanged;
        _isBound = true;
        RefreshHearts(_player.CurrentHealth, _player.MaxHealth);
        _heartRoot.SetActive(false);
    }

    private void OnDisable()
    {
        if (_isBound && _player != null)
        {
            _player.HealthChanged -= HandleHealthChanged;
        }
        _isBound = false;
    }

    private void Update()
    {
        if (_isBound && _heartRoot.activeSelf && Time.unscaledTime >= _hideAtTime)
        {
            _heartRoot.SetActive(false);
        }
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        RefreshHearts(currentHealth, maxHealth);
        bool showDamage = currentHealth < maxHealth;
        _heartRoot.SetActive(showDamage);
        if (showDamage)
        {
            _hideAtTime = Time.unscaledTime + _visibleDuration;
        }
    }

    private void RefreshHearts(float currentHealth, float maxHealth)
    {
        int fullHeartCount = maxHealth > 0.0f
            ? Mathf.CeilToInt(currentHealth / maxHealth * _fullHeartObjects.Length)
            : 0;
        fullHeartCount = Mathf.Clamp(fullHeartCount, 0, _fullHeartObjects.Length);
        for (int i = 0; i < _fullHeartObjects.Length; i++)
        {
            bool isFull = i < fullHeartCount;
            _fullHeartObjects[i].SetActive(isFull);
            _emptyHeartObjects[i].SetActive(!isFull);
        }
    }

    private bool TryValidateSetup()
    {
        if (_player == null || _heartRoot == null || _fullHeartObjects == null
            || _emptyHeartObjects == null || _fullHeartObjects.Length == 0
            || _fullHeartObjects.Length != _emptyHeartObjects.Length
            || float.IsNaN(_visibleDuration) || float.IsInfinity(_visibleDuration)
            || _visibleDuration < 0.1f)
        {
            Debug.LogError("[PlayerHeartView] 플레이어와 동일 개수의 하트 View가 필요합니다.", this);
            return false;
        }
        for (int i = 0; i < _fullHeartObjects.Length; i++)
        {
            if (_fullHeartObjects[i] == null || _emptyHeartObjects[i] == null)
            {
                Debug.LogError($"[PlayerHeartView] 하트 참조가 비어 있습니다. index={i}", this);
                return false;
            }
        }
        return true;
    }
}
