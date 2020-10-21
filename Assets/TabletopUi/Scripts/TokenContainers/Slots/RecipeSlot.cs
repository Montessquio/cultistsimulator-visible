﻿using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Core.Commands;
using Assets.Core.Entities;
using Assets.Core.Enums;
using Assets.Core.Interfaces;
using Assets.CS.TabletopUI.Interfaces;
using Assets.TabletopUi.Scripts;
using Assets.TabletopUi.Scripts.Infrastructure;
using Assets.TabletopUi.Scripts.Infrastructure.Events;
using Assets.TabletopUi.Scripts.Interfaces;
using Noon;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Assets.CS.TabletopUI {



    public class RecipeSlot : TokenContainer, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {

        public event System.Action<RecipeSlot, ElementStackToken, Context> onCardDropped;
        public event System.Action<ElementStackToken, Context> onCardRemoved;

        public override ContainerCategory ContainerCategory => ContainerCategory.Threshold;

        // DATA ACCESS
        public SlotSpecification GoverningSlotSpecification { get; set; }
        public IList<RecipeSlot> childSlots { get; set; }
        public RecipeSlot ParentSlot { get; set; }
        public bool Defunct { get; set; }
        public string AnimationTag { get; set; }

        // VISUAL ELEMENTS
        public RecipeSlotViz viz;

        public TextMeshProUGUI SlotLabel;
        public Graphic border;
        public GraphicFader slotGlow;
        public LayoutGroup slotIconHolder;

        public GameObject GreedyIcon;
        public GameObject ConsumingIcon;

        bool lastGlowState;

        public bool IsBeingAnimated { get; set; }

        public override bool AllowStackMerge { get { return false; } }

        public override bool AllowDrag {
            get {
                return !GoverningSlotSpecification.Greedy || IsBeingAnimated;
            }
        }

        public override bool IsGreedy
        {
            get { return GoverningSlotSpecification != null && GoverningSlotSpecification.Greedy; }
        }

        public bool IsConsuming
        {
            get { return GoverningSlotSpecification.Consumes; }
        }

        public enum SlotModifier { Locked, Ongoing, Greedy, Consuming };

        public RecipeSlot() {
            childSlots = new List<RecipeSlot>();
        }

        public override void Start() {
            ShowGlow(false, false);
            Registry.Get<LocalNexus>().TokenInteractionEvent.AddListener(ReactToDraggedToken);
            _notifiersForContainer.Add(Registry.Get<INotifier>());
            base.Start();
        }

        public void Initialise(SlotSpecification slotSpecification) {
            GoverningSlotSpecification = slotSpecification;
            //we need to do this first. Code checks if an ongoing slot is active by checking whether it has a slotspecification
            //slots with null slotspecification are inactive.

            if (slotSpecification == null)
                return;

            SlotLabel.text = slotSpecification.Label;

            GreedyIcon.SetActive(slotSpecification.Greedy);
            ConsumingIcon.SetActive(slotSpecification.Consumes);
        }


        void ReactToDraggedToken(TokenInteractionEventArgs args)
        {

            if (args.TokenInteractionType == TokenInteractionType.BeginDrag)
            {

                var stack = args.Token as ElementStackToken;

                if (stack == null)
                    return;
                if (GetElementStackInSlot() != null)
                    return; // Slot is filled? Don't highlight it as interactive
                if (IsBeingAnimated)
                    return; // Slot is being animated? Don't highlight
                if (IsGreedy)
                    return; // Slot is greedy? It can never take anything.
                if (stack.EntityId == "dropzone")
                    return; // Dropzone can never be put in a slot

                if (GetSlotMatchForStack(stack).MatchType == SlotMatchForAspectsType.Okay)
                    ShowGlow(true,false);
            }


            else if (args.TokenInteractionType == TokenInteractionType.EndDrag)
                    ShowGlow(false, false);


        }

        bool CanInteractWithDraggedObject(AbstractToken token) {
            if (lastGlowState == false || token == null)
                return false;

            var stack = token as ElementStackToken;

            if (stack == null)
                return false; // we only accept stacks

            //does the token match the slot? Check that first
            SlotMatchForAspects match = GetSlotMatchForStack(stack);

            return match.MatchType == SlotMatchForAspectsType.Okay;
        }

        // IGlowableView implementation

        public virtual void OnPointerEnter(PointerEventData eventData) {
            if (GoverningSlotSpecification.Greedy) // never show glow for greedy slots
                return;

            //if we're not dragging anything, and the slot is empty, glow the slot.
            if (!eventData.dragging) {
                if (GetTokenInSlot() == null)
                    ShowHoverGlow(true);
            }
            else if (CanInteractWithDraggedObject(eventData.pointerDrag.GetComponent<AbstractToken>())) {
                if (lastGlowState)
                    eventData.pointerDrag.GetComponent<AbstractToken>().HighlightPotentialInteractionWithToken(true);

                if (GetTokenInSlot() == null) // Only glow if the slot is empty
                    ShowHoverGlow(true);
            }
        }

        public virtual void OnPointerExit(PointerEventData eventData) {
            if (GoverningSlotSpecification.Greedy) // we're greedy? No interaction.
                return;

            if(eventData.dragging)
            {
                var potentialDragToken = eventData.pointerDrag.GetComponent<AbstractToken>();

                if (lastGlowState && potentialDragToken != null)
                    potentialDragToken.HighlightPotentialInteractionWithToken(false);
            }
            ShowHoverGlow(false);
        }

        public void SetGlowColor(UIStyle.TokenGlowColor colorType) {
            SetGlowColor(UIStyle.GetGlowColor(colorType));
        }

        public void SetGlowColor(Color color) {
            slotGlow.SetColor(color);
        }

        public void ShowGlow(bool glowState, bool instant) {
            lastGlowState = glowState;

            if (glowState)
                slotGlow.Show(instant);
            else
                slotGlow.Hide(instant);
        }

        // Separate method from ShowGlow so we can restore the last state when unhovering
        protected virtual void ShowHoverGlow(bool show) {
            // We're NOT dragging something and our last state was not "this is a legal drop target" glow, then don't show
            //if (AbstractToken.itemBeingDragged == null && !lastGlowState)
            //    return;

            if (show) {
                SetGlowColor(UIStyle.TokenGlowColor.OnHover);
                SoundManager.PlaySfx("TokenHover");
                slotGlow.Show();
            }
            else {
                SetGlowColor(UIStyle.TokenGlowColor.Default);
                //SoundManager.PlaySfx("TokenHoverOff");

                if (lastGlowState)
                    slotGlow.Show();
                else
                    slotGlow.Hide();
            }
        }

        public bool HasChildSlots() {
            if (childSlots == null)
                return false;
            return childSlots.Count > 0;
        }

        public bool TryPutElementStackInSlot(ElementStackToken stack)
        {

            //does the token match the slot? Check that first
            SlotMatchForAspects match = GetSlotMatchForStack(stack);

            if (match.MatchType != SlotMatchForAspectsType.Okay)
            {
                stack.SetXNess(TokenXNess.DoesntMatchSlotRequirements);
                stack.ReturnToStartPosition();

                var notifier = Registry.Get<INotifier>();

                var compendium = Registry.Get<ICompendium>();

                if (notifier != null)
                    notifier.ShowNotificationWindow(Registry.Get<ILocStringProvider>().Get("UI_CANTPUT"), match.GetProblemDescription(compendium), false);
            }
            else if (stack.Quantity != 1)
            {
                // We're dropping more than one?
                // set main stack to be returned to start position
                stack.SetXNess( TokenXNess.ReturningSplitStack);
                // And we split a new one that's 1 (leaving the returning card to be n-1)
                var newStack = stack.SplitAllButNCardsToNewStack(stack.Quantity - 1, new Context(Context.ActionSource.PlayerDrag));
                // And we put that into the slot
                AcceptStack(newStack, new Context(Context.ActionSource.PlayerDrag));
            }
            else
            {
                //it matches. Now we check if there's a token already there, and replace it if so:
                var currentOccupant = GetElementStackInSlot();

                // if we drop in the same slot where we came from, do nothing.
                if (currentOccupant == stack)
                {
                    stack.SetXNess(TokenXNess.ReturnedToStartingSlot);
                    return false;
                }

                if (currentOccupant != null)
                    throw new NotImplementedException("There's still a card in the slot when this reaches the slot; it wasn't intercepted by being dropped on the current occupant. Rework.");
                //currentOccupant.ReturnToTabletop();

                //now we put the token in the slot.
                stack.SetXNess(TokenXNess.PlacedInSlot);
                AcceptStack(stack, new Context(Context.ActionSource.PlayerDrag));
                SoundManager.PlaySfx("CardPutInSlot");
            }

            return true;
        }


        public void OnDrop(PointerEventData eventData)
        {

            var stack = eventData.pointerDrag.GetComponent<ElementStackToken>();

            if (GoverningSlotSpecification.Greedy) // we're greedy? No interaction.
                return;

            if (IsBeingAnimated || !eventData.dragging || stack==null)
                return;

            TryPutElementStackInSlot(stack);

        }

        public override void AcceptStack(ElementStackToken stack, Context context) {
        base.AcceptStack(stack,context);
        
        onCardDropped(this, stack, context);
        }

        

        public override void DisplayHere(IToken token, Context context) {
            base.DisplayHere(token, context);
            var stack = token as ElementStackToken;

            if (stack != null) {
                slotIconHolder.transform.SetAsLastSibling();
            }
        }

        public AbstractToken GetTokenInSlot() {
            return GetComponentInChildren<AbstractToken>();
        }

        public ElementStackToken GetElementStackInSlot()
        {
            if (GetStacks().Count() > 1)
            {
                NoonUtility.Log("Something weird in slot " + GoverningSlotSpecification.Id +
                                ": it has more than one stack, so we're just returning the first.");
                return GetStacks().First();

            }

            return GetStacks().SingleOrDefault();
        }

        public SlotMatchForAspects GetSlotMatchForStack(ElementStackToken stack)
        {
            //no multiple stack is ever permitted in a slot  EDIT: removed this because we have support for splitting the stack to get a single card out - CP
//			if (stack.Quantity > 1)
//				return new SlotMatchForAspects(new List<string>{"Too many!"}, SlotMatchForAspectsType.ForbiddenAspectPresent);

            if (stack.EntityId == "dropzone")
                return new SlotMatchForAspects(new List<string>(), SlotMatchForAspectsType.ForbiddenAspectPresent);
            if (GoverningSlotSpecification == null)
                return SlotMatchForAspects.MatchOK();
            else
                return GoverningSlotSpecification.GetSlotMatchForAspects(stack.GetAspects());
        }

        public override void SignalStackRemoved(ElementStackToken elementStackToken, Context context)
        {
            onCardRemoved(elementStackToken, context);
        }

        public override void TryMoveAsideFor(ElementStackToken potentialUsurper, AbstractToken incumbent, out bool incumbentMoved) {
            if (IsGreedy) { // We do not allow
                incumbentMoved = false;
                return;
            }

            //incomer is a token. Does it fit in the slot?
            if (GetSlotMatchForStack(potentialUsurper).MatchType==SlotMatchForAspectsType.Okay && potentialUsurper.Quantity == 1)
            {
                incumbentMoved = true;
                incumbent.ReturnToTabletop(new Context(Context.ActionSource.PlayerDrag)); //do this first; AcceptStack will trigger an update on the displayed aspects
                AcceptStack(potentialUsurper, new Context(Context.ActionSource.PlayerDrag));
            }
            else
                incumbentMoved = false;
        }



        /// <summary>
        /// path to slot expressed in underscore-separated slot specification labels: eg "work_sacrifice"
        /// </summary>
        public string SaveLocationInfoPath {
            get {
                string saveLocationInfo = GoverningSlotSpecification.Id;
                if (ParentSlot != null)
                    saveLocationInfo = ParentSlot.SaveLocationInfoPath + SaveConstants.SEPARATOR + saveLocationInfo;
                return saveLocationInfo;
            }
        }

        public override string GetSaveLocationForToken(AbstractToken token) {
            return SaveLocationInfoPath; //we don't currently care about the actual draggable
        }

        public override void ActivatePreRecipeExecutionBehaviour() {
            if (GoverningSlotSpecification.Consumes) {
                var stack = GetElementStackInSlot();

                if (stack != null)
                    stack.MarkedForConsumption = true;
            }
        }

        public bool Retire() {
            UnityEngine.Object.Destroy(gameObject);

            if (Defunct)
                return false;

            Defunct = true;
            return true;
        }

        public bool IsPrimarySlot()
        {
            return ParentSlot == null;
        }

        public void OnPointerClick(PointerEventData eventData) {
            bool highlightGreedy = GreedyIcon.gameObject.activeInHierarchy && eventData.hovered.Contains(GreedyIcon);
            bool highlightConsumes = ConsumingIcon.gameObject.activeInHierarchy && eventData.hovered.Contains(ConsumingIcon);

            Registry.Get<INotifier>().ShowSlotDetails(GoverningSlotSpecification, highlightGreedy, highlightConsumes);

        }
        /// <summary>
        ///  </summary>
        /// <param name="element"></param>
        /// <param name="maxToPurge"></param>
        /// <param name="context"></param>
        /// <returns>count of elements purged</returns>
        public int TryPurgeElement(Element element, int maxToPurge)
        {
            if (maxToPurge <= 0)
                return 0;

            var stackInSlot = GetElementStackInSlot();
            if (stackInSlot == null)
                return 0;
            //passing in maxToPurge just in case we someday want stacks in slots
            if (!stackInSlot.Defunct && stackInSlot.GetAspects().ContainsKey(element.Id)) 
            {
                int originalQuantity = stackInSlot.Quantity;
                stackInSlot.ModifyQuantity(-maxToPurge, new Context(Context.ActionSource.Purge));
                return originalQuantity;
            }

            return 0;

        }


    }
}
