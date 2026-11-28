//=============================================================================
// File:        KingPetController.cs
//
// Description: 챕터 0 왕 NPC 대화 종료 시 동행 왕(펫)으로 전환하고, 종료 컷씬 시작 시 제거한다.
//              동행 상태는 저장하지 않으므로 씬을 다시 불러오면 초기 배치로 돌아간다.
//
// History:     11/07/2026 + Created -- Claude
//=============================================================================

using UnityEngine;

/// <summary>왕 NPC 대화 종료를 감지해 펫으로 교체하고, EndCompanion 호출로 제거합니다.</summary>
[DisallowMultipleComponent]
public sealed class KingPetController : MonoBehaviour
{
    [Tooltip("대화가 끝나면 제거할 기존 왕 NPC 오브젝트입니다.")]
    [SerializeField] private GameObject _kingNpc;

    [Tooltip("씬에 배치한 동행용 왕(펫)입니다. 시작 시 비활성화됩니다.")]
    [SerializeField] private KingPetFollower _pet;

    [SerializeField] private PlayerMoveController _player;

    [Tooltip("이 대화 그래프가 끝나면 동행을 시작합니다.")]
    [SerializeField] private string _kingDialogueGraphId;

    private IDialogueService _dialogue;
    private bool _used;

    private void Start()
    {
        _pet.gameObject.SetActive(false);
        if (GameRoot.TryGetInstance(out GameRoot root) && root.Dialogue != null)
        {
            _dialogue = root.Dialogue;
            _dialogue.DialogueEnded += OnDialogueEnded;
        }
    }

    private void OnDestroy()
    {
        if (_dialogue != null) _dialogue.DialogueEnded -= OnDialogueEnded;
    }

    private void OnDialogueEnded(string graphId)
    {
        if (_used || graphId != _kingDialogueGraphId) return;
        _used = true;

        _pet.transform.position = _kingNpc.transform.position;
        _kingNpc.SetActive(false);
        _pet.gameObject.SetActive(true);
        _pet.Activate(_player);
    }

    /// <summary>챕터 0 종료 컷씬이 시작될 때 호출해 왕(펫)을 즉시 제거합니다.</summary>
    public void EndCompanion()
    {
        _used = true;
        _pet.Deactivate();
        _pet.gameObject.SetActive(false);
    }
}
