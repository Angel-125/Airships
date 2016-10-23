using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIModuleStaticLift : PartModule
    {
        [KSPField(guiName = "Lift", guiActive = true, guiUnits = "kN", guiFormat = "f9")]
        public double liftForce;

        [KSPField()]
        public bool debugMode;

        //The following fields are only shown when debugMode = true

        [KSPField(guiName = "Lift Multiplier", guiFormat = "f3")]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0f)]
        public float liftMultiplier;

        [KSPField(guiName = "Lift Gas")]
        public string liftGasResource = "Helium";

        [KSPField(guiName = "Lift Gas Density", guiUnits = "m^3", guiFormat = "f3")]
        protected double liftGasDensity = 0.178; //Helium: kg/m^3

        [KSPField(guiName = "Atm Density", guiUnits = "kg/m^3", guiFormat = "f3")]
        public double atmosphericDensity;

        [KSPField(guiName = "Gravity", guiFormat = "f3")]
        double forceOfGravity;

        public float prevLiftMultiplier;
        WBIModuleStaticLift[] liftModules;

        public override void OnStart(StartState state)
        {
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition definition;
            base.OnStart(state);

            definition = definitions[liftGasResource];
            if (definition != null)
                liftGasDensity = definition.density * 1000000; //units * 1000000 = kg/m^3

            //fields shown when debugMode = true
            Fields["liftGasResource"].guiActive = debugMode;
            Fields["liftGasDensity"].guiActive = debugMode;
            Fields["forceOfGravity"].guiActive = debugMode;
            Fields["atmosphericDensity"].guiActive = debugMode;
            Fields["liftMultiplier"].guiActive = debugMode;

            List<WBIModuleStaticLift> lifters = this.part.vessel.FindPartModulesImplementing<WBIModuleStaticLift>();
            lifters.Remove(this);
            if (lifters.Count > 0)
                liftModules = lifters.ToArray();

            prevLiftMultiplier = liftMultiplier;
        }

        public void FixedUpdate()
        {
            double liftGasAmount = this.part.Resources[liftGasResource].amount;
            forceOfGravity = FlightGlobals.ActiveVessel.gravityForPos.magnitude;
            Vector3d liftVector = (this.part.transform.position - this.vessel.mainBody.position).normalized; //liftVector = (this.part.vessel.CoM - this.vessel.mainBody.position).normalized;

            //Calculate atmospheric density
            atmosphericDensity = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(), FlightGlobals.getExternalTemperature());

            //Lift force = (atmospheric density - lifting gas density) * g * (units of lifting gas / 1000), in Newtons
            liftForce = (atmosphericDensity - liftGasDensity) * forceOfGravity * (liftGasAmount / 1000);
            if (liftMultiplier > 0.001)
                liftForce = liftForce * (1.0 + liftMultiplier);
            liftForce = liftForce / 1000; //kN

            //Apply lift force
            this.part.AddForceAtPosition(liftVector * (float)liftForce, this.part.WCoM);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (debugMode == false)
                return;
            WBIModuleStaticLift lifter;

            if (liftMultiplier != prevLiftMultiplier)
            {
                prevLiftMultiplier = liftMultiplier;

                for (int index = 0; index < liftModules.Length; index++)
                {
                    lifter = liftModules[index];
                    lifter.prevLiftMultiplier = liftMultiplier;
                    lifter.liftMultiplier = liftMultiplier;
                }
            }
        }
    }
}
