using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

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
    //Lift capacity states: Empty, Increasing, Maximum, Decreasing
    public enum LiftCapacityStates
    {
        Empty,
        Increasing,
        Maximum,
        Decreasing,
        Stopped
    }

    /// <summary>
    /// This part module provides static lift based upon the lifting gas density and volume.
    /// Be sure to account for the lifting gas's mass in the part mass.
    /// </summary>
    public class WBIModuleStaticLift : PartModule
    {
        #region Fields
        [KSPField(guiActive = true, guiActiveEditor= true, guiName = "#LOC_HEISENBERG_lift", guiUnits = "kN", guiFormat = "n2", groupStartCollapsed = true, groupName = "airshipControl", groupDisplayName = "#LOC_HEISENBERG_airshipGroupDisplayName")]
        public double liftForce;

        [KSPField]
        public bool debugMode;

        /// <summary>
        /// Density of the lifting gas in kg/L
        /// </summary>
        [KSPField]
        public double liftGasDensity = 0.008989f;

        /// <summary>
        /// Volume of the envelop in liters
        /// </summary>
        [KSPField]
        public double envelopeVolume = 375000;

        [KSPField(isPersistant = true)]
        public double currentVolume = 0;

        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_HEISENBERG_liftControlState", groupStartCollapsed = true, groupName = "airshipControl", groupDisplayName = "#LOC_HEISENBERG_airshipGroupDisplayName")]
        public LiftCapacityStates liftCapacityState;

        [KSPField]
        public double compressorRate = 0.025;

        //The following fields are only shown when debugMode = true
        [KSPField(guiName = "Lift Multiplier", guiFormat = "f3", groupStartCollapsed = true, groupName = "airshipControl", groupDisplayName = "#LOC_HEISENBERG_airshipGroupDisplayName")]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 300f, minValue = 0f)]
        public float liftMultiplier;
        #endregion

        #region Housekeeping
        public double liftGasDensityKgCubicMeters = 0f;
        public double liftGasVolumeLiters = 0;

        int partCount = -1;
        List<WBIModuleStaticLift> staticLifters;
        protected bool translationKeysActive = false;
        public float verticalSpeed = 0f;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //fields shown when debugMode = true
            Fields["liftMultiplier"].guiActive = debugMode;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //Run compressor/decompressor
            if (liftCapacityState == LiftCapacityStates.Decreasing && currentVolume > 0)
            {
                currentVolume -= (compressorRate * envelopeVolume) * TimeWarp.fixedDeltaTime;
                if (currentVolume < 0)
                {
                    currentVolume = 0;
                    liftCapacityState = LiftCapacityStates.Empty;
                }
            }
            else if (liftCapacityState == LiftCapacityStates.Increasing && currentVolume < envelopeVolume)
            {
                currentVolume += (compressorRate * envelopeVolume) * TimeWarp.fixedDeltaTime;
                if (currentVolume >= envelopeVolume)
                {
                    currentVolume = envelopeVolume;
                    liftCapacityState = LiftCapacityStates.Maximum;
                }
            }
        }
        #endregion

        #region Actions
        #endregion

        #region API
        /// <summary>
        /// Calculates lift force for the given atmospheric density and force of gravity, assuming maximum envelope volume is used.
        /// </summary>
        /// <param name="atmosphericDensity">The density of the atmopshere. Units are kg/m^3</param>
        /// <param name="forceOfGravity">The force of gravity. Units are m/sec^2</param>
        /// <returns>The lift force in Kilonewtons</returns>
        public double CalculateLiftForce(double atmosphericDensity, double forceOfGravity)
        {
            double liftForce = 0;

            //Buoyancy Force (Newtons) = (atm density - lift gas density)kg/L * (vol of displaced fluid)L * (force of gravity)m/sec^2
            double densityKgPerLiter = (atmosphericDensity - liftGasDensity) / 1000;
            liftForce = densityKgPerLiter * envelopeVolume * forceOfGravity;

            //Factor in lift multiplier
            if (liftMultiplier > 0.001)
                liftForce = liftForce * (1.0 + liftMultiplier);

            //Get kilonewtons
            liftForce = liftForce / 1000; //kN

            return liftForce;
        }

        public double CalculateLiftForce()
        {
            //Gravity
            double forceOfGravity = this.part.vessel.gravityForPos.magnitude;

            //Calculate atmospheric density: units are kg/m^3
            double atmosphericDensity = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(), FlightGlobals.getExternalTemperature());

            //Buoyancy Force (Newtons) = (atm density - lift gas density)kg/L * (vol of displaced fluid)L * (force of gravity)m/sec^2
            double densityKgPerLiter = (atmosphericDensity - liftGasDensity) / 1000;
            liftForce = densityKgPerLiter * currentVolume * forceOfGravity;

            //Factor in lift multiplier
            if (liftMultiplier > 0.001)
                liftForce = liftForce * (1.0 + liftMultiplier);

            //Get kilonewtons
            liftForce = liftForce / 1000; //kN

            return liftForce;
        }
        #endregion

        #region Helpers
        protected void applyPerPartLift()
        {
            //Calculate lift force
            CalculateLiftForce();

            //Divide lift force by vessel mass to get acceleration
            double accelerationForce = liftForce / this.part.vessel.GetTotalMass();

            //Calculate lift vector
            Vector3d accelerationVector = (this.part.WCoM - this.vessel.mainBody.position).normalized * liftForce;

            //Apply lift acceleration
            if (part.rb != null)
                part.rb.AddForce(accelerationVector);
        }

        protected void applyPerVesselLift()
        {
            //Only the first static lifter will create the lift force
            if (staticLifters[0] != this)
                return;

            //Calculate lift force for all the static lifters.
            double totalLiftForce = 0.0f;
            int lifterCount = staticLifters.Count;
            for (int index = 0; index < lifterCount; index++)
                totalLiftForce += staticLifters[index].CalculateLiftForce();

            //Divide lift force by vessel mass to get acceleration
            double accelerationForce = totalLiftForce / this.part.vessel.GetTotalMass();

            //Calculate lift vector
            Vector3d accelerationVector = (this.part.vessel.CoM - this.vessel.mainBody.position).normalized * accelerationForce;

            //Apply lift acceleration
            Part vesselPart;
            for (int index = 0; index < partCount; index++)
            {
                vesselPart = vessel.parts[index];
                if (vesselPart.rb != null)
                {
                    vesselPart.rb.AddForce(accelerationVector, ForceMode.Acceleration);
                }
            }
        }
        #endregion
    }
}
