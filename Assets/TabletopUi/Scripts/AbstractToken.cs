#pragma warning disable 0649
using System;
using System.Collections;
using Assets.Core.Interfaces;
using Assets.Core.Services;
using Assets.CS.TabletopUI.Interfaces;
using Assets.TabletopUi;
using Assets.TabletopUi.Scripts.Infrastructure;
using Assets.TabletopUi.Scripts.Infrastructure.Events;
using Assets.TabletopUi.Scripts.Interfaces;
using Assets.TabletopUi.Scripts.TokenContainers;
using Noon;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.CS.TabletopUI {

    public interface IToken
    {
        string EntityId { get; }
        void SetTokenContainer(TokenContainer newContainer, Context context);
        string name { get; }
        Transform transform { get; }
        RectTransform RectTransform { get; }
        GameObject gameObject { get; }
        void DisplayAtTableLevel();
        void SnapToGrid();
        bool NoPush { get; }
        TokenLocation Location { get; }
    }

    public enum TokenXNess
    {
        NoValidDestination,
        ValidDestination,
        DivertedByGreedySlot,
        DoesntMatchSlotRequirements,
        DroppedOnTableContainer,
        ReturningSplitStack,
        ReturnedToStartingSlot,
        PlacedInSlot,
        ElementDroppedOnTokenButCannotInteractWithIt,
        DroppedOnTokenWhichMovedAside,
        DroppedOnTokenWhichWontMoveAside,
        MergedIntoStack
    }

        [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class AbstractToken : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,IToken,IAnimatableToken {

        // STATIC FIELDS

        // ReSharper disable once NotAccessedField.Local
        // This is used in DelayedEndDrag, which occurs one frame after EndDrag. If it's set to true, the token will be returned to where it began the drag (default is false).


        // INSTANCE FIELDS

        public RectTransform rectTransform;
        [SerializeField] protected bool rotateOnDrag = true;
        
        [HideInInspector] public Vector2? lastTablePos = null; // if it was pulled from the table, save that position

        protected Transform startParent;
        protected Vector3 startPosition;
        protected int startSiblingIndex;
        protected Vector3 dragOffset;
        protected RectTransform rectCanvas;
        protected CanvasGroup canvasGroup;

        private float perlinRotationPoint = 0f;
        private float dragHeight = -8f; // Draggables all drag on a specifc height and have a specific "default height"

        public TokenContainer TokenContainer;
        protected TokenContainer OldTokenContainer; // Used to tell OldContainsTokens that this thing was dropped successfully

        public RectTransform RectTransform
        {
            get { return rectTransform; }
        }

        public TokenLocation Location => new TokenLocation(transform.localPosition);

        protected virtual void Awake() {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public abstract void StartArtAnimation();
        
        public abstract bool CanAnimate();

        public abstract string EntityId { get; }
        public bool IsBeingAnimated { get; set; }

        public bool Defunct { get; protected set; }
        public bool IsInAir { protected set; get; }
		public virtual bool NoPush { protected set; get; }

        public bool _currentlyBeingDragged { get; protected set; }

        protected bool _draggingEnabled = true;

     private TokenXNess TokenXNess { get; set; }

            public void Start()
        {
            if(TokenContainer.GetType()!=typeof(CardsPile))
               Registry.Get<LocalNexus>().TokenInteractionEvent.AddListener(ReactToDraggedToken);

            SetXNess(TokenXNess.NoValidDestination);

        }

        public void SetXNess(TokenXNess xness)
        {
            TokenXNess = xness;
        }


        public bool ShouldReturnToStart()
        {
            return TokenXNess == TokenXNess.NoValidDestination ||
                TokenXNess==TokenXNess.DivertedByGreedySlot ||
                TokenXNess==TokenXNess.ReturningSplitStack ||
                TokenXNess == TokenXNess.ReturnedToStartingSlot ||
                TokenXNess==TokenXNess.DroppedOnTokenWhichWontMoveAside;
        }

        protected virtual bool AllowsDrag() {
            return !IsBeingAnimated;
        }

        protected virtual bool ShouldShowHoverGlow() {
            return !Defunct && TokenContainer != null && !IsBeingAnimated && (TokenContainer.AllowDrag || TokenContainer.AlwaysShowHoverGlow) && AllowsDrag();
        }


        /// <summary>
        /// This is an underscore-separated x, y localPosition in the current transform/containsTokens
        /// but could be anything
        /// </summary>
        public string SaveLocationInfo {
            set {
                var locs = value.Split('_');
                if (float.TryParse(locs[0], out float x) && float.TryParse(locs[1], out float y))
                {
                    rectTransform.localPosition = new Vector3(x, y);
                }
                //if not, then we specified the location as eg 'slot'

            }
            get {
                return TokenContainer.GetSaveLocationForToken(this) + "_" + Guid.NewGuid();
            }
        }



		public virtual void SnapToGrid()
		{
			transform.localPosition = Registry.Get<Choreographer>().SnapToGrid( transform.localPosition );
		}

        public virtual void SetTokenContainer(TokenContainer newContainer, Context context) {
            OldTokenContainer = TokenContainer;
            TokenContainer = newContainer;
        }

        public bool IsInContainer(TokenContainer compareContainer, Context context) {
            return compareContainer == TokenContainer;
        }


        public void OnBeginDrag(PointerEventData eventData)
        {
            if (CanDrag(eventData))
            {
                _currentlyBeingDragged = true;
                Registry.Get<LocalNexus>().SignalTokenBeginDrag(this, eventData);
                StartDrag(eventData);
            }

        }

        bool CanDrag(PointerEventData eventData) {

            if (!_draggingEnabled)
                return false;

            if (!TokenContainer.AllowDrag || !AllowsDrag())
                return false;


            return true;
        }

        protected virtual void StartDrag(PointerEventData eventData) {
            
            
            if (rectCanvas == null)
                rectCanvas = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

            _currentlyBeingDragged = true;

            TokenXNess = TokenXNess.NoValidDestination;
            canvasGroup.blocksRaycasts = false;

            DisplayInAir();

            startPosition = rectTransform.localPosition;
            startParent = rectTransform.parent;
            startSiblingIndex = rectTransform.GetSiblingIndex();

			if (rectTransform.anchoredPosition.sqrMagnitude > 0.0f)	// Never store 0,0 as that's a slot position and we never auto-return to slots - CP
			{
	            lastTablePos = rectTransform.anchoredPosition;
			}

            rectTransform.SetParent(Registry.Get<IDraggableHolder>().RectTransform);
            rectTransform.SetAsLastSibling();

            //commented out because I *might* not need it; but if I do, we can probably calculate it on the fly.
            //if (this.EntityId=="dropzone")
            //{
            //    Vector3 pressPos;
            //    RectTransformUtility.ScreenPointToWorldPointInRectangle(Registry.Get<IDraggableHolder>().RectTransform, eventData.pressPosition, eventData.pressEventCamera, out pressPos);
            //    dragOffset = (startPosition + startParent.position) - pressPos;
            //}
            //else
            //{
                dragOffset = Vector3.zero;
          //  }

            SoundManager.PlaySfx("CardPickup");
			TabletopManager.RequestNonSaveableState( TabletopManager.NonSaveableType.Drag, true );

        }

        

        public void OnDrag(PointerEventData eventData)
        {
            if (!_currentlyBeingDragged)
                return;

            if (_draggingEnabled)
			{
                MoveObject(eventData);
            }
            else
			{
                eventData.pointerDrag = null; // cancel the drag
                OnEndDrag(eventData);
            }
        }



        public void MoveObject(PointerEventData eventData) {
            Vector3 dragPos;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(Registry.Get<IDraggableHolder>().RectTransform, eventData.position, eventData.pressEventCamera, out dragPos);

            // Potentially change this so it is using UI coords and the RectTransform?
            rectTransform.position = new Vector3(dragPos.x + dragOffset.x, dragPos.y + dragOffset.y, dragPos.z + dragHeight);

            // rotate object slightly based on pointer Delta
            if (rotateOnDrag && eventData.delta.sqrMagnitude > 10f) {
                // This needs some tweaking so that it feels more responsive, physica. Card rotates into the direction you swing it?
                perlinRotationPoint += eventData.delta.sqrMagnitude * 0.001f;
                transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -10 + Mathf.PerlinNoise(perlinRotationPoint, 0) * 20));
            }
        }


        public virtual void OnEndDrag(PointerEventData eventData) {
            Registry.Get<LocalNexus>().SignalTokenEndDrag(this,eventData);
            FinishDrag();
        }


        public virtual void FinishDrag() {
            if (_currentlyBeingDragged)
            {
                _currentlyBeingDragged = false;
                canvasGroup.blocksRaycasts = true;

                if (ShouldReturnToStart())
                    ReturnToStartPosition();

                TabletopManager.RequestNonSaveableState(TabletopManager.NonSaveableType.Drag, false);   // There is also a failsafe to catch unexpected aborts of Drag state - CP
                
            }
        }

        public void ReturnToStartPosition() {
            if (startParent == null) {
                //newly created token! If we try to set it to startposition, it'll disappear into strange places
                ReturnToTabletop(new Context(Context.ActionSource.PlayerDrag));
                return; // no sound on new token
            }

            SoundManager.PlaySfx("CardDragFail");
            var tabletopContainer = startParent.GetComponent<TabletopTokenContainer>();
            
            // Token was from tabletop - return it there. This auto-merges it back in case of ElementStacks
            // The map is not the tabletop but inherits from it, so we do the IsTabletop check
            if (tabletopContainer != null && tabletopContainer.IsTabletop) {
                ReturnToTabletop(new Context(Context.ActionSource.PlayerDrag));
            }
            else {
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.SetParent(startParent);
                rectTransform.SetSiblingIndex(startSiblingIndex);
                rectTransform.localPosition = startPosition;
            }
        }

        public abstract void OnDrop(PointerEventData eventData);


        public bool CanInteractWithTokenDroppedOn(GameObject objectDroppedOn)
        {
            var token = objectDroppedOn.GetComponent<AbstractToken>();
            if (token == null)
                return false;
            return CanInteractWithTokenDroppedOn(token);
        }

        public bool CanInteractWithTokenDroppedOn(AbstractToken token) {
            var element = token as ElementStackToken;

            if (element != null)
                return CanInteractWithTokenDroppedOn(element);
            else
                return CanInteractWithTokenDroppedOn(token as VerbAnchor);
        }



        public abstract bool CanInteractWithTokenDroppedOn(VerbAnchor tokenDroppedOn);
        public abstract bool CanInteractWithTokenDroppedOn(ElementStackToken stackDroppedOn);

        public void InteractWithTokenDroppedOn(GameObject objectDroppedOn)
        {
            var token = objectDroppedOn.GetComponent<AbstractToken>();
            if (token != null)
                 InteractWithTokenDroppedOn(token);
        }
        public void InteractWithTokenDroppedOn(AbstractToken token)
        {
            var element = token as ElementStackToken;

            if (element != null)
                InteractWithTokenDroppedOn(element);
            else
                InteractWithTokenDroppedOn(token as VerbAnchor);
        }

        public abstract void InteractWithTokenDroppedOn(VerbAnchor tokenDroppedOn);
        public abstract void InteractWithTokenDroppedOn(ElementStackToken stackDroppedOn);

        public abstract void OnPointerClick(PointerEventData eventData);

        public abstract void ReturnToTabletop(Context context);

        public virtual void DisplayInAir() {
            IsInAir = true;
        }

        public virtual void DisplayAtTableLevel() {
            rectTransform.anchoredPosition3D = new Vector3(rectTransform.anchoredPosition3D.x, rectTransform.anchoredPosition3D.y, 0f);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
			lastTablePos = rectTransform.anchoredPosition3D;
            IsInAir = false;
            NotifyChroniclerPlacedOnTabletop();
        }

        protected abstract void NotifyChroniclerPlacedOnTabletop();

        public virtual bool Retire() {
            Destroy(gameObject);
            Defunct = true;
            return true;
        }



        public abstract void ReactToDraggedToken(TokenInteractionEventArgs args);

        public abstract void HighlightPotentialInteractionWithToken(bool show);



        public abstract void OnPointerEnter(PointerEventData eventData);

        public abstract void OnPointerExit(PointerEventData eventData);

        public void BurnImageUnderToken(string burnImage)
        {
            Registry.Get<INotifier>()
                .ShowImageBurn(burnImage, this, 20f, 2f,
                    TabletopImageBurner.ImageLayoutConfig.CenterOnToken);
        }

    }
}