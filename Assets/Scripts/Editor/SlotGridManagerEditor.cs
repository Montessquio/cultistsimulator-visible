﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Assets.CS.TabletopUI;
using Assets.TabletopUi.Scripts.Services;

[CustomEditor(typeof(SlotGridManager))]
public class SlotGridManagerEditor : Editor {

    SlotGridManager manager;
	[SerializeField] List<Threshold> addedSlots = new List<Threshold>();

	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		manager = target as SlotGridManager;

		if (GUILayout.Button("Add Slot")) {
			manager.AddSlot(BuildSlot());
			manager.ReorderSlots();
		}

        if (GUILayout.Button("Remove First Slot") && addedSlots.Count > 0) {
            RemoveSlot(0);
            manager.ReorderSlots();
        }

        if (GUILayout.Button("Remove Random Slot") && addedSlots.Count > 0) {
            RemoveSlot( Random.Range(0, addedSlots.Count) );
            manager.ReorderSlots();
        }

        if (GUILayout.Button("Remove 2 Slots") && addedSlots.Count > 0) {
            int i = Random.Range(0, addedSlots.Count - 1);
            RemoveSlot(i);
            RemoveSlot(i); // removing from index means we use the same index for the next, otherwise we skip one
            manager.ReorderSlots();
        }

        if (GUILayout.Button("Remove Last Slot") && addedSlots.Count > 0) {
            RemoveSlot(addedSlots.Count - 1);
            manager.ReorderSlots();
        }
    }

    void RemoveSlot(int i) {
        manager.RetireSlot(addedSlots[i]);
        addedSlots.RemoveAt(i);
    }

	public virtual Threshold BuildSlot()
	{
		var slot = GameObject.Instantiate(manager.slotPrefab);
		addedSlots.Add(slot);
		return slot;    
	}
}
