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
    public class WBIGasStation : PartModule
    {
        /*
        [KSPField(guiActive = true, guiName = "Refuel %", guiFormat = "f2")]
        [UI_FloatRange(stepIncrement = 5f, maxValue = 100f, minValue = 1f)]
        public float refuelPercent = 100f;
         */

        [KSPField()]
        public string refuelResource = "LiquidFuel";

        [KSPField(guiActive = true, guiName = "Refuel Cost", guiUnits = "Funds", guiFormat = "f2")]
        public double refuelCost;

        [KSPField()]
        public float fuelIncreaseDistance = 100f;

        [KSPField()]
        public float fuelPercentIncrease = 1.0f;

        protected bool confirmedPurchase;
        protected double totalFuelCost;
        protected float fuelDistanceMeters;

        [KSPEvent(guiActive = true, guiName = "Buy LiquidFuel")]
        public void BuyResource()
        {
            PartResource resourceToRefuel = this.part.Resources[refuelResource];

            //Make sure that we can affort the refuel cost.
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                if (Funding.Instance.Funds < refuelCost)
                {
                    ScreenMessages.PostScreenMessage("Unable to afford the refueling cost.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
            }

            //Confirm purchase
            if (!confirmedPurchase)
            {
                confirmedPurchase = true;
                ScreenMessages.PostScreenMessage("Click a second time to confirm purchase.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            confirmedPurchase = false;

            //Pay refuel cost
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                Funding.Instance.AddFunds(-refuelCost, TransactionReasons.Any);
            
            //Refuel the tank
            resourceToRefuel.amount = resourceToRefuel.maxAmount;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            try
            {
                PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
                PartResourceDefinition resourceDef = definitions[refuelResource];

                totalFuelCost = resourceDef.unitCost * this.part.Resources[refuelResource].maxAmount;
                fuelDistanceMeters = fuelIncreaseDistance * 1000f;

                Events["BuyResource"].guiName = "Buy " + refuelResource;
            }
            catch (Exception ex)
            {
                Debug.Log("[WBIGasStation] error during OnStart: " + ex);
            }

        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (HighLogic.LoadedSceneIsFlight)
            {
                try
                {
                    //Make sure we're landed on the home world
                    if (vessel.situation == Vessel.Situations.LANDED)
                    {
                        if (vessel.mainBody.isHomeWorld)
                            Events["BuyResource"].guiActive = true;
                        else
                            Events["BuyResource"].guiActive = false;
                    }

                    //Not landed
                    else
                    {
                        Events["BuyResource"].guiActive = false;
                    }

                    double distance = SpaceCenter.Instance.GreatCircleDistance(SpaceCenter.Instance.cb.GetRelSurfaceNVector(this.part.vessel.latitude, this.part.vessel.longitude));
                    PartResource resourceToRefuel = this.part.Resources[refuelResource];

                    //Calculate the cost of completely refueling the tank.
                    refuelCost = totalFuelCost;
                    if (distance >= fuelDistanceMeters)
                        refuelCost *= ((distance / fuelDistanceMeters) * (1.0f + (fuelPercentIncrease / 100f)));

                    //Now calculate the cost of refueling the tank.
                    if (resourceToRefuel.amount < resourceToRefuel.maxAmount && resourceToRefuel.amount > 0f)
                        refuelCost *= 1 - (resourceToRefuel.amount / resourceToRefuel.maxAmount);

                    else if (resourceToRefuel.amount >= resourceToRefuel.maxAmount)
                        refuelCost = 0f;
                }
                catch (Exception ex)
                {
                    Debug.Log("[WBIGasStation] error during OnUpdate: " + ex);
                }
            }
        }
    }
}
