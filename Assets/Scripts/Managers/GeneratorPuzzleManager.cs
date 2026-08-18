using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GeneratorPuzzleManager : MonoBehaviour
{
    public static GeneratorPuzzleManager Instance;

    [Header("Knobs")]
    public RectTransform[] knobs = new RectTransform[4];

    [Header("Solution Angles")]
    public float[] solutionAngles = { 0f, 90f, 180f, 270f };

    [Header("Electricity Flow")]
    public ElectricityFlowVisualizer flowVisualizer;

    [Header("Main Hospital UI Canvas")]
    public Canvas hospitalUICanvas;        //drag UI Canvas here

    private Generator currentGenerator;
    private Canvas puzzleCanvas;
    private int selectedKnobIndex = -1;
    private bool[] isKnobLocked = new bool[4];


    void Awake()
    {
        Instance = this;
        //puzzleCanvas = GameObject.Find("GeneratorPuzzleCanvas").GetComponent<Canvas>();
        puzzleCanvas = hospitalUICanvas;
        if (puzzleCanvas != null)
            puzzleCanvas.enabled = false;
    }

    public void StartPuzzle(Generator generator)
    {
        if (puzzleCanvas == null)
        {
            Debug.Log("Puzzle is null"); 
            return;
        }
            

        currentGenerator = generator;
        puzzleCanvas.enabled = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isKnobLocked = new bool[4];
        selectedKnobIndex = -1;

        if (flowVisualizer != null)
            flowVisualizer.ResetFlow();

        //force UI to show and stay on top during generator puzzle
        if (hospitalUICanvas != null)
        {
            hospitalUICanvas.gameObject.SetActive(true);   
            hospitalUICanvas.sortingOrder = 100;           
            Debug.Log("Hospital UI forced ON with high sorting order");
        }

        //// Randomize knobs
        //for (int i = 0; i < knobs.Length; i++)
        //{
        //    if (knobs[i] != null)
        //    {
        //        float random = Random.Range(0, 4) * 90f;
        //        knobs[i].localEulerAngles = new Vector3(0, 0, random);
        //    }
        //}
    }

    void Update()
    {
        if (puzzleCanvas == null || !puzzleCanvas.enabled) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePuzzle();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            for (int i = 0; i < knobs.Length; i++)
            {
                if (knobs[i] != null && !isKnobLocked[i] &&
                    RectTransformUtility.RectangleContainsScreenPoint(knobs[i], Input.mousePosition))
                {
                    selectedKnobIndex = i;
                    break;
                }
            }
        }

        //use mouse wheel to turn knobs
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0 && selectedKnobIndex >= 0 && !isKnobLocked[selectedKnobIndex])
        {
            float currentZ = knobs[selectedKnobIndex].localEulerAngles.z;
            currentZ = (currentZ + scroll * 45f) % 360f;
            knobs[selectedKnobIndex].localEulerAngles = new Vector3(0, 0, currentZ);

            TryLockKnob(selectedKnobIndex);
        }
    }

    //lock generator knob when correct angle reached
    private void TryLockKnob(int index)
    {
        if (index < 0 || index >= 4 || isKnobLocked[index]) return;

        float current = knobs[index].localEulerAngles.z;
        if (Mathf.Abs(Mathf.DeltaAngle(current, solutionAngles[index])) <= 12f)
        {
            knobs[index].localEulerAngles = new Vector3(0, 0, solutionAngles[index]);
            isKnobLocked[index] = true;

            if (flowVisualizer != null)
            {
                flowVisualizer.OnKnobLocked(index);
            }

            if (IsPuzzleSolved())
            {
                Invoke("SolvePuzzle", 0.8f);
            }
        }
    }

    private bool IsPuzzleSolved()
    {
        for (int i = 0; i < 4; i++)
            if (!isKnobLocked[i]) return false;
        return true;
    }

    private void SolvePuzzle()
    {
        if (flowVisualizer != null)
            flowVisualizer.CompleteFlow();

        // use system for money
        HospitalStatsManager.Instance.SpendMoney(10);

        if (currentGenerator != null)
            currentGenerator.CompletePuzzle();

        GameManager.Instance.EndPowerOutage();

       
        HospitalStatsManager.Instance.ChangeComfort(+15);

        ClosePuzzle();

        Debug.Log("Generator Puzzle Solved!");
    }

    private void ClosePuzzle()
    {
        if (puzzleCanvas != null)
            puzzleCanvas.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //keep hospital UI visible when puzzle closes
        if (hospitalUICanvas != null)
        {
            hospitalUICanvas.gameObject.SetActive(true);
            hospitalUICanvas.sortingOrder = 0;   //reset order
        }
    }
}
