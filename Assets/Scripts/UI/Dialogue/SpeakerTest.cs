using UnityEngine;

public class SpeakerTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OverheadDialogueSpeaker speaker =
        GetComponent<OverheadDialogueSpeaker>();

        speaker.Say(
            "[wave]홍성원 머리...[/wave]" +
            "[wait=2][shake=5]<size=150%><color=#FF0000>존나세게때리기!!@!@!@@!!</color>[/shake]</size>" +
            "[wait=2][wave=8][shake=15]<size=250%><color=#FF0000>씨발그냥존나세게개때리기!!@#!@@!!</color>[/shake][/wave]</size>"
        );
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
