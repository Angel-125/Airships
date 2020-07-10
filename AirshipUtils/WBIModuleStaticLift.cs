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
        [KSPField(guiActive = true, guiActiveEditor= true, guiName = "Lift", guiUnits = "kN", guiFormat = "n2")]
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

        [KSPField(isPersistant = true, guiActive = true, guiName = "Lift Capacity")]
        public LiftCapacityStates liftCapacityState;

        [KSPField]
        public double compressorRate = 0.025;

        //The following fields are only shown when debugMode = true
        [KSPField(guiName = "Lift Multiplier", guiFormat = "f3")]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 300f, minValue = 0f)]
        public float liftMultiplier;

        [KSPField(guiName = "Atm Density", guiUnits = "kg/m^3", guiFormat = "f3")]
        public double atmosphericDensity;

        [KSPField(guiName = "Gravity", guiFormat = "f3")]
        double forceOfGravity;
        #endregion

        #region Housekeeping
        public double liftGasDensityKgCubicMeters = 0f;
        public double liftGasVolumeLiters = 0;

        int partCount = -1;
        List<WBIModuleStaticLift> staticLifters;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //fields shown when debugMode = true
            Fields["forceOfGravity"].guiActive = debugMode;
            Fields["atmosphericDensity"].guiActive = debugMode;
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

            //Apply lift
            applyPerPartLift();
        }
        #endregion

        #region API
        public void IncreaseLiftCapacity()
        {
            liftCapacityState = LiftCapacityStates.Increasing;
        }

        public void DecreaseLiftCapacity()
        {
            liftCapacityState = LiftCapacityStates.Decreasing;
        }

        public void StopLiftCapacityChange()
        {
            liftCapacityState = LiftCapacityStates.Stopped;
        }

        public double CalculateLiftForce()
        {
            //Gravity
            forceOfGravity = this.part.vessel.gravityForPos.magnitude;

            //Calculate atmospheric density: units are kg/m^3
            atmosphericDensity = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(), FlightGlobals.getExternalTemperature());

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
            this.part.rb.AddForce(accelerationVector);
        }

        protected void applyPerVesselLift()
        {
            //Update the static lifters.
            if (partCount != this.part.vessel.parts.Count)
            {
                partCount = this.part.vessel.parts.Count;
                staticLifters = this.part.vessel.FindPartModulesImplementing<WBIModuleStaticLift>();
            }

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
