﻿using UnityEngine;
using System.Collections;
using SecretHistories.UI;
using SecretHistories.UI.Scripts;
using SecretHistories.Constants;
using System;
using SecretHistories.Commands;
using SecretHistories.Entities;
using SecretHistories.Enums;
using SecretHistories.Fucine;
using SecretHistories.Elements;
using SecretHistories.Spheres;

[IsEmulousEncaustable(typeof(Sphere))]
public class SituationStorageSphere : Sphere,ISituationSubscriber
{
    //because this is just a sphere, I don't think it needs to be an ISituationAttachment
    public override SphereCategory SphereCategory => SphereCategory.SituationStorage;

    public override bool AllowDrag
    {
        get { return false; }
    }

    public override bool AllowStackMerge
    {
        get { return false; }
    }

    [SerializeField] private CanvasGroupFader canvasGroupFader;

    public void Initialise(Situation situation)
    { 
        situation.AddSubscriber(this);
        situation.AttachSphere(this);
    }



    public override void AcceptToken(Token token, Context context)
    {
        base.AcceptToken(token, context);
        if (context.actionSource == Context.ActionSource.SituationEffect)
            token.Shroud(true);
    }

    public void UpdateDisplay(Situation situation)
    {
        if(situation.State.IsActiveInThisState(this))
            canvasGroupFader.Show();
        else
            canvasGroupFader.Hide();
    }


    public void SituationStateChanged(Situation situation)
    {
        UpdateDisplay(situation);
    }

    public void TimerValuesChanged(Situation s)
    {
        //
    }

    public void SituationSphereContentsUpdated(Situation s)
    {
       //
    }

    public void ReceiveNotification(INotification n)
    {
        //
    }

}
