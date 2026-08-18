using UnityEngine;

public class IVMinigame : MonoBehaviour
{
    [Header("UI")]
    public GameObject minigameUI;
    public RectTransform needle;
    public RectTransform targetZone; // arm / vein target
    public RectTransform playArea;

    [Header("Arm Motion")]
    public float armMoveSpeedX = 80f;
    public float armMoveSpeedY = 55f;

    [Header("Bounds")]
    public float minX = -300f;
    public float maxX = 300f;
    public float minY = -120f;
    public float maxY = 120f;

    bool isPlaying;
    GameObject currentPlayer;
    Vector2 armVelocityDir = new Vector2(1f, 0.6f);

    public void StartGame(GameObject player)
    {
        currentPlayer = player;
        if (minigameUI != null) minigameUI.SetActive(true);
        isPlaying = true;

        var movement = player.GetComponent<SimplePlayerMovement>();
        if (movement != null) movement.SetControlsEnabled(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false; // needle acts as cursor

        SetBoundsFromPlayArea();
        SetRandomTargetPosition();

        if (MiniGameTimerUI.Instance != null)
            MiniGameTimerUI.Instance.StartTimer("IV Insertion", 45f, () => EndGame("Miss"));

        if (AudioManager.Instance != null)
            AudioManager.Instance.Play("equipment");
    }

    void Update()
    {
        if (!isPlaying) return;

        MoveArmTarget();
        FollowNeedleWithMouse();

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            CheckHit();

        if (Input.GetKeyDown(KeyCode.Escape))
            EndGame("Cancelled");
    }

    void FollowNeedleWithMouse()
    {
        if (needle == null || playArea == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            playArea, Input.mousePosition, null, out Vector2 local);

        local.x = Mathf.Clamp(local.x, minX, maxX);
        local.y = Mathf.Clamp(local.y, minY, maxY);
        needle.anchoredPosition = local;
    }

    void MoveArmTarget()
    {
        if (targetZone == null) return;

        Vector2 pos = targetZone.anchoredPosition;
        pos += armVelocityDir.normalized * new Vector2(armMoveSpeedX, armMoveSpeedY) * Time.deltaTime;

        if (pos.x > maxX - 40f || pos.x < minX + 40f)
            armVelocityDir.x *= -1f;
        if (pos.y > maxY - 30f || pos.y < minY + 30f)
            armVelocityDir.y *= -1f;

        // Mild organic drift
        pos.x += Mathf.Sin(Time.time * 1.7f) * 10f * Time.deltaTime;
        pos.y += Mathf.Cos(Time.time * 1.3f) * 8f * Time.deltaTime;

        pos.x = Mathf.Clamp(pos.x, minX + 20f, maxX - 20f);
        pos.y = Mathf.Clamp(pos.y, minY + 20f, maxY - 20f);
        targetZone.anchoredPosition = pos;
    }

    void SetRandomTargetPosition()
    {
        if (targetZone == null) return;
        targetZone.anchoredPosition = new Vector2(
            Random.Range(minX + 40f, maxX - 40f),
            Random.Range(minY + 30f, maxY - 30f));
    }

    void SetBoundsFromPlayArea()
    {
        if (playArea == null) return;
        float width = playArea.rect.width;
        float height = playArea.rect.height;
        float padding = 50f;
        minX = -width / 2f + padding;
        maxX = width / 2f - padding;
        minY = -height / 2f + padding;
        maxY = height / 2f - padding;
    }

    void CheckHit()
    {
        if (needle == null || targetZone == null)
        {
            EndGame("Miss");
            return;
        }

        float distance = Vector2.Distance(needle.anchoredPosition, targetZone.anchoredPosition);
        if (distance < 28f) EndGame("Perfect");
        else if (distance < 55f) EndGame("Good");
        else EndGame("Miss");
    }

    void EndGame(string result)
    {
        isPlaying = false;
        if (minigameUI != null) minigameUI.SetActive(false);
        if (MiniGameTimerUI.Instance != null) MiniGameTimerUI.Instance.StopTimer();

        Cursor.visible = true;

        if (currentPlayer != null)
        {
            var movement = currentPlayer.GetComponent<SimplePlayerMovement>();
            if (movement != null) movement.SetControlsEnabled(true);
        }

        if (HospitalStatsManager.Instance != null)
        {
            switch (result)
            {
                case "Perfect":
                    HospitalStatsManager.Instance.ChangeComfort(+15);
                    MedicineSupplyManager.Instance?.TryConsumeMedicine(1);
                    if (AudioManager.Instance != null) AudioManager.Instance.Play("success");
                    break;
                case "Good":
                    HospitalStatsManager.Instance.ChangeComfort(+8);
                    MedicineSupplyManager.Instance?.TryConsumeMedicine(1);
                    if (AudioManager.Instance != null) AudioManager.Instance.Play("success");
                    break;
                case "Miss":
                    HospitalStatsManager.Instance.ChangeComfort(-5);
                    HospitalStatsManager.Instance.ChangeSanitation(-5);
                    if (AudioManager.Instance != null) AudioManager.Instance.Play("fail");
                    break;
            }
        }

        Debug.Log("IV Result: " + result);
    }
}
