﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SecretHistories.Constants;
using SecretHistories.UI;
using Steamworks;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecretHistories.Spheres.Angels
{
    /// <summary>
    /// A HomingAngel is created when we drag a token from a sphere, so that the token can know to return preferentially to that token and that sphere.
    /// Not all spheres will create HomingAngels, though - some don't want to be homes and will preserve the existing home. Others might do other things, later.
    /// </summary>
    public class HomingAngel : IAngel
    {
        protected Token OriginToken;
        protected Vector3 PreferredHomingPosition;
        protected DateTime WhenHomingSet;
        protected Token TokenToBringHome;
        protected Sphere SphereToWatchOver;

        public HomingAngel(Token tokenToBringHome)
        {
            PreferredHomingPosition = tokenToBringHome.TokenRectTransform.anchoredPosition3D;
            WhenHomingSet = DateTime.Now;
            TokenToBringHome = tokenToBringHome;
        }

        public int Authority => 9;

        public void Act(float seconds, float metaseconds)
        {
            //once it's safely home, forget about its origin, mark this angel for removal
            if (TokenToBringHome.Sphere==SphereToWatchOver && TokenToBringHome.CurrentState.Docked())
                Retire();

        }

        public void SetWatch(Sphere sphere)
        {
            SphereToWatchOver = sphere;
        }

        public Sphere GetWatchedSphere()
        {
            return SphereToWatchOver;
        }

        public Token GetTokenToBringHome()
        {
            return TokenToBringHome;
        }
        public bool MinisterToDepartingToken(Token token, Context context)
        {
            return false; //if it's been purposefully removed, it's not our business.
        }

        public void SetOriginToken(Token originToken)
        {
            OriginToken = originToken;
        }

        public bool MinisterToEvictedToken(Token evictedToken, Context context)
        {
            if (evictedToken == TokenToBringHome)
            {
                if (OriginToken != null)
                    TryReturnToOriginTokenLocation(evictedToken, context);
                else 
                    TryReturnToHomeLocation(context);

                Retire();
                return true;
            }

            return false;
        }

        public void Retire()
        {
            Defunct = true;
        }


        //marked as in process of retirement
        public bool Defunct { get; protected set; }

        //marked as requesting retirement on next cycle

        public void ShowRelevantVisibleCharacteristic(List<VisibleCharacteristic> visibleCharacteristics)
        {
            //
        }

        public void HideRelevantVisibleCharacteristic(List<VisibleCharacteristic> visibleCharacteristics)
        {
            //
        }


        private void TryReturnToOriginTokenLocation(Token evictedToken, Context context)
        {
            var preferredDestination = OriginToken.TokenRectTransform.anchoredPosition3D;
            if(OriginToken.CanMergeWithToken(evictedToken))
                SendToken(TokenToBringHome, preferredDestination, context);
            else
            {
                var alternateDestination = SphereToWatchOver.Choreographer.GetClosestFreeLocalPosition(TokenToBringHome, preferredDestination);
                SendToken(TokenToBringHome, alternateDestination, context);
            }

        }

        private void TryReturnToHomeLocation(Context context)
        {
            var destination = SphereToWatchOver.Choreographer.GetClosestFreeLocalPosition(TokenToBringHome, PreferredHomingPosition);
            SendToken(TokenToBringHome,destination, context);
        }

        private void SendToken(Token token, Vector3 destination,Context context)
        {
            TokenTravelItinerary travellingHome =
                new TokenTravelItinerary(token.TokenRectTransform.anchoredPosition3D, destination)
                    .WithDestinationSpherePath(SphereToWatchOver.GetWildPath())
                    .WithDuration(NoonConstants.MOMENT_TIME_INTERVAL);

            travellingHome.Depart(token,context);
        }
    }
}
