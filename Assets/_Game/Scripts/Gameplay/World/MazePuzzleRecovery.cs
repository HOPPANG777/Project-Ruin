//=============================================================================
// File:        MazePuzzleRecovery.cs
//
// Description: 진행 중인 미로에서 플레이어 사망 시 체크포인트와 퍼즐을 복구합니다.
//
// History:     08/08/2026 + Created -- Codex
//
//=============================================================================

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MazePuzzleRecovery : MonoBehaviour
{
    [Header("Puzzle")]
    [Tooltip("사망 시 초기화할 진행 중인 미로 발판 퍼즐입니다.")]
    [SerializeField] private MazePlatePuzzleController _puzzle;
    [Tooltip("미로 재도전 위치입니다.")]
    [SerializeField] private Transform _checkpoint;

    [Header("Player")]
    [Tooltip("사망과 체력 복구를 감시할 플레이어입니다.")]
    [SerializeField] private UnitPlayer _player;
    [SerializeField] private PlayerMoveController _moveController;
    [SerializeField] private PlayerInteractionController _interactionController;

    private Rigidbody2D _playerBody;
    private bool _isSetupValid;
    private bool _isRecovering;
    private bool _ownsPlayerLock;

    private void OnEnable()
    {
        _isSetupValid = TryValidateSetup();
        if (!_isSetupValid)
        {
            return;
        }

        _player.TryGetComponent(out _playerBody);
        _player.Died += HandlePlayerDied;
    }

    private void OnDisable()
    {
        if (_player != null)
        {
            _player.Died -= HandlePlayerDied;
        }
        RestorePlayerControl();
        _isRecovering = false;
    }

    private void HandlePlayerDied()
    {
        if (!_isSetupValid || _isRecovering
            || _puzzle.State != MazePlatePuzzleState.MAZE_PLATE_PUZZLE_IN_PROGRESS)
        {
            return;
        }

        _isRecovering = true;
        SetPlayerControl(false);
        IStageFlowService stageFlow = GameRoot.TryGetInstance(out GameRoot gameRoot)
            ? gameRoot.StageFlow : null;
        if (stageFlow == null || stageFlow.IsTransitioning)
        {
            RecoverAtCheckpoint();
            FinishRecovery();
            return;
        }
        RecoverWithFadeAsync(stageFlow).Forget();
    }

    private async UniTaskVoid RecoverWithFadeAsync(IStageFlowService stageFlow)
    {
        try
        {
            await stageFlow.FadeThroughAsync(RecoverAtCheckpoint);
            if (_player != null && !_player.IsAlive)
            {
                RecoverAtCheckpoint();
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            RecoverAtCheckpoint();
        }
        finally
        {
            FinishRecovery();
        }
    }

    private void RecoverAtCheckpoint()
    {
        if (_player == null || _checkpoint == null || _puzzle == null)
        {
            return;
        }

        if (!_puzzle.TryResetPuzzle())
        {
            Debug.LogWarning("[MazePuzzleRecovery] 진행 중인 퍼즐을 초기화하지 못했습니다.", this);
        }
        _player.transform.position = _checkpoint.position;
        if (_playerBody != null)
        {
            _playerBody.linearVelocity = Vector2.zero;
            _playerBody.angularVelocity = 0.0f;
        }
        _player.TryRestoreFullHealth();
    }

    private void SetPlayerControl(bool canControl)
    {
        _interactionController.SetCanInteract(canControl);
        _moveController.SetCanMove(canControl);
        _ownsPlayerLock = !canControl;
    }

    private void RestorePlayerControl()
    {
        if (!_ownsPlayerLock)
        {
            return;
        }
        _interactionController?.SetCanInteract(true);
        _moveController?.SetCanMove(true);
        _ownsPlayerLock = false;
    }

    private void FinishRecovery()
    {
        RestorePlayerControl();
        _isRecovering = false;
    }

    private bool TryValidateSetup()
    {
        if (_puzzle != null && _checkpoint != null && _player != null
            && _moveController != null && _interactionController != null)
        {
            return true;
        }
        Debug.LogError("[MazePuzzleRecovery] 퍼즐, 체크포인트와 플레이어 참조가 필요합니다.", this);
        return false;
    }
}
