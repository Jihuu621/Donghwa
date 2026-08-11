using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class YarnHealthSlotView : MonoBehaviour
{
    [SerializeField] private int slotIndex;
    [SerializeField] private Image mainImage;
    [SerializeField] private Image ghostImage;

    public int SlotIndex => slotIndex;
    public RectTransform Root => (RectTransform)transform;
    public RectTransform MainRect => mainImage.rectTransform;
    public RectTransform GhostRect => ghostImage.rectTransform;

    public void Configure(int index, Image main, Image ghost)
    {
        slotIndex = index;
        mainImage = main;
        ghostImage = ghost;
    }

    public void SetState(int state, Sprite fullSprite, Sprite halfSprite)
    {
        ResetPose();

        Sprite sprite = state >= 2 ? fullSprite : state == 1 ? halfSprite : null;
        mainImage.sprite = sprite;
        mainImage.enabled = sprite != null;
        mainImage.color = sprite != null ? Color.white : Color.clear;

        ghostImage.enabled = false;
        ghostImage.sprite = null;
        ghostImage.color = Color.clear;
    }

    public void PrepareTransition(Sprite fromSprite, Sprite toSprite)
    {
        ResetPose();

        mainImage.sprite = toSprite;
        mainImage.enabled = toSprite != null;
        mainImage.color = toSprite != null ? Color.white : Color.clear;

        ghostImage.sprite = fromSprite;
        ghostImage.enabled = fromSprite != null;
        ghostImage.color = fromSprite != null ? Color.white : Color.clear;
    }

    public void SetMainAlpha(float alpha)
    {
        Color color = mainImage.color;
        color.a = Mathf.Clamp01(alpha);
        mainImage.color = color;
    }

    public void SetGhostAlpha(float alpha)
    {
        Color color = ghostImage.color;
        color.a = Mathf.Clamp01(alpha);
        ghostImage.color = color;
    }

    public void ResetPose()
    {
        if (mainImage == null || ghostImage == null)
        {
            return;
        }

        Root.localRotation = Quaternion.identity;
        Root.localScale = Vector3.one;
        MainRect.localRotation = Quaternion.identity;
        MainRect.localScale = Vector3.one;
        MainRect.anchoredPosition = Vector2.zero;
        GhostRect.localRotation = Quaternion.identity;
        GhostRect.localScale = Vector3.one;
        GhostRect.anchoredPosition = Vector2.zero;
    }
}
