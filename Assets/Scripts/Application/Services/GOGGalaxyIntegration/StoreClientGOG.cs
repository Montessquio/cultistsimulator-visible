﻿

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Galaxy.Api;

using SecretHistories.Enums;

namespace SecretHistories.Constants
{
    public class GOGStorefrontProvider : IStoreFrontClientProvider
    {
        public GOGStorefrontProvider()
        {
            Galaxy.Api.IAuthListener authListener = new GogAuthListener();

            Galaxy.Api.GalaxyInstance.ListenerRegistrar()
                .Register(Galaxy.Api.GalaxyTypeAwareListenerAuth.GetListenerType(), authListener);

            try
            {

                if(!GalaxyInstance.User().SignedIn())
                    GalaxyInstance.User().SignInGalaxy();
            }
            catch (GalaxyInstance.InvalidStateError e)
            {
                NoonUtility.Log(e.Message,1);
            }

        }

        public void SetAchievement(string achievementId, bool setStatus)
        {
            var gogStats = Galaxy.Api.GalaxyInstance.Stats();
            if (!GalaxyInstance.User().SignedIn())
            {
                NoonUtility.Log("Tried to set a GOG achievement, but user wasn't signed in, so we couldn't");
                return;
            }
                

            Galaxy.Api.IUserStatsAndAchievementsRetrieveListener statsRetrieveListener = new AchievementRequest(achievementId, setStatus, gogStats);
            Galaxy.Api.IStatsAndAchievementsStoreListener statsStoreListener = new GogStatsAndAchievementsStoreListener();
            Galaxy.Api.GalaxyInstance.ListenerRegistrar().Register(Galaxy.Api.GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve.GetListenerType(), statsRetrieveListener);
            Galaxy.Api.GalaxyInstance.ListenerRegistrar().Register(Galaxy.Api.GalaxyTypeAwareListenerStatsAndAchievementsStore.GetListenerType(), statsStoreListener);
            NoonUtility.Log($"Trying to set GOG achievement {achievementId} as {setStatus}",1,VerbosityLevel.Essential);

            gogStats.RequestUserStatsAndAchievements(); //when the request completes, the callback will fire Execute on the AchievementRequest we attached above

        }

        public Storefront Storefront
        {
            get { return Storefront.Gog; }
        }

        public void DoUpdate()
        {
            GalaxyInstance.ProcessData();
        }
    }

    


        public class GogAuthListener : IAuthListener
    {
        public override void OnAuthSuccess()
        {
            NoonUtility.Log("GOG Galaxy logged on?" + GalaxyInstance.User().IsLoggedOn());
            NoonUtility.Log("GOG Galaxy signed in? " + GalaxyInstance.User().SignedIn());
        }

        public override void OnAuthFailure(FailureReason failureReason)
        {
            NoonUtility.Log("GOG Galaxy auth failed:" + failureReason, 1);
        }

        public override void OnAuthLost()
        {
            NoonUtility.Log("Authentication lost",10);
        }
    }

    public class AchievementRequest : IUserStatsAndAchievementsRetrieveListener

    {
        private string _forAchievementId;
        private bool _setStatus;
        private IStats _gogStats;


        public AchievementRequest(string achievementId, bool setStatus, IStats gogStats)
        {
            if (string.IsNullOrEmpty(achievementId))
                throw new ApplicationException("Whoa there, don't try to pass an empty achievementid to a GOG achievement request");
            if (gogStats == null)
                throw new ApplicationException("Whoa there, we need an actual stats object in our GOG achievement request");

            _forAchievementId = achievementId;
            _setStatus = setStatus;
            _gogStats = gogStats;

        }

        private void Execute()
        {
            try { 
            if (_setStatus)
                _gogStats.SetAchievement(_forAchievementId);
            else
                _gogStats.ClearAchievement(_forAchievementId);

                _gogStats.StoreStatsAndAchievements();
            NoonUtility.Log("Set GOG achievement: " + _forAchievementId + " (" + _setStatus + ")", 1);

            }
            catch(GalaxyInstance.InvalidArgumentError e)
            {
                NoonUtility.Log(e.Message,1);
            }

            
        }

        public override void OnUserStatsAndAchievementsRetrieveSuccess(GalaxyID userID)
        {
            NoonUtility.Log("Retrieved achievements: about to set " + _forAchievementId + " as " + _setStatus, 1,VerbosityLevel.Essential);
            Execute();
        }

        public override void OnUserStatsAndAchievementsRetrieveFailure(GalaxyID userID, FailureReason failureReason)
        {
            NoonUtility.Log("Couldn't retrieve achievements: " + failureReason, 1, VerbosityLevel.Essential);
        }

    }

    //not in use!
    //public class AchievementList : IUserStatsAndAchievementsRetrieveListener

    //{
    //    // ReSharper disable once NotAccessedField.Local
    //    private IStats _gogStats;


    //    public AchievementList(IStats gogStats)
    //    {
    //        _gogStats = gogStats;
    //    }


    //    public override void OnUserStatsAndAchievementsRetrieveSuccess(GalaxyID userID)
    //    {
    //        NoonUtility.Log("Retrieved achievements: about to list them."); //or I would if there was something in the API for this

    //    }

    //    public override void OnUserStatsAndAchievementsRetrieveFailure(GalaxyID userID, FailureReason failureReason)
    //    {
    //        NoonUtility.Log("Couldn't retrieve achievements: " + failureReason,  1, VerbosityLevel.Essential);
    //    }

    //}

    public class GogStatsAndAchievementsStoreListener : IStatsAndAchievementsStoreListener
    {
        public override void OnUserStatsAndAchievementsStoreSuccess()
        {
            NoonUtility.Log("Stored achievements", 1, VerbosityLevel.Essential);

        }

        public override void OnUserStatsAndAchievementsStoreFailure(FailureReason failureReason)
        {
            NoonUtility.Log("Couldn't store achievements: " + failureReason,1,VerbosityLevel.Essential);
        }
    }

        
    }
