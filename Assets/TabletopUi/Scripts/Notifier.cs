﻿#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Core;
using Assets.Core.Interfaces;
using Assets.CS.TabletopUI.Interfaces;
using Assets.TabletopUi.Scripts.Services;
using UnityEngine;
using Assets.Core.Entities;
using Assets.TabletopUi.Scripts.Infrastructure.Events;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Assets.CS.TabletopUI {



    public class Notifier : MonoBehaviour, INotifier {
        
        [Header("Notification")]
        [SerializeField] Transform notificationHolder;
        [SerializeField] NotificationLog notificationLog;
		[SerializeField] NotificationWindow saveErrorWindow;
		[SerializeField] NotificationWindow saveDeniedWindow;

        [Header("Token Details")]
        [SerializeField] TokenDetailsWindow tokenDetails;
        [SerializeField] AspectDetailsWindow aspectDetails;

        [Header("Image Burner")]
        [SerializeField] private TabletopImageBurner tabletopBurner;



        public void Start() {
            tokenDetails.gameObject.SetActive(false); // ensure this is turned off at the start
            aspectDetails.gameObject.SetActive(false);
			saveErrorWindow.gameObject.SetActive(false);
			saveDeniedWindow.gameObject.SetActive(false);

            Registry.Get<Concursum>().ShowNotificationEvent.AddListener(ShowNotificationWindow);
            
            Registry.Get<SphereCatalogue>().Subscribe(this);

        }

        // Notifications


		// Text Log Disabled
        public void PushTextToLog(string text) {
        	notificationLog.AddText(text);
        }

        public void ShowNotificationWindow(NotificationArgs args)
        {
            ShowNotificationWindow(args.Title,args.Description,args.DuplicatesAllowed);
        }

        public void ShowNotificationWindow(string title, string description, bool duplicatesAllowed = true) {
       
            float duration = (title.Length + description.Length) / 5; //average reading speed in English is c. 15 characters a second
       
            
            // If no duplicates are allowed, then find any duplicates and hide them
            if (!duplicatesAllowed)
            {
	            foreach (var window in GetDuplicateNotificationWindow(title, description))
		            window.Hide();
            }
            
            var notification = BuildNotificationWindow(duration);
            notification.SetDetails(title, description);
			notification.Show();
        }

        private NotificationWindow BuildNotificationWindow(float duration) {
            var notification = Registry.Get<PrefabFactory>().CreateLocally<NotificationWindow>(notificationHolder);
            notification.SetDuration(duration);
            return notification;
        }

        private IEnumerable<NotificationWindow> GetDuplicateNotificationWindow(string title, string description)
        {
	        return notificationHolder
		        .GetComponentsInChildren<NotificationWindow>()
		        .Where(window => window.Title == title && window.Description == description);
        }

        // Token Details

        // Variant to link to token decay
        public void ShowCardElementDetails(Element element, ElementStackToken token)
		{
			if (token.name == "Card_dropzone")	// Clunky but reliable
			{
				return;
			}
            tokenDetails.ShowElementDetails(element, token);
            aspectDetails.Hide();
        }

        public void ShowElementDetails(Element element, bool fromDetailsWindow = false) {
            if (element.IsAspect == false) {
                tokenDetails.ShowElementDetails(element);
                aspectDetails.Hide();
                return;
            }

            // The following only happens for aspects
            aspectDetails.ShowAspectDetails(element, !fromDetailsWindow);

            if (fromDetailsWindow)
                tokenDetails.ResetTimer(); // ensure the token window timer is restored
            else 
                tokenDetails.Hide(); // hide the token window
        }
        
        public void ShowSlotDetails(SlotSpecification slot, bool highlightGreedy, bool highlightConsumes) {
            tokenDetails.ShowSlotDetails(slot);
            tokenDetails.HighlightSlotIcon(highlightGreedy, highlightConsumes);
            aspectDetails.Hide();
        }

        public void ShowDeckDetails(DeckSpec deckSpec, int quantity) {
            tokenDetails.ShowDeckDetails(deckSpec, quantity);
            aspectDetails.Hide();
        }

        public void HideDetails()
		{
            tokenDetails.Hide();
            aspectDetails.Hide();
        }

		public void ShowSaveError( bool on )
		{
			if (TabletopManager.IsInMansus())
			{
				if (saveDeniedWindow == null)
					return;

				if (on)
				{
					saveDeniedWindow.Show();
				}
				else
				{
					saveDeniedWindow.HideNoDestroy();
				}
			}
			else
			{
				if (saveErrorWindow == null)
					return;

				if (on)
				{
					saveErrorWindow.Show();
				}
				else
				{
					saveErrorWindow.HideNoDestroy();
				}
			}
		}


        // TabletopImageBurner

        public void ShowImageBurn(string spriteName, Token token, float duration, float scale, TabletopImageBurner.ImageLayoutConfig alignment) {
            tabletopBurner.ShowImageBurn(spriteName, token, duration, scale, alignment);
        }

        public void NotifyStacksChangedForContainer(TokenEventArgs args)
        {
            //
        }

        public void OnTokenClicked(TokenEventArgs args)
        {
            ShowCardElementDetails(args.Element,args.Token);
        }

        public void OnTokenReceivedADrop(TokenEventArgs args)
        {
            //
        }

        public void OnTokenPointerEntered(TokenEventArgs args)
        {
            //
        }

        public void OnTokenPointerExited(TokenEventArgs args)
        {
            //
        }

        public void OnTokenDoubleClicked(TokenEventArgs args)
        {
            HideDetails();
        }

        public void OnTokenDragged(TokenEventArgs args)
        {
            //
        }
    }
}
