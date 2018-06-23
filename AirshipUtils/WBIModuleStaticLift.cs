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
    /// <summary>
    /// This part module provides static lift based upon the lifting gas density and volume.
    /// Be sure to account for the lifting gas's mass in the part mass.
    /// </summary>
    public class WBIModuleStaticLift : PartModule
    {
        [KSPField(guiActive = true, guiActiveEditor= true, guiName = "Lift", guiUnits = "kN", guiFormat = "n2")]
        public double liftForce;

        [KSPField]
        public bool debugMode;

        //The following fields are only shown when debugMode = true
        [KSPField(guiName = "Lift Multiplier", guiFormat = "f3")]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0f)]
        public float liftMultiplier;

        [KSPField(guiName = "Lift Gas")]
        public string liftGasResource = "Phlogiston";

        [KSPField(guiName = "Atm Density", guiUnits = "kg/m^3", guiFormat = "f3")]
        public double atmosphericDensity;

        [KSPField(guiName = "Gravity", guiFormat = "f3")]
        double forceOfGravity;

        public double liftGasDensityKgCubicMeters = 0f;
        protected double liftGasDensity = 0.0000000899; //Phlogiston(hydrogen) metric tons per liter
        int partCount = -1;
        List<WBIModuleStaticLift> staticLifters;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            SetupLiftGas();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //fields shown when debugMode = true
            Fields["liftGasResource"].guiActive = debugMode;
            Fields["forceOfGravity"].guiActive = debugMode;
            Fields["atmosphericDensity"].guiActive = debugMode;
            Fields["liftMultiplier"].guiActive = debugMode;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                if (HighLogic.LoadedSceneIsEditor)
                    CalculateLiftForce();
                return;
            }

            //Update the static lifters.
            updateStaticLifters();

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
//            this.part.AddForceAtPosition(liftVector * (float)liftForce, this.part.vessel.CoM);
        }

        public virtual void SetupLiftGas()
        {
            PartResourceDefinition definition = ResourceHelper.DefinitionForResource(liftGasResource);
            float tonnesPerLiter = definition.density / definition.volume;

            liftGasDensityKgCubicMeters = liftGasDensity * 1000000;
        }

        public double CalculateLiftForce()
        {
            if (!this.part.Resources.Contains(liftGasResource))
                return -1.0f;

            double liftGasAmount = this.part.Resources[liftGasResource].amount;

            if (HighLogic.LoadedSceneIsFlight)
            {
                //Gravity
                forceOfGravity = this.part.vessel.gravityForPos.magnitude;

                //Calculate atmospheric density
                atmosphericDensity = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(), FlightGlobals.getExternalTemperature());
            }

            //Editor: Calculate for currently selected body

            //Lift force = (atmospheric density - lifting gas density) * g * (units of lifting gas / 1000), in Newtons
            liftForce = (atmosphericDensity - liftGasDensityKgCubicMeters) * forceOfGravity * (liftGasAmount / 1000);

            //Factor in lift multiplier
            if (liftMultiplier > 0.001)
                liftForce = liftForce * (1.0 + liftMultiplier);

            //Get kilonewtons
            liftForce = liftForce / 1000; //kN

            return liftForce;
        }

        protected virtual void updateStaticLifters()
        {
            if (partCount != this.part.vessel.parts.Count)
            {
                partCount = this.part.vessel.parts.Count;
                staticLifters = this.part.vessel.FindPartModulesImplementing<WBIModuleStaticLift>();
            }
        }
    }
}
