using UnityEngine;

[DisallowMultipleComponent]
public sealed class OverheadDialogueSpeaker : MonoBehaviour
{
    [Header("Bubble")]
    [SerializeField] private OverheadSpeechBubble bubblePrefab;
    [SerializeField] private Transform dialogueAnchor;
    [SerializeField] private Vector3 anchorOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private bool hideWhenSpeakerIsDisabled = true;

    private OverheadSpeechBubble _bubble;
    private float _bubbleScaleMultiplier = 1f;

    public bool IsSpeaking => _bubble != null && _bubble.IsVisible;
    public OverheadSpeechBubble CurrentBubble => _bubble;

    public void Say(string dialogue)
    {
        GetOrCreateBubble().Show(dialogue);
    }

    public void Say(string dialogue, float holdDuration)
    {
        GetOrCreateBubble().Show(dialogue, holdDuration);
    }

    public void StopSpeaking(bool immediate = false)
    {
        if (_bubble != null)
        {
            _bubble.Hide(immediate);
        }
    }

    public void SetDialogueAnchor(Transform anchor)
    {
        dialogueAnchor = anchor;
        if (_bubble != null)
        {
            ApplyTarget(_bubble);
        }
    }

    public void SetBubblePrefab(OverheadSpeechBubble prefab)
    {
        if (_bubble != null || prefab == null) return;
        bubblePrefab = prefab;
    }

    public void SetBubbleScaleMultiplier(float multiplier)
    {
        _bubbleScaleMultiplier = Mathf.Max(0.1f, multiplier);
        if (_bubble != null)
        {
            _bubble.SetScaleMultiplier(_bubbleScaleMultiplier);
        }
    }

    private OverheadSpeechBubble GetOrCreateBubble()
    {
        if (_bubble != null)
        {
            return _bubble;
        }

        if (bubblePrefab != null)
        {
            _bubble = Instantiate(bubblePrefab);
        }
        else
        {
            GameObject bubbleObject = new GameObject($"{name}_SpeechBubble");
            _bubble = bubbleObject.AddComponent<OverheadSpeechBubble>();
        }

        _bubble.name = $"{name}_SpeechBubble";
        _bubble.SetScaleMultiplier(_bubbleScaleMultiplier);
        ApplyTarget(_bubble);
        return _bubble;
    }

    private void ApplyTarget(OverheadSpeechBubble bubble)
    {
        if (dialogueAnchor != null)
        {
            bubble.SetTarget(dialogueAnchor, Vector3.zero);
        }
        else
        {
            bubble.SetTarget(transform, anchorOffset);
        }
    }

    private void OnDisable()
    {
        if (hideWhenSpeakerIsDisabled && _bubble != null)
        {
            _bubble.Hide(true);
        }
    }

    private void OnDestroy()
    {
        if (_bubble != null)
        {
            Destroy(_bubble.gameObject);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Play Dialogue Preview")]
    private void PlayPreview()
    {
        if (Application.isPlaying)
        {
            Say("[wave]안녕하세요![/wave] [wait=0.2][shake=1.4]<color=#FF7FA8>여기예요.</color>[/shake]");
        }
    }
#endif
}
