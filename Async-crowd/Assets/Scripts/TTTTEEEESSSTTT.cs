/* using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Klak.Motion;
using RootMotion.Demos;
using RootMotion.FinalIK;

public class ExperimentController : MonoBehaviour
{
    [Header("Participant Info")]
    public int participantID = 0;
    public enum GenderMatchedAvatar { woman = 0, man = 1 };
    public GenderMatchedAvatar genderMatchedAvatar;

    public enum VisuomotorType { Sync = 1, Async = 2, Delay = 3 };

    [Header("Conditions")]
    public VisuomotorType vmType;
    private VisuomotorType selectedVmType;
    public float currentTime;
    public bool isQuestionnaireDone;

    private bool isAvatarRunning;
    private bool isQuestionnaireRunning;
    public float exposureDuration = 180f;

    [Header("Avatars")]
    public GameObject femaleAvatar;
    public GameObject maleAvatar;
    public GameObject syncAvatar;
    private SkinnedMeshRenderer rocketboxSMR;

    public GameObject leftArmBM;
    public GameObject rightArmBM;

    [Header("Hand Tracking")]
    public GameObject leftHandTracking;
    public GameObject rightHandTracking;
    public Vector3 rotationOffset = new Vector3(0, 0, 180); // Public field for rotation offset

    [Header("Procedure")]
    public GameObject pointer;
    public GameObject startBox;
    public GameObject mainInstructionsCanvas;
    public TMP_Text mainInstructions;
    private QuestionnaireController questionnaireController;
    public bool isStartFlagOn;
    private bool isCountDown;

    public VRIKCalibrationBasic ikCalibration;

    // Queue to store movements for delay condition
    private Queue<MovementData> movementQueue = new Queue<MovementData>();
    public float delayTime = 1.5f;

    void Start()
    {
        startBox.SetActive(false);
        pointer.GetComponent<Renderer>().enabled = false;

        syncAvatar = (genderMatchedAvatar == GenderMatchedAvatar.woman) ? femaleAvatar : maleAvatar;
        ikCalibration = syncAvatar.GetComponent<VRIKCalibrationBasic>();
        if (genderMatchedAvatar == GenderMatchedAvatar.woman) maleAvatar.SetActive(false);
        else femaleAvatar.SetActive(false);

        rocketboxSMR = syncAvatar.GetComponentInChildren<SkinnedMeshRenderer>();

        leftArmBM.transform.localPosition = Vector3.zero;
        leftArmBM.GetComponent<BrownianMotion>().enabled = false;
        rightArmBM.transform.localPosition = Vector3.zero;
        rightArmBM.GetComponent<BrownianMotion>().enabled = false;

        questionnaireController = this.GetComponent<QuestionnaireController>();
        selectedVmType = vmType; // Initialize the selectedVmType to the starting condition
        rocketboxSMR.enabled = false;

        mainInstructions.text = mainInstructions.text + "\n\n" +
            "Participant ID: " + participantID.ToString() + ", " + genderMatchedAvatar + " avatar";

        if (vmType == VisuomotorType.Delay)
        {
            InvokeRepeating("UpdateBuffer", 0, Time.fixedDeltaTime);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) || isStartFlagOn)
        {
            isStartFlagOn = false;
            startBox.SetActive(false);
            pointer.GetComponent<Renderer>().enabled = false;
            StartCoroutine(StartCondition());
        }
        else
        {
            if (OVRPlugin.GetHandTrackingEnabled() && !isCountDown && !isAvatarRunning && !isQuestionnaireRunning)
            {
                startBox.SetActive(true);
                pointer.GetComponent<Renderer>().enabled = true;
            }
        }

        if (isCountDown)
        {
            currentTime += Time.deltaTime;
            mainInstructions.text = "Stretch out your arms for calibration. The study will begin in " + (5f - currentTime).ToString("F0") + " seconds.";
        }

        if (isAvatarRunning)
        {
            currentTime += Time.deltaTime;

            if (currentTime < exposureDuration)
            {
                if (selectedVmType == VisuomotorType.Sync)
                {
                    // Sync condition, do nothing special
                }
                else if (selectedVmType == VisuomotorType.Async)
                {
                    EnableBrownianMotion();
                }
                else if (selectedVmType == VisuomotorType.Delay)
                {
                    ApplyDelayedMovements();
                }
            }
            else
            {
                DisableBrownianMotion();
                rocketboxSMR.enabled = false;

                isAvatarRunning = false;
                isQuestionnaireRunning = true;
                questionnaireController.questionnaireCanvas.SetActive(true);
                questionnaireController.InitializeQuestionnaire();
                mainInstructionsCanvas.SetActive(false);
            }
        }

        if (isQuestionnaireRunning)
        {
            if (isQuestionnaireDone)
            {
                isQuestionnaireDone = false;
                questionnaireController.questionnaireCanvas.SetActive(false);
                isQuestionnaireRunning = false;
                currentTime = 0f;
                DisplayEndMessage();
            }
        }
    }

    IEnumerator StartCondition()
    {
        isCountDown = true;
        ikCalibration.calibrateAvatar = true;

        yield return new WaitForSeconds(5f);
        ikCalibration.calibrateAvatar = true;

        isCountDown = false;
        currentTime = 0f;
        isAvatarRunning = true;
        mainInstructions.text = "Please tilt your head downwards as if looking down at your body.";
        rocketboxSMR.enabled = true;

        yield return 0;
    }

    private void DisplayEndMessage()
    {
        mainInstructionsCanvas.SetActive(true);
        mainInstructions.text = "Thank you for your participation. This is the end of the experiment.";
    }

    void UpdateBuffer()
    {
        if (movementQueue.Count > delayTime / Time.fixedDeltaTime)
        {
            movementQueue.Dequeue();
        }

        // Capture current movement data
        MovementData currentMovement = new MovementData()
        {
            leftHandPosition = leftHandTracking.transform.position,
            leftHandRotation = leftHandTracking.transform.rotation,
            rightHandPosition = rightHandTracking.transform.position,
            rightHandRotation = rightHandTracking.transform.rotation
        };

        movementQueue.Enqueue(currentMovement);
    }

    void ApplyDelayedMovements()
    {
        if (movementQueue.Count > 0)
        {
            MovementData delayedMovement = movementQueue.Peek();

            syncAvatar.GetComponent<VRIK>().solver.leftArm.target.position = delayedMovement.leftHandPosition;
            syncAvatar.GetComponent<VRIK>().solver.leftArm.target.rotation = delayedMovement.leftHandRotation;
            syncAvatar.GetComponent<VRIK>().solver.rightArm.target.position = delayedMovement.rightHandPosition;
            syncAvatar.GetComponent<VRIK>().solver.rightArm.target.rotation = delayedMovement.rightHandRotation;
        }
    }

    private void EnableBrownianMotion()
    {
        if (!leftArmBM.GetComponent<BrownianMotion>().enabled)
            leftArmBM.GetComponent<BrownianMotion>().enabled = true;

        if (!rightArmBM.GetComponent<BrownianMotion>().enabled)
            rightArmBM.GetComponent<BrownianMotion>().enabled = true;
    }

    private void DisableBrownianMotion()
    {
        if (leftArmBM.GetComponent<BrownianMotion>().enabled)
        {
            leftArmBM.transform.localPosition = Vector3.zero;
            leftArmBM.GetComponent<BrownianMotion>().enabled = false;
        }
        if (rightArmBM.GetComponent<BrownianMotion>().enabled)
        {
            rightArmBM.transform.localPosition = Vector3.zero;
            rightArmBM.GetComponent<BrownianMotion>().enabled = false;
        }
    }

    private struct MovementData
    {
        public Vector3 leftHandPosition;
        public Quaternion leftHandRotation;
        public Vector3 rightHandPosition;
        public Quaternion rightHandRotation;
    }
}
*/