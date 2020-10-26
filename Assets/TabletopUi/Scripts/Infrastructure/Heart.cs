﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Core.Commands;
using Assets.Core.Entities;
using Assets.CS.TabletopUI;
using Assets.TabletopUi;
using Assets.TabletopUi.Scripts.Infrastructure;
using Assets.TabletopUi.Scripts.Services;
using Noon;


/// <summary>
/// Top-level object
/// </summary>
public class Heart : MonoBehaviour
{
    [SerializeField] private Transform allContent;
    private HashSet<AnchorAndSlot> outstandingSlotsToFill=new HashSet<AnchorAndSlot>();
    private int beatCounter = 0;
    //do major housekeeping every n beats
    private const int HOUSEKEEPING_CYCLE_BEATS = 20; //usually, a second
	// Autosave tracking is now done in TabletopManager.Update()
    private const float BEAT_INTERVAL_SECONDS = 0.05f;

    private const string METHODNAME_BEAT="Beat"; //so we don't get a tiny daft typo with the Invoke
    private GameSpeedState gameSpeedState=new GameSpeedState();

    private float timerBetweenBeats=0f;

    public void Update()
    {
        try
        {
            timerBetweenBeats += Time.deltaTime;

        if (timerBetweenBeats > BEAT_INTERVAL_SECONDS)
        {
            timerBetweenBeats -= BEAT_INTERVAL_SECONDS;
            if (gameSpeedState.GetEffectiveGameSpeed() == GameSpeed.Fast)
                Beat(BEAT_INTERVAL_SECONDS * 3);
            else if (gameSpeedState.GetEffectiveGameSpeed()==GameSpeed.Normal)
                Beat(BEAT_INTERVAL_SECONDS);
            else if (gameSpeedState.GetEffectiveGameSpeed() == GameSpeed.Paused)
                Beat(0f);
            else
               NoonUtility.Log("Unknown game speed state: " + gameSpeedState.GetEffectiveGameSpeed());
        }
        }
        catch (Exception e)
        {
            NoonUtility.LogException(e);
            throw;
        }

    }

    public void FastForward(float interval)
    {
        Beat(interval);
    }


    public void RespondToSpeedControlCommand(SpeedControlEventArgs args)
    {
        gameSpeedState.SetGameSpeedCommand(args.ControlPriorityLevel,args.GameSpeed);

        if (gameSpeedState.GetEffectiveGameSpeed() == GameSpeed.Paused)
            StopBeating();
        else
           StartBeating();

    }


    public void StopBeating()
    {
        //    CancelInvoke(METHODNAME_BEAT);
        //do nothing, these days, actually.
    }


    public void StartBeating()
	{
        //CancelInvoke(METHODNAME_BEAT);
        //InvokeRepeating(METHODNAME_BEAT,0, BEAT_INTERVAL_SECONDS);
        beatCounter = HOUSEKEEPING_CYCLE_BEATS; // Force immediate housekeeping check on resume - CP

    } 

    
    public void Beat(float beatInterval)
    {
  

   beatCounter++;


        DetermineOutstandingSlots(beatInterval);
        DecayStacksOnTable(beatInterval);
        DecayStacksInResults(beatInterval);
        
        if (beatCounter >= HOUSEKEEPING_CYCLE_BEATS)
        {
            beatCounter = 0;
            TryToFillOutstandingSlots();
        }

    }

    public void DecayStacksOnTable(float beatInterval)
    {
        var tabletopManager = Registry.Get<TabletopManager>();

        tabletopManager.DecayStacksOnTable(beatInterval);

    }

    public void DecayStacksInResults(float beatInterval)
    {
        var tabletopManager = Registry.Get<TabletopManager>();

        tabletopManager.DecayStacksInResults(beatInterval);

    }

    private void DetermineOutstandingSlots(float beatInterval)
    {
        var situationControllers = Registry.Get<SituationsCatalogue>().GetRegisteredSituations();

        foreach (var sc in situationControllers)
        {
            HeartbeatResponse response = sc.ExecuteHeartbeat(beatInterval);

            foreach (var tokenAndSlot in response.SlotsToFill)
            {
                if (!OutstandingSlotAlreadySaved(tokenAndSlot))
                    outstandingSlotsToFill.Add(tokenAndSlot);
            }
        }
    }

    private void TryToFillOutstandingSlots()
    {
        outstandingSlotsToFill = Registry.Get<TabletopManager>()
            .FillTheseSlotsWithFreeStacks(outstandingSlotsToFill);
    }


    public async void  OnApplicationQuit()
    {
        var saveTask = Registry.Get<TabletopManager>().SaveGameAsync(true,SourceForGameState.DefaultSave);
        await saveTask;
    }


    bool OutstandingSlotAlreadySaved(AnchorAndSlot slot) {
        foreach (var item in outstandingSlotsToFill)
            if (item.Token == slot.Token && item.Threshold == slot.Threshold)
                return true;

        return false;
    }

    //remove any outstanding state when loading the game
    public void Clear()
    {
        outstandingSlotsToFill.Clear();
    }
}
