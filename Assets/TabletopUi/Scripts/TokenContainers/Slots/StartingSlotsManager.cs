﻿#pragma warning disable 0649

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Core.Entities;
using Assets.Core.Interfaces;
using Assets.CS.TabletopUI;
using Assets.CS.TabletopUI.Interfaces;
using UnityEngine;
using Assets.TabletopUi.Scripts.Services;
using Assets.TabletopUi.Scripts;
using Assets.TabletopUi.Scripts.Infrastructure;

namespace Assets.TabletopUi.SlotsContainers {
    public class StartingSlotsManager : AbstractSlotsManager {

        [SerializeField] SlotGridManager gridManager;
        public CanvasGroupFader canvasGroupFader;

        protected RecipeSlot primarySlot;

        public override void Initialise(IVerb verb) {
            base.Initialise(verb);
            
            var primarySlotSpecification = verb.Slot;
            if(primarySlotSpecification!=null)
                primarySlot = BuildSlot(primarySlotSpecification.Label, primarySlotSpecification, null);
            else
                primarySlot = BuildSlot("Primary recipe slot", new SlotSpecification(), null);


            var otherslots = verb.Slots;
            if(otherslots!=null)
                foreach (var s in otherslots)
                BuildSlot(s.Label, s, null);;

        }

        public void DoReset() {
            if (GetAllSlots().Count > 1) {
                //RemoveAnyChildSlotsWithEmptyParent(); // created an infinite loop - Martin
                ArrangeSlots();
            }
        }

        public override void RespondToStackAdded(RecipeSlot slot, ElementStackToken stack, Context context) {


            situationController.StartingSlotsUpdated();

            if (slot.IsPrimarySlot() && stack.HasChildSlotsForVerb(situationController.GetTokenId()))
                AddSlotsForStack(stack, slot);

            ArrangeSlots();

            situationController.TryResizeWindow(GetAllSlots().Count);

        }

        protected void AddSlotsForStack(ElementStackToken stack, RecipeSlot parentSlot) {

            foreach (var childSlotSpecification in stack.GetChildSlotSpecificationsForVerb(situationController.GetTokenId()))
            {
                var slot = BuildSlot("childslot of " + stack.EntityId, childSlotSpecification, parentSlot);
                parentSlot.childSlots.Add(slot);
            }
        }

        public override void RespondToStackRemoved(ElementStackToken stack, Context context) {
            // startingSlots updated may resize window
            situationController.StartingSlotsUpdated();

            // Only update the slots if we're doing this manually, otherwise don't
            // Addendum: We also do this when retiring a card - Martin
            if (context.IsManualAction() || context.actionSource == Context.ActionSource.Retire)
                RemoveAnyChildSlotsWithEmptyParent(context);

            ArrangeSlots();
            situationController.StartingSlotsUpdated();

        }

        public void RemoveAnyChildSlotsWithEmptyParent(Context context) {
            // We get a copy of the list, since it modifies itself when slots are removed
            List<RecipeSlot> currentSlots = new List<RecipeSlot>(GetAllSlots());

            foreach (RecipeSlot s in currentSlots) {
                if (s != null && s.GetElementStackInSlot() == null && s.childSlots.Count > 0) {
                    List<RecipeSlot> currentChildSlots = new List<RecipeSlot>(s.childSlots);
                    s.childSlots.Clear();

                    foreach (RecipeSlot cs in currentChildSlots)
                        ClearAndDestroySlot(cs, context);
                }
            }

            situationController.StartingSlotsUpdated();
        }

        protected override RecipeSlot BuildSlot(string slotName, SlotSpecification slotSpecification, RecipeSlot parentSlot, bool wideLabel = false) {
            var slot = base.BuildSlot(slotName, slotSpecification, parentSlot, wideLabel);
            gridManager.AddSlot(slot);
            return slot;
        }

        protected override void ClearAndDestroySlot(RecipeSlot slot, Context context) {
            if (slot == null)
                return;
            if (slot.Defunct)
                return;

            validSlots.Remove(slot);

            // This is all copy & paste from the parent class except for the last line
            if (slot.childSlots.Count > 0) {
                List<RecipeSlot> childSlots = new List<RecipeSlot>(slot.childSlots);
                foreach (var cs in childSlots)
                    ClearAndDestroySlot(cs, context);

                slot.childSlots.Clear();
            }

            //Destroy the slot *before* returning the token to the tabletop
            //otherwise, the slot will fire OnCardRemoved again, and we get an infinte loop
            gridManager.RetireSlot(slot);

            if (context != null && context.actionSource == Context.ActionSource.SituationStoreStacks)
                return; // Don't return the tokens to tabletop if we

            AbstractToken tokenContained = slot.GetTokenInSlot();

            if (tokenContained != null)
                tokenContained.ReturnToTabletop(context);
        }

        public void ArrangeSlots() {
            gridManager.ReorderSlots();
        }

        public void SetGridNumPerRow() {
            gridManager.SetNumPerRow();
        }
    }

}
