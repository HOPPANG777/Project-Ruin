//=============================================================================
// File: BeamResetPad.cs
// Description: 플레이어가 발판에 새로 들어왔을 때 등록된 퍼즐을 한 번 초기화한다.
// History: 10/06/2026 + Created -- Codex
//=============================================================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>복수 플레이어 충돌체의 접촉을 합쳐 발판 진입당 한 번 초기화합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class BeamResetPad : MonoBehaviour
{
    [Tooltip("플레이어 진입을 감지할 Trigger입니다. 광선을 차단하지 않습니다.")]
    [SerializeField] private Collider2D _trigger;
    // 매 프레임 또는 플레이어의 자식 충돌체마다 초기화하지 않습니다.
    private readonly HashSet<Collider2D> _occupants = new HashSet<Collider2D>();
    private BeamPuzzleController _owner;

    /// <summary>명시적으로 등록한 관리자입니다.</summary>
    public BeamPuzzleController Owner => _owner;
    /// <summary>발판 진입 Trigger입니다.</summary>
    public Collider2D Trigger => _trigger;

    /// <summary>발판을 하나의 관리자에만 연결합니다.</summary>
    public bool TryBind(BeamPuzzleController owner)
    {
        if (_owner != null && _owner != owner) return false;
        _owner = owner;
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerInteractionController>() == null) return;
        _occupants.RemoveWhere(collider => collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy);
        bool wasEmpty = _occupants.Count == 0;
        if (_occupants.Add(other) && wasEmpty) _owner?.TryResetPuzzle();
    }

    private void OnTriggerExit2D(Collider2D other) => _occupants.Remove(other);
    private void OnDisable() => _occupants.Clear();
}
