using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Plays the button-click SFX on pointer down so it is not delayed until mouse-up.
/// </summary>
public class UiButtonPressSfx : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        var button = GetComponent<Button>();
        if (button != null && !button.interactable)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }
}
