﻿using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using ICities;
using ColossalFramework;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ChirpBanner
{
    public class ChirpyBannerMod : IUserMod
    {
       public string Description
       {
          get { return "Replaces Chirpy with a scrolling Marquee banner."; }
       }

       public string Name
       {
          get { return "Chirpy Banner"; }
       }
    }

	// Adds a button to configure the ChirperBanner
	public class LoadingExtension : LoadingExtensionBase
	{
		ILoading loading;
		public override void OnCreated(ILoading loading)
		{
			this.loading = loading;
		}

		public override void OnLevelLoaded(LoadMode mode)
		{
			// Get the UIView object. This seems to be the top-level object for most
			// of the UI.
			var uiView = UIView.GetAView();

			// Add a new button to the view.
			var button = (UIButton)uiView.AddUIComponent(typeof(UIButton));

			// Set the text to show on the button.
			//button.text = "Click Me!";

			// Set the button dimensions.
			button.width = 30;
			button.height = 30;

			// Style the button to look like a menu button.
			button.normalBgSprite = "ButtonMenu";
			button.normalFgSprite = "ChirperIcon";
			button.disabledBgSprite = "ButtonMenuDisabled";
			button.hoveredBgSprite = "RoundBackBigHovered";
			button.focusedBgSprite = "ButtonMenuFocused";
			button.pressedBgSprite = "RoundBackBigPressed";

			// Enable button sounds.
			button.playAudioEvents = true;

			// Place the button.
			button.transformPosition = new Vector3(1.61f, 1.0f);

			// Respond to button click.
			button.eventClick += ButtonClick;

			if (loading.managers.threading.simulationPaused) 
			{
				ChirpyBanner.CurrentConfig.ScrollSpeed = 0;
			}
		}

		private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam)
		{
			ChirpyBanner.theBannerConfigPanel.ShowPanel(Vector2.zero, true);
		}
	}

	// Function to thread the Chirp stuff
	// Code for threading courtesy of https://github.com/onewaycitystreets/StreetDirectionViewer/blob/0107f423af6a79a81dfce3e6942d01df9db0065f/StreetDirectionViewer.cs
	/*
	public class ChirpyThreadingExtension : ThreadingExtensionBase {

		public IThreading threading;
		public override void OnCreated(IThreading threading) {
			this.threading = threading;
				
		}

		public override void OnReleased() {

		}

		public override void OnUpdate(float realTimeDelta, float simulationTimeDelta) {
			// OnCreate appears to be called too early to add things to the UI, so add the button
			// in OnUpdate.

		}

		public override void OnBeforeSimulationTick() {

		}

		public void OnCreated(ILoading loading) {
			// This doesn't seem to be called. Maybe the plugin manager
			// sees that it implements IThreadingExtension first and doesn't
			// look for other interfaces?
		}

		public void OnLevelLoaded(LoadMode mode) {

		}

		public void OnLevelUnloading() {

		}

}*/
	public class MyIThreadingExtension: IThreadingExtension
	{
		static IThreading threading = null;
		//Thread: Main
		public void OnCreated(IThreading _threading)
		{
			threading = _threading;
		}

		static public IThreading getThreading(){
			return threading;
		}
		//Thread: Main
		public void OnReleased()
		{
			threading = null;
		}
		static public void addTask2Main(System.Action action)
		{
			if (threading != null)
			{
				threading.QueueMainThread(action);
			}
		}
		static public void addTask2Sim(System.Action action)
		{
			if (threading != null)
			{
				threading.QueueSimulationThread(action);
			}
		}

		//Thread: Main
		public void OnUpdate(float realTimeDelta, float simulationTimeDelta)
		{
			moveChirps();
		}
		//Thread: Simulation
		public void OnBeforeSimulationTick()
		{

		}
		//Thread: Simulation
		public void OnBeforeSimulationFrame()
		{
			
		}
		//Thread: Simulation
		public void OnAfterSimulationFrame()
		{

		}
		//Thread: Simulation
		public void OnAfterSimulationTick()
		{

		}

		public void moveChirps() {
			if (BannerPanel.hasChirps) {
				bool bPopIt = false;
				float currentTrailingEdge = 0;
				foreach (BannerLabelStruct bls in ChirpyBanner.theBannerPanel.BannerLabelsQ) {
					if (bls.IsCurrentlyVisible)
					{
						bls.Label.relativePosition = new Vector3(bls.Label.relativePosition.x - Time.deltaTime * ChirpyBanner.CurrentConfig.ScrollSpeed, bls.Label.relativePosition.y, bls.Label.relativePosition.z);
						bls.RelativePosition = bls.Label.relativePosition;
						//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("chirp position! {0}", bls.RelativePosition));


						// is it off to the left entirely?                              
						if ((bls.Label.relativePosition.x + bls.Label.width) <= 0)
						{
							bPopIt = true;
							bls.IsCurrentlyVisible = false;
							bls.Label.isVisible = false;
						}
						else
						{
							// still in banner (in whole or in part)
							currentTrailingEdge += bls.RelativePosition.x + bls.Label.width;
							bls.Label.isVisible = true;
						}
					}
					else // I'm not yet visible...
					{
						// is there room for me to start scrolling?
						if (currentTrailingEdge < ChirpyBanner.theBannerPanel.width)
						{
							// yes, there's room
							bls.Label.relativePosition = new Vector3(ChirpyBanner.theBannerPanel.width, bls.Label.relativePosition.y, bls.Label.relativePosition.z);
							bls.RelativePosition = bls.Label.relativePosition;
							bls.IsCurrentlyVisible = true;
							bls.Label.isVisible = true;
							currentTrailingEdge += bls.RelativePosition.x + bls.Label.width; 
						}
					}
				}

				if (bPopIt)
				{
					BannerLabelStruct blsfree = ChirpyBanner.theBannerPanel.BannerLabelsQ.Dequeue();
					ChirpyBanner.theBannerPanel.RemoveBannerLabel(blsfree);
					//this.RemoveUIComponent(blsfree.Label);
				}					
			}			
		}
			

	}

   public class ChirpyBanner : IChirperExtension
   {
      public static MyConfig CurrentConfig;
      public static BannerPanel theBannerPanel;
      public static IChirper BuiltinChirper;

      public static BannerConfiguration theBannerConfigPanel;

      public void OnCreated(IChirper chirper)
      {
         // read config file for settings
         string configName = "ChirpBannerConfig.xml";
         CurrentConfig = MyConfig.Deserialize(configName);

         if (CurrentConfig == null)
         {
            CurrentConfig = new MyConfig();

            MyConfig.Serialize(configName, CurrentConfig);
         }
         // if old version, update with new
         else if (CurrentConfig.version == 0 || CurrentConfig.version < 5) // update this when we add any new settings                  
         {
            CurrentConfig.version = 5;
            MyConfig.Serialize(configName, CurrentConfig);
         }

         BuiltinChirper = chirper;

         if (CurrentConfig.DestroyBuiltinChirper)
         {
            chirper.ShowBuiltinChirper(false);
         }

         CreateBannerConfigUI();
         CreateBannerUI();

      }

      public void OnMessagesUpdated()
      {
      }



	// Based on https://github.com/AtheMathmo/SuperChirperMod/blob/master/SuperChirper/ChirpFilter.cs
	// But in the opposite direction
	public static bool FilterMessage(string input)
	{

		switch (input)
		{
		// Handles ID's of all nonsense chirps.
		case LocaleID.CHIRP_ASSISTIVE_TECHNOLOGIES:
		case LocaleID.CHIRP_ATTRACTIVE_CITY:
		case LocaleID.CHIRP_CHEAP_FLOWERS:
		case LocaleID.CHIRP_DAYCARE_SERVICE:
		case LocaleID.CHIRP_HAPPY_PEOPLE:
		case LocaleID.CHIRP_HIGH_TECH_LEVEL:
		case LocaleID.CHIRP_LOW_CRIME:
		case LocaleID.CHIRP_NEW_FIRE_STATION:
		case LocaleID.CHIRP_NEW_HOSPITAL:
		case LocaleID.CHIRP_NEW_MAP_TILE:
		case LocaleID.CHIRP_NEW_MONUMENT:
		case LocaleID.CHIRP_NEW_PARK:
		case LocaleID.CHIRP_NEW_PLAZA:
		case LocaleID.CHIRP_NEW_POLICE_HQ:
		case LocaleID.CHIRP_NEW_TILE_PLACED:
		case LocaleID.CHIRP_NEW_UNIVERSITY:
		case LocaleID.CHIRP_NEW_WIND_OR_SOLAR_PLANT:
		case LocaleID.CHIRP_ORGANIC_FARMING:
		case LocaleID.CHIRP_POLICY:
		case LocaleID.CHIRP_PUBLIC_TRANSPORT_EFFICIENCY:
		case LocaleID.CHIRP_RANDOM:
		case LocaleID.CHIRP_STUDENT_LODGING:
			return false;
		default:
			return true;
		}
	}

      public void OnNewMessage(IChirperMessage message)
      {
			bool important = false;
         if (message != null && theBannerPanel != null)
         {
            try
            {
               string citizenMessageID = string.Empty;

               CitizenMessage cm = message as CitizenMessage;
               if (cm != null)
               {
                  //DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("found citmess MessageID: {0} GetText: {1}", cm.m_messageID, cm.GetText()));
                  citizenMessageID = cm.m_messageID;
               }

               if (!string.IsNullOrEmpty(citizenMessageID))
               {
				  // TODO: Do stuff if the Chirper message is actually important.
				  // Hope is to mimic SimCity's ticker.
				  // List of LocaleIDs available here: https://github.com/cities-skylines/Assembly-CSharp/wiki/LocaleID
				  important = FilterMessage(cm.m_messageID);
               }
            }
            catch (Exception ex)
            {
               DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("ChirpBanner.OnNewMessage threw Exception: {0}", ex.Message));
            }
           
            // use rich styled text
            // Colossal markup uses sliiiiiightly different tags than unity.
            // munge our config strings to fit
            string nameColorTag = CurrentConfig.NameColor;
            string textColorTag = CurrentConfig.MessageColor;

            if (nameColorTag.Length == 9) // ie: #001122FF
            {
               nameColorTag = nameColorTag.Substring(0, 7); // drop alpha bits
            }

            if (textColorTag.Length == 9) // ie: #001122FF
            {
               textColorTag = textColorTag.Substring(0, 7); // drop alpha bits
            }
			
				if (important) {
					//DebugOutputPanel.AddMessage (ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format ("Chirp is important: {0}", message.text));
					//DebugOutputPanel.AddMessage (ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format ("CurrentConfig.NameColor: {0}", CurrentConfig.NameColor));
					//DebugOutputPanel.AddMessage (ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format ("CurrentConfig.MessageColor: {0}", CurrentConfig.MessageColor));
					textColorTag = "#FF0000";
					nameColorTag = "#FF0000";
				}

				// Check for CurrentConfig.FilterChirps
				if (CurrentConfig.FilterChirps) {
					// If Chirp is deemed not important as a result of above LocaleIDs, just return, do nothing
					if (!important) {
						return;
					}
				} else {
            
			//DebugOutputPanel.AddMessage (ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format ("textColorTag: {0}", textColorTag));
			//DebugOutputPanel.AddMessage (ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format ("nameColorTag: {0}", nameColorTag));
			DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("chirp! {0}", message.text));

            string str = String.Format("<color{0}>{1}</color> : <color{2}>{3}</color>", nameColorTag, message.senderName, textColorTag, message.text);
            theBannerPanel.CreateBannerLabel(str, message.senderID);
			//MyIThreadingExtension.addTask2Main(() => { theBannerPanel.CreateBannerLabel(str, message.senderID); });
			//MyIThreadingExtension.addTask2Main(() => { DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("chirp! {0}", message.text)); });	
				}
         }
      }

      public void OnReleased()
      {
         if (theBannerPanel != null)
         {
            theBannerPanel = null;
         }
				
      }

      public void OnUpdate()
      {
      }

      private void CreateBannerConfigUI()
      {
         // create UI using ColossalFramework UI classes
         UIView uiv = UIView.GetAView();

         if (uiv == null)
         {
            return;
         }

         theBannerConfigPanel = (BannerConfiguration)uiv.AddUIComponent(typeof(BannerConfiguration));

         if (theBannerConfigPanel != null)
         {
            theBannerConfigPanel.isVisible = false; // start off hidden, until banner clicked
            theBannerConfigPanel.Initialize(this);
         }
      }

      private void CreateBannerUI()
      {
         // create UI using ColossalFramework UI classes
         UIView uiv = UIView.GetAView();

         if (uiv == null)
         {
            return;
         }

         theBannerPanel = (BannerPanel)uiv.AddUIComponent(typeof(BannerPanel));

         if (theBannerPanel != null)
         {
            //theBannerPanel.ScrollSpeed = CurrentConfig.ScrollSpeed;
            //byte bAlpha = (byte)(ushort)(255f * CurrentConfig.BackgroundAlpha);

            //theBannerPanel.BackgroundColor = new Color32(0, 0, 0, bAlpha);
            //theBannerPanel.FontSize = CurrentConfig.TextSize;
            theBannerPanel.Initialize();

            // add mouse click handler to us here
            //theBannerPanel.eventClick += BannerPanel_eventClick;
            theBannerPanel.eventMouseUp += BannerPanel_eventMouseUp;
         }
      }

      public void BannerPanel_eventClick(UIComponent component, UIMouseEventParameter eventParam)
      {
      }

      public void BannerPanel_eventMouseUp(UIComponent component, UIMouseEventParameter eventParam)
      {
         if (eventParam.buttons == UIMouseButton.Right)
         {
            // show config window at position of click
            theBannerConfigPanel.ShowPanel(eventParam.position, false);
         }
      }

   }

   public class BannerLabelStruct
   {
      public Vector3 RelativePosition; // relative to parent BannerPanel
      public UILabel Label;
      public bool IsCurrentlyVisible;
   }

   // A UIPanel that contains a queue of UILabels (one for each chirp to be displayed)
   // - as UILabels are added, the are put in a queue with geometry information (current position, extents)
   // - OnUpdate() handler moves each panel across the screen (all in line, like ducks :)
   // - when a UILable moves to the left such that it's out of the panel, it gets deleted and popped off the queue
   public class BannerPanel : UIPanel
   {
      // members
      bool Shutdown = false;

      const int banner_inset = 60;
      const int label_y_inset = 5;

		public static UILabel ChirpLabel;

      public Color32 BackgroundColor = new Color32(0, 0, 0, 0xff);
      UIFont OurLabelFont = null;

		public Queue<BannerLabelStruct> BannerLabelsQ = new Queue<BannerLabelStruct>();

		public static bool hasChirps = false;
      
      public void Initialize()
      {
         UIView uiv = UIView.GetAView();

         if (uiv == null)
         {
            Cleanup();
            return;
         }

         int viewWidth = (int)uiv.GetScreenResolution().x;
         int viewHeight = (int)uiv.GetScreenResolution().y;
         
         this.autoSize = true;

         this.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left;
         this.backgroundSprite = "GenericPanel";

         this.position = new Vector3((-viewWidth / 2) + banner_inset, (viewHeight / 2));
         this.width = viewWidth - (banner_inset * 2);
        
         this.height = 25;

         this.color = BackgroundColor;
     	 this.opacity = 1.0f;

         this.autoLayout = false;
         this.clipChildren = true;
         //this.isInteractive = true;
		ChirpLabel = this.AddUIComponent<UILabel>();

         this.SendToBack();
      }

      public void Cleanup()
      {
         Shutdown = true;
      }

		public void RemoveBannerLabel(BannerLabelStruct blsfree) {
			this.RemoveUIComponent(blsfree.Label);
		}

      public void CreateBannerLabel(string chirpStr, uint senderID)
      {
         if (Shutdown || string.IsNullOrEmpty(chirpStr))
         {
            return;
         }
			hasChirps = true;
			ChirpLabel = this.AddUIComponent<UILabel>();
			if (ChirpLabel != null) {
				//newLabel.autoSize = true;
				//newLabel.autoHeight = true;
				ChirpLabel.verticalAlignment = UIVerticalAlignment.Middle;
				ChirpLabel.textAlignment = UIHorizontalAlignment.Left;
				ChirpLabel.relativePosition = new Vector3 ((this.width), 0);
				ChirpLabel.height = this.height;
				ChirpLabel.padding = new RectOffset (0, 0, 5, 5);

				ChirpLabel.textScaleMode = UITextScaleMode.ScreenResolution;
				ChirpLabel.textScale = (float)ChirpyBanner.CurrentConfig.TextSize / 20f;
				ChirpLabel.opacity = 1.0f;
				ChirpLabel.processMarkup = true;
				ChirpLabel.text = chirpStr;

				ChirpLabel.objectUserData = (object)new InstanceID () {
					Citizen = senderID
				};

				ChirpLabel.eventClick += (UIComponent comp, UIMouseEventParameter p) => {
					if (!((UnityEngine.Object)p.source != (UnityEngine.Object)null) || !((UnityEngine.Object)ToolsModifierControl.cameraController != (UnityEngine.Object)null))
						return;
					InstanceID id = (InstanceID)p.source.objectUserData;
					if (!InstanceManager.IsValid (id))
						return;
					ToolsModifierControl.cameraController.SetTarget (id, ToolsModifierControl.cameraController.transform.position, true); 
				};

				if (OurLabelFont == null) {
					OurLabelFont = ChirpLabel.font;
				}


				if (OurLabelFont != null) {
					//OurLabelFont.size = ChirpyBanner.CurrentConfig.TextSize;
					UIFontRenderer fr = OurLabelFont.ObtainRenderer ();
					fr.textScale = ChirpLabel.textScale;

					if (fr != null) {
						Vector2 ms = fr.MeasureString (chirpStr);

						ChirpLabel.width = ms.x;
						ChirpLabel.height = ms.y;

						if (this.height != (ChirpLabel.height + 0)) {
							this.height = ChirpLabel.height + 0;
						}
					}
				}

				BannerLabelStruct bls = new BannerLabelStruct ();
				bls.RelativePosition = new Vector3 (this.width, label_y_inset); // starting position is off screen, at max extent of parent panel
				bls.Label = ChirpLabel;
				bls.IsCurrentlyVisible = true;
				BannerLabelsQ.Enqueue(bls);
			}
      }
   }

   public class MyConfig
   {      
      public bool DestroyBuiltinChirper = true;
      public int ScrollSpeed = 0;
      public int TextSize = 20; // pixels
      public string MessageColor = "#FFFFFFFF";
      public string NameColor = "#31C3FFFF";
      public int version = 0;
	  public float BackgroundAlpha = 0f;
		public bool FilterChirps = false;

      public static void Serialize(string filename, MyConfig config)
      {
         try
         {
            var serializer = new XmlSerializer(typeof(MyConfig));

            using (var writer = new StreamWriter(filename))
            {
               serializer.Serialize(writer, config);
            }
         }
         catch { }
      }

      public static MyConfig Deserialize(string filename)
      {
         var serializer = new XmlSerializer(typeof(MyConfig));

         try
         {
            using (var reader = new StreamReader(filename))
            {
               MyConfig config = (MyConfig)serializer.Deserialize(reader);

               // sanity checks
               if (config.ScrollSpeed < 1) config.ScrollSpeed = 1;
               if (config.ScrollSpeed > 200) config.ScrollSpeed = 200;
               if (config.TextSize < 1 || config.TextSize > 100) config.TextSize = 20;

               return config;
            }
         }
         catch { }

         return null;
      }

   }
}
