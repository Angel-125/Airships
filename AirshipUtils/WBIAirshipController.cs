using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

/*
Source code copyright 2022, by Michael Billard (Angel-125)
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
        public struct TemperaturePressureItem
        {
            public double temperature;
            public double pressure;
        }

        #region Fields
        [KSPField]
        public bool debugMode = true;

        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_HEISENBERG_liftControlState",groupStartCollapsed = true, groupName = "airshipControl", groupDisplayName = "#LOC_HEISENBERG_airshipGroupDisplayName")]
        public LiftCapacityStates liftCapacityState;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_HEISENBERG_totalLift", guiUnits = "kN", guiFormat = "n2", groupStartCollapsed = true, groupName = "airshipControl", groupDisplayName = "#LOC_HEISENBERG_airshipGroupDisplayName")]
        public double totalLiftForce;

        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_HEISENBERG_maxVerticalSpeed", guiUnits = "m/s", guiFormat = "n2", groupStartCollapsed = true, groupName = "airshipControl", groupDisplayName = "#LOC_HEISENBERG_airshipGroupDisplayName")]
        [UI_FloatRange(maxValue = 20, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 0.5f)]
        public float maxVerticalSpeed = 0f;

        // Debug fields
        [KSPField(guiName = "Atm Density", guiUnits = "kg/m^3", guiFormat = "f3", groupStartCollapsed = true, groupName = "airshipControl", groupDisplayName = "#LOC_HEISENBERG_airshipGroupDisplayName")]
        public double atmosphericDensity;

        [KSPField(guiName = "Gravity", guiFormat = "f3", guiUnits = "m/s^2", groupStartCollapsed = true, groupName = "airshipControl", groupDisplayName = "#LOC_HEISENBERG_airshipGroupDisplayName")]
        double forceOfGravity;

        [KSPField(guiName = "Vert. Acceleraion", guiFormat = "f3", guiUnits = "m/s^2", groupStartCollapsed = true, groupName = "airshipControl", groupDisplayName = "#LOC_HEISENBERG_airshipGroupDisplayName")]
        double verticalAcceleration;
        #endregion

        #region Houskeeping
        List<WBIModuleStaticLift> liftModules;
        List<WBIAirshipController> airshipControllers;
        int partCount = 0;
        bool isLiftingOff = false;
        List<double> atmosphericDensityTable = null;
        List<double> liftForceTable = null;
        CelestialBody celestialBody = null;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            airshipControllers = part.vessel.FindPartModulesImplementing<WBIAirshipController>();
            liftModules = part.vessel.FindPartModulesImplementing<WBIModuleStaticLift>();
            partCount = part.vessel.parts.Count;

            Fields["forceOfGravity"].guiActive = debugMode;
            Fields["atmosphericDensity"].guiActive = debugMode;
            Fields["verticalAcceleration"].guiActive = debugMode;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Update modules and tables
            if (partCount != part.vessel.parts.Count)
            {
                partCount = part.vessel.parts.Count;
                airshipControllers = part.vessel.FindPartModulesImplementing<WBIAirshipController>();
                liftModules = part.vessel.FindPartModulesImplementing<WBIModuleStaticLift>();

                // Build our lift force table.
                buildLiftForceTable(part.vessel.mainBody, part.vessel.mainBody.GeeASL);
            }

            // Only the first airship controller handles the control logic.
            if (airshipControllers[0] != this)
                return;

            // Get max operating altitude
            double maxAltitude = CalculateMaxAltitude(part.vessel.mainBody);

            // If we're going up, and we're within 1km of max altitude, then slow our ascent until we reach max altitude.

            //

            // Stop expanding the envelopes if our vertical acceleration meets or exceeds our desired maximum.
            if (verticalAcceleration - forceOfGravity >= maxVerticalSpeed)
            {
                liftCapacityState = LiftCapacityStates.Stopped;
                updateCompressorStates(liftCapacityState);
            }

            // If we were increasing lift capacity but our acceleration has slowed below our desired rate then increase lift capacity.

            // If our envelopes are a maximum expansion and our acceleration has slowed then we need to stop climbing.

            if (verticalAcceleration > 0)
            {
                Vector3d accelerationVector = (this.part.vessel.CoM - this.vessel.mainBody.position).normalized * verticalAcceleration;
                ApplyAccelerationVector(accelerationVector);
            }

            /*
            if (maxVerticalSpeed != 0)
            {
                float liftAcceleration = (float)forceOfGravity;
                if (maxVerticalSpeed > 0 && vessel.verticalSpeed < maxVerticalSpeed)
                    liftAcceleration += maxVerticalSpeed;
                else if (maxVerticalSpeed < 0 && vessel.verticalSpeed > maxVerticalSpeed)
                    liftAcceleration += maxVerticalSpeed;
                Vector3d accelerationVector = (this.part.vessel.CoM - this.vessel.mainBody.position).normalized * liftAcceleration;

                ApplyAccelerationVector(accelerationVector);
            }

            /*
            // Monitor vertical ascent.
            if (liftCapacityState == LiftCapacityStates.Increasing && part.vessel.verticalSpeed > maxVerticalSpeed)
            {
                liftCapacityState = LiftCapacityStates.Stopped;
                updateCompressorStates(liftCapacityState);
            }
            else if (liftCapacityState == LiftCapacityStates.Decreasing && part.vessel.verticalSpeed < -maxVerticalSpeed)
            {
                liftCapacityState = LiftCapacityStates.Stopped;
                updateCompressorStates(liftCapacityState);
            }
            else if (part.vessel.situation == Vessel.Situations.FLYING)
            {
                liftCapacityState = LiftCapacityStates.Stopped;
                updateCompressorStates(liftCapacityState);
            }
            */
        }

        #endregion

        #region API
        public double CalculateMaxAltitude(CelestialBody body)
        {
            // if the body has no atmosphere, then we're not going anywhere
            if (!body.atmosphere)
                return 0;

            double totalMass = part.vessel.GetTotalMass();
            double liftForce = 0;
            double liftAcceleration = 0;
            int count = liftForceTable.Count;
            double maxAltitude = 0;

            // Go through each entry in the lift force table, calculate the lift acceleration based on vessel mass, and when we find a value that matches the body's gee force, we've
            // found max altitude.
            for (int altitudeIndex = 0; altitudeIndex < count; altitudeIndex++)
            {
                // Calculate lift acceleration from lift force
                liftForce = liftForceTable[altitudeIndex];
                liftAcceleration = liftForce / totalMass;

                // If lift acceleration matches the body's gravity, then we've found max altitude.
                if (liftAcceleration < body.GeeASL)
                {
                    totalLiftForce = liftForce;
                    maxAltitude = altitudeIndex * 1000f;
                    break;
                }
            }

            return maxAltitude;
        }

        public double GetTotalLiftForce()
        {
            int count = liftModules.Count;
            double liftForce = 0;
            for (int index = 0; index < count; index++)
            {
                liftForce += liftModules[index].CalculateLiftForce();
            }

            return liftForce;
        }

        public double GetTotalLiftForce(double atmosphericDensity, double forceOfGravity)
        {
            int count = liftModules.Count;
            double liftForce = 0;
            for (int index = 0; index < count; index++)
            {
                liftForce += liftModules[index].CalculateLiftForce(atmosphericDensity, forceOfGravity);
            }

            return liftForce;
        }

        public void ApplyAccelerationVector(Vector3d accelerationVector)
        {
            int partCount = vessel.parts.Count;
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

        #region Events
        [KSPEvent(guiActive = true, guiName = "#LOC_HEISENBERG_increaseStaticLift", groupStartCollapsed = true, groupName = "airshipControl", groupDisplayName = "#LOC_HEISENBERG_airshipGroupDisplayName")]
        public void IncreaseLiftCapacity()
        {
            liftCapacityState = LiftCapacityStates.Increasing;
            updateCompressorStates(LiftCapacityStates.Increasing);
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_HEISENBERG_decreaseStaticLift", groupStartCollapsed = true, groupName = "airshipControl", groupDisplayName = "#LOC_HEISENBERG_airshipGroupDisplayName")]
        public void DecreaseLiftCapacity()
        {
            liftCapacityState = LiftCapacityStates.Decreasing;
            updateCompressorStates(LiftCapacityStates.Decreasing);
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_HEISENBERG_stopStaticLift", groupStartCollapsed = true, groupName = "airshipControl", groupDisplayName = "#LOC_HEISENBERG_airshipGroupDisplayName")]
        public void StopCompressors()
        {
            liftCapacityState = LiftCapacityStates.Stopped;
            updateCompressorStates(LiftCapacityStates.Stopped);
        }
        #endregion

        #region Actions
        [KSPAction("#LOC_HEISENBERG_increaseStaticLift", actionGroup = KSPActionGroup.None)]
        public void IncreaseLiftAction(KSPActionParam param)
        {
            IncreaseLiftCapacity();
        }

        [KSPAction("#LOC_HEISENBERG_decreaseStaticLift", actionGroup = KSPActionGroup.None)]
        public void DecreaseLiftAction(KSPActionParam param)
        {
            DecreaseLiftCapacity();
        }

        [KSPAction("#LOC_HEISENBERG_stopStaticLift", actionGroup = KSPActionGroup.Brakes)]
        public void StopLiftAction(KSPActionParam param)
        {
            StopCompressors();
        }
        #endregion

        #region Helpers
        List<double> buildLiftForceTable(CelestialBody body, double forceOfGravity)
        {
            liftForceTable = new List<double>();

            int currentIteration = 0;
            double altitude = 0;
            double atmopshericDensity = 0;
            double liftForce = 0;

            while (altitude < body.atmosphereDepth && currentIteration < 1000)
            {
                atmopshericDensity = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(altitude, body), FlightGlobals.getExternalTemperature(altitude, body));
                liftForce = GetTotalLiftForce(atmopshericDensity, forceOfGravity);

                liftForceTable.Add(liftForce);

                altitude += 1000f;
                currentIteration += 1;
            }

            return liftForceTable;
        }

        protected void updateCompressorStates(LiftCapacityStates liftCapacityState)
        {
            int count = liftModules.Count;

            for (int index = 0; index < count; index++)
            {
                liftModules[index].liftCapacityState = liftCapacityState;
            }
        }
        #endregion
    }
}
