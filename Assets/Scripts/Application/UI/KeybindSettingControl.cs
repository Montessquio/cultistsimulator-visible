﻿using System;
using System.Collections;
using System.Collections.Generic;
using SecretHistories.Entities;
using SecretHistories.UI;

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeybindSettingControl : AbstractSettingControl
{
    [SerializeField] public TextMeshProUGUI ActionLabel;
    [SerializeField] public TMP_InputField keybindingInputField;

    [SerializeField]
    public InputActionAsset inputActionAsset;

    private KeybindSettingControlStrategy strategy;
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    public override string SettingId
    {
        get { return strategy.SettingId; }
    }

    public override string TabId
    {
        get { return strategy.SettingTabId; }
    }

    // Start is called before the first frame update
    public override void Initialise(Setting settingToBind)
    {
        if (settingToBind == null)
        {
            NoonUtility.Log("Trying to create a keybind setting control with a null setting entity");
            return;
        }

        strategy = new KeybindSettingControlStrategy();
        strategy.Initialise(settingToBind);
        gameObject.name = "KeybindSetting_" + strategy.SettingId;
        ActionLabel.GetComponent<Babelfish>().UpdateLocLabel(strategy.SettingHint);
        keybindingInputField.text = strategy.GetKeybindDisplayValue();
        _initialisationComplete = true;
    }

    public void OnInputSelect()
    {
        keybindingInputField.text = String.Empty;
       rebindingOperation=strategy.Rebind(inputActionAsset, keybindingInputField);
    }

    public void OnValueChanged()
        {
            Debug.Log("OnValueChanged");
        }

        public void OnEndEdit()
    {
        Debug.Log("OnEndEdit");
    }

        public void OnDeselect()
        {
            Debug.Log("Deselect");
        if(rebindingOperation!=null)
        {
            rebindingOperation.Cancel();
            rebindingOperation.Dispose();
            keybindingInputField.text = strategy.GetKeybindDisplayValue();
        }

    }



    public override void Update()
    {

    }

    public override void SetInteractable(bool interactable)
    {
        throw new NotImplementedException();
    }
}
