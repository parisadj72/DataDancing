﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FootGestureController_UserStudy : MonoBehaviour
{
    [Header("Prefabs or OBJ in Scene")]
    public ExperimentManager EM;
    public DashboardController_UserStudy DC;
    public LogManager logManager;
    public Transform GroundLandmarks; 

    [Header("Two Feet")]
    // left foot
    public Transform leftFoot;
    public Transform leftFootToe;
    public FootToeCollision leftFootToeCollision;
    //public ShoeRecieve leftSR;
    public ReceiveInt leftReceive;
    public Transform leftPressFeedback;
    public Transform leftFeedbackCircle;
    // right foot
    public Transform rightFoot;
    public Transform rightFootToe;
    public FootToeCollision rightFootToeCollision;
    //public ShoeRecieve rightSR;
    public ReceiveInt rightReceive;
    public Transform rightPressFeedback;
    public Transform rightFeedbackCircle;

    [Header("PressureSensor")]
    public int pressToSelectThresholdLeft = 0;
    public int holdThresholdLeft = 500;
    public int releaseThresholdLeft = 1000;
    public int pressToSelectThresholdRight = 0;
    public int holdThresholdRight = 500;
    public int releaseThresholdRight = 1000;

    // pressure sensor
    [HideInInspector] public bool leftNormalPressFlag = false;
    [HideInInspector] public bool rightNormalPressFlag = false;
    [HideInInspector] public bool leftHoldingFlag = false;
    [HideInInspector] public bool rightHoldingFlag = false;

    private Vector3 previousLeftPosition;
    public bool leftMoving = false;
    private Vector3 previousRightPosition;
    public bool rightMoving = false;

    [HideInInspector] public Transform[] movingOBJs;

    [HideInInspector] public Vector3[] previousMovingPosition;

    private float leftTotalDistance = 0;
    private float rightTotalDistance = 0;

    // Start is called before the first frame update
    void Start()
    {
        movingOBJs = new Transform[2];
        previousMovingPosition = new Vector3[2];
        previousMovingPosition[0] = Vector3.zero;
        previousMovingPosition[1] = Vector3.zero;

        previousLeftPosition = leftFoot.position;
        previousRightPosition = rightFoot.position;
    }

    // Update is called once per frame
    void Update()
    {
        FootInteractionFeedback();

        PressureSensorDetector();
        previousLeftPosition = leftFoot.position;
        previousRightPosition = rightFoot.position;
    }

    #region Pressure Sensor Detection
    private void PressureSensorDetector()
    {
        // Press Detect - Left
        if (leftReceive.shoeReceiver != 9999 && leftReceive.shoeReceiver <= pressToSelectThresholdLeft && !leftNormalPressFlag)
            leftNormalPressFlag = true;
        if (leftNormalPressFlag && leftReceive.shoeReceiver != 9999 && leftReceive.shoeReceiver > releaseThresholdLeft)
        {
            leftNormalPressFlag = false;
            if (leftTotalDistance < 0.1f && rightTotalDistance < 0.1f)
            {
                logManager.WriteInteractionToLog("Foot Interaction", "Left Foot Press");
                RunPressToSelect("left");
            }
        }

        // Press Detect - Right
        if (rightReceive.shoeReceiver != 9999 && rightReceive.shoeReceiver <= pressToSelectThresholdRight && !rightNormalPressFlag)
            rightNormalPressFlag = true;
        if (rightNormalPressFlag && rightReceive.shoeReceiver != 9999 && rightReceive.shoeReceiver > releaseThresholdRight)
        {
            rightNormalPressFlag = false;
            if (leftTotalDistance < 0.1f && rightTotalDistance < 0.1f)
            {
                logManager.WriteInteractionToLog("Foot Interaction", "Right Foot Press");
                RunPressToSelect("right");
            }
        }

        // Sliding Detect - Left
        if (leftReceive.shoeReceiver != 9999 && leftFootToeCollision.TouchedObjs.Count > 0)
        {
            if (leftReceive.shoeReceiver < holdThresholdLeft)
                leftHoldingFlag = true;
            else
                leftHoldingFlag = false;
        }

        // Sliding Detect - Right
        if (rightReceive.shoeReceiver != 9999 && rightFootToeCollision.TouchedObjs.Count > 0)
        {
            if (rightReceive.shoeReceiver < holdThresholdRight)
                rightHoldingFlag = true;
            else
                rightHoldingFlag = false;
        }

        if (Vector3.Distance(leftFoot.position, previousLeftPosition) > 0.005f && leftHoldingFlag) // left moving
            leftMoving = true;
        else if (Vector3.Distance(leftFoot.position, previousLeftPosition) <= 0.005f && leftReceive.shoeReceiver != 9999 && leftReceive.shoeReceiver > releaseThresholdLeft) // left still
            leftMoving = false;

        if(leftFoot.position.y > 0.1f)
            leftMoving = false;

        if (Vector3.Distance(rightFoot.position, previousRightPosition) > 0.005f && rightHoldingFlag) // right moving
            rightMoving = true;
        else if (Vector3.Distance(rightFoot.position, previousRightPosition) <= 0.005f && rightReceive.shoeReceiver != 9999 && rightReceive.shoeReceiver > releaseThresholdRight) // right still
            rightMoving = false;

        if (rightFoot.position.y > 0.1f)
            rightMoving = false;

        if (leftMoving)
            leftTotalDistance += Vector3.Distance(leftFoot.position, previousLeftPosition);
        else
            leftTotalDistance = 0;

        if (rightMoving)
            rightTotalDistance += Vector3.Distance(rightFoot.position, previousRightPosition);
        else
            rightTotalDistance = 0;

        RunPressToSlide();
    }
    #endregion

    #region Foot Press using pressure sensor
    private void RunPressToSelect(string foot)
    {
        if (foot == "left" && leftFootToeCollision.TouchedObjs.Count > 0)
        {
            foreach (Transform t in leftFootToeCollision.TouchedObjs)
            {
                if (t != null)
                {
                    if (t.GetComponent<Vis>().Selected)
                        DC.RemoveExplicitSelection(t);
                    else
                        DC.AddExplicitSelection(t);
                }
            }
        }

        if (foot == "right" && rightFootToeCollision.TouchedObjs.Count > 0)
        {
            foreach (Transform t in rightFootToeCollision.TouchedObjs)
            {
                if (t != null) {
                    if (t.GetComponent<Vis>().Selected)
                        DC.RemoveExplicitSelection(t);
                    else
                        DC.AddExplicitSelection(t);
                }
            }
        }
    }

    private void RunPressToSlide()
    {

        if (leftMoving && rightMoving)
        {
            //Debug.Log("left moving");
            if (leftFootToeCollision.TouchedObjs.Count > 0)
            {
                movingOBJs[0] = leftFootToeCollision.TouchedObjs[0];
                if (movingOBJs[0] != null) {
                    if (movingOBJs[0].parent != null && movingOBJs[0].parent != leftFoot)
                    {
                        movingOBJs[0].parent = leftFoot;
                        movingOBJs[0].GetComponent<Vis>().Moving = true;
                    }

                    logManager.WriteInteractionToLog("Foot Interaction", "Left Sliding " + movingOBJs[0].name);
                    previousMovingPosition[0] = movingOBJs[0].position;
                }
            }

            if (rightFootToeCollision.TouchedObjs.Count > 0)
            {
                movingOBJs[1] = rightFootToeCollision.TouchedObjs[0];

                if (movingOBJs[1] != null) {
                    if (movingOBJs[1].parent != null && movingOBJs[1].parent != rightFoot)
                    {
                        movingOBJs[1].parent = rightFoot;
                        movingOBJs[1].GetComponent<Vis>().Moving = true;
                    }

                    logManager.WriteInteractionToLog("Foot Interaction", "Right Sliding " + movingOBJs[1].name);
                    previousMovingPosition[1] = movingOBJs[1].position;
                }
            }
        }
        else if (leftMoving && !rightMoving)
        {
            DetachRightFoot();

            //Debug.Log("left moving");
            if (leftFootToeCollision.TouchedObjs.Count > 0)
            {
                movingOBJs[0] = leftFootToeCollision.TouchedObjs[0];
                if (movingOBJs[0] != null) {
                    if (movingOBJs[0].parent != null && movingOBJs[0].parent != leftFoot)
                    {
                        movingOBJs[0].parent = leftFoot;
                        movingOBJs[0].GetComponent<Vis>().Moving = true;
                    }

                    logManager.WriteInteractionToLog("Foot Interaction", "Left Sliding " + movingOBJs[0].name);
                    previousMovingPosition[0] = movingOBJs[0].position;
                }
            }
            else
                movingOBJs[0] = null;

        }
        else if (!leftMoving && rightMoving)
        {
            DetachLeftFoot();

            if (rightFootToeCollision.TouchedObjs.Count > 0)
            {
                movingOBJs[1] = rightFootToeCollision.TouchedObjs[0];

                if (movingOBJs[1] != null) {
                    if (movingOBJs[1].parent != null && movingOBJs[1].parent != rightFoot)
                    {
                        movingOBJs[1].parent = rightFoot;
                        movingOBJs[1].GetComponent<Vis>().Moving = true;
                    }

                    logManager.WriteInteractionToLog("Foot Interaction", "Right Sliding " + movingOBJs[1].name);
                    previousMovingPosition[1] = movingOBJs[1].position;
                }
                
            }
            else
                movingOBJs[1] = null;

        }
        else if (!leftMoving && !rightMoving)
        {
            DetachLeftFoot();
            DetachRightFoot();
        }


        if (previousMovingPosition[0] != Vector3.zero && movingOBJs[0] != null && Vector3.Distance(previousMovingPosition[0], movingOBJs[0].position) > 0.5f)
        {
            movingOBJs[0].position = previousMovingPosition[0];
        }

        if (previousMovingPosition[1] != Vector3.zero && movingOBJs[1] != null && Vector3.Distance(previousMovingPosition[1], movingOBJs[1].position) > 0.5f)
        {
            movingOBJs[1].position = previousMovingPosition[1];
        }
    }
    #endregion

    #region Utilities

    private void DetachLeftFoot() {
        if (movingOBJs[0] != null)
        {
            movingOBJs[0].parent = EM.GroundDisplay;
            movingOBJs[0].GetComponent<Vis>().Moving = false;
            previousMovingPosition[0] = Vector3.zero;

            movingOBJs[0] = null;
        }
    }

    private void DetachRightFoot()
    {
        if (movingOBJs[1] != null)
        {
            movingOBJs[1].parent = EM.GroundDisplay;
            movingOBJs[1].GetComponent<Vis>().Moving = false;
            previousMovingPosition[1] = Vector3.zero;

            movingOBJs[1] = null;
        }
    }

    private void FootInteractionFeedback() {
        // pressure feedback right
        if (EM.GetCurrentLandmarkFOR() == ReferenceFrames.Floor && rightReceive.shoeReceiver != 9999 && rightReceive.shoeReceiver < 2000f &&
            rightFootToeCollision.TouchedObjs.Count > 0)
        {
            rightPressFeedback.gameObject.SetActive(true);
            rightPressFeedback.transform.eulerAngles = Vector3.zero;

            float delta = 4095f - pressToSelectThresholdRight;

            rightFeedbackCircle.localScale = Vector3.one * ((4095f - rightReceive.shoeReceiver) / delta * 0.09f + 0.01f);
            if (rightFeedbackCircle.localScale.x > 1)
                rightFeedbackCircle.localScale = Vector3.one;

            if (rightReceive.shoeReceiver <= pressToSelectThresholdRight && !rightMoving)
                rightFeedbackCircle.GetComponent<MeshRenderer>().material.color = new Color(0, 0, 1, 0.4f);
            else if (rightReceive.shoeReceiver < holdThresholdRight)
                rightFeedbackCircle.GetComponent<MeshRenderer>().material.color = new Color(1, 0.92f, 0.016f, 0.4f);
            else
                rightFeedbackCircle.GetComponent<MeshRenderer>().material.color = new Color(1, 0, 0, 0.4f);
        }
        else
            rightPressFeedback.gameObject.SetActive(false);

        // pressure feedback left
        if (EM.GetCurrentLandmarkFOR() == ReferenceFrames.Floor && leftReceive.shoeReceiver != 9999 && leftReceive.shoeReceiver < 2000f &&
            leftFootToeCollision.TouchedObjs.Count > 0)
        {
            leftPressFeedback.gameObject.SetActive(true);
            leftPressFeedback.transform.eulerAngles = Vector3.zero;

            float delta = 4095f - pressToSelectThresholdLeft;

            leftFeedbackCircle.localScale = Vector3.one * ((4095f - leftReceive.shoeReceiver) / delta * 0.09f + 0.01f);
            if (leftFeedbackCircle.localScale.x > 1)
                leftFeedbackCircle.localScale = Vector3.one;

            if (leftReceive.shoeReceiver <= pressToSelectThresholdLeft && !leftMoving)
                leftFeedbackCircle.GetComponent<MeshRenderer>().material.color = new Color(0, 0, 1, 0.4f);
            else if (leftReceive.shoeReceiver < holdThresholdLeft)
                leftFeedbackCircle.GetComponent<MeshRenderer>().material.color = new Color(1, 0.92f, 0.016f, 0.4f);
            else
                leftFeedbackCircle.GetComponent<MeshRenderer>().material.color = new Color(1, 0, 0, 0.4f);
        }
        else
            leftPressFeedback.gameObject.SetActive(false);
    }
    #endregion
}
