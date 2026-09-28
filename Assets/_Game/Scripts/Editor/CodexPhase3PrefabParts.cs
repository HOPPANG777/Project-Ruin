//=============================================================================
// File:        CodexPhase3PrefabParts.cs
// Description: 도감 Phase 3 HUD, 닫기와 창 이동 버튼을 생성하고 연결한다.
// History:     08/22/2026 + Created -- Codex
//              08/23/2026 + 초기 표시 플래시 차단 -- Codex
//=============================================================================

using UnityEngine;
using UnityEngine.UI;

internal static class CodexPhase3PrefabParts
{
    internal static void Bind(GameObject root, GameObject book, CodexPresenter codexPresenter)
    {
        CanvasGroup bookGroup = book.GetComponent<CanvasGroup>();
        bookGroup.alpha = 1;
        bookGroup.interactable = false;
        bookGroup.blocksRaycasts = false;
        Button hud = CreateButton("CodexHudButton", root.transform, "도감",
            new Vector2(-32, -32), new Vector2(170, 52), Vector2.one);
        Button close = CreateButton("CloseButton", book.transform, "X",
            new Vector2(1428, 742), new Vector2(48, 48), Vector2.zero);
        Button inventory = CreateButton("InventoryButton", book.transform, "<",
            new Vector2(12, 370), new Vector2(56, 80), Vector2.zero);
        Button map = CreateButton("MapButton", book.transform, ">",
            new Vector2(1432, 370), new Vector2(56, 80), Vector2.zero);
        map.interactable = false;

        CodexNavigationPresenter navigation = root.GetComponent<CodexNavigationPresenter>();
        CodexUiElementFactory.SetReference(navigation, "_codexPresenter", codexPresenter);
        CodexUiElementFactory.SetReference(navigation, "_hudOpenButton", hud);
        CodexUiElementFactory.SetReference(navigation, "_closeButton", close);
        CodexUiElementFactory.SetReference(navigation, "_inventoryButton", inventory);
        CodexUiElementFactory.SetReference(navigation, "_mapButton", map);
    }

    private static Button CreateButton(string name, Transform parent, string label,
        Vector2 position, Vector2 size, Vector2 anchor)
    {
        Button button = CodexUiElementFactory.CreateButton(name, parent, label, Vector2.zero);
        CodexUiElementFactory.SetRect(button.GetComponent<RectTransform>(), position, size, anchor);
        return button;
    }
}
