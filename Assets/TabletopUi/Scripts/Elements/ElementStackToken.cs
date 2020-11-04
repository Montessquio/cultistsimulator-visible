﻿#pragma warning disable 0649

using System;
using System.Collections.Generic;
using Assets.Core;
using Assets.Core.Interfaces;
using Assets.CS.TabletopUI.Interfaces;
using Assets.TabletopUi.Scripts;
using Assets.TabletopUi.Scripts.Services;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using Assets.Core.Commands;
using Assets.Core.Entities;
using Assets.Core.Enums;
using Assets.Core.Services;
using Assets.Logic;
using Assets.TabletopUi.Scripts.Elements;
using Assets.TabletopUi.Scripts.Elements.Manifestations;
using Assets.TabletopUi.Scripts.Infrastructure;
using Assets.TabletopUi.Scripts.Infrastructure.Events;
using Assets.TabletopUi.Scripts.Interfaces;
using Assets.TabletopUi.Scripts.TokenContainers;
using Assets.TabletopUi.Scripts.UI;
using Noon;
using UnityEngine.InputSystem;

// Should inherit from a "TabletopToken" base class same as VerbBox

namespace Assets.CS.TabletopUI {



    public class ElementStackToken : AbstractToken, IArtAnimatableToken
    {
        public const float SEND_STACK_TO_SLOT_DURATION = 0.2f;

        public event System.Action<ElementStackToken> onTurnFaceUp; // only used in the map to hide the other cards
        public event System.Action<float> onDecay;

 
        private IElementManifestation _manifestation;
        private Element _element;
        private int _quantity;

		// Cache aspect lists because they are EXPENSIVE to calculate repeatedly every frame - CP
		private IAspectsDictionary _aspectsDictionaryInc;		// For caching aspects including self 
		private IAspectsDictionary _aspectsDictionaryExc;		// For caching aspects excluding self
		private bool _aspectsDirtyInc = true;
		private bool _aspectsDirtyExc = true;

        // Interaction handling - CP
		protected bool singleClickPending = false;

        public float LifetimeRemaining { get; set; }
        public override bool NoPush
        {
            get { return _manifestation.NoPush; }
        }
        private bool shrouded = false;
        public Source StackSource { get; set; }

        private ElementStackToken originStack = null; // if it was pulled from a stack, save that stack!
        private Dictionary<string,int> _currentMutations; //not strictly an aspects dictionary; it can contain negatives
        private IlluminateLibrarian _illuminateLibrarian;
    

        private HashSet<ITokenObserver> observers=new HashSet<ITokenObserver>();

        //set true when the Chronicler notices it's been placed on the desktop. This ensures we don't keep spamming achievements / Lever requests. It isn't persisted in saves! which is probably fine.
        public bool PlacementAlreadyChronicled=false;


        public bool AddObserver(ITokenObserver observer)
        {
            if (observers.Contains(observer))
                return false;

            observers.Add(observer);
            return true;
        }

        public bool RemoveObserver(ITokenObserver observer)
        {
            if(!observers.Contains(observer))
                return false;
            observers.Remove(observer);
            return true;

        }

        public override string EntityId {
            get { return _element == null ? null : _element.Id; }
        }
        virtual public string Label
        {
            get { return _element == null ? null : _element.Label; }
        }

        virtual public string Icon
        {
            get { return _element == null ? null : _element.Icon; }
        }

        public string EntityWithMutationsId 
        {
	        // Generate a unique ID for a combination of entity ID and mutations
	        // IDs will look something like: entity_id?mutation_1=2&mutation_2=-1
	        get
	        {
		        var mutations = GetCurrentMutations();
		        return EntityId + "?" + string.Join(
			               "&", 
			               mutations.Keys
				               .Where(m => mutations[m] != 0)
				               .OrderBy(x => x)
				               .Select(m => $"{m}={mutations[m]}"));
	        }
        }

        virtual public bool Unique
        {
            get
            {
                if (_element == null)
                    return false;
                return _element.Unique;
            }
        }

        virtual public string UniquenessGroup
        {
            get { return _element == null ? null : _element.UniquenessGroup; }
        }

        virtual public bool Decays
		{
            get { return _element.Decays; }
        }

        virtual public int Quantity {
            get { return Defunct ? 0 : _quantity; }
        }

        virtual public Vector2? LastTablePos
		{
            get { return lastTablePos; }
			set { lastTablePos = value; }
        }

        virtual public bool MarkedForConsumption { get; set; }

        virtual public IlluminateLibrarian IlluminateLibrarian
        {
            get { return _illuminateLibrarian; }
            set { _illuminateLibrarian = value; }
        }


        protected override void Awake()
		{
            base.Awake();
        }

        protected void OnDisable()
		{
            // this resets any animation frames so we don't get stuck when deactivating mid-anim
            _manifestation.ResetAnimations();

        }

        
        public void SetQuantity(int quantity, Context context)
		{
			_quantity = quantity;
			if (quantity <= 0)
			{
			    if (context.actionSource == Context.ActionSource.Purge)
			        Retire(RetirementVFX.CardLight);
                else
				    Retire(RetirementVFX.CardBurn);
                return;
			}

			if (quantity > 1 && (Unique || !string.IsNullOrEmpty(UniquenessGroup)))
			{
				_quantity = 1;
			}
			_aspectsDirtyInc = true;
            if(!TokenContainer.ContentsHidden)
			    _manifestation.UpdateText(_element,quantity);

            TokenContainer.NotifyStacksChanged();
        }

        public void ModifyQuantity(int change,Context context) {
            SetQuantity(_quantity + change, context);
        }



        virtual public Dictionary<string, int> GetCurrentMutations()
        {
            return new Dictionary<string, int>(_currentMutations);
        }
        virtual public Dictionary<string, string> GetCurrentIlluminations()
        {
            return IlluminateLibrarian.GetCurrentIlluminations();
        }

        virtual public void SetMutation(string aspectId, int value,bool additive)
        {
            if (_currentMutations.ContainsKey(aspectId))
            {
                if (additive)
                    _currentMutations[aspectId] += value;
                else
					_currentMutations[aspectId] = value;

                if (_currentMutations[aspectId] == 0)
                    _currentMutations.Remove(aspectId);
            }
            else if (value != 0)
			{
				_currentMutations.Add(aspectId,value);
			}
			_aspectsDirtyExc = true;
			_aspectsDirtyInc = true;
        }




        virtual public Dictionary<string, List<MorphDetails>> GetXTriggers() {
            return _element.XTriggers;
        }

        virtual public IAspectsDictionary GetAspects(bool includeSelf = true)
        {
            //if we've somehow failed to populate an element, return empty aspects, just to exception-proof ourselves
    
            
            var tabletop = Registry.Get<TabletopManager>(false) as TabletopManager;

            if (_element == null || tabletop==null)
                return new AspectsDictionary();
            
			if (!tabletop._enableAspectCaching)
			{
				_aspectsDirtyInc = true;
				_aspectsDirtyExc = true;
			}

			if (includeSelf)
			{
				if (_aspectsDirtyInc)
				{
					if (_aspectsDictionaryInc==null)
						_aspectsDictionaryInc=new AspectsDictionary();
					else
						_aspectsDictionaryInc.Clear();	// constructor is expensive

					_aspectsDictionaryInc.CombineAspects(_element.AspectsIncludingSelf);
					_aspectsDictionaryInc[_element.Id] = _aspectsDictionaryInc[_element.Id] * Quantity; //This might be a stack. In this case, we always want to return the multiple of the aspect of the element itself (only).

					_aspectsDictionaryInc.ApplyMutations(_currentMutations);

					if (tabletop._enableAspectCaching)
						_aspectsDirtyInc = false;

					tabletop.NotifyAspectsDirty();
				}
				return _aspectsDictionaryInc;
			}
			else
			{
				if (_aspectsDirtyExc)
				{
					if (_aspectsDictionaryExc==null)
						_aspectsDictionaryExc=new AspectsDictionary();
					else
						_aspectsDictionaryExc.Clear();	// constructor is expensive

					_aspectsDictionaryExc.CombineAspects(_element.Aspects);

					_aspectsDictionaryExc.ApplyMutations(_currentMutations);

					if (tabletop._enableAspectCaching)
						_aspectsDirtyExc = false;

					tabletop.NotifyAspectsDirty();
				}
				return _aspectsDictionaryExc;
			}
        }

        virtual public List<SlotSpecification> GetChildSlotSpecificationsForVerb(string forVerb) {
            return _element.Slots.Where(cs=>cs.ActionId==forVerb || cs.ActionId==string.Empty).ToList();
        }

        virtual public bool HasChildSlotsForVerb(string verb) {
            return _element.HasChildSlotsForVerb(verb);
        }

        private void InitialiseIfStackIsNew()
        {
            //these things should only be initialised if we've just created the stack
            //if we're repopulating, they'll already exist

            if (_currentMutations == null)
                _currentMutations = new Dictionary<string, int>();
            if (_illuminateLibrarian == null)
                _illuminateLibrarian = new IlluminateLibrarian();
            
            //add any observers that we can find in the context
            var debugTools = Registry.Get<DebugTools>(false);
            if (debugTools != null)
                AddObserver(debugTools);

            _manifestation = TokenContainer.CreateElementManifestation(this);
        }


        /// <summary>
        /// This is uses both for population and for repopulation - eg when an xtrigger transforms a stack
        /// Note that it (intentionally) resets the timer.
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="quantity"></param>
        /// <param name="source"></param>
        public void Populate(string elementId, int quantity, Source source)
		{
            
            _element = Registry.Get<ICompendium>().GetEntityById<Element>(elementId);
            if (_element==null)
			{
				NoonUtility.Log("Trying to create nonexistent element! - '" + elementId + "'");
                this.Retire();
                return;
            }

            InitialiseIfStackIsNew();


            try
            {
                SetQuantity(quantity, new Context(Context.ActionSource.Unknown)); // this also toggles badge visibility through second call

                _manifestation.InitialiseVisuals(_element);
                _manifestation.UpdateText(_element,quantity);
                LifetimeRemaining = _element.Lifetime;
                _manifestation.UpdateDecayVisuals(LifetimeRemaining, _element,0,_currentlyBeingDragged);
           
            _manifestation.Unhighlight(HighlightType.CanMerge);
            _manifestation.Unhighlight(HighlightType.CanFitSlot);


                PlacementAlreadyChronicled = false; //element has changed, so we want to relog placement
                MarkedForConsumption = false; //If a stack has just been transformed into another element, all sins are forgiven. It won't be consumed.
			
				_aspectsDirtyExc = true;
				_aspectsDirtyInc = true;

                StackSource = source;

            }
            catch (Exception e)
            {
                NoonUtility.Log("Couldn't create element with ID " + elementId + " - " + e.Message + "(This might be an element that no longer exists being referenced in a save file?)");
                Retire(RetirementVFX.None);
            }
        }








        public override void ReturnToTabletop(Context context)
		{
			bool stackBothSides = true;

            //if we have an origin stack and the origin stack is on the tabletop, merge it with that.
            //We might have changed the element that a stack is associated with... so check we can still merge it
            if (originStack != null && originStack.IsOnTabletop() && CanMergeWith(originStack))
			{
                originStack.MergeIntoStack(this);
                return;
            }
            else
			{
                var tabletop = Registry.Get<TabletopManager>()._tabletop;
                var existingStacks = tabletop.GetStacks();

				if (!_element.Unique)            // If we're not unique, auto-merge us!
				{
					//check if there's an existing stack of that type to merge with
					foreach (var stack in existingStacks)
					{
						if (CanMergeWith(stack))
						{
							var elementStack = stack as ElementStackToken;
							elementStack.MergeIntoStack(this);
							return;
						}
					}
				}
			
				if (lastTablePos == null)	// If we've never been on the tabletop, use the drop zone
				{
				// If we get here we have a new card that won't stack with anything else. Place it in the "in-tray"
					lastTablePos = GetDropZoneSpawnPos();
					stackBothSides = false;
                }
			}

            Registry.Get<Choreographer>().ArrangeTokenOnTable(this, context, lastTablePos, false, stackBothSides);	// Never push other cards aside - CP
        }

		public Vector2 GetDropZoneSpawnPos()
		{
			var tabletop = Registry.Get<TabletopManager>()._tabletop;
			var existingStacks = tabletop.GetStacks();

			Vector2 spawnPos = Vector2.zero;
			AbstractToken	dropZoneObject = null;
			Vector3			dropZoneOffset = new Vector3(0f,0f,0f);

			foreach (var stack in existingStacks)
			{
				AbstractToken tok = stack as AbstractToken;
				if (tok!=null && tok.EntityId == "dropzone")
				{
					dropZoneObject = tok;
					break;
				}
			}
			if (dropZoneObject == null)
			{
				dropZoneObject = CreateDropZone();		// Create drop zone now and add to stacks
			}

			if (dropZoneObject != null)	// Position card near dropzone
			{
				spawnPos = Registry.Get<Choreographer>().GetTablePosForWorldPos(dropZoneObject.transform.position + dropZoneOffset);
			}
			
			return spawnPos;	
		}

		private AbstractToken CreateDropZone()
		{
			var tabletop = Registry.Get<TabletopManager>() as TabletopManager;

            var dropZone = tabletop._tabletop.ProvisionElementStack("dropzone", 1, Source.Fresh(),
                new Context(Context.ActionSource.Loading)) as ElementStackToken;

            dropZone.Populate("dropzone", 1, Source.Fresh());

            // Accepting stack will trigger overlap checks, so make sure we're not in the default pos but where we want to be.
            dropZone.transform.position = Vector3.zero;
            
            dropZone.transform.localScale = Vector3.one;
            return dropZone as AbstractToken;
		}



        private bool IsOnTabletop() {
            return transform.parent.GetComponent<TabletopTokenContainer>() != null;
        }

        public void MergeIntoStack(ElementStackToken stackMergedIntoThisOne) {
            SetQuantity(Quantity + stackMergedIntoThisOne.Quantity,new Context(Context.ActionSource.Merge));
            stackMergedIntoThisOne.Retire(RetirementVFX.None);

            _manifestation.Highlight(HighlightType.AttentionPls);
        }



        // Called from TokenContainer, usually after StacksManager told it to
        public override void SetTokenContainer(TokenContainer newTokenContainer, Context context) {
            OldTokenContainer = TokenContainer;

            if (OldTokenContainer != null && OldTokenContainer != newTokenContainer)
            {
                OldTokenContainer.OnStackRemoved(this, context);
                if(OldTokenContainer.ContentsHidden && !newTokenContainer.ContentsHidden)
                 _manifestation.UpdateText(_element,Quantity);
            }

            TokenContainer = newTokenContainer;

        }

        protected override void NotifyChroniclerPlacedOnTabletop()
        {
            Registry.Get<Chronicler>()?.TokenPlacedOnTabletop(this);
        }

        public override bool Retire()
        {
            return Retire(RetirementVFX.CardBurn);
        }


        
        public override bool Retire(RetirementVFX vfxName)
        {

            if (Defunct)
                return false;

            var hlc = Registry.Get<HighlightLocationsController>(false);
            if (hlc != null)
                hlc.DeactivateMatchingHighlightLocation(_element?.Id);

            var tabletop = Registry.Get<TabletopManager>(false);
            if (tabletop != null)
                tabletop.NotifyAspectsDirty();  // Notify tabletop that aspects will need recompiling

            SetTokenContainer(Registry.Get<NullContainer>(), new Context(Context.ActionSource.Retire)); // notify the view container that we're no longer here

            //now take care of the Unity side of things.

            Defunct = true;
            FinishDrag(); // Make sure we have the drag aborted in case we're retiring mid-drag (merging stack frex)

            //we really want to pass down the VFX; and we ideally wouldn't pass down the canvasgroup, os that's ugly.
            //we also want to make sure that we don't do anything else with the manifestation once we retire it
            //down in Manifestation, OnAnimDone from the instantiated effect destroys the game object.

            var manifestationToRetire = _manifestation;
            _manifestation=new NullElementManifestation();

            Destroy(this.gameObject);

            return manifestationToRetire.Retire(vfxName);

        }



        protected override bool AllowsDrag()
        {
            return !shrouded && !_manifestation.RequestingNoDrag;
        }

        protected override bool ShouldShowHoverGlow() {
            // interaction is always possible on facedown cards to turn them back up
            return shrouded || base.ShouldShowHoverGlow();
        }

        virtual public bool AllowsIncomingMerge() {
            if (Decays || _element.Unique || IsBeingAnimated || IsInAir || TokenContainer.GetType()==typeof(RecipeSlot))
                return false;
            else
                return TokenContainer.AllowStackMerge;
        }

        virtual public bool AllowsOutgoingMerge() {
            if (Decays || _element.Unique || IsBeingAnimated)
                return false;
            else
                return true;	// If outgoing, it doesn't matter what its current container is - CP
        }
        

		private void SendStackToNearestValidSlot()
		{
			if (TabletopManager.IsInMansus())	// Prevent SendTo while in Mansus
				return;
            var tabletopTokenContainer = TokenContainer as TabletopTokenContainer;

            if(tabletopTokenContainer==null)
				return;


            Dictionary<TokenContainer, Situation> candidateThresholds = new Dictionary<TokenContainer, Situation>();
			var registeredSituations = Registry.Get<SituationsCatalogue>().GetRegisteredSituations();
			foreach(Situation situation in registeredSituations)
            {
                try
                {

                var candidateThreshold=situation.GetFirstAvailableThresholdForStackPush(this);
                if(candidateThreshold!=null)
                    candidateThresholds.Add(candidateThreshold,situation);
                }
                catch (Exception e)
                {
                    NoonUtility.LogWarning("Problem adding a candidate threshold to list of valid thresholds - does a valid threshold belong to more than one situation? - "  + e.Message);
                }
            }

            if (candidateThresholds.Any())
            {
                TokenContainer selectedCandidate=null;
                float selectedSlotDist = float.MaxValue;

                foreach (TokenContainer candidate in candidateThresholds.Keys)
                {
                    Vector3 distance = candidateThresholds[candidate].GetAnchorLocation().Position - transform.position;
                    //Debug.Log("Dist to " + tokenpair.Token.EntityId + " = " + dist.magnitude );
                    if (!candidate.CurrentlyBlockedFor(BlockDirection.Inward))
                        distance = Vector3.zero;    // Prioritise open windows above all else
                    if (distance.sqrMagnitude < selectedSlotDist)
                    {
                        selectedSlotDist = distance.sqrMagnitude;
                        selectedCandidate = candidate;
                    }
                }

                if (selectedCandidate != null)
                {

                    var candidateAnchorLocation = candidateThresholds[selectedCandidate].GetAnchorLocation();
                    SplitAllButNCardsToNewStack(1, new Context(Context.ActionSource.DoubleClickSend));
                    tabletopTokenContainer.SendViaContainer.PrepareElementForSendAnim(this, candidateAnchorLocation); // this reparents the card so it can animate properly
                    tabletopTokenContainer.SendViaContainer.MoveElementToSituationSlot(this,candidateAnchorLocation, selectedCandidate, SEND_STACK_TO_SLOT_DURATION);

                }
            }

			// Now find the best target from that list
		

			
		}

        public override void HighlightPotentialInteractionWithToken(bool show)
        {
            if(show)
                _manifestation.Highlight(HighlightType.CanInteractWithOtherToken);
            else
                _manifestation.Unhighlight(HighlightType.CanInteractWithOtherToken);

        }

        public override void OnPointerEnter(PointerEventData eventData)
		{
            foreach (var o in observers)
            {
                o.OnStackPointerEntered(this, eventData);
            }
            _manifestation.Highlight(HighlightType.Hover);
            
			var tabletopManager = Registry.Get<TabletopManager>(false);
            if(tabletopManager!=null ) //eg we might have a face down card on the credits page - in the longer term, of course, this should get interfaced
            {
                if (!shrouded)
                    tabletopManager.SetHighlightedElement(EntityId, Quantity);
                else
                    tabletopManager.SetHighlightedElement(null);

                if (!eventData.dragging)
                { 
                    //Display any HighlightLocations tagged for this element, unless we're currently dragging something else
                    var hlc = Registry.Get<HighlightLocationsController>();
                    hlc.ActivateOnlyMatchingHighlightLocation(_element.Id);
                }
            }
        }

		public override void OnPointerExit(PointerEventData eventData)
		{
            foreach (var o in observers)
            {
                o.OnStackPointerExited(this, eventData);
            }

            _manifestation.Unhighlight(HighlightType.Hover);

            var ttm = Registry.Get<TabletopManager>(false);
                if(ttm!=null)
                {
                Registry.Get<TabletopManager>().SetHighlightedElement(null);

                    //No longer display any HighlightLocations tagged for this element
                    if(!_currentlyBeingDragged)
                    { 
                        var hlc = Registry.Get<HighlightLocationsController>();
                        hlc.DeactivateMatchingHighlightLocation(_element.Id);
                    }
                }
        }

        public override void AnimateTo(float duration, Vector3 startPos, Vector3 endPos, Action<AbstractToken> animDone, float startScale, float endScale)
        {
            var tokenAnim = gameObject.AddComponent<TokenAnimation>();
            tokenAnim.onAnimDone += animDone;
            tokenAnim.SetPositions(startPos, endPos);
            tokenAnim.SetScaling(startScale, endScale);
            tokenAnim.StartAnim(duration);

        }

        public override void ReactToDraggedToken(TokenInteractionEventArgs args)
        {
            if(args.TokenInteractionType==TokenInteractionType.BeginDrag)
            {
                ElementStackToken stack = args.Token as ElementStackToken;
                ;
                if (Defunct || stack == null)
                    return;

                
                if (stack.CanMergeWith(this))
                {
                    _manifestation.Highlight(HighlightType.CanMerge);
                   
                }
            }
            else if (args.TokenInteractionType == TokenInteractionType.EndDrag)
            {
               _manifestation.Unhighlight(HighlightType.CanMerge);
            }
            
        }



        public override void OnPointerClick(PointerEventData eventData)
        {
    
            if (eventData.clickCount > 1)
			{
				// Double-click, so abort any pending single-clicks
				singleClickPending = false;
                foreach (var o in observers)
                {
                    o.OnStackDoubleClicked(this, eventData, this._element);
                }


                SendStackToNearestValidSlot();
			}
			else
			{
				// Single-click BUT might be first half of a double-click
				// Most of these functions are OK to fire instantly - just the ShowCardDetails we want to wait and confirm it's not a double
				singleClickPending = true;

    
                if (shrouded)
				{
                    Unshroud(false);

                    if (onTurnFaceUp != null)
                        onTurnFaceUp(this);

                }
				else
				{
                    foreach (var o in observers)
                    {
                        o.OnStackClicked(this, eventData, this._element);
                    }
                }

				// this moves the clicked sibling on top of any other nearby cards.
				if (TokenContainer.GetType() != typeof(RecipeSlot) && TokenContainer.GetType()!=typeof(ExhibitCards) )
					transform.SetAsLastSibling();
			}
        }

        public override void OnDrop(PointerEventData eventData) {
            foreach (var o in observers)
            {
                o.OnStackDropped(this, eventData);
            }

            InteractWithTokenDroppedOn(eventData.pointerDrag);

        }



        public bool CanMergeWith(ElementStackToken intoStack)
		{
            return	intoStack.EntityId == this.EntityId &&
					(intoStack as ElementStackToken) != this &&
					intoStack.AllowsIncomingMerge() &&
					this.AllowsOutgoingMerge() &&
					intoStack.GetCurrentMutations().IsEquivalentTo(GetCurrentMutations());
        }

        public override bool CanInteractWithTokenDroppedOn(ElementStackToken stackDroppedOn)
        {
            //element dropped on element
            return CanMergeWith(stackDroppedOn);
        }

        public override void InteractWithTokenDroppedOn(ElementStackToken stackDroppedOn) {
            //element dropped on element
            if (CanInteractWithTokenDroppedOn(stackDroppedOn)) {
                stackDroppedOn.SetQuantity(stackDroppedOn.Quantity + this.Quantity,new Context(Context.ActionSource.Unknown));
                SetXNess(TokenXNess.MergedIntoStack);
                SoundManager.PlaySfx("CardPutOnStack");

                
                Retire(RetirementVFX.None);
            }
            else {
                ShowNoMergeMessage(stackDroppedOn);

                var droppedOnToken = stackDroppedOn as AbstractToken;
                bool moveAsideFor = false;
                droppedOnToken.TokenContainer.TryMoveAsideFor(this, droppedOnToken, out moveAsideFor);

                if (moveAsideFor)
                    SetXNess(TokenXNess.DroppedOnTokenWhichMovedAside);
            }
        }

        public override bool CanInteractWithTokenDroppedOn(VerbAnchor tokenDroppedOn)
        {
            //verb dropped on element - FIXED
            return false; // a verb anchor can't be dropped on anything
        }

        public override void InteractWithTokenDroppedOn(VerbAnchor tokenDroppedOn)
        {
            //Verb dropped on element - FIXED
            
            this.TokenContainer.TryMoveAsideFor(this,tokenDroppedOn, out bool  moveAsideFor);

            if (moveAsideFor)
                SetXNess(TokenXNess.DroppedOnTokenWhichMovedAside);
            else
                SetXNess(TokenXNess.DroppedOnTokenWhichWontMoveAside);
        }



        void ShowNoMergeMessage(ElementStackToken stackDroppedOn) {
            if (stackDroppedOn.EntityId != this.EntityId)
                return; // We're dropping on a different element? No message needed.

            if (stackDroppedOn.Decays)
			{
                Registry.Get<Notifier>().ShowNotificationWindow(Registry.Get<ILocStringProvider>().Get("UI_CANTMERGE"), Registry.Get<ILocStringProvider>().Get("UI_DECAYS"), false);
            }
        }

        public ElementStackToken SplitAllButNCardsToNewStack(int n, Context context) {
            if (Quantity > n)
            {
                var cardLeftBehind =
                    TokenContainer.ProvisionElementStack(EntityId, Quantity - n, Source.Existing(), context) as ElementStackToken;
                foreach (var m in GetCurrentMutations())
	                cardLeftBehind.SetMutation(m.Key, m.Value, false); //brand new mutation, never needs to be additive

                originStack = cardLeftBehind;

                //goes weird when we pick things up from a slot. Do we need to refactor to Accept/Gateway in order to fix?
                SetQuantity(n,context);

                // Accepting stack will trigger overlap checks, so make sure we're not in the default pos but where we want to be.
                cardLeftBehind.transform.position = transform.position;
                
                // Accepting stack may put it to pos Vector3.zero, so this is last
                cardLeftBehind.transform.position = transform.position;
                return cardLeftBehind;
            }

            return null;
        }

        protected override void StartDrag(PointerEventData eventData)
        {
            _currentlyBeingDragged = true;

            IsInAir = true; // This makes sure we don't consider it when checking for overlap
 _manifestation.OnBeginDragVisuals();

            if (!Keyboard.current.shiftKey.wasPressedThisFrame)
			{
                SplitAllButNCardsToNewStack(1, new Context(Context.ActionSource.PlayerDrag));
			}
            base.StartDrag(eventData); // To ensure all events fire at the end
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);

        }
      


        public void Decay(float interval) {
            //passing a negative interval overrides and ensures it'll always decay
            if (!Decays && interval>=0)
			    return;
            

			var stackAnim = this.gameObject.GetComponent<TokenAnimationToSlot>();
			if (stackAnim)
			{
				return;	// Do not decay while being dragged into greedy slot (#1335) - CP
			}


            LifetimeRemaining = LifetimeRemaining - interval;

            if (LifetimeRemaining <= 0 || interval<0) {
                // We're dragging this thing? Then return it?
                if (_currentlyBeingDragged) {
                    // Set our table pos based on our current world pos
                    lastTablePos = Registry.Get<Choreographer>().GetTablePosForWorldPos(transform.position);
                    // Then cancel our drag, which will return us to our new pos
                    FinishDrag();
                }

                // If we DecayTo, then do that. Otherwise straight up retire the card
                if (string.IsNullOrEmpty(_element.DecayTo))
                    Retire(RetirementVFX.CardBurn);
                else
                    DecayTo(_element.DecayTo);
            }

            if (shrouded)
                Unshroud(true); //never leave a decaying card face down.


		    _manifestation.UpdateDecayVisuals(LifetimeRemaining,_element,interval,_currentlyBeingDragged );

          if (onDecay != null)
                onDecay(LifetimeRemaining);
        }

        
        


        public bool DecayTo(string elementId)
		{
            // Save this, since we're retiring and that sets quantity to 0
            int quantity = Quantity;

            try
            {

                var cardLeftBehind= TokenContainer.ProvisionElementStack(elementId, quantity, Source.Existing(),
                    new Context(Context.ActionSource.ChangeTo)) as ElementStackToken;

                foreach(var m in this.GetCurrentMutations())
                    cardLeftBehind.SetMutation(m.Key,m.Value,false); //brand new mutation, never needs to be additive
                cardLeftBehind.lastTablePos = lastTablePos;
                cardLeftBehind.originStack = null;

                // Accepting stack will trigger overlap checks, so make sure we're not in the default pos but where we want to be.
                cardLeftBehind.transform.position = transform.position;

                // Put it behind the card being burned
                cardLeftBehind.transform.SetSiblingIndex(transform.GetSiblingIndex() - 1);

                // Accepting stack may put it to pos Vector3.zero, so this is last
                cardLeftBehind.transform.position = transform.position;

                Retire(RetirementVFX.CardTransformWhite);
            }
            catch (Exception e)
            {
                NoonUtility.Log($"Something bad happened when trying to turn the {EntityId} card on the desktop into a {elementId} card: " + e.Message,1,VerbosityLevel.Essential);
                return false;
            }

            return true;
        }

        public void Understate()
        {
            canvasGroup.alpha = 0.3f;
        }

        public void Emphasise()
        {
            canvasGroup.alpha = 1f;
        }


        public void Unshroud(bool instant = false)
        {
            shrouded = false;
            _manifestation.DoRevealEffect(instant);

            //if a card has just been turned face up in a situation, it's now an existing, established card
            if (StackSource.SourceType == SourceType.Fresh)
                StackSource = Source.Existing();

        }

        public void Shroud(bool instant = false) {
            shrouded = true;
            _manifestation.DoShroudEffect(instant);

        
        }

        public bool Shrouded() {
            return shrouded;
        }



        public override bool CanAnimateArt()
        {
            if (gameObject == null)
                return false;

            if (gameObject.activeInHierarchy == false)
                return false; // can not animate if deactivated

            if (_element == null)
                return false;

            return _manifestation.CanAnimate();
        }


        public override void StartArtAnimation() {
            if (!CanAnimateArt())
                return;
            _manifestation.BeginArtAnimation(_element.Icon);
           
        }


        public override void MoveObject(PointerEventData eventData) {
            Vector3 dragPos;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(Registry.Get<IDraggableHolder>().RectTransform, eventData.position, eventData.pressEventCamera, out dragPos);

            // Potentially change this so it is using UI coords and the RectTransform?
            rectTransform.position = new Vector3(dragPos.x + dragOffset.x, dragPos.y + dragOffset.y, dragPos.z + dragHeight);

            _manifestation.DoMove(rectTransform);

            // rotate object slightly based on pointer Delta
            if (rotateOnDrag && eventData.delta.sqrMagnitude > 10f) {
                // This needs some tweaking so that it feels more responsive, physica. Card rotates into the direction you swing it?
                perlinRotationPoint += eventData.delta.sqrMagnitude * 0.001f;
                transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -10 + Mathf.PerlinNoise(perlinRotationPoint, 0) * 20));
            }
        }
    }
}
