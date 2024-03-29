﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;
namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Button_Segmentation : ViveSR_Experience_IButton
    {
        ViveSR_Experience_NPCGenerator npcGenerator;

        [SerializeField] GameObject HintLocatorPrefab;

        List<ViveSR_Experience_Chair> MR_Chairs = new List<ViveSR_Experience_Chair>();
        List<GameObject> HintLocators = new List<GameObject>();

        ViveSR_Experience_StaticMesh StaticMeshScript;
        List<SceneUnderstandingObjects.Element> SegResults;
        ViveSR_Experience_ActionSequence ActionSequence;

        ViveSR_Experience_Portal PortalScript;

        protected override void AwakeToDo()
        {
            ButtonType = MenuButton.Segmentation;

            npcGenerator = GetComponent<ViveSR_Experience_NPCGenerator>(); 
        }

        protected override void StartToDo()
        {
            PortalScript = ViveSR_Experience_Demo.instance.PortalScript;
            StaticMeshScript = ViveSR_Experience_Demo.instance.StaticMeshScript;
            EnableButton(StaticMeshScript.CheckChairExist());
        }

        public override void ActionToDo()
        {
            ViveSR_Experience_Demo.instance.realWorldFloor.SetActive(isOn);
            if (isOn)
            {
                //wait for tutorial segmentation handler to reaction on UI before turning it off
                ViveSR_Experience.instance.WaitForOneFrame(() =>
                {
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(false);
                    ViveSR_Experience_Demo.instance.Tutorial.ToggleTutorial(false);
                });

                ActionSequence = ViveSR_Experience_ActionSequence.CreateActionSequence(gameObject);  

                ActionSequence.AddAction(() => StaticMeshScript.LoadMesh(true, () => ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "Loading Mesh...", false), ActionSequence.ActionFinished));
                ActionSequence.AddAction(() =>
                {
                    ViveSR_Experience_Demo.instance.Rotator.RenderButtons(true);        
                    ViveSR_Experience_Demo.instance.Tutorial.ToggleTutorial(true);
                    ViveSR_Experience_HintMessage.instance.SetHintMessage(hintType.onController, "", false);
                    SegResults = StaticMeshScript.GetSegmentationInfo(SceneUnderstandingObjectType.CHAIR);

                    StaticMeshScript.GenerateHintLocators(SegResults);
                    LoadChair();
                    ViveSR_Experience_ControllerDelegate.touchpadDelegate += handleTouchpad_Play;
                    ActionSequence.ActionFinished();
                });
                ActionSequence.StartSequence();
            }
            else
            {
                ActionSequence.StopSequence();
                StaticMeshScript.LoadMesh(false);

               ViveSR_Experience_ControllerDelegate.touchpadDelegate -= handleTouchpad_Play;
                PortalScript.PortalManager.TurnOffCamera();
                PortalScript.PortalManager.gameObject.SetActive(false);

                StaticMeshScript.ClearHintLocators();
                npcGenerator.ClearScene();
            }
        }

        public void handleTouchpad_Play(ButtonStage buttonStage, Vector2 axis)
        {
            TouchpadDirection touchpadDirection = ViveSR_Experience_ControllerDelegate.GetTouchpadDirection(axis, false);
            switch (buttonStage)
            {
                case ButtonStage.PressDown:   
                    switch (touchpadDirection)
                    {
                        case TouchpadDirection.Up:
                            ViveSR_Experience.instance.WaitForOneFrame(()=>
                            {
                                if (isOn)
                                {
                                    StaticMeshScript.ClearHintLocators();
                                    PortalScript.PortalManager.gameObject.SetActive(true);
                                    PortalScript.PortalManager.TurnOnCamera();
                                    npcGenerator.Play(MR_Chairs);
                                }
                            });
                            break;
                    }                             
                    break;
            }
        }

        void LoadChair()
        {
            foreach (ViveSR_Experience_Chair MR_Chair in MR_Chairs) Destroy(MR_Chair.gameObject);
            MR_Chairs.Clear();

            foreach (GameObject go in HintLocators) Destroy(go);
            HintLocators.Clear();

            List<SceneUnderstandingObjects.Element> ChairElements = StaticMeshScript.GetSegmentationInfo(SceneUnderstandingObjectType.CHAIR);

            for (int i = 0; i < ChairElements.Count; i++)
            {
                GameObject go = new GameObject("MR_Chair" + i, typeof(ViveSR_Experience_Chair));
                ViveSR_Experience_Chair chair = go.GetComponent<ViveSR_Experience_Chair>();
                chair.CreateChair(new Vector3(ChairElements[i].position[0].x, ChairElements[i].position[0].y, ChairElements[i].position[0].z), new Vector3(ChairElements[i].forward.x, ChairElements[i].forward.y, ChairElements[i].forward.z));
                MR_Chairs.Add(chair);
            }
        }
    }
}