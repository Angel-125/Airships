/*
Source code copyright 2018, by Michael Billard (Angel-125)
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
    public class WBICableDrawer : PartModule
    {
        [KSPField()]
        public float cableWidth = 0.06f;

        [KSPField()]
        public string startTransformName = string.Empty;

        [KSPField()]
        public string endTransformName = string.Empty;

        LineRenderer lineRenderer;
        GameObject cable;
        Transform startTransform, endTransform;
        Color cableColor;

        public void Destroy()
        {
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
            GameObject.DestroyImmediate(lineRenderer);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Material mat = new Material(Shader.Find("Particles/Additive"));

            //Get the start and end points
            if (string.IsNullOrEmpty(startTransformName))
            {
                Debug.Log("[WBICableDrawer] - startTransformName is null");
                return;
            }

            if (string.IsNullOrEmpty(endTransformName))
            {
                Debug.Log("[WBICableDrawer] - endTransformName is null");
                return;
            }

            startTransform = this.part.FindModelTransform(startTransformName);
            if (startTransform == null)
            {
                Debug.Log("[WBICableDrawer] - startTransform is null");
                return;
            }

            endTransform = this.part.FindModelTransform(endTransformName);
            if (startTransform == null)
            {
                Debug.Log("[WBICableDrawer] - endTransform is null");
                return;
            }
            
            //Setup the cable renderer
            cableColor = rgbToColor(50f, 50f, 50f);
            cable = new GameObject();
            cable.name = "Cable";
            lineRenderer = cable.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.material = mat;
            lineRenderer.SetColors(cableColor, cableColor);
            lineRenderer.SetWidth(cableWidth, cableWidth);
            lineRenderer.SetVertexCount(2);
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (startTransform == null || endTransform == null)
                return;

            lineRenderer.SetPosition(0, startTransform.position);
            lineRenderer.SetPosition(1, endTransform.position);
        }

        protected Color rgbToColor(float red, float green, float blue)
        {
            return new Color(red / 255, green / 255, blue / 255);
        }
    }

}
