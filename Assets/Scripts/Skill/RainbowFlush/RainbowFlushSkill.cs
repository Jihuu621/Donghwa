using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RainbowFlushSkill : MonoBehaviour
{
    public enum SkillState { Ready, Gathering, Idle, Firing, End }
    public enum HandType { None, OnePair, TwoPair, Triple, HighHand } // 포카드/스트 통합

    [Header("Settings")]
    public GameObject cardPrefab;
    public float rotateSpeed = 200f;
    public float targetRadius = 1.5f;
    public float lerpSpeed = 5f;

    [Header("Run Time")]
    public SkillState currentState = SkillState.Ready;
    private List<CardProjectile> activeCards = new List<CardProjectile>();
    private List<CardProjectile.Rank> firedRanks = new List<CardProjectile.Rank>();
    private float globalAngle = 0f;
    private float currentRadius = 5f;
    private int totalCards = 5;

    private CardProjectile.Suit[] suitOrder = {
        CardProjectile.Suit.Clover, CardProjectile.Suit.Heart,
        CardProjectile.Suit.Diamond, CardProjectile.Suit.Spade, CardProjectile.Suit.Joker
    };

    public SkillState IsSkill() => currentState;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && currentState == SkillState.Ready) GenerateCards();

        if (currentState != SkillState.Ready)
        {
            UpdateSkillLogic();
            if (currentState != SkillState.Gathering && Input.GetMouseButtonDown(0)) TryFireCard();
        }
    }

    private void GenerateCards()
    {
        currentState = SkillState.Gathering;
        currentRadius = 20f;
        activeCards.Clear();
        firedRanks.Clear();

        for (int i = 0; i < totalCards; i++)
        {
            GameObject newCard = Instantiate(cardPrefab, transform.position, Quaternion.identity);
            CardProjectile script = newCard.GetComponent<CardProjectile>();

            CardProjectile.Suit suit = suitOrder[i];
            CardProjectile.Rank rank = (suit == CardProjectile.Suit.Joker) ?
                                        CardProjectile.Rank.None : (CardProjectile.Rank)Random.Range(0, 4);

            script.SetCardInfo(suit, rank);
            activeCards.Add(script);
        }
    }

    private void TryFireCard()
    {
        if (activeCards.Count == 0) return;

        CardProjectile card = activeCards[0];
        activeCards.RemoveAt(0);

        if (card.mySuit == CardProjectile.Suit.Joker)
        {
            DetermineHand(card); // 조커에게 족보 데이터 주입
        }
        else
        {
            firedRanks.Add(card.myRank);
        }

        float lookDir = transform.localScale.x > 0 ? 1f : -1f;
        card.Launch(new Vector3(lookDir, 0, 0));

        if (activeCards.Count == 0) StartCoroutine(ResetSkill());
        else currentState = SkillState.Firing;
    }

    private void DetermineHand(CardProjectile joker)
    {
        // 스트레이트 체크
        bool isAKQJ = firedRanks.SequenceEqual(new[] { CardProjectile.Rank.A, CardProjectile.Rank.K, CardProjectile.Rank.Q, CardProjectile.Rank.J });
        bool isJQKA = firedRanks.SequenceEqual(new[] { CardProjectile.Rank.J, CardProjectile.Rank.Q, CardProjectile.Rank.K, CardProjectile.Rank.A });
        bool isStraight = isAKQJ || isJQKA;

        var counts = firedRanks.GroupBy(r => r).Select(g => g.Count()).ToList();

        // 기본 밸런싱 수치 (None)
        float r = 1.7f, d = 250f, dur = 3.7f, amt = 37f;

        if (isStraight || counts.Contains(4))
        { // High Hand (포카/스트)
            Debug.Log("포카스트");
            r = 10.0f; d = 650f; dur = 10f; amt = 100f;
        }
        else if (counts.Count(c => c == 2) == 2)
        { // Two Pair
            Debug.Log("투페어");
            r = 4.5f; d = 550f; dur = 7f; amt = 77f;
        }
        else if (counts.Contains(3))
        { // Triple
            Debug.Log("트리플");
            r = 3.7f; d = 500f; dur = 7f; amt = 77f;
        }
        else if (counts.Contains(2))
        { // One Pair
            Debug.Log("원페어");
            r = 2.5f; d = 350f; dur = 5f; amt = 45f;
        }

        joker.SetExplosionData(r, d, dur, amt);
        Debug.Log($"[Hand] 결과 반영: {r}m 범위 폭발 준비");
    }

    private void UpdateSkillLogic()
    {
        globalAngle += rotateSpeed * Time.deltaTime;
        if (currentState == SkillState.Gathering)
        {
            currentRadius = Mathf.MoveTowards(currentRadius, targetRadius, Time.deltaTime * 15f);
            if (currentRadius <= targetRadius) currentState = SkillState.Idle;
        }

        for (int i = 0; i < activeCards.Count; i++)
        {
            float step = 360f / activeCards.Count;
            float targetRel = i * step;
            float smoothedRel = Mathf.LerpAngle(activeCards[i].currentAngle - globalAngle, targetRel, Time.deltaTime * lerpSpeed);
            activeCards[i].UpdateOrbit(transform, globalAngle + smoothedRel, currentRadius);
        }
    }

    IEnumerator ResetSkill()
    {
        yield return new WaitForSeconds(0.5f);
        currentState = SkillState.Ready;
    }
}