using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public enum SideElevatorSequences
    {
        Done,
        Deploy,
        Lower,
        Raise,
        Stow
    }

    public class WBISideElevator : PartModule
    {
        ModuleAnimateGenericSFX deployStowAnimation = null;
        WBIAnimation upDownAnimation = null;
        List<SideElevatorSequences> elevatorSequencer = new List<SideElevatorSequences>();
        int sequenceIndex = -1;

        [KSPField()]
        public string upDownAnimationName = string.Empty;

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 10.0f, guiName = "Lower Elevator")]
        public void LowerElevator()
        {
            if (deployStowAnimation == null || upDownAnimation == null)
                return;

            //If the elevator is lowered already then we're done.
            if (upDownAnimation.isDeployed)
                return;

            //If we're in the middle of a sequence then we're done.
            if (sequenceIndex >= 0)
                return;

            //Clear the sequencer
            elevatorSequencer.Clear();
            sequenceIndex = 0;

            //If the elevator is stowed, then unstow it before lowering.
            if (deployStowAnimation.Events["Toggle"].guiName == deployStowAnimation.startEventGUIName)
                elevatorSequencer.Add(SideElevatorSequences.Deploy);

            //Lower the elevator
            elevatorSequencer.Add(SideElevatorSequences.Lower);
            elevatorSequencer.Add(SideElevatorSequences.Done);
            UpdateGUI();
            playAnimation();
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 10.0f, guiName = "Raise Elevator")]
        public void RaiseElevator()
        {
            if (deployStowAnimation == null || upDownAnimation == null)
                return;

            //If the elevator is raised already then we're done.
            if (upDownAnimation.isDeployed == false)
                return;

            //If we're in the middle of a sequence then we're done.
            if (sequenceIndex >= 0)
                return;

            //Clear the sequencer
            elevatorSequencer.Clear();
            sequenceIndex = 0;

            //Raise the elevator
            elevatorSequencer.Add(SideElevatorSequences.Raise);
            elevatorSequencer.Add(SideElevatorSequences.Done);
            UpdateGUI();
            playAnimation();
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 10.0f, guiName = "Unstow Elevator")]
        public void UnstowElevator()
        {
            if (deployStowAnimation == null || upDownAnimation == null)
                return;

            //If the elevator is already unstowed then we're done.
            if (deployStowAnimation.Events["Toggle"].guiName == deployStowAnimation.endEventGUIName)
                return;

            //If we're in the middle of a sequence then we're done.
            if (sequenceIndex >= 0)
                return;

            //Clear the sequencer
            elevatorSequencer.Clear();
            sequenceIndex = 0;

            //Unstow the elevator
            elevatorSequencer.Add(SideElevatorSequences.Deploy);
            elevatorSequencer.Add(SideElevatorSequences.Done);
            UpdateGUI();
            playAnimation();
        }

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 10.0f, guiName = "Stow Elevator")]
        public void StowElevator()
        {
            if (deployStowAnimation == null || upDownAnimation == null)
                return;

            //If the elevator is stowed, then we're done.
            if (deployStowAnimation.Events["Toggle"].guiName == deployStowAnimation.startEventGUIName)
                return;

            //If we're in the middle of a sequence then we're done.
            if (sequenceIndex >= 0)
                return;

            //Clear the sequencer
            elevatorSequencer.Clear();
            sequenceIndex = 0;

            //If the elevator is lowered then raise it
            if (upDownAnimation.isDeployed)
                elevatorSequencer.Add(SideElevatorSequences.Raise);

            //Stow the elevator
            elevatorSequencer.Add(SideElevatorSequences.Stow);
            elevatorSequencer.Add(SideElevatorSequences.Done);
            UpdateGUI();
            playAnimation();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Get the animations
            deployStowAnimation = this.part.FindModuleImplementing<ModuleAnimateGenericSFX>();
            if (deployStowAnimation != null)
            {
                deployStowAnimation.Events["Toggle"].guiActiveEditor = false;
                deployStowAnimation.Events["Toggle"].guiActive = false;
                deployStowAnimation.Events["Toggle"].guiActiveUnfocused = false;
            }
            List<WBIAnimation> wbiAnimations = this.part.FindModulesImplementing<WBIAnimation>();
            foreach (WBIAnimation wbiAnim in wbiAnimations)
            {
                if (wbiAnim.animationName == upDownAnimationName)
                {
                    upDownAnimation = wbiAnim;
                    break;
                }
            }
            if (upDownAnimation != null)
                upDownAnimation.showGui(false);
            UpdateGUI();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (deployStowAnimation == null || upDownAnimation == null)
                return;

            //If we have no sequences then we're done
            if (sequenceIndex == -1)
                return;
            if (elevatorSequencer.Count == 0)
                return;

            //Run the sequencer
            SideElevatorSequences sequence = elevatorSequencer[sequenceIndex];
            switch (sequence)
            {
                case SideElevatorSequences.Done:
                    UpdateGUI();
                    sequenceIndex = -1;
                    break;

                case SideElevatorSequences.Deploy:
                case SideElevatorSequences.Stow:
                    if (deployStowAnimation.aniState == ModuleAnimateGeneric.animationStates.LOCKED)
                    {
                        sequenceIndex += 1;
                        if (sequenceIndex >= elevatorSequencer.Count)
                            sequenceIndex = -1;
                        playAnimation();
                    }
                    break;

                case SideElevatorSequences.Raise:
                case SideElevatorSequences.Lower:
                    if (upDownAnimation.animation.isPlaying == false)
                    {
                        sequenceIndex += 1;
                        if (sequenceIndex >= elevatorSequencer.Count)
                            sequenceIndex = -1;
                    }
                    playAnimation();
                    break;

                default:
                    break;
            }
        }

        public void UpdateGUI()
        {
            if (deployStowAnimation == null || upDownAnimation == null)
                return;

            //If the elevator is unstowed, then it can be stowed, and either raised or lowered depending upon its state.
            if (deployStowAnimation.Events["Toggle"].guiName == deployStowAnimation.endEventGUIName)
            {
                //Show the stow button
                Events["StowElevator"].guiActive = true;
                Events["StowElevator"].guiActiveUnfocused = true;
                Events["StowElevator"].guiActiveEditor = true;

                //Hide the unstow button
                Events["UnstowElevator"].guiActive = false;
                Events["UnstowElevator"].guiActiveUnfocused = false;
                Events["UnstowElevator"].guiActiveEditor = false;

                //If the elevator is raised then it can be lowered
                if (upDownAnimation.isDeployed == false)
                {
                    //Show the lower button
                    Events["LowerElevator"].guiActive = true;
                    Events["LowerElevator"].guiActiveUnfocused = true;
                    Events["LowerElevator"].guiActiveEditor = true;

                    //Hide the raise button
                    Events["RaiseElevator"].guiActive = false;
                    Events["RaiseElevator"].guiActiveUnfocused = false;
                    Events["RaiseElevator"].guiActiveEditor = false;
                }

                //If the elevator is lowered then it can be raised.
                else if (upDownAnimation.isDeployed)
                {
                    //Show the raise button
                    Events["RaiseElevator"].guiActive = true;
                    Events["RaiseElevator"].guiActiveUnfocused = true;
                    Events["RaiseElevator"].guiActiveEditor = true;

                    //Hide the lower button
                    Events["LowerElevator"].guiActive = false;
                    Events["LowerElevator"].guiActiveUnfocused = false;
                    Events["LowerElevator"].guiActiveEditor = false;
                }
            }

            //If the elevator is stowed then it can be unstowed and lowered.
            else if (deployStowAnimation.Events["Toggle"].guiName == deployStowAnimation.startEventGUIName)
            {
                //Show the unstow button
                Events["UnstowElevator"].guiActive = true;
                Events["UnstowElevator"].guiActiveUnfocused = true;
                Events["UnstowElevator"].guiActiveEditor = true;

                //Hide the stow button
                Events["StowElevator"].guiActive = false;
                Events["StowElevator"].guiActiveUnfocused = false;
                Events["StowElevator"].guiActiveEditor = false;

                //Show the lower button
                Events["LowerElevator"].guiActive = true;
                Events["LowerElevator"].guiActiveUnfocused = true;
                Events["LowerElevator"].guiActiveEditor = true;

                //Hide the raise button
                Events["RaiseElevator"].guiActive = false;
                Events["RaiseElevator"].guiActiveUnfocused = false;
                Events["RaiseElevator"].guiActiveEditor = false;
            }
        }

        protected void playAnimation()
        {
            if (sequenceIndex == -1)
                return;

            SideElevatorSequences sequence = elevatorSequencer[sequenceIndex];
            switch (sequence)
            {
                case SideElevatorSequences.Deploy:
                case SideElevatorSequences.Stow:
                    deployStowAnimation.Toggle();
                    break;

                case SideElevatorSequences.Lower:
                case SideElevatorSequences.Raise:
                    upDownAnimation.ToggleAnimation();
                    break;
            }
        }
    }
}
