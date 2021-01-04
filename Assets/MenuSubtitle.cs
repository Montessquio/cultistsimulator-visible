﻿using System.Collections;
using System.Collections.Generic;
using SecretHistories.Entities;
using SecretHistories.UI;
using SecretHistories.Services;

using TMPro;
using UnityEngine;

public class MenuSubtitle : MonoBehaviour
{

    [SerializeField] public TextMeshProUGUI SubtitleText;

    [SerializeField] public TextMeshProUGUI SubtitleTextShadow;

    public void UpdateWithLocValue(string text)
    {
        string currentCultureId = Registry.Get<Config>().GetConfigValue(NoonConstants.CULTURE_SETTING_KEY);

        var currentCulture = Registry.Get<Compendium>().GetEntityById<Culture>(currentCultureId);


        Babelfish subfish = SubtitleText.gameObject.GetComponent<Babelfish>();
		if (subfish)
		{ 
			subfish.UpdateLocLabel(text); 
            subfish.SetValuesFromCulture(currentCulture);
        }

        Babelfish shadowfish = SubtitleTextShadow.gameObject.GetComponent<Babelfish>();
		if (shadowfish)
		{
            shadowfish.UpdateLocLabel(text);
            shadowfish.SetValuesFromCulture(currentCulture);
        }

    }


    public void SetText(string text)
    {
        SubtitleText.text = text;
        SubtitleTextShadow.text = text;
    }

}
