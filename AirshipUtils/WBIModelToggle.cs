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
    public class WBIModelToggle : PartModule
    {
        [KSPField()]
        public string meshTransforms = string.Empty;

        [KSPField()]
        public string autoShowWithNode = string.Empty;

        [KSPField(isPersistant = true)]
        public bool isVisible = true;

        [KSPField]
        public string meshVisibleName = "Show Endcap";

        [KSPField]
        public string meshHiddenName = "Hide Endcap";

        [KSPEvent(guiActiveEditor = true)]
        public void ToggleModel()
        {
            isVisible = !isVisible;

            showHideModel(isVisible);
        }

        protected void showHideModel(bool visible)
        {
            string[] tagTransforms = meshTransforms.Split(';');
            Transform[] targets;

            foreach (string transform in tagTransforms)
            {
                //Get the targets
                targets = part.FindModelTransforms(transform);
                if (targets == null)
                {
                    Debug.Log("No targets found for " + transform);
                    return;
                }

                foreach (Transform target in targets)
                {
                    target.gameObject.SetActive(isVisible);
                    Collider collider = target.gameObject.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = visible;
                }
            }

            if (visible)
                Events["ToggleModel"].guiName = meshHiddenName;
            else
                Events["ToggleModel"].guiName = meshVisibleName;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            showHideModel(false);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            showHideModel(isVisible);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsEditor)
                checkAttachmentNode();
        }

        protected void checkAttachmentNode()
        {
            Debug.Log("checkAttachmentNode called");
            if (this.part.attachNodes == null)
            {
                Debug.Log("No attach nodes");
                return;
            }

            AttachNode[] nodes = this.part.attachNodes.ToArray();
            for (int index = 0; index < nodes.Length; index++)
            {
                Debug.Log("Checking node: " + nodes[index].id);
                if (nodes[index].id == autoShowWithNode && nodes[index].attachedPart != null)
                {
                    isVisible = true;
                    showHideModel(isVisible);
                    return;
                }
            }
        }

    }
}
