// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.LoveHate
{

    /// <summary>
    /// Manages a debug canvas that reports a faction member's state.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    [RequireComponent(typeof(FactionMember))]
    public class FactionMemberDebugger : MonoBehaviour, IModifyPadDeedEventHandler, IRememberDeedEventHandler
    {

        [Tooltip("This key toggles visibility.")]
        public KeyCode toggleKey = KeyCode.BackQuote;

        [Tooltip("Debugger canvas in which to show state. Can be a prefab.")]
        public FactionMemberDebuggerCanvas debuggerCanvas;

        public float verticalOffset = 2.5f;

        public bool visible = true;

        protected FactionMember member { get; set; }

        protected virtual void Start()
        {
            if (debuggerCanvas == null)
            {
                enabled = false;
                return;
            }
            if (debuggerCanvas.onlyInDebugBuild && !Debug.isDebugBuild)
            {
                Destroy(this);
                return;
            }
            if (!debuggerCanvas.gameObject.activeInHierarchy)
            {
                debuggerCanvas = Instantiate(debuggerCanvas) as FactionMemberDebuggerCanvas;
                debuggerCanvas.transform.SetParent(transform);
                debuggerCanvas.transform.localPosition = new Vector3(0, verticalOffset, 0);
            }
            debuggerCanvas.gameObject.SetActive(visible);
            member = GetComponentInParent<FactionMember>() ?? GetComponentInChildren<FactionMember>();
            UpdatePadText();
        }

        protected virtual void Update()
        {
            if (InputDeviceManager.IsKeyDown(toggleKey))
            {
                debuggerCanvas.gameObject.SetActive(!debuggerCanvas.gameObject.activeSelf);
                visible = debuggerCanvas.gameObject.activeSelf;
            }
            else if (visible != debuggerCanvas.gameObject.activeSelf)
            {
                debuggerCanvas.gameObject.SetActive(visible);
            }
        }

        public virtual void UpdatePadText()
        {
            if (debuggerCanvas.padText != null)
            {
                debuggerCanvas.padText.text = "P:" + (int)member.pad.pleasure + " A:" + (int)member.pad.arousal +
                    " D:" + (int)member.pad.dominance + " H:" + (int)member.pad.happiness;
            }
        }

        public virtual void OnModifyPad(float happinessChange, float pleasureChange, float arousalChange, float dominanceChange)
        {
            UpdatePadText();
        }

        public virtual void OnRememberDeed(Rumor rumor)
        {
            if (debuggerCanvas.memoryCountText != null)
            {
                debuggerCanvas.memoryCountText.text = "Memories:" + member.shortTermMemory.Count;
            }
            if (debuggerCanvas.lastMemoryText != null)
            {
                debuggerCanvas.lastMemoryText.text = "Last:<" + member.factionManager.GetFactionSilent(rumor.actorFactionID).name + "," +
                    rumor.tag + "," + member.factionManager.GetFactionSilent(rumor.targetFactionID).name + ">";
            }
        }

    }
}