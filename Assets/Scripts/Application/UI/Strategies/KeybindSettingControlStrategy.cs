﻿using SecretHistories.Entities;
using SecretHistories.UI;
using SecretHistories.Services;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SecretHistories.UI
{
    public class KeybindSettingControlStrategy : SettingControlStrategy
    {
        public string GetKeybindDisplayValue()
        {
            return boundSetting.GetCurrentValueAsHumanReadableString();
        }

        public InputActionRebindingExtensions.RebindingOperation Rebind(InputActionAsset inputActionAsset,
            TMP_InputField input)
        {
            inputActionAsset.actionMaps[0].Disable();
            var action = inputActionAsset.FindAction(SettingId);
            var rebinding = action.PerformInteractiveRebinding().WithControlsExcluding("mouse");
            rebinding.OnComplete(r =>
            {
                input.text = r.selectedControl.displayName;
                
              //  input.OnDeselect(null);
                inputActionAsset.actionMaps[0].Enable();
                
                boundSetting.CurrentValue = action.bindings[0].overridePath;

                Watchman.Get<Concursum>().ContentUpdatedEvent.Invoke(new ContentUpdatedArgs{Message = "Changed a key binding, which might need reflecting in on-screen prompts"});

                EventSystem.current.SetSelectedGameObject(null);
                r.Dispose();
            });


            rebinding.Start();
            return rebinding;

        }


    }
}