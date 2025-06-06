using System;
using Unity.Robotics;
using UnityEngine;

namespace Unity.Robotics.UrdfImporter.Control
{
    public enum RotationDirection { None = 0, Positive = 1, Negative = -1 };
    public enum ControlType { PositionControl };

    public class Controller : MonoBehaviour
    {
        private ArticulationBody[] articulationChain;
        private Color[] prevColor;
        private int previousIndex;

        [InspectorReadOnly(hideInEditMode: true)]
        public string selectedJoint;
        [HideInInspector]
        public int selectedIndex;

        public ControlType control = ControlType.PositionControl;
        public float stiffness;
        public float damping;
        public float forceLimit;
        public float speed = 5f;
        public float torque = 100f;
        public float acceleration = 5f;

        [Tooltip("Color to highlight the currently selected join")]
        public Color highLightColor = new Color(1.0f, 0, 0, 1.0f);

        void Start()
        {
            previousIndex = selectedIndex = 1;
            gameObject.AddComponent<FKRobot>();
            articulationChain = GetComponentsInChildren<ArticulationBody>();
            int defDyanmicVal = 10;
            foreach (ArticulationBody joint in articulationChain)
            {
                joint.gameObject.AddComponent<JointControl>();
                joint.jointFriction = defDyanmicVal;
                joint.angularDamping = defDyanmicVal;
                ArticulationDrive currentDrive = joint.xDrive;
                currentDrive.forceLimit = forceLimit;
                joint.xDrive = currentDrive;
            }
            DisplaySelectedJoint(selectedIndex);
            StoreJointColors(selectedIndex);
        }

        void Update()
        {
            SetSelectedJointIndex(selectedIndex);
            UpdateDirection(selectedIndex);
        }

        void SetSelectedJointIndex(int index)
        {
            if (articulationChain.Length > 0)
                selectedIndex = (index + articulationChain.Length) % articulationChain.Length;
        }

        private void Highlight(int selectedIndex)
        {
            if (selectedIndex == previousIndex || selectedIndex < 0 || selectedIndex >= articulationChain.Length)
                return;

            ResetJointColors(previousIndex);
            StoreJointColors(selectedIndex);
            DisplaySelectedJoint(selectedIndex);

            Renderer[] rendererList = articulationChain[selectedIndex].transform.GetChild(0).GetComponentsInChildren<Renderer>();
            foreach (var mesh in rendererList)
            {
                MaterialExtensions.SetMaterialColor(mesh.material, highLightColor);
            }
        }

        void DisplaySelectedJoint(int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= articulationChain.Length)
                return;
            selectedJoint = articulationChain[selectedIndex].name + " (" + selectedIndex + ")";
        }

        private void UpdateDirection(int jointIndex)
        {
            if (jointIndex < 0 || jointIndex >= articulationChain.Length)
                return;

            JointControl current = articulationChain[jointIndex].GetComponent<JointControl>();
            if (previousIndex != jointIndex)
            {
                JointControl previous = articulationChain[previousIndex].GetComponent<JointControl>();
                previous.direction = RotationDirection.None;
                previousIndex = jointIndex;
            }

            if (current.controltype != control)
            {
                UpdateControlType(current);
            }

            current.direction = RotationDirection.None;
        }

        private void StoreJointColors(int index)
        {
            Renderer[] materialLists = articulationChain[index].transform.GetChild(0).GetComponentsInChildren<Renderer>();
            prevColor = new Color[materialLists.Length];
            for (int counter = 0; counter < materialLists.Length; counter++)
            {
                prevColor[counter] = MaterialExtensions.GetMaterialColor(materialLists[counter]);
            }
        }

        private void ResetJointColors(int index)
        {
            Renderer[] previousRendererList = articulationChain[index].transform.GetChild(0).GetComponentsInChildren<Renderer>();
            for (int counter = 0; counter < previousRendererList.Length; counter++)
            {
                MaterialExtensions.SetMaterialColor(previousRendererList[counter].material, prevColor[counter]);
            }
        }

        public void UpdateControlType(JointControl joint)
        {
            joint.controltype = control;
            if (control == ControlType.PositionControl)
            {
                ArticulationDrive drive = joint.joint.xDrive;
                drive.stiffness = stiffness;
                drive.damping = damping;
                joint.joint.xDrive = drive;
            }
        }

        public void OnGUI()
        {
            GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.UpperCenter;
            GUI.Label(new Rect(Screen.width / 2 - 200, 10, 400, 20), "Controller active. No input bindings in use.", centeredStyle);
        }
    }
}
