/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace WildBlueIndustries
{
    public enum ElevatorStates
    {
        None,
        DoneLowering,
        DoneRaising,
        OpenDoors,
        CloseDoors,
        Locked,
        LowerRailings,
        Lowering,
        RaiseRailings,
        Raising,
        RestoreToOriginal,
        OpeningDoors,
        ClosingDoors,
        PlayLoop,
    }

    public class WBIElevator : PartModule
    {
        [KSPField(guiActive = true, guiName = "Status")]
        public string status = "Locked";

        [KSPField()]
        public string elevatorTransformName = "ElevatorPanel";

        [KSPField()]
        public float maxElevatorSpeed = 2.0f;

        [KSPField(guiActive = true, guiName = "Distance (m)", guiFormat = "f2", isPersistant = true)]
        public float travelDistance = 0.0f;

        [KSPField(guiName = "Speed: (m/sec)", guiActive = true, guiActiveEditor = true, guiFormat = "f2")]
        public float elevatorSpeed = 1.0f;

        [KSPField(guiName = "Elevator Throttle", isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 1f, maxValue = 100f, minValue = 0f)]
        public float elevatorThrottle = 25f;

        [KSPField()]
        public float maxCableLength = 50f;

        [KSPField()]
        public string doorAnimationName = string.Empty;

        [KSPField()]
        public string railingsTransformName = string.Empty;

        [KSPField]
        public string loopSoundURL = string.Empty;

        [KSPField]
        public float loopSoundPitch = 1.0f;

        [KSPField]
        public float loopSoundVolume = 0.5f;

        [KSPField]
        public string stopSoundURL = string.Empty;

        [KSPField]
        public float stopSoundPitch = 1.0f;

        [KSPField]
        public float stopSoundVolume = 0.5f;

        protected Transform elevatorTransform;
        protected Transform railingsTransform;
        protected ElevatorStates elevatorState;
        protected Vector3 originalPosition;
        protected bool restoreOriginalPosition;
        protected ModuleAnimateGeneric doorAnimation;
        protected bool raiseElevator;
        protected List<ElevatorStates> stateSequence = new List<ElevatorStates>();
        protected int stateIndex;
        protected AudioSource loopSound = null;
        protected AudioSource stopSound = null;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Get transforms and animations
            if (string.IsNullOrEmpty(elevatorTransformName) == false)
            {
                elevatorTransform = this.part.FindModelTransform(elevatorTransformName);
                if (elevatorTransform != null)
                    originalPosition = elevatorTransform.position;
            }

            if (string.IsNullOrEmpty(railingsTransformName) == false)
            {
                railingsTransform = this.part.FindModelTransform(railingsTransformName);
                setRailingsVisible(false);
            }

            if (string.IsNullOrEmpty(doorAnimationName) == false)
            {
                List<ModuleAnimateGeneric> animations = this.part.FindModulesImplementing<ModuleAnimateGeneric>();
                foreach (ModuleAnimateGeneric animation in animations)
                {
                    if (animation.animationName == doorAnimationName)
                    {
                        doorAnimation = animation;
                        doorAnimation.Events["Toggle"].active = false;
                        doorAnimation.Fields["status"].guiActive = false;
                        break;
                    }
                }
            }

            //Setup events
            Events["LowerElevator"].active = true;
            Events["RaiseElevator"].active = false;
            Events["StopElevator"].active = false;
            Events["FineControlUp"].active = true;
            Events["FineControlDown"].active = true;
            Events["LowerElevator"].unfocusedRange = maxCableLength;
            Events["RaiseElevator"].unfocusedRange = maxCableLength;
            Events["StopElevator"].unfocusedRange = maxCableLength;

            //Setup sounds
            if (!string.IsNullOrEmpty(loopSoundURL))
            {
                loopSound = gameObject.AddComponent<AudioSource>();
                loopSound.clip = GameDatabase.Instance.GetAudioClip(loopSoundURL);
                loopSound.loop = true;
                loopSound.pitch = loopSoundPitch;
                loopSound.volume = GameSettings.SHIP_VOLUME * loopSoundVolume;
            }

            if (!string.IsNullOrEmpty(stopSoundURL))
            {
                stopSound = gameObject.AddComponent<AudioSource>();
                stopSound.clip = GameDatabase.Instance.GetAudioClip(stopSoundURL);
                stopSound.pitch = stopSoundPitch;
                stopSound.volume = GameSettings.SHIP_VOLUME * stopSoundVolume;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Fine Control: Up")]
        public void FineControlUp()
        {
            elevatorTransform.Translate(0f, 0.01f, 0f);
        }

        [KSPEvent(guiActive = true, guiName = "Fine Control: Down")]
        public void FineControlDown()
        {
            elevatorTransform.Translate(0f, -0.01f, 0f);
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3.0f, guiName = "Lower Elevator")]
        public void LowerElevator()
        {
            Events["FineControlUp"].active = false;
            Events["FineControlDown"].active = false;
            Events["LowerElevator"].active = false;
            Events["RaiseElevator"].active = false;
            Events["StopElevator"].active = true;

            //Down:
            //Raise Railings, Open Doors, Lowering, <elevator stops>, Lower Railings
            stateSequence.Clear();
            stateIndex = -1;
            if (railingsTransform != null)
                stateSequence.Add(ElevatorStates.RaiseRailings);
            stateSequence.Add(ElevatorStates.OpenDoors);
            stateSequence.Add(ElevatorStates.PlayLoop);
            stateSequence.Add(ElevatorStates.Lowering);
            stateSequence.Add(ElevatorStates.LowerRailings);
            stateSequence.Add(ElevatorStates.DoneLowering);
            setNextState();
        }

        [KSPAction("Lower Elevator")]
        public virtual void LowerElevatorAction(KSPActionParam param)
        {
            LowerElevator();
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3.0f, guiName = "Raise Elevator")]
        public void RaiseElevator()
        {
            Events["FineControlUp"].active = false;
            Events["FineControlDown"].active = false;
            Events["LowerElevator"].active = false;
            Events["RaiseElevator"].active = false;
            Events["StopElevator"].active = true;

            //Up:
            //Raise Railings, Raising, <elevator stops>, Lower Railings, close doors
            stateSequence.Clear();
            stateIndex = -1;
            if (railingsTransform != null)
                stateSequence.Add(ElevatorStates.RaiseRailings);
            stateSequence.Add(ElevatorStates.PlayLoop);
            stateSequence.Add(ElevatorStates.Raising);
            stateSequence.Add(ElevatorStates.LowerRailings);
            stateSequence.Add(ElevatorStates.CloseDoors);
            stateSequence.Add(ElevatorStates.DoneRaising);
            setNextState();
        }

        [KSPAction("Raise Elevator")]
        public virtual void RaiseElevatorAction(KSPActionParam param)
        {
            RaiseElevator();
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3.0f, guiName = "Stop Elevator")]
        public void StopElevator()
        {
            elevatorState = ElevatorStates.Locked;
            stateSequence.Clear();
            Events["FineControlUp"].active = true;
            Events["FineControlDown"].active = true;
            Events["LowerElevator"].active = true;
            Events["RaiseElevator"].active = true;
            Events["StopElevator"].active = false;
            playStopSound();
        }

        [KSPAction("Stop Elevator")]
        public virtual void StopElevatorAction(KSPActionParam param)
        {
            StopElevator();
        }

        public void FixedUpdate()
        {
            float distanceDelta;

            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (elevatorTransform == null)
                return;

            //Make sure the door animation button is invisible
            if (doorAnimation != null)
                doorAnimation.Events["Toggle"].active = false;
            if (elevatorState == ElevatorStates.Locked)
                return;

            //Calcuate elevator speed
            elevatorSpeed = maxElevatorSpeed * (elevatorThrottle / 100.0f);
            if (elevatorSpeed < 0.001f)
                return;

            switch (elevatorState)
            {
                case ElevatorStates.PlayLoop:
                    playLoopSound();
                    setNextState();
                    break;

                case ElevatorStates.DoneLowering:
                    stateSequence.Clear();
                    Events["FineControlUp"].active = true;
                    Events["FineControlDown"].active = true;
                    Events["RaiseElevator"].active = true;
                    Events["StopElevator"].active = false;
                    elevatorState = ElevatorStates.Locked;
                    status = "Locked";
                    playStopSound();
                    break;

                case ElevatorStates.DoneRaising:
                    stateSequence.Clear();
                    Events["FineControlUp"].active = true;
                    Events["FineControlDown"].active = true;
                    Events["LowerElevator"].active = true;
                    Events["StopElevator"].active = false;
                    elevatorState = ElevatorStates.Locked;
                    status = "Locked";
                    playStopSound();
                    break;

                case ElevatorStates.CloseDoors:
                    if (loopSound != null && loopSound.isPlaying)
                        loopSound.Stop();
                    if (doorAnimation == null)
                    {
                        setNextState();
                    }
                    else
                    {
                        doorAnimation.Toggle();
                        elevatorState = ElevatorStates.ClosingDoors;
                        status = "Closing Doors";
                    }
                    break;

                case ElevatorStates.OpenDoors:
                    if (doorsAreClosed() == false)
                    {
                        setNextState();
                    }
                    else
                    {
                        elevatorState = ElevatorStates.OpeningDoors;
                        status = "Opening Doors";
                    }
                    break;

                case ElevatorStates.OpeningDoors:
                case ElevatorStates.ClosingDoors:
                    Events["LowerElevator"].active = false;
                    Events["RaiseElevator"].active = false;
                    if (doorAnimation.aniState == ModuleAnimateGeneric.animationStates.LOCKED || doorAnimation.aniState == ModuleAnimateGeneric.animationStates.CLAMPED)
                    {
                        setNextState();
                    }
                    break;

                case ElevatorStates.RaiseRailings:
                    setRailingsVisible();
                    setNextState();
                    break;

                case ElevatorStates.LowerRailings:
                    setRailingsVisible(false);
                    setNextState();
                    break;

                case ElevatorStates.Lowering:
                    status = "Lowering";
                    Events["StopElevator"].active = true;
                    distanceDelta = -elevatorSpeed * TimeWarp.fixedDeltaTime;
                    travelDistance -= distanceDelta;

                    elevatorTransform.Translate(0f, distanceDelta, 0f);

                    //Check cable distance.
                    if (travelDistance >= maxCableLength)
                    {
                        StopElevator();
                        Events["FineControlUp"].active = false;
                        Events["FineControlDown"].active = false;
                        Events["LowerElevator"].active = false;
                        ScreenMessages.PostScreenMessage("Maximum length of elevator cable reached.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    break;

                case ElevatorStates.Raising:
                    status = "Raising";
                    Events["StopElevator"].active = true;

                    distanceDelta = elevatorSpeed * TimeWarp.fixedDeltaTime;
                    travelDistance -= distanceDelta;
                    elevatorTransform.Translate(0f, distanceDelta, 0f);

                    //If we've are back to our original position then we're done.
                    if (travelDistance < 0.001f)
                        setNextState();

                    break;

                default:
                    break;
            }
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            //Don't let the elevator continue to lower if we hit the ground.
            if (this.part.GroundContact && elevatorState == ElevatorStates.Lowering)
                setNextState();
        }

        public void OnCollisionStay(Collision collision)
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            //Don't let the elevator continue to lower if we hit the ground.
            if (this.part.GroundContact && elevatorState == ElevatorStates.Lowering)
                setNextState();
        }

        public void playLoopSound()
        {
            if (loopSound != null && loopSound.isPlaying == false)
                loopSound.Play();
        }

        public void playStopSound()
        {
            if (stopSound != null)
                stopSound.Play();

            if (loopSound != null)
                loopSound.Stop();
        }

        protected void setRailingsVisible(bool isVisible = true)
        {
            railingsTransform.gameObject.SetActive(isVisible);

            Collider collider = railingsTransform.gameObject.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = isVisible;
        }

        protected bool doorsAreClosed()
        {
            if (doorAnimation == null)
                return false;

            //If the doors are closed then open them
            if (doorAnimation.Events["Toggle"].guiName == doorAnimation.startEventGUIName)
            {
                if (doorAnimation.aniState != ModuleAnimateGeneric.animationStates.MOVING)
                {
                    doorAnimation.Toggle();
                    return true;
                }
            }

            return false;
        }

        protected void setNextState()
        {
            stateIndex += 1;

            if (stateIndex <= stateSequence.Count - 1)
                elevatorState = stateSequence[stateIndex];
            else
                elevatorState = ElevatorStates.Locked;
        }
    }
}
