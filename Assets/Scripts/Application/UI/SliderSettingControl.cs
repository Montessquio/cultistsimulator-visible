﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SecretHistories.Constants;
using SecretHistories.Entities;
using SecretHistories.UI;
using SecretHistories.Services;

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SecretHistories.UI
{
    public class SliderSettingControl: AbstractSettingControl
    {
#pragma warning disable 649
        [SerializeField]
        private TextMeshProUGUI SliderHint;
        [SerializeField]
        private Slider Slider;
        [SerializeField]
        private TextMeshProUGUI SliderValueLabel;
#pragma warning restore 649


        private SliderSettingControlStrategy strategy;

        public override string SettingId
        {
            get { return strategy.SettingId; }
        }

        public override string TabId
        {
            get { return strategy.SettingTabId; }
        }


        public override void SetInteractable(bool interactable)
        {


            if (interactable)
            {
                Slider.interactable = true;
                SliderValueLabel.alpha = (1f);
            }
            else
            {
                Slider.interactable = false;
                SliderValueLabel.alpha = (0.3f);
            }
        }


        public override void Initialise(Setting settingToBind)
        {
            if (settingToBind == null)
            {
                NoonUtility.Log("Trying to create a slider setting control with a null setting entity");
                return;
            }

            if (settingToBind.Id == NoonConstants.RESOLUTION)
                strategy = new ResolutionSliderSettingControlStrategy();
            else
                strategy = new FucineSliderSettingControlStrategy();

            strategy.Initialise(settingToBind);
            strategy.SetSliderValues(Slider);
            SliderHint.GetComponent<Babelfish>().UpdateLocLabel(strategy.SettingHint);
            
            SliderValueLabel.GetComponent<Babelfish>().UpdateLocLabel(strategy.GetLabelForValue(Slider.value));


            gameObject.name = "SliderSetting_" + strategy.SettingId;


            _initialisationComplete = true;

        }

        public void OnValueChanged(float changingToValue)
        {
            //I added this guard clause because otherwise the OnValueChanged event can fire while the slider initial values are being set -
            //for example, if the minvalue is set to > the default control value of 0. This could be fixed by
            //adding the listener in code rather than the inspector, but I'm hewing away from that. It could also be 'fixed' by changing the
            //order of the initialisation steps, but that's half an hour of my time I don't want to lose again next time I fiddle :) - AK
            if (_initialisationComplete)
            {
                SoundManager.PlaySfx("UISliderMove");
                newSettingValueQueued = changingToValue;
                string newValueLabel = strategy.GetLabelForValue((float)newSettingValueQueued);
                SliderValueLabel.GetComponent<Babelfish>().UpdateLocLabel(newValueLabel);
            }
        }

        public override void Update()
        {
            //eg: we don't want to change  resolution until the mouse button is released
            if (!Pointer.current.press.isPressed && newSettingValueQueued != null)
            {
                strategy.OnSliderValueChangeComplete((float)newSettingValueQueued);
                newSettingValueQueued = null;
            }
        }

    }



    }

