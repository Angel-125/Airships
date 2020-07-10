using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2019, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIAirshipController: PartModule
    {
        List<WBIModuleStaticLift> liftModules;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            liftModules = this.part.vessel.FindPartModulesImplementing<WBIModuleStaticLift>();
        }

        [KSPEvent(guiActive = true, guiName = "Increase lift capacity")]
        public void IncreaseLiftCapacity()
        {
            updateCompressorStates(LiftCapacityStates.Increasing);
        }

        [KSPEvent(guiActive = true, guiName = "Decrease lift capacity")]
        public void DecreaseLiftCapacity()
        {
            updateCompressorStates(LiftCapacityStates.Decreasing);
        }

        [KSPEvent(guiActive = true, guiName = "Stop changing lift")]
        public void StopCompressors()
        {
            updateCompressorStates(LiftCapacityStates.Stopped);
        }

        protected void updateCompressorStates(LiftCapacityStates liftCapacityState)
        {
            int count = liftModules.Count;

            for (int index = 0; index < count; index++)
            {
                liftModules[index].liftCapacityState = liftCapacityState;
            }
        }
    }
}
