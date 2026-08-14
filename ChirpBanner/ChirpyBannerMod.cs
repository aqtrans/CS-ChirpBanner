using System;
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

namespace ChirpBanner
{
	public class ChirpyBannerMod : IUserMod
	{
		// Interesting trick picked up from https://github.com/earalov/Skylines-NoPillars/blob/8079649da8574bb5b1ef00dfcae58d0d854efed2/NoPillars/NoPillarsMod.cs
		private static bool _optionsLoaded;

		public string Description {
			get { return "Replaces Chirpy with a scrolling Marquee banner."; }
		}

		public string Name {
			get {
				if (!_optionsLoaded) {
					MyConfig.LoadConfig ();
					_optionsLoaded = true;
				}
				return "Chirpy Banner+"; 
			}
		}

		public void OnSettingsUI (UIHelperBase helper)
		{
			float ScrollSpeedf = (float)MyConfig.ConfigHolder.Config.ScrollSpeed;

			UIHelperBase group = helper.AddGroup ("ChirpBanner+ Options");
			group.AddCheckbox ("Hide built-in Chirper", MyConfig.ConfigHolder.Config.DestroyBuiltinChirper, CheckHideChirper);
			group.AddCheckbox ("Filter out unimportant Chirps", MyConfig.ConfigHolder.Config.FilterChirps, CheckFilterChirper);
			group.AddCheckbox ("Make important Chirps red", MyConfig.ConfigHolder.Config.ColorChirps, CheckColorChirper);
			group.AddSlider ("Scroll Speed", 50, 200, 1.0f, ScrollSpeedf, CheckScrollSpeed);
			//group.AddSlider ("Chirp Size", 5, 100, 1.0f, ChirpyBanner.CurrentConfig.TextSize, CheckChirpSize);
			group.AddSlider ("Transparency", 0.1f, 1, 0.10f, MyConfig.ConfigHolder.Config.BackgroundAlpha, CheckTransparency);
			//group.AddSlider ("Banner Width", 0, 1, 0.10f, MyConfig.ConfigHolder.Config.BannerWidth, CheckWidth);

			UIHelperBase version = helper.AddGroup ("v2.6.1");

			//group.AddSlider("My Slider", 0, 1, 0.01f, 0.5f, EventSlide);
			//group.AddDropdown("My Dropdown", new string[] { "First Entry", "Second Entry", "Third Entry" }, -1, EventSel);
			//group.AddSpace(250);
			//group.AddButton("My Button", EventClick);
			//group.AddTextfield ("My Textfield", "Default value", EventTextChanged, EventTextSubmitted);
		}
		
		public void CheckHideChirper (bool c)
		{
			MyConfig.ConfigHolder.Config.DestroyBuiltinChirper = c;
			MyConfig.SaveConfig ();
			if (ChirpyBanner.theBannerPanel != null) {
				if (c) {
					ChirpyBanner.BuiltinChirper.ShowBuiltinChirper(false);
				} else {
					ChirpyBanner.BuiltinChirper.ShowBuiltinChirper(true);
				}
				
			}
		}

		public void CheckFilterChirper (bool c)
		{
			MyConfig.ConfigHolder.Config.FilterChirps = c;
			MyConfig.SaveConfig ();
		}

		public void CheckColorChirper (bool c)
		{
			MyConfig.ConfigHolder.Config.ColorChirps = c;
			MyConfig.SaveConfig ();
		}

		public void CheckScrollSpeed (float c)
		{
			int ScrollSpeedi = (int)c;
			MyConfig.ConfigHolder.Config.ScrollSpeed = ScrollSpeedi;
			MyConfig.SaveConfig ();
		}

		public void CheckChirpSize (float c)
		{
			int ChirpSizei = (int)c;
			MyConfig.ConfigHolder.Config.TextSize = ChirpSizei;
			MyConfig.SaveConfig ();
		}

		public void CheckTransparency (float c)
		{
			MyConfig.ConfigHolder.Config.BackgroundAlpha = c;
			MyConfig.SaveConfig ();
			if (ChirpyBanner.theBannerPanel != null) {
				ChirpyBanner.theBannerPanel.opacity = MyConfig.ConfigHolder.Config.BackgroundAlpha;
			}			
		}

		/* 
		public void CheckWidth (float c)
		{
			MyConfig.ConfigHolder.Config.BannerWidth = c;
			MyConfig.SaveConfig ();
			if (ChirpyBanner.theBannerPanel != null) {
				UIView uiv = UIView.GetAView ();
				int viewWidth = (int)uiv.GetScreenResolution ().x;
				const int banner_inset = 60;
				ChirpyBanner.theBannerPanel.width = (viewWidth * MyConfig.ConfigHolder.Config.BannerWidth) - (banner_inset * 2);
			}
		}
		*/

	}

	public class ChirpMoverThread: IThreadingExtension
 	{
		static IThreading threading = null;
		//Thread: Main
		public void OnCreated (IThreading _threading)
		{
			threading = _threading;
		}

		static public IThreading getThreading ()
		{
			return threading;
		}
		//Thread: Main
		public void OnReleased ()
		{
			threading = null;
		}

		static public void addTask2Main (System.Action action)
		{
			if (threading != null) {
				threading.QueueMainThread (action);
			}
		}

		static public void addTask2Sim (System.Action action)
		{
			if (threading != null) {
				threading.QueueSimulationThread (action);
			}
		}

		//Thread: Main
		public void OnUpdate (float realTimeDelta, float simulationTimeDelta)
		{
			if (!threading.simulationPaused) {
				moveChirps (simulationTimeDelta);
			}
		}
		//Thread: Simulation
		public void OnBeforeSimulationTick ()
		{
			
		}
		//Thread: Simulation
		public void OnBeforeSimulationFrame ()
		{
			
		}
		//Thread: Simulation
		public void OnAfterSimulationFrame ()
		{

		}
		//Thread: Simulation
		public void OnAfterSimulationTick ()
		{

		}

		public void moveChirps (float simulationTimeDelta)
		{
			if (BannerPanel.hasChirps) {
				bool bPopIt = false;
				float currentTrailingEdge = MyConfig.ConfigHolder.Config.TrailingEdgeOffset;
		        // Move calculations that don't change per-item out of the loop
		        float moveAmount = simulationTimeDelta * MyConfig.ConfigHolder.Config.ScrollSpeed;


				foreach (BannerLabelStruct bls in ChirpyBanner.theBannerPanel.BannerLabelsQ) {

					//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("chirp y position: {0}", bls.Label.relativePosition.y));
					//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("chirp x position: {0}", bls.Label.relativePosition.x));
					//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("chirp width: {0}", bls.Label.width));

					if (bls.IsCurrentlyVisible) {
						var pos = bls.Label.relativePosition;
						pos.x -= moveAmount;
						bls.Label.relativePosition = pos;
						bls.RelativePosition = bls.Label.relativePosition;

						//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("bls.Label.width: {0}", bls.Label.width));
						//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("bls.Label.relativePosition: {0}", bls.Label.relativePosition));

						// is it off to the left entirely?                              
						if ((bls.Label.relativePosition.x + bls.Label.width) <= 0) {
							//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("off to the left entirely"));

							bPopIt = true;
							bls.IsCurrentlyVisible = false;
							bls.Label.isVisible = false;
						} else {
							//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("still in banner"));

							// still in banner (in whole or in part)
							currentTrailingEdge += (bls.RelativePosition.x + bls.Label.width);
							bls.Label.isVisible = true;
						}
					} else { // I'm not yet visible...

						//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("currentTrailingEdge: {0}", currentTrailingEdge));

						// is there room for me to start scrolling?
						if (currentTrailingEdge < ChirpyBanner.theBannerPanel.width) {
							//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("currentTrailingEdge: {0}", currentTrailingEdge));
							//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("chirp width: {0}", bls.Label.width));

							// yes, there's room
							bls.Label.relativePosition = new Vector3 (ChirpyBanner.theBannerPanel.width, bls.Label.relativePosition.y, bls.Label.relativePosition.z);
							bls.RelativePosition = bls.Label.relativePosition;
							bls.IsCurrentlyVisible = true;
							bls.Label.isVisible = true;
							currentTrailingEdge += bls.RelativePosition.x + bls.Label.width; 
						}
					}
				}

				if (bPopIt) {
					BannerLabelStruct blsfree = ChirpyBanner.theBannerPanel.BannerLabelsQ.Dequeue ();
					//ChirpyBanner.theBannerPanel.RemoveBannerLabel(blsfree);
					ChirpyBanner.theBannerPanel.RemoveUIComponent (blsfree.Label);
				}					
			}			
		}
			

	}

	public class ChirpyBanner : IChirperExtension
	{
		public static BannerPanel theBannerPanel;
		public static IChirper BuiltinChirper;

		public void OnCreated (IChirper chirper)
		{
			// read config file for settings
			//MyConfig.LoadConfig ();

			BuiltinChirper = chirper;

			if (MyConfig.ConfigHolder.Config.DestroyBuiltinChirper) {
				chirper.ShowBuiltinChirper (false);
			}

			CreateBannerUI ();

		}

		public void OnMessagesUpdated ()
		{
		}



		// Based on https://github.com/AtheMathmo/SuperChirperMod/blob/master/SuperChirper/ChirpFilter.cs
		// But in the opposite direction
		public static bool FilterMessage (string input)
		{

			switch (input) {
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

		public void OnNewMessage (IChirperMessage message)
		{
			bool important = false;
			if (message != null && theBannerPanel != null) {
				try {
					string citizenMessageID = string.Empty;

					CitizenMessage cm = message as CitizenMessage;
					if (cm != null) {
						//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("found citmess MessageID: {0} GetText: {1}", cm.m_messageID, cm.GetText()));
						citizenMessageID = cm.m_messageID;
					}

					if (!string.IsNullOrEmpty (citizenMessageID)) {
						// TODO: Do stuff if the Chirper message is actually important.
						// Hope is to mimic SimCity's ticker.
						// List of LocaleIDs available here: https://github.com/cities-skylines/Assembly-CSharp/wiki/LocaleID
						important = FilterMessage (cm.m_messageID);
					}
				} catch (Exception ex) {
					DebugOutputPanel.AddMessage (ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format ("ChirpBanner.OnNewMessage threw Exception: {0}", ex.Message));
				}

				string str = String.Format ("{0}: {1}", message.senderName, message.text);

				// Check for CurrentConfig.FilterChirps
				if (MyConfig.ConfigHolder.Config.FilterChirps) {
					// If Chirp is deemed not important as a result of above LocaleIDs, just return, do nothing
					if (!important) {
						//DebugOutputPanel.AddMessage (ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format ("Chirp is not important: {0}", message.text));
						return;
					}
					// Otherwise, do something
					if (important) {
						//DebugOutputPanel.AddMessage (ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format ("Chirp is important: {0}", message.text));
						theBannerPanel.color = new Color32(255, 0, 0, 255); 
						theBannerPanel.CreateBannerLabel (str, message.senderID);
					}
				} else {
					theBannerPanel.CreateBannerLabel (str, message.senderID);
				}
            
				//DebugOutputPanel.AddMessage (ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format ("textColorTag: {0}", textColorTag));
				//DebugOutputPanel.AddMessage (ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format ("nameColorTag: {0}", nameColorTag));
				//DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("chirp! {0}", message.text));            
				
				//MyIThreadingExtension.addTask2Main(() => { theBannerPanel.CreateBannerLabel(str, message.senderID); });
				//MyIThreadingExtension.addTask2Main(() => { DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("chirp! {0}", message.text)); });	
			}
		}

		public void OnReleased ()
		{
			if (theBannerPanel != null) {
				theBannerPanel = null;
			}
				
		}

		public void OnUpdate ()
		{
			if (theBannerPanel != null)
			{
				theBannerPanel.SendToBack();
			}			
		}

		private void CreateBannerUI ()
		{
			// create UI using ColossalFramework UI classes
			UIView uiv = UIView.GetAView ();

			if (uiv == null) {
				return;
			}

			//UISprite chirpSprite = (ChirpBannerSprite)uiv.AddUIComponent (typeof(ChirpBannerSprite));

			//UIComponent parent = UIView.Find("ChirpBannerSprite");
			theBannerPanel = (BannerPanel)uiv.AddUIComponent (typeof(BannerPanel));

			if (theBannerPanel != null) {
				//theBannerPanel.ScrollSpeed = CurrentConfig.ScrollSpeed;
				//byte bAlpha = (byte)(ushort)(255f * CurrentConfig.BackgroundAlpha);

				//theBannerPanel.BackgroundColor = new Color32(0, 0, 0, bAlpha);
				//theBannerPanel.FontSize = CurrentConfig.TextSize;
				theBannerPanel.SendToBack();

				// Tests for overlapping:
				//theBannerPanel.CreateBannerLabel("This is a really long chirp! really chirpy chirpy!", 481516);
				//theBannerPanel.CreateBannerLabel("OMG", 123456);
				//theBannerPanel.CreateBannerLabel("OMG1222222222222233333333", 1234567);
				//theBannerPanel.CreateBannerLabel("OMG22222222222 222222  4434", 12345678);
			}
		}

	}

	public class BannerLabelStruct
	{
		public Vector3 RelativePosition;
		// relative to parent BannerPanel
		public UILabel Label;
		public bool IsCurrentlyVisible;
	}

/* 
	public class ChirpBannerSprite : UIButton 
	{
		public override void Start () {
			UIView uiv = UIView.GetAView ();
			int viewWidth = (int)uiv.GetScreenResolution ().x;
			int viewHeight = (int)uiv.GetScreenResolution ().y;
		
			//UISprite c = uiv.AddUIComponent<UISprite>();
			this.normalBgSprite = "ChirperIcon";
			this.position = new Vector3 ((-viewWidth / 2) + 60, (viewHeight / 2));
			UIDragHandle dh = (UIDragHandle)this.AddUIComponent (typeof(UIDragHandle));
		}
	}
*/
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
		public Queue<BannerLabelStruct> BannerLabelsQ = new Queue<BannerLabelStruct> ();
		public static bool hasChirps = false;

		public override void Start () {
			UIView uiv = UIView.GetAView ();
			int viewWidth = (int)uiv.GetScreenResolution ().x;
			int viewHeight = (int)uiv.GetScreenResolution ().y;

			if (uiv == null) {
				Cleanup ();
				return;
			}

			this.autoSize = true;
			this.anchor = UIAnchorStyle.Top | UIAnchorStyle.CenterHorizontal;
			this.backgroundSprite = "GenericPanel";
			this.color = new Color32 (0, 0, 0, 0xff);
			this.height = 25.0f;
			//this.height = (float)ChirpyBanner.CurrentConfig.TextSize + 20;
			
			float insetX = (viewWidth - this.width) / 2f;
			this.relativePosition = new Vector3(insetX, 0f, 0f);
			//this.CenterToParent();
			//this.position = new Vector3 ((-viewWidth / 2) + banner_inset, (viewHeight / 2));
			this.opacity = MyConfig.ConfigHolder.Config.BackgroundAlpha;
			this.width = viewWidth * 0.75f;
			//this.width = (viewWidth * MyConfig.ConfigHolder.Config.BannerWidth) - (banner_inset * 2);
			this.maximumSize = new Vector2(viewWidth, 25f);
			this.minimumSize = new Vector2(27f, 25f);

			UISprite chirpSprite = this.AddUIComponent<UISprite>();
            chirpSprite.spriteName = "ChirperIcon";
            chirpSprite.relativePosition = new Vector3(-1f, -1f);
            chirpSprite.size = new Vector2(27f, 27f);
			chirpSprite.BringToFront();

			this.autoLayout = false;
			this.clipChildren = true;
			this.SendToBack();

			UIDragHandle dh = (UIDragHandle)this.AddUIComponent (typeof(UIDragHandle));
			dh.size = new Vector2(25f, 25f);
			dh.relativePosition = new Vector3(0f, 0f); 

			UIResizeHandle rh = (UIResizeHandle)this.AddUIComponent (typeof(UIResizeHandle));
			rh.backgroundSprite = "buttonresize";
			rh.AlignTo(this, UIAlignAnchor.BottomRight);
			rh.size = new Vector2(16f, 16f);
			rh.relativePosition = new Vector3(rh.relativePosition.x - 16, rh.relativePosition.y - 16, 0);

		}

		/* 
		// SetConfig() is called every frame via OnUpdate(), ensuring 
		public void SetConfig ()
		{
			UIView uiv = UIView.GetAView ();
			int viewWidth = (int)uiv.GetScreenResolution ().x;
			int viewHeight = (int)uiv.GetScreenResolution ().y;

			this.opacity = MyConfig.ConfigHolder.Config.BackgroundAlpha;
			this.width = (viewWidth * MyConfig.ConfigHolder.Config.BannerWidth) - (banner_inset * 2);

		}
		*/

		public void Cleanup ()
		{
			Shutdown = true;
		}

		public void RemoveBannerLabel (BannerLabelStruct blsfree)
		{
			this.RemoveUIComponent (blsfree.Label);
		}

		public void CreateBannerLabel (string chirpStr, uint senderID)
		{
			if (Shutdown || string.IsNullOrEmpty (chirpStr)) {
				return;
			}
			hasChirps = true;
			ChirpLabel = this.AddUIComponent<UILabel> ();
			if (ChirpLabel != null) {
				//ChirpLabel.autoHeight = true;
				//ChirpLabel.autoSize = true;
				ChirpLabel.verticalAlignment = UIVerticalAlignment.Middle;
				ChirpLabel.textAlignment = UIHorizontalAlignment.Left;
				ChirpLabel.relativePosition = new Vector3 ((this.width), 0);
				//ChirpLabel.height = (float)ChirpyBanner.CurrentConfig.TextSize;
				ChirpLabel.height = MyConfig.ConfigHolder.Config.LabelHeight;
				ChirpLabel.padding = new RectOffset (0, 0, 2, 2);

				ChirpLabel.textScaleMode = UITextScaleMode.None;
				ChirpLabel.textScale = 1.0f;
				ChirpLabel.autoHeight = false;
				ChirpLabel.opacity = 1.0f;
				ChirpLabel.processMarkup = true;
				ChirpLabel.text = chirpStr;

				ChirpLabel.objectUserData = (object)new InstanceID () {
					Citizen = senderID
				};

				ChirpLabel.eventClick += (UIComponent comp, UIMouseEventParameter p) => {
					if (p.source == null || ToolsModifierControl.cameraController == null)
						return;
					InstanceID id = (InstanceID)p.source.objectUserData;
					if (!InstanceManager.IsValid(id))
						return;
					ToolsModifierControl.cameraController.SetTarget(id, ToolsModifierControl.cameraController.transform.position, true); 
				};

				BannerLabelStruct bls = new BannerLabelStruct ();
				bls.RelativePosition = new Vector3 (this.width, label_y_inset); // starting position is off screen, at max extent of parent panel
				bls.Label = ChirpLabel;
				bls.IsCurrentlyVisible = false;
				BannerLabelsQ.Enqueue (bls);
				ChirpLabel.SendToBack();
			}
		}
	}

		public class MyConfig
		{
			public bool DestroyBuiltinChirper = true;
			public int ScrollSpeed = 50;
			public int TextSize = 20;
			public int version = 0;
			public float BackgroundAlpha = 1.0f;
			public float TrailingEdgeOffset = 20f;
			public float LabelHeight = 30.0f;
			public bool FilterChirps = false;
			public bool ColorChirps = false;
			public int currversion = 6;


		public static class ConfigHolder {
				public static MyConfig Config = new MyConfig();
						
		}
		/*
		public static void Serialize (string filename, MyConfig config)
		{
			try {
				var serializer = new XmlSerializer (typeof(MyConfig));

				using (var writer = new StreamWriter (filename)) {
					serializer.Serialize (writer, config);
				}
			} catch {
					DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("currentTrailingEdge: {0}", currentTrailingEdge));
			}
		}

		public static MyConfig Deserialize (string filename)
		{
			var serializer = new XmlSerializer (typeof(MyConfig));

			try {
				using (var reader = new StreamReader (filename)) {
					MyConfig config = (MyConfig)serializer.Deserialize (reader);

					// sanity checks
					if (config.ScrollSpeed < 50)
						config.ScrollSpeed = 50;
					if (config.ScrollSpeed > 200)
						config.ScrollSpeed = 200;
					if (config.TextSize < 4 || config.TextSize > 100)
						config.TextSize = 20;

					return config;
				}
			} catch {
			}

			return null;
		}
		*/

		public static void SaveConfig ()
		{
				try {
					var xmlSerializer = new XmlSerializer(typeof(MyConfig));
					using (var writer = new StreamWriter ("ChirpBannerConfig.xml")) {
						xmlSerializer.Serialize(writer, ConfigHolder.Config);
					}
				}
				catch (Exception e) {
					//Debug.LogErrorFormat ("Unexpected {0} while saving options: {1}\n{2}", e.GetType ().Name, e.Message, e.StackTrace);
					DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("Error saving config: {0}", e.Message));
				}
			
		}

		public static void LoadConfig ()
		{
				try {
					var xmlSerializer = new XmlSerializer(typeof(MyConfig));
					using (var reader = new StreamReader("ChirpBannerConfig.xml")) {
						ConfigHolder.Config = (MyConfig)xmlSerializer.Deserialize(reader);
					}
				}
				catch (FileNotFoundException) {
					DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, string.Format("No config xml"));
				}
	
			/*
			//MyConfig.Serialize("ChirpBannerConfig.xml", ChirpyBanner.CurrentConfig);
			ChirpyBanner.CurrentConfig = MyConfig.Deserialize ("ChirpBannerConfig.xml");
			if (ChirpyBanner.CurrentConfig == null) {
				ChirpyBanner.CurrentConfig = new MyConfig ();

				MyConfig.Serialize ("ChirpBannerConfig.xml", ChirpyBanner.CurrentConfig);
			}
			// if old version, update with new
			if (ChirpyBanner.CurrentConfig.version == 0 || ChirpyBanner.CurrentConfig.version < currversion) { // update this when we add any new settings                  
				ChirpyBanner.CurrentConfig.version = currversion;
				MyConfig.Serialize ("ChirpBannerConfig.xml", ChirpyBanner.CurrentConfig);
			}

			MyConfig.Serialize ("ChirpBannerConfig.xml", ChirpyBanner.CurrentConfig);
			*/
		}

	}
}
