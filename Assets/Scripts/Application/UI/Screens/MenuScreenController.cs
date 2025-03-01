﻿// based on LoadingScreenManager
// --------------------------------
// built by Martin Nerurkar (http://www.martin.nerurkar.de)
// for Nowhere Prophet (http://www.noprophet.com)
//
// Licensed under GNU General Public License v3.0
// http://www.gnu.org/licenses/gpl-3.0.txt

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Assets.Scripts.Application.Entities.NullEntities;
using SecretHistories.Assets.Scripts.Application.Entities;
using SecretHistories.Assets.Scripts.Application.Infrastructure.Modding;
using SecretHistories.Entities;
using SecretHistories.Enums;
using SecretHistories.Fucine;
using SecretHistories.UI;
using SecretHistories.Constants;
using SecretHistories.Constants.Modding;
using SecretHistories.Services;
using SecretHistories.Enums.UI;
using SecretHistories.Infrastructure;
using SecretHistories.Infrastructure.Persistence;
using TMPro;

public class MenuScreenController : LocalNexus {

    // can be used to disable interaction when we start loading into a scene
    public EventSystem eventSystem;

    [Header("Buttons")]
    public Button newGameButton;
    public Button continueGameButton;
    public Button purgeButton;
    public Button modsButton;
    public Button languageButton;

    [Header("Overlays")]
    public CanvasGroupFader modal;
    public CanvasGroupFader purgeConfirm;
	public CanvasGroupFader credits;
	public CanvasGroupFader settings;
	public CanvasGroupFader languageMenu;
    public CanvasGroupFader versionNews;
    public CanvasGroupFader modsPanel;
    public CanvasGroupFader startDLCLegacyConfirmPanel;
    public OptionsPanel optionsPanel;


    [Header("Hints")]
    public GameObject brokenSaveMessage;
    public TextMeshProUGUI VersionNumber;
    public Animation versionAnim;
    [SerializeField] public Notifier notifier;

    [Header("DLC & Mods")]
    public Transform legacyStartEntries;
    public MenuLegacyStartEntry LegacyStartEntryPrefab;
    public TextMeshProUGUI steamWorkshopDownloadLink;

    [Header("Localisation")]
    public Transform LanguagesAvailable;
    public GameObject languageChoicePrefab;

    [SerializeField] private GameObject MenuBlocksHolder;

    bool canTakeInput;
    int sceneToLoad;
    VersionNumber currentVersion;
    CanvasGroupFader currentOverlay;


    [SerializeField]
    public ModsDisplayPanel modsDisplayPanel;

    private static readonly StartableLegacySpec[] DlcEntrySpecs =
    {
        new StartableLegacySpec(
            "dancer",
            "UI_DLC_TITLE_DANCER",
            new Dictionary<Storefront, string>
            {
                {Storefront.Steam, "https://store.steampowered.com/app/871650/Cultist_Simulator_The_Dancer/"},
                {Storefront.Gog, "https://www.gog.com/game/cultist_simulator_the_dancer"},
                {Storefront.Humble, "https://www.humblebundle.com/store/cultist-simulator-the-dancer"},
                {Storefront.Unknown,"https://www.cultistsimulator.com" }
            },
            true,
            null),
        new StartableLegacySpec(
            "priest",
            "UI_DLC_TITLE_PRIEST",
            new Dictionary<Storefront, string>
            {
                {Storefront.Steam, "https://store.steampowered.com/app/871651/Cultist_Simulator_The_Priest/"},
                {Storefront.Gog, "https://www.gog.com/game/cultist_simulator_the_priest"},
                {Storefront.Humble, "https://www.humblebundle.com/store/cultist-simulator-the-priest"},
                {Storefront.Unknown,"https://www.cultistsimulator.com" }
            },
            true,
                null),
        new StartableLegacySpec(
            "ghoul",
            "UI_DLC_TITLE_GHOUL",
            new Dictionary<Storefront, string>
            {
                {Storefront.Steam, "https://store.steampowered.com/app/871900/Cultist_Simulator_The_Ghoul/"},
                {Storefront.Gog, "https://www.gog.com/game/cultist_simulator_the_ghoul"},
                {Storefront.Humble, "https://www.humblebundle.com/store/cultist-simulator-the-ghoul"},
                {Storefront.Unknown,"https://www.cultistsimulator.com" }
            },
            true,
                null)
        ,
        new StartableLegacySpec(
            "exile",
            "UI_DLC_TITLE_EXILE",
            new Dictionary<Storefront, string>
            {
                {Storefront.Steam, "https://store.steampowered.com/app/1259930/Cultist_Simulator_The_Exile"},
                {Storefront.Gog, "https://www.gog.com/game/cultist_simulator_the_exile"},
                {Storefront.Humble, "https://www.humblebundle.com/store/cultist-simulator-the-exile"},
                {Storefront.Unknown,"https://www.cultistsimulator.com" }
            },
            true,
            null
            )
    };

    
    void Start() {

        InitialiseServices();

        UpdateAndShowMenu();
        
        var concursum = Watchman.Get<Concursum>();

        concursum.ContentUpdatedEvent.AddListener(OnContentUpdated);
        DoMenuBlockRotation();
    }



    private void OnContentUpdated(ContentUpdatedArgs args)
    {
        BuildLegacyStartsPanel(Watchman.Get<MetaInfo>().Storefront);
    }


    void InitialiseServices()
	{
        currentVersion = Watchman.Get<MetaInfo>().VersionNumber;

        var store = Watchman.Get<MetaInfo>().Storefront;

        BuildLegacyStartsPanel(store);
        modsDisplayPanel.Initialise(store);
        BuildLanguagesAvailablePanel();

        new Watchman().Register(notifier); //we register a different notifier in the later tabletop scene

    }




    void UpdateAndShowMenu()
    {


        var pp = GamePersistenceProvider.GetBestGuessGamePersistence();
        bool savedGameExists=pp.SaveExists();

        // Show the buttons as needed
 
        newGameButton.gameObject.SetActive(!savedGameExists);
        continueGameButton.gameObject.SetActive(savedGameExists && pp.IsValid());
        purgeButton.gameObject.SetActive(savedGameExists);

      
        brokenSaveMessage.gameObject.SetActive(savedGameExists && !pp.IsValid());
        UpdateVersionNumber();
        HideAllOverlays();

        // now we can take input
		canTakeInput = true;
    }

    void UpdateVersionNumber() {
        // Show the current version number
        VersionNumber.text = currentVersion.Version;

       // if (hasNews)
     //       versionAnim.Play();
      //  else
     //       versionAnim.Stop();
    }

    void HideAllOverlays() {
        modal.gameObject.GetComponent<CanvasGroupFader>().Hide();
        purgeConfirm.Hide();
        credits.Hide();
        versionNews.Hide();
        modsPanel.Hide();
        languageMenu.Hide();
        startDLCLegacyConfirmPanel.Hide();


    }

#region -- View Changes ------------------------



    void ShowOverlay(CanvasGroupFader overlay) {
		if (currentOverlay != null)
            return;

        currentOverlay = overlay;

        overlay.Show();
        modal.Show();
    }

   public void HideCurrentOverlay() {

       if (currentOverlay != null)
       {
           currentOverlay.Hide();
           currentOverlay = null;
        }
        if(optionsPanel.isActiveAndEnabled)
            optionsPanel.ToggleVisibility();
       

        modal.Hide();
        
    }

#endregion

#region -- User Actions via Scene Buttons ------------------------

    public void BeginGameWithDefaultLegacy()
	{
        if (!canTakeInput)
            return;

        var defaultLegacy = Watchman.Get<Compendium>().GetEntitiesAsList<Legacy>().First();
        BeginNewSaveWithSpecifiedLegacy(defaultLegacy.Id);

    }

    public void ContinueGame()
	{
        if (!canTakeInput)
            return;

        var protag = Watchman.Get<Stable>().Protag();


        if (protag.State==CharacterState.Viable) {
            //back into the game!
            Watchman.Get<StageHand>().LoadGameOnTabletop(new DefaultGamePersistenceProvider());
            return;
        }

        Watchman.Get<StageHand>().LegacyChoiceScreen();
    }


    public void TryPurgeSave() {
        if (!canTakeInput)
            return;

        ShowOverlay(purgeConfirm);
    }

    public void PurgeSave() {
        if (!canTakeInput)
            return;

        var defaultPersistenceProvider = new DefaultGamePersistenceProvider();

        defaultPersistenceProvider.PurgeSaveFileIrrevocably();
        

        currentOverlay = null; // to ensure we can re-open another overlay afterwards
        Watchman.Get<StageHand>().MenuScreen();
    }


    public void BeginNewSaveWithSpecifiedLegacy(string legacyId)
    {
        var legacy= Watchman.Get<Compendium>().GetEntityById<Legacy>(legacyId);
        var freshGamePersistenceProvider=new FreshGameProvider(legacy);
       
        Watchman.Get<StageHand>().LoadGameOnTabletop(freshGamePersistenceProvider);
    }

    public void ShowCredits() {
        if (!canTakeInput)
            return;

        ShowOverlay(credits);
	}

	public void ShowSettings() {
		if (!canTakeInput)
			return;

        modal.Show();
        ToggleOptionsEvent.Invoke();
	}

	public void ShowLanguage()
	{
		if (!canTakeInput)
			return;
		
		ShowOverlay(languageMenu);
	}

	public void SetLanguage( string lang_code )
	{
		if (!canTakeInput)
			return;

        var culture = Watchman.Get<Compendium>().GetEntityById<Culture>(lang_code);

        Watchman.Get<Concursum>().SetNewCulture(culture);

        HideCurrentOverlay();

        UpdateAndShowMenu();
	}

    public void ShowVersionHints() {
        if (!canTakeInput)
            return;

        ShowOverlay(versionNews);
        versionAnim.Stop(); // Ensure that the anim is no longer playing
    }


    public void ShowModsPanel()
    {
        if (!canTakeInput)
            return;
        
        ShowOverlay(modsPanel);
    }

    public void ShowStartLegacyConfirmPanel(Legacy legacy)
    {
        HideCurrentOverlay();
        StartDLCLegacyConfirm confirmPanelComponent=startDLCLegacyConfirmPanel.GetComponent<StartDLCLegacyConfirm>();
        confirmPanelComponent.LegacyId = legacy.Id;
        confirmPanelComponent.TitleText.text = legacy.Label;
        confirmPanelComponent.DescriptionText.text =$"\"{legacy.Description}\"";
        confirmPanelComponent.msc = this;

        ShowOverlay(startDLCLegacyConfirmPanel);
        
    }


    private void BuildLegacyStartsPanel(Storefront store)
    {
        foreach (Transform legacyStartEntry in legacyStartEntries)
            Destroy(legacyStartEntry.gameObject);

        var legacies = Watchman.Get<Compendium>().GetEntitiesAsList<Legacy>();

        var allNewStartLegacies = legacies.Where(l => l.NewStart).ToList();


        steamWorkshopDownloadLink.enabled = store == Storefront.Steam;

        var startableLegacySpecs=new List<StartableLegacySpec>();


        //Look for legacies which match a DLC spec
        foreach (var spec in DlcEntrySpecs)
        {
            var matchingLegacy = allNewStartLegacies.SingleOrDefault(l => l.Id == spec.Id);


            //if a legacy is present, that DLC is installed. Add the legacy to the spec for the startable, and remove it from the list of legacies to match.
            if (matchingLegacy != null)
            {
                spec.Legacy = matchingLegacy;
                allNewStartLegacies.Remove(matchingLegacy);
            }
            
            startableLegacySpecs.Add(spec);
        }

        //These are legacies which don't match DLCEntries: so they're mods.
        foreach (var remainingLegacy in allNewStartLegacies)
        {
            
            var specForLegacy=new StartableLegacySpec(remainingLegacy.Id,remainingLegacy.Label, new Dictionary<Storefront, string>(),false,remainingLegacy );
            startableLegacySpecs.Add(specForLegacy);
        }

   
        startableLegacySpecs=startableLegacySpecs.OrderByDescending(ls => ls.ReleasedByWf).ToList();


        foreach (var legacySpec in startableLegacySpecs)
        {
            var legacyStartEntry = Instantiate(LegacyStartEntryPrefab, legacyStartEntries);
            legacyStartEntry.Initialize(legacySpec, store, this);
        }


    }
    


    private void BuildLanguagesAvailablePanel()
    {
        foreach(Transform languageAvailable in LanguagesAvailable)
            Destroy(languageAvailable.gameObject);



        foreach (var culture in Watchman.Get<Compendium>().GetEntitiesAsList<Culture>().Where(c=>c.Released))
        {
            var languageChoice =Instantiate(languageChoicePrefab).GetComponent<LanguageChoice>();
            languageChoice.transform.SetParent(LanguagesAvailable,false);
            languageChoice.Label.text = culture.Endonym;
            languageChoice.Label.font = Watchman.Get<ILocStringProvider>().GetFont(LanguageManager.eFontStyle.Button, culture.FontScript);
            languageChoice.gameObject.GetComponent<Button>().onClick.AddListener(()=>SetLanguage(culture.Id));
        }

    
    }
    


    public void Exit() {
        if (!canTakeInput)
            return;

        Application.Quit();
    }

    public void DoMenuBlockRotation()
    {
     
        int? menuBlockId=Watchman.Get<Config>().GetConfigValueAsInt(NoonConstants.MENU_BLOCK_ID);
        if (menuBlockId == null)
            menuBlockId = 1;

        var availableBlocks = MenuBlocksHolder.GetComponentsInChildren<MenuContentBlock>(true);
        int maxBlockIndex = availableBlocks.Length; //might not always be this if we want to weight block display
        int nextBlockId = (int)menuBlockId + 1;
        if (nextBlockId > maxBlockIndex)
            nextBlockId = 1;

        Watchman.Get<Config>().PersistConfigValue(NoonConstants.MENU_BLOCK_ID, nextBlockId.ToString());
           
        foreach (var block in availableBlocks)
            if(block.ID>=menuBlockId)
            {
                block.gameObject.SetActive(true);
                break;
            }

    }
    public void BrowseFiles()
    {
        OpenInFileBrowser.Open(Application.persistentDataPath);
    }

    #endregion
}
